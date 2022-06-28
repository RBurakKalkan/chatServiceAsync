using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Client
{
    class Program
    {
        private static readonly Socket ClientSocket = new Socket
            (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private const int PORT = 100;

        static void Main()
        {
            Console.Title = "Client";
            ConnectToServer();
            RequestLoop();
            Exit();
        }
        public static string nickName = string.Empty;
        private static void ConnectToServer()
        {
            int attempts = 0;

            while (!ClientSocket.Connected)
            {
                try
                {
                    attempts++;
                    Console.WriteLine("Connection attempt " + attempts);
                    ClientSocket.Connect(IPAddress.Loopback, PORT); // Change IPAddress.Loopback to a remote address.
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

        private static void RequestLoop()
        {

            while (true)
            {
                SendRequest();

            }
        }
        /// <summary>
        /// Listens the broadcaster on server.
        /// </summary>
        static void ConsoleWriter()
        {
            while (true)
            {
                ReceiveResponse();
            }
        }
        /// <summary>
        /// Close socket and exit program.
        /// </summary>
        private static void Exit()
        {
            SendString("exit"); // Tell the server we are exiting
            ClientSocket.Shutdown(SocketShutdown.Both);
            ClientSocket.Close();
            Environment.Exit(0);
        }

        /// <summary>
        /// Sets your nickname and sends your string to the interested method (the that will send it to the server in ASCII encoding).
        /// </summary>
        private static void SendRequest()
        {
            string request = nickName + " : " + Console.ReadLine();
            SendString(request);

            if (request.ToLower() == "exit")
            {
                Exit();
            }
        }

        /// <summary>
        /// Sends a string to the server with ASCII encoding.
        /// </summary>
        private async static void SendString(string text)
        {
            try
            {
                byte[] buffer = Encoding.ASCII.GetBytes(text);
                await ClientSocket.SendAsync(buffer, SocketFlags.None);
            }
            catch (Exception)
            {
                Exit();
            }
        }

        private static void ReceiveResponse()
        {
            try
            {
                var buffer = new byte[2048];
                int received = ClientSocket.Receive(buffer, SocketFlags.None);
                if (received == 0) return;
                var data = new byte[received];
                Array.Copy(buffer, data, received);
                string text = Encoding.ASCII.GetString(data);
                if (!text.Contains("$$$$$$$$$$")) // this is a control that will prevent the broadcaster to show the client its own message.
                {
                    Console.WriteLine(" << " + text);
                }
                else if (text == "exit")
                {
                    Exit();
                }
            }
            catch (Exception)
            {
                Exit();
            }
        }
    }
}
