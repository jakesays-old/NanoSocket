using Std.NanoMsg.Internal;
using Std.NanoMsg.Native;

namespace Std.NanoMsg
{
    public enum SocketOption
    {
        Linger = 1,
        SendBuffer = 2,
        ReceiveBuffer = 3,
        SendTimeout = 4,
        ReceiveTimeout = 5,
        ReconnectIvl = 6,
        ReconnectIvlMax = 7,
        SendPriority = 8,
        SendFileDescriptor = 10,
        ReceiveFileDescriptor = 11,
        Domain = 12,
        Protocol = 13,
        Ipv4Only = 14,
        TcpNodelay = 1,
        SurveyorDeadline = 1,
        RequestResendInterval = 1,
        SubSubscribe = 1,
        SubUnsubscribe = 2
    }
}