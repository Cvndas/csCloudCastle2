namespace CloudLib;

public class ProtocolConstants
{
    public const int MEGABYTE_LEN = 1_048_576;
    public const int BYTE_LEN = 1;
    public const int MAX_PAYLOAD_LEN = 20 * MEGABYTE_LEN;
    public const int HEADER_LEN = 40;
    public const int TERMINATOR_SEQUENCE_LEN = 4;
    public const int MAX_MESSAGE_LEN = MAX_PAYLOAD_LEN + HEADER_LEN + TERMINATOR_SEQUENCE_LEN;
}
