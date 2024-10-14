using System.Net;
using System.Net.Sockets;

namespace CloudLib;

public record ConnectionResources
{
    public TcpClient? TcpClient { get; init; }
    public NetworkStream? Stream { get; init; }

    public void Cleanup()
    {
        Console.WriteLine("-------- PERFORMING CLEANUP OF RESOURCES ----------");
        TcpClient?.Dispose();
        Stream?.Dispose();
    }
}