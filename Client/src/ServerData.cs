using System.Net.Sockets;
using System.Net;

namespace Client.src;

public struct ServerData
{
    public TcpClient? TcpClient;
    public NetworkStream? Stream;
}