using System.Net.Sockets;
using System.Diagnostics;
using CloudLib;


namespace CloudLib;

public static class SenderReceiver
{

    /// <summary>
    /// Throws IO Exception if disconnection occurs.
    /// </summary>
    public static void SendMessage(NetworkStream stream, byte flag, byte[] payload)
    {
        int payloadLen = payload.Length;
        byte[] sendBuffer = new byte[CloudProtocol.HEADER_LEN + payloadLen];
        byte[] payloadLengthBytes = BitConverter.GetBytes(payloadLen);
        sendBuffer[0] = flag;
        Array.Copy(payloadLengthBytes, 0, sendBuffer, CloudProtocol.FLAG_LEN, payloadLengthBytes.Count());
        Array.Copy(payload, 0, sendBuffer, CloudProtocol.HEADER_LEN, payloadLen);
        stream.Write(sendBuffer);
    }





    /// <summary>
    /// Doesn't throw exceptions. <br/>
    /// Check Flag for ServerFlags.DISCONNECTION
    /// </summary>
    public static (byte flag, byte[] payload) ReceiveMessage(NetworkStream stream)
    {
        try {
            byte[] headerBuffer = new byte[CloudProtocol.HEADER_LEN];
            int headerBytesRead = 0;

            do {
                headerBytesRead += stream.Read(headerBuffer, 0, CloudProtocol.HEADER_LEN);
            } while (headerBytesRead < CloudProtocol.HEADER_LEN);

            byte flag = ProtocolHeader.GetGenericFlag(headerBuffer);
            int payloadLen = ProtocolHeader.GetPayloadLen(headerBuffer);

            Debug.Assert(payloadLen >= 0);
            if (payloadLen < 0) {
                return ((byte)ServerFlags.INVALID_FLAG, Array.Empty<byte>());
            }

            byte[] payloadBuffer = new byte[payloadLen];
            int payloadBytesRead = 0;
            do {
                payloadBytesRead += stream.Read(payloadBuffer, 0, payloadLen);
            } while (payloadBytesRead < payloadLen);

            return (flag, payloadBuffer);
        }

        catch (IOException) {
            return ((byte)ServerFlags.DISCONNECTION, Array.Empty<byte>());
        }
    }





    /// <summary>
    /// Blocking read function that can be cancelled. <br/>
    /// Does NOT throw an error. <br/>
    /// Checkk ServerFlags.DISCONNECTION and ServerFlags.READ_CANCELED
    /// </summary>
    public static async Task<(byte flag, byte[] payload)> ReceiveMessageCancellable(NetworkStream stream, CancellationToken token)
    {
        try {
            byte[] headerBuffer = new byte[CloudProtocol.HEADER_LEN];
            int headerBytesRead = 0;
            Task<int> headerBytesReadTask;

            do {
                headerBytesReadTask = stream.ReadAsync(headerBuffer, 0, CloudProtocol.HEADER_LEN, token);
                headerBytesRead += await (headerBytesReadTask);
            } while (headerBytesRead < CloudProtocol.HEADER_LEN && stream.DataAvailable);

            byte flag = ProtocolHeader.GetGenericFlag(headerBuffer);
            int payloadLen = ProtocolHeader.GetPayloadLen(headerBuffer);
            if (payloadLen < 0) {
                return ((byte)ServerFlags.INVALID_FLAG, Array.Empty<byte>());
            }

            byte[] payloadBuffer = new byte[payloadLen];
            int payloadBytesRead = 0;
            do {
                payloadBytesRead += await stream.ReadAsync(payloadBuffer, 0, payloadLen, token);
            } while (payloadBytesRead < payloadLen);

            return (flag, payloadBuffer);
        }
        catch (OperationCanceledException) {
            // Still consume all remaining data in the stream.
            Console.WriteLine("ReceiveMessagesCancellable canceled. Consuming remaining messages.");
            while (stream.DataAvailable) {
                ReceiveMessage(stream);
            }
            return ((byte)ServerFlags.READ_CANCELED, Array.Empty<byte>());
        }
        catch (IOException) {
            return ((byte)ServerFlags.DISCONNECTION, Array.Empty<byte>());
        }
    }
}