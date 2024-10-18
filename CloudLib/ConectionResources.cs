using System.Net;
using System.Net.Sockets;

namespace CloudLib;

public record ConnectionResources
{
    public ConnectionResources(TcpClient tcpClient, NetworkStream stream, int id){
        this.tcpClient = tcpClient;
        this.stream = stream;
        this.id = id;
    }

    public readonly TcpClient tcpClient;
    public readonly NetworkStream stream;

    // TODO during transfer to dashboard: set the username upon login.
    public readonly string? Username;
    public readonly int id;

    public void Cleanup()
    {
        Console.WriteLine("-------- PERFORMING CLEANUP OF RESOURCES ----------");
        tcpClient.Dispose();
        stream.Dispose();
    }
}