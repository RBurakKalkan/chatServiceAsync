using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Client.CS
{
    public class Sender
    {
        /// <summary>
        /// Listens the broadcaster on server.
        /// </summary>
        public static void SendLoop()
        {
            while (true)
            {
                SendRequest();
            }
        }
        /// <summary>
        /// Sets your nickname and sends your string to the interested method (the that will send it to the server in ASCII encoding).
        /// </summary>
        private static void SendRequest()
        {
            string request = Receiver.nickName + " : " + Console.ReadLine();
            SendString(request);

            if (request.ToLower() == "exit")
            {
                Receiver.Exit();
            }
        }
        /// <summary>
        /// Sends a string to the server asynchronously with ASCII encoding.
        /// </summary>
        private async static void SendString(string text)
        {
            try
            {
                byte[] buffer = Encoding.ASCII.GetBytes(text);
                await Receiver.ClientSocket.SendAsync(buffer, SocketFlags.None);
            }
            catch (Exception)
            {
                Receiver.Exit();
            }
        }
    }
}
