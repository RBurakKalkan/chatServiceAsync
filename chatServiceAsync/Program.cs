using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    class Program
    {
        private static readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<Socket> clientSockets = new List<Socket>();
        //private static  Dictionary<Socket, bool> socketSpamControl = new Dictionary<Socket, bool>();
        public class warnedSocketInfo
        {
            public bool listedControl { get; set; }
            public bool gotWarned { get; set; }
        }
        public static Dictionary<Socket, warnedSocketInfo> socketSpamControl = new Dictionary<Socket, warnedSocketInfo>();

        private const int BUFFER_SIZE = 2048;
        private const int PORT = 100;
        private static readonly byte[] buffer = new byte[BUFFER_SIZE];
        static Stopwatch stopWatch = new Stopwatch();

        static void Main()
        {
            Console.Title = "Server";
            SetupServer();
            Console.ReadLine(); // When we press enter close everything
            CloseAllSockets();
        }

        private static void SetupServer()
        {
            Console.WriteLine("Setting up server...");
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, PORT));
            serverSocket.Listen(0);
            serverSocket.BeginAccept(AcceptCallback, null);
            Console.WriteLine("Server setup complete");
        }

        /// <summary>
        /// Close all connected client (we do not need to shutdown the server socket as its connections
        /// are already closed with the clients).
        /// </summary>
        private static void CloseAllSockets()
        {
            foreach (Socket socket in clientSockets)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            serverSocket.Close();
        }

        private static void AcceptCallback(IAsyncResult AR)
        {
            Socket socket;

            try
            {
                socket = serverSocket.EndAccept(AR);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            clientSockets.Add(socket);
            warnedSocketInfo a = new warnedSocketInfo();
            a.listedControl = true;
            a.gotWarned = false;
            socketSpamControl.Add(socket, a);
            socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
            Console.WriteLine("Client connected, waiting for request...");
            serverSocket.BeginAccept(AcceptCallback, null);
        }


        private static void ReceiveCallback(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
            int received;

            try
            {
                if (current.Connected)
                {
                    received = current.EndReceive(AR);
                }
                else
                {
                    return;
                }
            }
            catch (SocketException)
            {
                Console.WriteLine("Client forcefully disconnected");
                current.Close();
                clientSockets.Remove(current);
                return;
            }
            byte[] recBuf = new byte[received];
            Array.Copy(buffer, recBuf, received);
            string text = Encoding.ASCII.GetString(recBuf);
            Console.WriteLine(text);
            byte[] data = Encoding.ASCII.GetBytes(text);
            if (clientSockets.Count > 0)
            {
                foreach (Socket socket in clientSockets)
                {
                    if (socket != current)
                    {
                        socket.Send(data);
                        socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
                    }
                    else
                    {

                        byte[] dataToUser = Encoding.ASCII.GetBytes(text + "$$$$$$$$$$");
                        current.Send(dataToUser);
                        current.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current);
                        if (socketSpamControl.GetValueOrDefault(current).listedControl)
                        {
                            socketSpamControl.GetValueOrDefault(current).listedControl = false;
                            stopWatch.Start();
                        }
                        else
                        {
                            if (stopWatch.ElapsedMilliseconds <= 1000)
                            {
                                byte[] warning;
                                if (socketSpamControl.GetValueOrDefault(current).gotWarned)
                                {
                                    warning = Encoding.ASCII.GetBytes("You've been warned. Sorry...");
                                    current.Send(warning);
                                    clientSockets.Remove(current);
                                    warning = Encoding.ASCII.GetBytes("exit");
                                    current.Send(warning);
                                    socketSpamControl.Remove(current);
                                    current.Shutdown(SocketShutdown.Both);
                                    current.Close();
                                    return;
                                }
                                else
                                {
                                    warning = Encoding.ASCII.GetBytes("Do NOT spam chat!!! \nYou'll get banned if you do it once again.");
                                    current.Send(warning);
                                    socketSpamControl.GetValueOrDefault(current).gotWarned = true;
                                }
                            }
                            else
                            {
                                socketSpamControl.GetValueOrDefault(current).listedControl = false;
                            }
                            socketSpamControl.GetValueOrDefault(current).listedControl = true;
                            stopWatch.Reset();
                        }
                    }
                }
            }


            //current.Send(data);
            //current.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current);
        }
    }
}
