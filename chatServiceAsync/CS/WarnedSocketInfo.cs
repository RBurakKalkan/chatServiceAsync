using System.Collections.Generic;
using System.Net.Sockets;

namespace Server
{
    public class WarnedSocketInfo
    {
        public long elapsedTime { get; set; }
        public int warnedState { get; set; }

        private static Dictionary<Socket, WarnedSocketInfo> mSocketSpamControl;
        public static Dictionary<Socket, WarnedSocketInfo> SocketSpamControl
        {
            get => mSocketSpamControl = mSocketSpamControl ?? new Dictionary<Socket, WarnedSocketInfo>();
            set => mSocketSpamControl = value;
        }
    }
}
