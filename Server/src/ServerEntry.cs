#define PRINTING_AUTH_STATES

global using System.Text;
global using System.Net.Sockets;
global using System.Net;
global using System.Diagnostics;

global using static CloudLib.CloudProtocol;
global using static CloudLib.SenderReceiver;
using System.ComponentModel;

namespace Server.src;


public class ServerEntry
{
    static ListenerInstance? _listenerInstance;
    public static void Main()
    {
        #if DEBUG
        Console.WriteLine("Server - Debug");
        #else  
        Console.WriteLine("Server - Release");
        #endif

        Console.CancelKeyPress += ( (object? sender, ConsoleCancelEventArgs args) => CleanupResources() );

        _listenerInstance = ListenerInstance.Instance;
        _listenerInstance.Start();
        // Note: The listener creates Authentication managers if necessary.
    }

    private static void CleanupResources()
    {
        Console.WriteLine("Caught the sigint.");
        _listenerInstance?.StopListening();
        Environment.Exit(0);
    }
}