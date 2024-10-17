#define PRINTING_AUTH_STATES

global using System.Text;
global using System.Net.Sockets;
global using System.Net;
global using System.Diagnostics;

global using static CloudLib.ProtocolConstants;
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

        if (!System.IO.Directory.GetCurrentDirectory().EndsWith("3_CloudCastle2")) {
            Console.WriteLine("Please launch the server via the official launch script, or via"
            + " the debugging task.");
            return;
        }

        // Route the debug output into standard out.
        var myWriter = new TextWriterTraceListener(Console.Out);
        Trace.Listeners.Add(myWriter);

        Console.CancelKeyPress += ((object? sender, ConsoleCancelEventArgs args) => CleanupResources());

        _listenerInstance = ListenerInstance.Instance;
        _listenerInstance.Start();

        // Note: The listener creates its own Authentication managers when necessary.
    }

    private static void CleanupResources()
    {
        Console.WriteLine("Caught the sigint.");
        _listenerInstance?.StopListening();
        Environment.Exit(0);
    }
}