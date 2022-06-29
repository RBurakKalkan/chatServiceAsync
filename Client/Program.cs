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
        static Receiver receiver = new Receiver();
        static void Main()
        {
            receiver.StartDefault();
        }
    }
}
