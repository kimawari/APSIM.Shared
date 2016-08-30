// -----------------------------------------------------------------------
// <copyright file="JobManager.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace APSIM.Shared.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Threading;
    using System.Linq;

    /// <summary>A class for managing asynchronous running of jobs.</summary>
    [Serializable]
    public class JobManager
    {
        /// <summary>An interface that defines a class as requiring a dedicated CPU core.</summary>
        public interface IComputationalyTimeConsuming
        {
        }

        /// <summary>A runnable interface.</summary>
        public interface IRunnable
        {
            /// <summary>Called to start the job. Can throw on error.</summary>
            /// <param name="jobManager">The job manager this job is running in.</param>
            /// <param name="workerThread">Is cancellation pending?</param>
            void Run(JobManager jobManager, BackgroundWorker workerThread);
        }

        /// <summary>The maximum number of processors used by this job manager.</summary>
        [NonSerialized]
        private int MaximumNumOfProcessors = 2;

        /// <summary>A job queue of running jobs.</summary>
        [NonSerialized]
        private List<Job> jobs = new List<Job>();

        /// <summary>Main scheduler thread that goes through all jobs and sets them running.</summary>
        [NonSerialized]
        private BackgroundWorker schedulerThread = null;

        /// <summary>Gets a value indicating whether there are more jobs to run.</summary>
        private bool MoreJobsToRun
        {
            get
            {
                lock (this)
                {
                    return jobs.Find(job => !job.IsCompleted) != null;
                }
            }
        }

        /// <summary>Gets the number of jobs still to run.</summary>
        public int NumberOfJobsStillToComplete
        {
            get
            {
                lock (this)
                {
                    return jobs.Count(job => !job.IsCompleted);
                }
            }
        }

        /// <summary>Gets the percentage of jobs completed.</summary>
        public double PercentComplete
        {
            get
            {
                lock (this)
                {
                    if (jobs.Count == 0)
                        return 0;
                    else
                        return jobs.Count(job => job.IsCompleted) * 1.0 / jobs.Count * 100.0;
                }
            }
        }

        /// <summary>Clear all completed jobs.</summary>
        public void ClearCompletedJobs()
        {
            lock (this)
            {
                jobs.RemoveAll(job => job.IsCompleted);
            }
        }

        /// <summary>Occurs when all jobs completed.</summary>
        public event EventHandler AllJobsCompleted;

        /// <summary>Count the number of specified job types in the queue of jobs</summary>
        public int CountJobTypeInQueue<T>()
        {
            lock (this)
            {
                return jobs.Count(job => job.RunnableJob is T);
            }
        }

        /// <summary>Returns true if the specified job type is already in the queue of jobs, otherwise returns false</summary>
        public bool IsJobTypeInQueue<T>()
        {
            lock (this)
            {
                return jobs.Exists(job => (job.RunnableJob is T));
            }
        }

        /// <summary>Has the specified job and all its child jobs finished?</summary>
        /// <param name="runnableJob">The job to check.</param>
        public bool IsJobCompleted(IRunnable runnableJob)
        {
            lock (this)
            {
                Job job = jobs.Find(j => j.RunnableJob == runnableJob);
                if (job == null)
                    throw new Exception("Cannot find job");
                return job.IsJobAndChildJobsComplete();
            }
        }
        
        /// <summary>Returns true if all child jobs are completed.</summary>
        public bool AreChildJobsComplete(IRunnable runnableJob)
        {
            lock (this)
            {
                Job job = jobs.Find(j => j.RunnableJob == runnableJob);
                if (job == null)
                    throw new Exception("Cannot find job");
                return job.AreChildJobsComplete();
            }
        }

        /// <summary>
        /// Return the total elapsed time for the specified job and all child jobs (ms)
        /// </summary>
        /// <param name="runnableJob">The job to locate.</param>
        /// <returns>The elapsed time in milliseconds</returns>
        public long ElapsedTime(IRunnable runnableJob)
        {
            lock (this)
            {
                Job job = jobs.Find(j => j.RunnableJob == runnableJob);
                if (job == null)
                    throw new Exception("Cannot find job");
                return job.TotalElapsedTime;
            }
        }

        /// <summary>Get exceptions from the specified job</summary>
        /// <param name="runnableJob">The job to check.</param>
        public List<Exception> Errors(IRunnable runnableJob)
        {
            lock (this)
            {
                Job job = jobs.Find(j => j.RunnableJob == runnableJob);
                if (job == null)
                    throw new Exception("Cannot find job");
                List<Exception> errors = new List<Exception>();
                job.Errors(errors);
                return errors;
            }
        }

        /// <summary>Initializes a new instance of the <see cref="JobManager"/> class.</summary>
        /// <param name="maximumNumberOfProcessors">The maximum number of cores to use.</param>
        public JobManager(int maximumNumberOfProcessors = -1)
        {
            if (maximumNumberOfProcessors != -1)
                MaximumNumOfProcessors = maximumNumberOfProcessors;
            else
            {
                string NumOfProcessorsString = Environment.GetEnvironmentVariable("NUMBER_OF_PROCESSORS");
                if (NumOfProcessorsString != null)
                    MaximumNumOfProcessors = Convert.ToInt32(NumOfProcessorsString);
                MaximumNumOfProcessors = System.Math.Max(MaximumNumOfProcessors, 1);
            }
        }

        /// <summary>Add a job to the list of jobs that need running.</summary>
        /// <param name="runnableJob">The job to add to the queue</param>
        public void AddJob(IRunnable runnableJob)
        {
            lock (this)
            {
                Job newJob = new Job(this, runnableJob);
                jobs.Add(newJob);
            }
        }

        /// <summary>Add a job to the list of jobs that need running.</summary>
        /// <param name="parentRunnableJob">The parent job</param>
        /// <param name="runnableJob">The job to add to the parent job</param>
        public void AddChildJob(IRunnable parentRunnableJob, IRunnable runnableJob)
        {
            lock (this)
            {
                Job parentJob = jobs.Find(job => job.RunnableJob == parentRunnableJob);
                if (parentJob == null)
                    throw new Exception("Cannot find job");
                Job newJob = new Job(this, runnableJob);
                parentJob.ChildJobs.Add(newJob);
                jobs.Add(newJob);
            }
        }

        /// <summary>
        /// Start the jobs asynchronously. If 'waitUntilFinished'
        /// is true then control won't return until all jobs have finished.
        /// </summary>
        /// <param name="waitUntilFinished">if set to <c>true</c> [wait until finished].</param>
        public void Start(bool waitUntilFinished)
        {
            schedulerThread = new BackgroundWorker();
            schedulerThread.WorkerSupportsCancellation = true;
            schedulerThread.WorkerReportsProgress = true;
            schedulerThread.DoWork += DoWork;
            schedulerThread.RunWorkerCompleted += OnWorkerCompleted;
            schedulerThread.RunWorkerAsync();
                
            if (waitUntilFinished)
            {
                while (MoreJobsToRun)
                    Thread.Sleep(200);
            }
        }

        /// <summary>Run the jobs synchronously, without extra threads.</summary>
        /// <remarks>Non threaded runs are useful for profiling.</remarks>
        public void Run()
        {
            foreach (Job job in jobs)
                job.Run(this, null);
            
            if (AllJobsCompleted != null)
                AllJobsCompleted.Invoke(this, new EventArgs());
        }

        /// <summary>Stop all jobs currently running in the scheduler.</summary>
        public void Stop()
        {
            lock (this)
            {
                // Change status of jobs.
                foreach (Job job in jobs)
                    job.Stop();
            }

            if (schedulerThread != null)
                schedulerThread.CancelAsync();            
        }

        /// <summary>Called when main worker thread has exited.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RunWorkerCompletedEventArgs"/> instance containing the event data.</param>
        private void OnWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (AllJobsCompleted != null)
                AllJobsCompleted.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Main DoWork method for the scheduler thread. NB this does NOT run on the UI thread.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DoWorkEventArgs"/> instance containing the event data.</param>
        private void DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            
            // Main worker thread for keeping jobs running
            while (!bw.CancellationPending && NumberOfJobsStillToComplete > 0)
            {
                int i = GetNextJobToRun();
                if (i != -1)
                {
                    lock (this) 
                    {
                        BackgroundWorker worker = new BackgroundWorker();
                        jobs[i].Start(worker);
                        worker.DoWork += jobs[i].DoWork;
                        worker.WorkerSupportsCancellation = true;
                        worker.RunWorkerAsync(this);
                    }
                }
                Thread.Sleep(300);
            }
        }

        /// <summary>Return the index of next job to run or -1 if nothing to run.</summary>
        /// <returns>Index of job or -1.</returns>
        private int GetNextJobToRun()
        {
            lock (this)
            {
                int index = 0;
                int countRunning = 0;
                foreach (Job job in jobs)
                {
                    if (countRunning == MaximumNumOfProcessors)
                        return -1;

                    // Is this job running?
                    if (!job.IsCompleted)
                    {
                        if (!job.IsRunning)
                            return index;     // not running so return it to be run next.
                        else if (job.RunnableJob.GetType().GetInterface("IComputationalyTimeConsuming") != null)
                            countRunning++;   // is running.
                    }
                    index++;
                }
            }
            
            return -1;
        }



        /// <summary>A job class used only by JobManager.</summary>
        class Job
        {
            private JobManager jobManager;

            /// <summary>The background worker thread.</summary>
            private BackgroundWorker worker;

            /// <summary>Has the job finished running.</summary>
            private bool isCompleted = false;

            /// <summary>The elapsed time for this job (ms) - not child jobs.</summary>
            private long elapsedTime;

            /// <summary>Gets the exception. Can be null if no error. Set by JobManager.</summary>
            internal Exception Error { get; private set; }

            /// <summary>Called to start the job. Can throw on error.</summary>
            internal JobManager.IRunnable RunnableJob { get; private set; }

            /// <summary>A collection of child jobs spawned by this job.</summary>
            internal List<Job> ChildJobs { get; set; }



            /// <summary>Constructor</summary>
            /// <param name="jobManager">Parent job manager.</param>
            /// <param name="runnableJob">Runnable job</param>
            internal Job(JobManager jobManager, IRunnable runnableJob)
            {
                this.jobManager = jobManager;
                this.RunnableJob = runnableJob;
                ChildJobs = new List<Job>();
            }

            /// <summary>Returns true if job and all child jobs are completed.</summary>
            internal bool IsCompleted {  get { return isCompleted; } }

            /// <summary>Returns true if job and all child jobs are completed.</summary>
            internal bool IsJobAndChildJobsComplete()
            {
                return isCompleted && ChildJobs.TrueForAll(job => job.IsJobAndChildJobsComplete());
            }

            /// <summary>Returns true if all child jobs are completed.</summary>
            internal bool AreChildJobsComplete()
            {
                return ChildJobs.TrueForAll(job => job.IsCompleted);
            }

            /// <summary>Returns true if job and all child jobs are completed.</summary>
            internal bool IsRunning {  get { return worker != null; } }

            /// <summary>Return total elapsed time of this job and any child jobs (ms).</summary>
            internal long TotalElapsedTime
            {
                get
                {
                    long sum = elapsedTime;
                    foreach (Job childJob in ChildJobs)
                        sum += childJob.elapsedTime;
                    return sum;
                }
            }

            /// <summary>Returns a list of exceptions from this job and all child jobs.</summary>
            internal void Errors(List<Exception> errors)
            {
                if (Error != null)
                    errors.Add(Error);
                if (ChildJobs != null)
                    ChildJobs.ForEach(job => job.Errors(errors));
            }

            /// <summary>Begin this job.</summary>
            /// <param name="workerThread">The thread the job is running on.</param>
            internal void Start(BackgroundWorker workerThread)
            {
                worker = workerThread;
            }

            /// <summary>Run the job on the current thread</summary>
            /// <param name="jobManager">The parent job manager.</param>
            /// <param name="workerThread">Cancellation pending?</param>
            internal void Run(JobManager jobManager, BackgroundWorker workerThread)
            {
                try
                {
                    RunnableJob.Run(jobManager, workerThread);
                }
                catch (Exception err)
                {
                    Error = err;
                }
                finally
                {
                    worker = null;
                    isCompleted = true;
                }
            }

            /// <summary>Do work event handler.</summary>
            /// <param name="sender">Job manager</param>
            /// <param name="e">Thread arguments</param>
            internal void DoWork(object sender, DoWorkEventArgs e)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                Run(jobManager, sender as System.ComponentModel.BackgroundWorker);
                stopwatch.Stop();
                elapsedTime = stopwatch.ElapsedMilliseconds;
            }

            /// <summary>Stop the job.</summary>
            internal void Stop()
            {
                if (IsRunning && worker.IsBusy)
                    worker.CancelAsync();
                worker = null;
                isCompleted = true;
            }
        }

    }
}
