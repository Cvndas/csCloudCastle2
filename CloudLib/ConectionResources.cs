using System.Net;
using System.Net.Sockets;

namespace CloudLib;

public record ConnectionResources
{
    public TcpClient? TcpClient { get; init; }
    public NetworkStream? Stream { get; init; }

    public void Cleanup()
    {
        TcpClient?.Dispose();
        Stream?.Dispose();
    }
}