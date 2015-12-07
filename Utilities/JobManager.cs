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

    /// <summary>A class for managing asynchronous running of jobs.</summary>
    [Serializable]
    public class JobManager
    {
        /// <summary>A runnable interface.</summary>
        public interface IRunnable
        {
            /// <summary>Gets a value indicating whether this instance is computationally time consuming.</summary>
            bool IsComputationallyTimeConsuming { get; }

            /// <summary>Gets a value indicating whether this job is completed. Set by JobManager.</summary>
            bool IsCompleted { get; set; }

            /// <summary>Gets the error message. Can be null if no error. Set by JobManager.</summary>
            string ErrorMessage { get; set; }

            /// <summary>Called to start the job. Can throw on error.</summary>
            /// <param name="sender">The sender.</param>
            /// <param name="e">The <see cref="DoWorkEventArgs"/> instance containing the event data.</param>
            void Run(object sender, DoWorkEventArgs e);
        }

        private class JobInfo
        {
            public BackgroundWorker worker;
            public IRunnable job;
        }

        /// <summary>The maximum number of processors used by this job manager.</summary>
        private int MaximumNumOfProcessors = 1;

        private int numCompleted = 0;

        /// <summary>A job queue containing all jobs.</summary>
        private List<JobInfo> jobs = new List<JobInfo>();

        /// <summary>Main scheduler thread that goes through all jobs and sets them running.</summary>
        [NonSerialized]
        private BackgroundWorker schedulerThread = null;

        /// <summary>All jobs done?</summary>
        private bool allDone = false;

        /// <summary>
        /// Gets a value indicating whether there are more jobs to run.
        /// </summary>
        /// <value><c>true</c> if [more jobs to run]; otherwise, <c>false</c>.</value>
        private bool MoreJobsToRun
        {
            get
            {
                return !allDone;
            }
        }

        /// <summary>
        /// Gets the number of jobs still to run.
        /// </summary>
        /// <value><c>true</c> if [more jobs to run]; otherwise, <c>false</c>.</value>
        public int JobCount
        {
            get
            {
                return jobs.Count;
            }
        }

        /// <summary>A list of all completed jobs.</summary>
        public List<IRunnable> CompletedJobs
        {
            get
            {
                List<IRunnable> completedJobs = new List<IRunnable>();
                foreach (JobInfo job in jobs)
                {
                    if (job.job.IsCompleted)
                        completedJobs.Add(job.job);
                }
                return completedJobs;
            }
        }


        /// <summary>Occurs when all jobs completed.</summary>
        public event EventHandler AllJobsCompleted;

        /// <summary>
        /// Gets or sets a value indicating whether some jobs had errors.
        /// </summary>
        public bool SomeHadErrors { get; set; }

        /// <summary>Initializes a new instance of the <see cref="JobManager"/> class.</summary>
        public JobManager()
        {
            string NumOfProcessorsString = Environment.GetEnvironmentVariable("NUMBER_OF_PROCESSORS");
            if (NumOfProcessorsString != null)
                MaximumNumOfProcessors = Convert.ToInt32(NumOfProcessorsString);
            MaximumNumOfProcessors = System.Math.Max(MaximumNumOfProcessors, 1);
        }

        /// <summary>Add a job to the list of jobs that need running.</summary>
        /// <param name="job">The job to add to the queue</param>
        public void AddJob(IRunnable job)
        {
            lock (this) { jobs.Add(new JobInfo() { job = job }); }
        }

        /// <summary>
        /// Start the jobs asynchronously. If 'waitUntilFinished'
        /// is true then control won't return until all jobs have finished.
        /// </summary>
        /// <param name="waitUntilFinished">if set to <c>true</c> [wait until finished].</param>
        public void Start(bool waitUntilFinished)
        {
            SomeHadErrors = false;
            allDone = false;
            schedulerThread = new BackgroundWorker();
            schedulerThread.WorkerSupportsCancellation = true;
            schedulerThread.WorkerReportsProgress = true;
            schedulerThread.DoWork += DoWork;
            schedulerThread.RunWorkerAsync();
            schedulerThread.RunWorkerCompleted += OnWorkerCompleted;

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
            SomeHadErrors = false;
            allDone = false;
            DoWorkEventArgs args = new DoWorkEventArgs(this);
            foreach (JobInfo job in jobs)
            {
                try
                {
                    job.job.Run(this, args);
                    job.job.IsCompleted = true;
                    CompletedJobs.Add(job.job);
                }
                catch (Exception err)
                {
                    job.job.ErrorMessage = err.ToString();
                    SomeHadErrors = true;
                }
            }
            jobs.Clear();
            allDone = true;
        }

        /// <summary>Stop all jobs currently running in the scheduler.</summary>
        public void Stop()
        {
            lock (this)
            {
                // Change status of jobs.
                foreach (JobInfo job in jobs)
                {
                    job.job.IsCompleted = true;
                    if (job.worker.IsBusy)
                        job.worker.CancelAsync();
                }
            }

            if (schedulerThread != null)
                schedulerThread.CancelAsync();            
        }

        /// <summary>Called when [worker completed].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RunWorkerCompletedEventArgs"/> instance containing the event data.</param>
        private void OnWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (AllJobsCompleted != null)
                AllJobsCompleted.Invoke(this, new EventArgs());
            allDone = true;
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
            do
            {
                int i = GetNextJobToRun();
                if (i != -1)
                {
                    BackgroundWorker worker = new BackgroundWorker();
                    jobs[i].worker = worker;
                    worker.DoWork += jobs[i].job.Run;
                    worker.WorkerSupportsCancellation = true;
                    worker.RunWorkerAsync(this);
                }
                else
                    Thread.Sleep(30);

            }
            while (!bw.CancellationPending && numCompleted < JobCount);
        }

        /// <summary>Gets a job</summary>
        /// <param name="bw">Background worker of job to find</param>
        /// <returns>The IRunnable job.</returns>
        private int GetJob(BackgroundWorker bw)
        {
            for (int i = 0; i < jobs.Count; i++)
            {
                if (jobs[i].worker == bw)
                    return i;
            }

            throw new Exception("Cannot find job.");
        }

        /// <summary>Return the index of next job to run or -1 if nothing to run.</summary>
        /// <returns>Index of job or -1.</returns>
        private int GetNextJobToRun()
        {
            int index = 0;
            int countRunning = 0;
            numCompleted = 0;
            foreach (JobInfo job in jobs)
            {
                if (countRunning == MaximumNumOfProcessors)
                {
                    return -1;
                }

                if (job.worker != null && !job.worker.IsBusy)
                    numCompleted++;

                // Is this job running?
                if (job.worker == null)
                    return index;     // not running so return it to be run next.
                else if (job.job.IsComputationallyTimeConsuming && job.worker.IsBusy)
                    countRunning++;   // is running.

                index++;
            }
            
            return -1;
        }
    }
}
