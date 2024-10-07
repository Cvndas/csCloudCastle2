using System.Text;
using System.Net;

namespace CloudLib;
public static class ProtocolHeader
{
    /*
    ** ::: ::: ::: ::: Message structure ::: ::: :::  
    ** [1 byte : flagbyte] [4 byte : payloadlen] [var length payload] [4 byte : terminator sequence]
    ** ::: ::: ::: ::: ----------------- ::: ::: ::: 
    */

    public const uint TERMINATOR_SEQUENCE = 2348182392;

    public static byte[] GetProtocolHeader(byte[] payload)
    {
        // TODO Header 
        return new byte[0];
    }


}