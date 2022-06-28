using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClientWindows
{
    public partial class Form1 : Form
    {
        private static readonly Socket ClientSocket = new Socket
            (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private const int PORT = 100;
        private void ConnectToServer()
        {
            int attempts = 0;

            while (!ClientSocket.Connected)
            {
                try
                {
                    attempts++;
                    listBox1.Items.Add("Connection attempt " + attempts);
                    // Change IPAddress.Loopback to a remote IP to connect to a remote host.
                    ClientSocket.Connect(IPAddress.Loopback, PORT);
                }
                catch (SocketException)
                {
                    listBox1.Items.Clear();
                }
            }

            //Console.Clear();
            Console.WriteLine("Connected");
        }
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SendRequest(textBox1.Text);
            ReceiveResponse();
        }
        private static void SendRequest(string text)
        {
            Console.Write("Send a request: ");
            string request = text;
            SendString(request);

            //if (request.ToLower() == "exit")
            //{
            //    Exit();
            //}
        }
        private static void SendString(string text)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(text);
            ClientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            ConnectToServer();
        }
    


        //private const int PORT = 100;
        private void ReceiveResponse()
        {
            var buffer = new byte[2048];
            int received = ClientSocket.Receive(buffer, SocketFlags.None);
            if (received == 0) return;
            var data = new byte[received];
            Array.Copy(buffer, data, received);
            string text = Encoding.ASCII.GetString(data);
            listBox1.Items.Add(text);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            ReceiveResponse();
        }
    }
}
