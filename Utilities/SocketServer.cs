// -----------------------------------------------------------------------
// <copyright file="SocketServer.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace APSIM.Shared.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    /// <summary>
    /// An asynchronous socket server based on the MicroSoft one here:
    ///     https://msdn.microsoft.com/en-us/library/fx6588te(v=vs.110).aspx
    /// </summary>
    public class SocketServer
    {
        /// <summary>Thread signal.</summary>
        private static ManualResetEvent allDone = new ManualResetEvent(false);

        /// <summary>A container of commands</summary>
        private Dictionary<string, EventHandler<CommandArgs>> commands = new Dictionary<string, EventHandler<CommandArgs>>();

        /// <summary>Should the server keep listening for socket connections?</summary>
        private bool keepListening = true;

        /// <summary>Error event argument class.</summary>
        public class ErrorArgs : EventArgs
        {
            /// <summary>Error message.</summary>
            public string message;
        }

        /// <summary>Invoked when an error occurs.</summary>
        public event EventHandler<ErrorArgs> Error;


        /// <summary>Argument class passed to command handler.</summary>
        public class CommandArgs : EventArgs
        {
            /// <summary>The currently open socket. Can be used to send back command to client.</summary>
            public Socket socket;

            /// <summary>The object going with the command.</summary>
            public object obj;
        }

        /// <summary>Command object that clients send to server.</summary>
        [Serializable]
        public class CommandObject
        {
            /// <summary>Name of comamnd</summary>
            public string name;

            /// <summary>An optional object to go with the command.</summary>
            public object data;
        }

        /// <summary>Add a new command.</summary>
        /// <param name="commandName">The name of the command.</param>
        /// <param name="handler">The handler name.</param>
        public void AddCommand(string commandName, EventHandler<CommandArgs> handler)
        {
            commands.Add(commandName, handler);
        }

        /// <summary>Start listening for socket connections</summary>
        /// <param name="portNumber">Port number to listen on.</param>
        public void StartListening(int portNumber)
        {
            try
            {
                keepListening = true;

                // Establish the local endpoint for the socket.
                IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, portNumber);

                // Create a TCP/IP socket.
                using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    // Bind the socket to the local endpoint and listen for incoming connections.
                    listener.Bind(localEndPoint);
                    listener.Listen(1000);

                    AsyncCallback ar;
                    while (keepListening)
                    {
                        // Set the event to nonsignaled state.
                        allDone.Reset();

                        // Start an asynchronous socket to listen for connections.
                        ar = new AsyncCallback(AcceptCallback);
                        listener.BeginAccept(ar, listener);

                        // Wait until a connection is made before continuing.
                        allDone.WaitOne();
                    }
                }
            }
            catch (Exception err)
            {
                if (Error != null)
                    Error.Invoke(this, new ErrorArgs() { message = err.ToString() });
            }
        }

        /// <summary>Stop listening for socket connections</summary>
        public void StopListening()
        {
            keepListening = false;
            allDone.Set();
        }

        /// <summary>Accept a socket connection</summary>
        /// <param name="ar">Socket parameters.</param>
        public void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                if (keepListening)
                {
                    // Signal the main thread to continue.
                    allDone.Set();

                    // Get the socket that handles the client request.
                    Socket listener = (Socket)ar.AsyncState;
                    Socket handler = listener.EndAccept(ar);

                    // Create the state object.
                    StateObject state = new StateObject();
                    state.workSocket = handler;
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                                         new AsyncCallback(ReadCallback), state);
                }
            }
            catch (Exception err)
            {
                if (Error != null)
                    Error.Invoke(this, new ErrorArgs() { message = err.ToString() });
            }
        }

        /// <summary>Callback for a socket read.</summary>
        /// <param name="ar">Socket parameters.</param>
        public void ReadCallback(IAsyncResult ar)
        {
            try
            { 
                string content = string.Empty;

                // Retrieve the state object and the handler socket
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.workSocket;

                // Read data from the client socket. 
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There  might be more data, so store the data received so far.
                    state.data.Write(state.buffer, 0, bytesRead);

                    if (state.numBytesExpected == 0)
                        state.numBytesExpected = BitConverter.ToInt32(state.buffer, 0);

                    // Check for correct number of bytes
                    if (state.data.Length != state.numBytesExpected + 4)
                    {
                        // Not all data received. Get more.
                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                                             new AsyncCallback(ReadCallback), state);
                    }
                    else
                    {
                        // All done process command.
                        ProcessCommand(DecodeData(state.data.ToArray()), state.workSocket);
                    }
                }
            }
            catch (Exception err)
            {
                if (Error != null)
                    Error.Invoke(this, new ErrorArgs() { message = err.ToString() });
            }
        }

        /// <summary>Process the command</summary>
        /// <param name="obj"></param>
        /// <param name="socket">The socket currently open.</param>
        private void ProcessCommand(object obj, Socket socket)
        {
            CommandObject command = obj as CommandObject;
            if (!commands.ContainsKey(command.name))
                throw new Exception("Cannot find a handler for command: " + command.name);
            CommandArgs args = new CommandArgs();
            args.socket = socket;
            args.obj = command.data;
            commands[command.name].Invoke(this, args);
        }

        /// <summary>Send data through socket.</summary>
        /// <param name="handler">The socket.</param>
        /// <param name="obj">Object to send</param>
        public void Send(Socket handler, object obj)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = EncodeData(obj);

            // Begin sending the data to the remote device.
            handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
        }

        /// <summary>Callback for sending data.</summary>
        /// <param name="ar">Socket parameters.</param>
        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>Encode the object into a series of bytes</summary>
        /// <param name="o">The object to encode</param>
        /// <returns>The encoded object as a byte array.</returns>
        private static byte[] EncodeData(object o)
        {
            MemoryStream memStream = ReflectionUtilities.BinarySerialise(o) as MemoryStream;
            byte[] bytes = new byte[memStream.Length + 4];
            BitConverter.GetBytes((int)memStream.Length).CopyTo(bytes, 0);
            memStream.ToArray().CopyTo(bytes, 4);
            return bytes;
        }

        /// <summary>Decode a byte array into an object.</summary>
        /// <param name="bytes">The byte array.</param>
        /// <returns>The newly created object</returns>
        private static object DecodeData(byte[] bytes)
        {
            MemoryStream memStream = new MemoryStream(bytes);
            memStream.Seek(4, SeekOrigin.Begin);
            return ReflectionUtilities.BinaryDeserialise(memStream);
        }

        /// <summary>
        /// State object for reading client data asynchronously
        /// </summary>
        private class StateObject
        {
            /// <summary>Client socket</summary>
            public Socket workSocket = null;
            /// <summary>Size of receive buffer.</summary>
            public const int BufferSize = 16384;
            /// <summary>Number of bytes expected.</summary>
            public int numBytesExpected;
            /// <summary>Receive buffer.</summary>
            public byte[] buffer = new byte[BufferSize];
            /// <summary>Received data.</summary>
            public MemoryStream data = new MemoryStream();
        }


        /////////////////////////////////////////////////////////////////////////
        // The following method is useful for socket client applications
        /////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Send an object to the socket server, wait for a response and return the
        /// response as an object.
        /// </summary>
        /// <param name="serverName">The server name.</param>
        /// <param name="port">The port number.</param>
        /// <param name="obj">The object to send.</param>
        public static object Send(string serverName, int port, object obj)
        {
            TcpClient Server = new TcpClient(serverName, Convert.ToInt32(port));
            MemoryStream s = new MemoryStream();
            try
            {
                Byte[] bData = EncodeData(obj);
                Server.GetStream().Write(bData, 0, bData.Length);
                Byte[] bytes = new Byte[819200];

                // Loop to receive all the data sent by the client.
                int numBytesExpected = 0;
                int totalNumBytes = 0;
                int i = 0;
                int NumBytesRead;
                bool allDone = false;
                do
                {
                    NumBytesRead = Server.GetStream().Read(bytes, 0, bytes.Length);
                    s.Write(bytes, 0, NumBytesRead);
                    totalNumBytes += NumBytesRead;

                    if (numBytesExpected == 0 && totalNumBytes > 4)
                        numBytesExpected = BitConverter.ToInt32(bytes, 0);
                    if (numBytesExpected + 4 == totalNumBytes)
                        allDone = true;

                    i++;
                }
                while (!allDone);

                // Decode the bytes and return.
                return DecodeData(s.ToArray());
            }
            finally
            {
                if (Server != null) Server.Close();
            }
        }


    }
}
