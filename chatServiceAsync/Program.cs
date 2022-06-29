
namespace Server
{
    partial class Program
    {

        public static Listener mListener = new Listener();

        static void Main()
        {
            mListener.StartDefault();
        }
    }
}
