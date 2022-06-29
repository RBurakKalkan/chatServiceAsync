using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    public class Listener
    {
        #region MyRegion

        //Constant Defines
        private int BUFFER_SIZE { get; set; } //= 2048;
        private int PORT { get; set; } //= 100;

        private string myTitle { get; set; }

        #endregion

        //bla bla..
        private void InitVariables()
        {
            BUFFER_SIZE = 2048;
            PORT = 100;

            myTitle = "Setting...";
        }

        public Listener()
        {
            InitVariables();
        }

        public void SetTitle(string str) => myTitle = str;

        public void Start(string ip, int port)
        {
            PORT = port;
        }
        public void Start(int port)
        {
            PORT = port;
        }





        private void SetupServer()
        {
        }
    }

    public static class Foo
    {
        static Foo()
        {

        }
    }

}
