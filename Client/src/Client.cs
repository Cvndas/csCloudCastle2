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
        try {
            Console.WriteLine("Welcome (client).");
            ClientInstance instance = ClientInstance.Instance();
            instance.Start();
        }
        catch (ExitProgram) {
            Console.WriteLine("Exiting the program.");
        }
    }
}