global using System.Text;
global using System.Net.Sockets;
global using System.Net;
global using System.Diagnostics;

global using static CloudLib.ProtocolConstants;


namespace Client.src;

class Program
{
    public static void Main()
    {
#if DEBUG
        Console.WriteLine("Client - Debug");
#else
        Console.WriteLine("Client- Release");
#endif

        try {
            Console.WriteLine("Welcome (client).");
            ClientInstance instance = ClientInstance.Instance();
            instance.Start();
        }
        catch (Exception e) {
            Console.WriteLine("Unhandled exception caught in ClientEntry:");
            Console.WriteLine("Exception Type: " + e.GetType());
            Console.WriteLine("Message: " + e.Message);
        }
        finally {
            Console.WriteLine("Exiting the program.");
        }
    }
}