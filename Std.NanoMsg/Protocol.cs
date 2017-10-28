using Std.NanoMsg.Internal;
using Std.NanoMsg.Native;

namespace Std.NanoMsg
{
    public enum Protocol
    {
        Pair = 16,
        Publish = 32,
        Subscribe = 33,
        Request = 48,
        Reply = 49,
        Push = 80,
        Pull = 81,
        Surveyor = 96,
        Respondent = 97,
        Bus = 112
    }
}