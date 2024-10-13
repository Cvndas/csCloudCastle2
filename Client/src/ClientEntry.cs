global using System.Text;
global using System.Net.Sockets;
global using System.Net;
global using System.Diagnostics;

global using static CloudLib.CloudProtocol;
global using static System.Console;


namespace Client.src;

class Program
{
    public static void Main()
    {
#if DEBUG
        WriteLine("Client - Debug");
#else
        WriteLine("Client- Release");
#endif

        try {
            WriteLine("Welcome (client).");
            ClientInstance instance = ClientInstance.Instance();
            instance.Start();
        }
        catch (Exception e){
            WriteLine("Unhandled exception caught in ClientEntry:");
            WriteLine("Exception Type: " + e.GetType());
            WriteLine("Message: " + e.Message);
        }
        finally {
            WriteLine("Exiting the program.");
        }
    }
}