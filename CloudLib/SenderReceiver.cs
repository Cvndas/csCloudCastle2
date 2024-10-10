using System.Net.Sockets;
using CloudLib;

namespace CloudLib;

public static class SenderReceiver
{
    public static void ClientSendMessage(NetworkStream stream, ClientFlags flag, byte[] payload)
    {
        SendMessage(stream, flag, payload);
    }
    public static void ClientSendChatMessage(NetworkStream stream, string username, string body)
    {
        // TODO 
    }
    // TODO : Add more abstractions.

    private static void SendMessage(NetworkStream stream, ClientFlags flag, byte[] payload)
    {
        // TODO 
    }
    private static List<(byte flag, byte[] payload, int payloadlen)> ReceiveMessages(NetworkStream stream, int messageCount)
    {
        // TODO 
        return new List<(byte flag, byte[] payload, int payloadlen)>();
    }
    private static async Task<List<(byte flag, byte[] payload, int payloadlen)>> CancellableReceiveMessages(NetworkStream stream, int messageCount)
    {
        // TODO 
        return await Task.FromResult(new List<(byte flag, byte[] payload, int payloadlen)>());
    }
    private static (byte flag, byte[] payload, int payloadlen) ParseMessage(byte[] message)
    {
        // TODO 
        return ((byte)System.Text.Encoding.UTF8.GetBytes(0.ToString())[0], new byte[0], 0);
    }
}