using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Client
{
    public class Receiver
    {
        #region Variables & Definitions
        private static int Port { get; set; }
        private static int BufferSize { get; set; }
        private static Socket mClientSocket;
        public static Socket ClientSocket
        {
            get => mClientSocket = mClientSocket ?? new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            set => mClientSocket = value;
        }
        private static IPAddress iPAddress { get; set; }
        private void InitVariables()
        {
            iPAddress = IPAddress.Loopback;
            BufferSize = 2048;
            Port = 100;
        }

        public static string nickName { get; private set; }
        private static void CredentialControl(Socket socket, IPAddress _iPadress, int _Port)
        {
            if (socket != ClientSocket)
            {
                ClientSocket = socket;
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
        #endregion

        /// <summary>
        /// Tries to the connect to the remote server with given socket,_iPadress and _Port
        /// </summary>
        private static void ConnectToServer(Socket socket, IPAddress _iPadress, int _Port)
        {
            int attempts = 0;
            CredentialControl(socket, _iPadress, _Port);
            while (!ClientSocket.Connected)
            {
                try
                {
                    attempts++;
                    Console.WriteLine("Connection attempt " + attempts);
                    socket.Connect(_iPadress, _Port);
                }
                catch (SocketException)
                {
                    Console.Clear();
                }
            }

            Console.Clear();
            Console.WriteLine("Connected.\nPlease enter your nickname...");
            Thread consoleWriter = new Thread(new ThreadStart(ConsoleWriter));
            consoleWriter.Start();
            nickName = Console.ReadLine();
            Console.WriteLine("You can chat now...");
        }
        /// <summary>
        /// Listens the broadcaster on server in a loop.
        /// </summary>
        private static void ConsoleWriter()
        {
            while (true)
            {
                Thread.Sleep(100);
                ReceiveResponse();
            }
        }
        /// <summary>
        /// Listens the broadcaster on server.
        /// </summary>
        private async static void ReceiveResponse()
        {
            try
            {
                var buffer = new byte[2048];
                int received = await ClientSocket.ReceiveAsync(buffer, SocketFlags.None);
                if (received == 0) return;
                var data = new byte[received];
                Array.Copy(buffer, data, received);
                string text = Encoding.ASCII.GetString(data);
                if (!text.Contains("$$$$$$$$$$")) // this is a control that will prevent the broadcaster to show the client its own message.
                {
                    Console.WriteLine(" << " + text);
                }
                else if (text == "You've been warned. Sorry...") Exit();
            }
            catch (Exception) { Exit(); }
        }
        /// <summary>
        /// Close socket and exit program.
        /// </summary>
        public static void Exit()
        {
            ClientSocket.Shutdown(SocketShutdown.Both);
            ClientSocket.Close();
            Environment.Exit(0);
        }
        /// <summary>
        /// Start application with Default initialized variables.
        /// </summary>
        public void StartDefault()
        {
            InitVariables();
            ConnectToServer(ClientSocket, iPAddress, Port);
            Sender.SendLoop();
            Exit();
        }
        /// <summary>
        /// Start application with given socket, ipaddress and port.
        /// </summary>
        public void StartDefault(Socket socket, IPAddress _iPadress, int _Port)
        {
            ConnectToServer(socket, _iPadress, _Port);
            Sender.SendLoop();
            Exit();
        }
    }
}
