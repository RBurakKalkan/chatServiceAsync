using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        private static readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static List<Socket> clientSockets = new List<Socket>();
        public class warnedSocketInfo
        {
            public bool listedControl { get; set; }
            public bool gotWarned { get; set; }
            public long elapsedTime { get; set; }
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


        private static async void ReceiveCallback(IAsyncResult AR)
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
            await Task.Run(async () =>
            {
                await Task.Delay(100);
                spamControl(current, data, text);
            });
        }
        /// <summary>
        /// Prevents a user to spam chat (send more than 1 message per second and punishes if happens)
        /// and broadcasts one client's messages to every client.
        /// </summary>
        private static void spamControl(Socket current, byte[] data, string text)
        {

            foreach (Socket socket in clientSockets)
            {
                if (socket != current) // Send the receiving messages to the other users.
                {
                    socket.Send(data);
                    socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
                }
                else
                {
                    // check every users warned states by server and punish them if necessary.
                    byte[] dataToUser = Encoding.ASCII.GetBytes(text + "$$$$$$$$$$");
                    current.Send(dataToUser);
                    current.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current);
                    if (socketSpamControl[current].listedControl)
                    {
                        socketSpamControl[current].listedControl = false;
                        stopWatch.Start();
                    }
                    else
                    {
                        socketSpamControl[current].elapsedTime = stopWatch.ElapsedMilliseconds;
                        if (socketSpamControl[current].elapsedTime <= 1000)
                        {
                            byte[] warning;
                            if (socketSpamControl[current].gotWarned)
                            {
                                warning = Encoding.ASCII.GetBytes("You've been warned. Sorry...");
                                current.Send(warning);
                                clientSockets.Remove(current);
                                Console.WriteLine("Client banned.");
                                socketSpamControl.Remove(current);
                                current.Shutdown(SocketShutdown.Both);
                                current.Close();
                                return;
                            }
                            else
                            {
                                warning = Encoding.ASCII.GetBytes("Do NOT spam chat!!! You'll get banned if you do it once again.");
                                current.Send(warning);
                                socketSpamControl[current].gotWarned = true;
                            }
                        }
                        socketSpamControl[current].listedControl = true;
                        socketSpamControl[current].elapsedTime = 0;
                        stopWatch.Reset();
                    }
                }
            }
        }
    }
}
