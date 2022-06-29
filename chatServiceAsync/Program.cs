using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    partial class Program
    {
        private static readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);


        private static List<Socket> mClientSockets;
        public static List<Socket> ClientSockets
        {
            get => mClientSockets = mClientSockets ?? new List<Socket>();
            set => mClientSockets = value;
        }
        //public static Dictionary<Socket, warnedSocketInfo> socketSpamControl = new Dictionary<Socket, warnedSocketInfo>();

        private static Dictionary<Socket, WarnedSocketInfo> mSocketSpamControl;
        public static Dictionary<Socket, WarnedSocketInfo> SocketSpamControl
        {
            get => mSocketSpamControl = mSocketSpamControl ?? new Dictionary<Socket, WarnedSocketInfo>();
            set => mSocketSpamControl = value;
        }

        public static Listener mListener { get; set; }

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
            foreach (Socket socket in ClientSockets)
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

            ClientSockets.Add(socket);
            WarnedSocketInfo a = new WarnedSocketInfo();
            //a.listedControl = true;
            //a.gotWarned = false;
            a.warnedState = 0;
            SocketSpamControl.Add(socket, a);
            socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
            Console.WriteLine("Client connected, waiting for request...");
            serverSocket.BeginAccept(AcceptCallback, null);
        }
        static Socket previousSender;

        private static void ReceiveCallback(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
            var received = 0;

            try
            {
                if (!current.Connected) return;
                received = current.EndReceive(AR);
            }
            catch (SocketException)
            {
                Console.WriteLine("Client forcefully disconnected");
                ClientSockets.Remove(current);
                current.Close();
                return;
            }
            byte[] recBuf = new byte[received];
            Array.Copy(buffer, recBuf, received);
            string text = Encoding.ASCII.GetString(recBuf);
            Console.WriteLine(text);
            byte[] data = Encoding.ASCII.GetBytes(text);
            SpamControl(current, data, text);
            previousSender = current;
        }

        bool SenderContorl => true;


        /// <summary>
        /// Prevents a user to spam chat (send more than 1 message per second and punishes if happens)
        /// and broadcasts one client's messages to every client.
        /// </summary>
        public async static void SpamControl(Socket current, byte[] data, string text)
        {

            var socketWithoutCurrent = ClientSockets.Where(x => x != current).ToList();

            foreach (Socket socket in socketWithoutCurrent)
            {
                if (socket != current) // Send the receiving messages to the other users.
                {
                    await socket.SendAsync(data, SocketFlags.None);
                    socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
                }


            }


            // check every users warned states by server and punish them if necessary.
            byte[] dataToUser = Encoding.ASCII.GetBytes(text + "$$$$$$$$$$");
            await current.SendAsync(dataToUser, SocketFlags.None);
            current.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current);
            byte[] warning;
            SocketSpamControl[current].elapsedTime = stopWatch.ElapsedMilliseconds;
            if (current == previousSender)
            {
                switch (SocketSpamControl[current].warnedState)
                {
                    case 0:
                        stopWatch.Start();
                        SocketSpamControl[current].warnedState = 1;
                        break;
                    case 1:
                        if (SocketSpamControl[current].elapsedTime <= 1000)
                        {
                            warning = Encoding.ASCII.GetBytes("Do NOT spam chat!!! You'll get banned if you do it once again.");
                            await current.SendAsync(warning, SocketFlags.None);
                            Console.WriteLine("Client warned.");
                            SocketSpamControl[current].warnedState = 2;
                        }
                        break;
                    case 2:
                        if (SocketSpamControl[current].elapsedTime <= 1000)
                        {
                            warning = Encoding.ASCII.GetBytes("You've been warned. Sorry...");
                            await current.SendAsync(warning, SocketFlags.None);
                            ClientSockets.Remove(current);
                            Console.WriteLine("Client banned.");
                            SocketSpamControl.Remove(current);
                            current.Shutdown(SocketShutdown.Both);
                            current.Close();
                            return;
                        }
                        break;
                    default:

                        break;
                }
                SocketSpamControl[current].elapsedTime = 0;
                stopWatch.Reset();
                stopWatch.Start();
            }


            //if (SocketSpamControl[current].warnedState == 0)
            //{
            //    stopWatch.Start();
            //    SocketSpamControl[current].warnedState = 1;
            //}
            //else if (SocketSpamControl[current].warnedState == 1 && SocketSpamControl[current].elapsedTime <= 1000)
            //{
            //    warning = Encoding.ASCII.GetBytes("Do NOT spam chat!!! You'll get banned if you do it once again.");
            //    await current.SendAsync(warning, SocketFlags.None);
            //    Console.WriteLine("Client warned.");
            //    SocketSpamControl[current].warnedState = 2;

            //}
            //else if (SocketSpamControl[current].warnedState == 2 && SocketSpamControl[current].elapsedTime <= 1000)
            //{
            //    warning = Encoding.ASCII.GetBytes("You've been warned. Sorry...");
            //    await current.SendAsync(warning, SocketFlags.None);
            //    ClientSockets.Remove(current);
            //    Console.WriteLine("Client banned.");
            //    SocketSpamControl.Remove(current);
            //    current.Shutdown(SocketShutdown.Both);
            //    current.Close();
            //    return;
            //}
            //else
            //{
            //SocketSpamControl[current].elapsedTime = 0;
            //SocketSpamControl[current].warnedState = SocketSpamControl[current].warnedState == 2 ? 2 : SocketSpamControl[current].warnedState;
            //stopWatch.Reset();
            //stopWatch.Start();
            //}
            //});
        }
    }
}
