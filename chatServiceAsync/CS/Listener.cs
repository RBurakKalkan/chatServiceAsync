using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    public class Listener
    {
        #region Variables & Definitions
        private static int BufferSize { get; set; }
        private static int Port { get; set; }
        static Stopwatch SWatchSpamTimer = new Stopwatch();
        private static byte[] Buffer { get; set; }
        private static IPAddress iPAddress { get; set; }
        private static Socket previousSender { get; set; }
        private void InitVariables()
        {
            iPAddress = IPAddress.Any;
            BufferSize = 2048;
            Port = 100;
            Buffer = new byte[BufferSize];
        }
        private static void CredentialControl(Socket socket, IPAddress _iPadress, int _Port)
        {
            if (socket != serverSocket)
            {
                serverSocket = socket;
            }
            if (_iPadress != iPAddress)
            {
                iPAddress = _iPadress;
            }
            if (Port != _Port)
            {
                Port = _Port;
            }
        }
        private static Socket mServerSocket;
        public static Socket serverSocket
        {
            get => mServerSocket = mServerSocket ?? new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            set => mServerSocket = value;
        }
        #endregion
        /// <summary>
        /// Makes required configurations according to the provided credentials then runs the Server.
        /// </summary>
        private static void SetupServer(Socket socket, IPAddress _iPadress, int _Port)
        {
            CredentialControl(socket, _iPadress, _Port);
            Console.WriteLine("Setting up server...");
            socket.Bind(new IPEndPoint(_iPadress, (int)_Port));
            socket.Listen(0);
            socket.BeginAccept(AcceptCallback, null);
            Console.WriteLine("Server setup complete");
        }
        /// <summary>
        /// Accepts client connections.
        /// </summary>
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

            WarnedSocketInfo a = new WarnedSocketInfo();
            a.warnedState = 0;
            WarnedSocketInfo.SocketSpamControl.Add(socket, a);
            serverSocket.BeginAccept(AcceptCallback, null);
            socket.BeginReceive(Buffer, 0, BufferSize, SocketFlags.None, ReceiveCallback, socket);
            Console.WriteLine("Client connected, waiting for request...");
        }
        /// <summary>
        /// Receives sent messages and fills console with them.
        /// </summary>
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
                WarnedSocketInfo.SocketSpamControl.Remove(current);
                current.Close();
                return;
            }
            byte[] recBuf = new byte[received];
            Array.Copy(Buffer, recBuf, received);
            string text = Encoding.ASCII.GetString(recBuf);
            Console.WriteLine(text);
            byte[] data = Encoding.ASCII.GetBytes(text);
            SpamControl(current, data, text);
            previousSender = current;
        }
        /// <summary>
        /// Prevents a user to spam chat (send more than 1 message per second and punishes if happens)
        /// and broadcasts one client's messages to every client.
        /// </summary>
        public async static void SpamControl(Socket current, byte[] data, string text)// Made it public just to unit test.
        {

            var socketWithoutCurrent = WarnedSocketInfo.SocketSpamControl.Keys.Where(x => x != current).ToList();

            foreach (Socket socket in socketWithoutCurrent)
            {
                if (socket != current) // Send the receiving messages to the other users.
                {
                    await socket.SendAsync(data, SocketFlags.None);
                    socket.BeginReceive(Buffer, 0, BufferSize, SocketFlags.None, ReceiveCallback, socket);
                }
            }


            // check every users warned states by server and punish them if necessary.
            byte[] dataToUser = Encoding.ASCII.GetBytes(text + "$$$$$$$$$$");
            await current.SendAsync(dataToUser, SocketFlags.None);
            current.BeginReceive(Buffer, 0, BufferSize, SocketFlags.None, ReceiveCallback, current);
            byte[] warning;
            WarnedSocketInfo.SocketSpamControl[current].elapsedTime = SWatchSpamTimer.ElapsedMilliseconds;
            if (current == previousSender)
            {
                switch (WarnedSocketInfo.SocketSpamControl[current].warnedState)
                {
                    case 0:
                        SWatchSpamTimer.Start();
                        WarnedSocketInfo.SocketSpamControl[current].warnedState = 1;
                        break;
                    case 1:
                        if (WarnedSocketInfo.SocketSpamControl[current].elapsedTime <= 1000)
                        {
                            warning = Encoding.ASCII.GetBytes("Do NOT spam chat!!! You'll get banned if you do it once again.");
                            await current.SendAsync(warning, SocketFlags.None);
                            Console.WriteLine("Client warned.");
                            WarnedSocketInfo.SocketSpamControl[current].warnedState = 2;
                        }
                        break;
                    case 2:
                        if (WarnedSocketInfo.SocketSpamControl[current].elapsedTime <= 1000)
                        {
                            warning = Encoding.ASCII.GetBytes("You've been warned. Sorry...");
                            await current.SendAsync(warning, SocketFlags.None);
                            Console.WriteLine("Client banned.");
                            WarnedSocketInfo.SocketSpamControl.Remove(current);
                            current.Shutdown(SocketShutdown.Both);
                            current.Close();
                            return;
                        }
                        break;
                    default:

                        break;
                }
                WarnedSocketInfo.SocketSpamControl[current].elapsedTime = 0;
                SWatchSpamTimer.Reset();
                SWatchSpamTimer.Start();
            }
        }
        /// <summary>
        /// Close all connected client (we do not need to shutdown the server socket as its connections
        /// are already closed with the clients).
        /// </summary>
        private static void CloseAllSockets(Socket ServerSocket)
        {
            foreach (Socket socket in WarnedSocketInfo.SocketSpamControl.Keys)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            ServerSocket.Close();
        }
        /// <summary>
        /// Runs server with default credentials (on local machine).
        /// </summary>
        public void StartDefault()
        {
            InitVariables();
            Console.Title = "Server";
            SetupServer(serverSocket, iPAddress, Port);
            Console.ReadLine(); // When we press enter close everything
            CloseAllSockets(serverSocket);
        }
        /// <summary>
        /// If required you can use this method to give another Remote PC IP, Port and Socket info.
        /// </summary>
        public void Start(Socket socket, IPAddress ipAddress, int Port)
        {
            Console.Title = "Server";
            SetupServer(socket, ipAddress, Port);
            Console.ReadLine(); // When we press enter close everything
            CloseAllSockets(socket);
        }
    }
}
