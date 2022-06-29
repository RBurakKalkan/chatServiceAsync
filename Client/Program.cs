
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
