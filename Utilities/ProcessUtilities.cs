// -----------------------------------------------------------------------
// <copyright file="ProcessUtilities.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace APSIM.Shared.Utilities
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// A collection of utilities for dealing with processes (threads)
    /// </summary>
    public class ProcessUtilities
    {
        /// <summary>
        /// Enumeration for holding compilation modes
        /// </summary>
        public enum CompilationMode
        {
            /// <summary>
            /// Unknown
            /// </summary>
            Invalid,
            /// <summary>
            /// Native Win32 code
            /// </summary>
            Native,
            /// <summary>
            /// Common Language Runtime
            /// </summary>
            CLR,
            /// <summary>
            /// Mixed mode
            /// </summary>
            Mixed
        };

        /// <summary>
        /// Determine if the file refered to is a native win32 or a CLR assembly.
        /// Mixed mode assemblies are CLR.
        /// Visual C++ Developer Center. http://msdn2.microsoft.com/en-us/library/c91d4yzb(VS.80).aspx
        /// </summary>
        /// <param name="filename">File name of the Assembly or native dll to probe.</param>
        /// <returns>Compilation mode.</returns>
        static public CompilationMode isManaged(string filename)
        {
            try
            {
                byte[] data = new byte[4096];
                FileInfo file = new FileInfo(filename);
                Stream fin = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                /*Int32 iRead =*/
                fin.Read(data, 0, 4096);
                fin.Close();

                // If we are running on Linux, the executable/so will start with the string 0x7f + 'ELF'
                // If the 5 byte is 1, it's a 32-bit image (2 indicates 64-bit)
                // If the 16th byte is 3, it's a shared object; 2 means it's an executable
                // If it's a Mono/.Net assembly, we should the the "Windows" header

                // If we're on Linux, see if it's a hash bang script. Should really
                // check executable flag via Mono.Unix.Native.Syscall.stat() too
                if (System.IO.Path.VolumeSeparatorChar == '/' &&
                    Convert.ToChar(data[0]) == '#' &&
                    Convert.ToChar(data[1]) == '!')
                    return CompilationMode.Native;
                // For now, if we're on Linux just see if it has an "ELF" header
                if (System.IO.Path.VolumeSeparatorChar == '/' && data[0] == 0x7f && data[1] == 'E' && data[2] == 'L' && data[3] == 'F')
                    return CompilationMode.Native;

                // Verify this is a executable/dll
                if (UInt16FromBytes(data, 0) != 0x5a4d)
                    return CompilationMode.Invalid;

                uint headerOffset = UInt32FromBytes(data, 0x3c);  // This will get the address for the WinNT header

                //at the file offset specified at offset 0x3c, is a 4-byte
                //signature that identifies the file as a PE format image file. This signature is “PE\0\0”
                if (UInt32FromBytes(data, headerOffset) != 0x00004550)
                    return CompilationMode.Invalid;

                //uint machineType = UInt16FromBytes(data, headerOffset + 4); //type of machine
                uint optionalHdrBase = headerOffset + 24;
                //uint exportTableAddr = UInt32FromBytes(data, optionalHdrBase + 96);     //.edata
                uint exportTableSize = UInt32FromBytes(data, optionalHdrBase + 96 + 4); //.edata size

                Int32 iLightningAddr = (int)headerOffset + 24 + 208;    //CLR runtime header addr & size
                Int32 iSum = 0;
                Int32 iTop = iLightningAddr + 8;

                for (int i = iLightningAddr; i < iTop; ++i)
                    iSum |= data[i];

                if (iSum == 0)
                    return CompilationMode.Native;
                else
                {
                    if (exportTableSize > 0)
                        return CompilationMode.Mixed;
                    else
                        return CompilationMode.CLR;
                }
            }
            catch (Exception e)
            {
                throw (e);
            }
        }

		/// <summary>
        /// 
        /// </summary>
        static private UInt32 UInt32FromBytes(byte[] p, uint offset)
        {
            return (UInt32)(p[offset + 3] << 24 | p[offset + 2] << 16 | p[offset + 1] << 8 | p[offset]);
        }
        
        /// <summary>
        /// 
        /// </summary>
        static private UInt16 UInt16FromBytes(byte[] p, uint offset)
        {
            return (UInt16)(p[offset + 1] << 8 | p[offset]);
        }

        /// <summary>A class for running an external process, redirecting all stdout and stderr.</summary>
        public class ProcessWithRedirectedOutput
        {
            private StringBuilder output = new StringBuilder();
            private StringBuilder error = new StringBuilder();
            private Process process;

            /// <summary>Invoked when the process exits.</summary>
            public EventHandler Exited;

            /// <summary>Executable</summary>
            public string Executable { get; private set; }

            /// <summary>Arguments</summary>
            public string Arguments { get; private set; }

            /// <summary>Return the exit code</summary>
            public int ExitCode { get { return process.ExitCode; } }

            /// <summary>Return the standard output</summary>
            public string StdOut { get { if (output.Length == 0) return null; else return output.ToString(); } }

            /// <summary>Return the standard error</summary>
            public string StdErr { get { if (error.Length == 0) return null; else return error.ToString(); } }

            /// <summary>Run the specified executable with the specified arguments and working directory.</summary>
            public void Start(string executable, string arguments, string workingDirectory, bool redirectOutput)
            {
                Executable = executable;
                Arguments = arguments;
                if (!File.Exists(executable))
                    throw new Exception("Cannot find executable " + executable + ". File not found.");
                process = new Process();
                process.StartInfo.FileName = executable;
                process.StartInfo.Arguments = arguments;
                // Determine whether or not the file is an executable; execute from the shell if it's not
                process.StartInfo.UseShellExecute = isManaged(executable) == CompilationMode.Invalid;
                process.StartInfo.CreateNoWindow = true;
                if (!process.StartInfo.UseShellExecute)
                {
                    process.StartInfo.RedirectStandardOutput = redirectOutput;
                    process.StartInfo.RedirectStandardError = redirectOutput;
                }
                process.StartInfo.WorkingDirectory = workingDirectory;

                // Set our event handler to asynchronously read the output.
                if (redirectOutput)
                {
                    process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
                    process.ErrorDataReceived += new DataReceivedEventHandler(ErrorHandler);
                }
                process.Exited += OnExited;
                process.EnableRaisingEvents = true;
                process.Start();
                if (redirectOutput)
                {
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }
            }

            /// <summary>Process has exited</summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void OnExited(object sender, EventArgs e)
            {
                Thread.Sleep(500);  // wait for any stdout/stderr writing.
                if (Exited != null)
                    Exited.Invoke(this, e);
            }

            /// <summary>Wait until process exits.</summary>
            public void WaitForExit()
            {
                process.WaitForExit();
            }

            /// <summary>Kill the process.</summary>
            public void Kill()
            {
                process.Kill();
            }

            /// <summary>Handler for all strings written to StdOut</summary>
            /// <param name="sendingProcess"></param>
            /// <param name="outLine"></param>
            private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
            {
                if (outLine.Data != null && outLine.Data != string.Empty)
                    output.Append(outLine.Data + Environment.NewLine);
            }

            /// <summary>Handler for all strings written to StdErr</summary>
            /// <param name="sendingProcess"></param>
            /// <param name="outLine"></param>
            private void ErrorHandler(object sendingProcess, DataReceivedEventArgs outLine)
            {
                if (outLine.Data != null && outLine.Data != string.Empty)
                    error.Append(outLine.Data + Environment.NewLine);
            }
        }
    }
}
