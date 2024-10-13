#define PRINTING_AUTH_STATES

global using System.Text;
global using System.Net.Sockets;
global using System.Net;
global using System.Diagnostics;

global using static System.Console;
global using static CloudLib.CloudProtocol;
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
        // TODO before registration: authenticationmanager should only be initialized if
        // there are no authenticationManagers yet, if the number of managers is < the max,
        // and if the listener has received an incoming connection.
        AuthenticationManager authenticationManager = AuthenticationManager.Instance;


        _listenerInstance.Start();
    }

    private static void CleanupResources()
    {
        Console.WriteLine("Caught the sigint.");
        _listenerInstance?.StopListening();
        Environment.Exit(0);
    }
}