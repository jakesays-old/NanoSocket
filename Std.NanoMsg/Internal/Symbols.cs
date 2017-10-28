

// ReSharper disable InconsistentNaming
// ReSharper disable UnassignedReadonlyField

namespace Std.NanoMsg.Internal
{
    internal static class Symbols
    {
        public static readonly int NN_NS_NAMESPACE;

        public static readonly int NN_VERSION_CURRENT;

        public static readonly int NN_VERSION_REVISION;

        public static readonly int NN_VERSION_AGE;

        public static readonly int AF_SP;

        public static readonly int AF_SP_RAW;

        public static readonly int NN_INPROC;

        public static readonly int NN_IPC;

        public static readonly int NN_TCP;

        public static readonly int NN_PAIR;

        public static readonly int NN_PUB;

        public static readonly int NN_SUB;

        public static readonly int NN_REP;

        public static readonly int NN_REQ;

        public static readonly int NN_PUSH;

        public static readonly int NN_PULL;

        public static readonly int NN_SURVEYOR;

        public static readonly int NN_RESPONDENT;

        public static readonly int NN_BUS;

        public static readonly int NN_SOCKADDR_MAX;

        public static readonly int NN_SOL_SOCKET;

        public static readonly int NN_LINGER;

        public static readonly int NN_SNDBUF;

        public static readonly int NN_RCVBUF;

        public static readonly int NN_SNDTIMEO;

        public static readonly int NN_RCVTIMEO;

        public static readonly int NN_RECONNECT_IVL;

        public static readonly int NN_RECONNECT_IVL_MAX;

        public static readonly int NN_SNDPRIO;

        public static readonly int NN_SNDFD;

        public static readonly int NN_RCVFD;

        public static readonly int NN_DOMAIN;

        public static readonly int NN_PROTOCOL;

        public static readonly int NN_IPV4ONLY;

        public static readonly int NN_SUB_SUBSCRIBE;

        public static readonly int NN_SUB_UNSUBSCRIBE;

        public static readonly int NN_REQ_RESEND_IVL;

        public static readonly int NN_SURVEYOR_DEADLINE;

        public static readonly int NN_TCP_NODELAY;

        public static readonly int NN_DONTWAIT;

        public static readonly int EADDRINUSE;

        public static readonly int EADDRNOTAVAIL;

        public static readonly int EAFNOSUPPORT;

        public static readonly int EAGAIN;

        public static readonly int EBADF;

        public static readonly int ECONNREFUSED;

        public static readonly int EFAULT;

        public static readonly int EFSM;

        public static readonly int EINPROGRESS;

        public static readonly int EINTR;

        public static readonly int EINVAL;

        public static readonly int EMFILE;

        public static readonly int ENAMETOOLONG;

        public static readonly int ENETDOWN;

        public static readonly int ENOBUFS;

        public static readonly int ENODEV;

        public static readonly int ENOMEM;

        public static readonly int ENOPROTOOPT;

        public static readonly int ENOTSOCK;

        public static readonly int ENOTSUP;

        public static readonly int EPROTO;

        public static readonly int EPROTONOSUPPORT;

        public static readonly int ETERM;

        public static readonly int ETIMEDOUT;

        public static readonly int EACCES;

        public static readonly int ECONNABORTED;

        public static readonly int ECONNRESET;

        public static readonly int EHOSTUNREACH;

        public static readonly int EMSGSIZE;

        public static readonly int ENETRESET;

        public static readonly int ENETUNREACH;

        public static readonly int ENOTCONN;

        static Symbols()
        {
            var symbolCount = Library.nn_symbol_count();

            for (var symbolIndex = 0; symbolIndex < symbolCount; ++symbolIndex)
            {
                var symbolText = Library.nn_symbol(symbolIndex, out var value);

                if (symbolText == null)
                {
                    break;
                }

                switch (symbolText)
                {
                    case "NN_NS_NAMESPACE":
                        NN_NS_NAMESPACE = value;
                        break;

                    case "NN_VERSION_CURRENT":
                        NN_VERSION_CURRENT = value;
                        break;

                    case "NN_VERSION_REVISION":
                        NN_VERSION_REVISION = value;
                        break;

                    case "NN_VERSION_AGE":
                        NN_VERSION_AGE = value;
                        break;

                    case "AF_SP":
                        AF_SP = value;
                        break;

                    case "AF_SP_RAW":
                        AF_SP_RAW = value;
                        break;

                    case "NN_INPROC":
                        NN_INPROC = value;
                        break;

                    case "NN_IPC":
                        NN_IPC = value;
                        break;

                    case "NN_TCP":
                        NN_TCP = value;
                        break;

                    case "NN_PAIR":
                        NN_PAIR = value;
                        break;

                    case "NN_PUB":
                        NN_PUB = value;
                        break;

                    case "NN_SUB":
                        NN_SUB = value;
                        break;

                    case "NN_REP":
                        NN_REP = value;
                        break;

                    case "NN_REQ":
                        NN_REQ = value;
                        break;

                    case "NN_PUSH":
                        NN_PUSH = value;
                        break;

                    case "NN_PULL":
                        NN_PULL = value;
                        break;

                    case "NN_SURVEYOR":
                        NN_SURVEYOR = value;
                        break;

                    case "NN_RESPONDENT":
                        NN_RESPONDENT = value;
                        break;

                    case "NN_BUS":
                        NN_BUS = value;
                        break;

                    case "NN_SOCKADDR_MAX":
                        NN_SOCKADDR_MAX = value;
                        break;

                    case "NN_SOL_SOCKET":
                        NN_SOL_SOCKET = value;
                        break;

                    case "NN_LINGER":
                        NN_LINGER = value;
                        break;

                    case "NN_SNDBUF":
                        NN_SNDBUF = value;
                        break;

                    case "NN_RCVBUF":
                        NN_RCVBUF = value;
                        break;

                    case "NN_SNDTIMEO":
                        NN_SNDTIMEO = value;
                        break;

                    case "NN_RCVTIMEO":
                        NN_RCVTIMEO = value;
                        break;

                    case "NN_RECONNECT_IVL":
                        NN_RECONNECT_IVL = value;
                        break;

                    case "NN_RECONNECT_IVL_MAX":
                        NN_RECONNECT_IVL_MAX = value;
                        break;

                    case "NN_SNDPRIO":
                        NN_SNDPRIO = value;
                        break;

                    case "NN_SNDFD":
                        NN_SNDFD = value;
                        break;

                    case "NN_RCVFD":
                        NN_RCVFD = value;
                        break;

                    case "NN_DOMAIN":
                        NN_DOMAIN = value;
                        break;

                    case "NN_PROTOCOL":
                        NN_PROTOCOL = value;
                        break;

                    case "NN_IPV4ONLY":
                        NN_IPV4ONLY = value;
                        break;

                    case "NN_SUB_SUBSCRIBE":
                        NN_SUB_SUBSCRIBE = value;
                        break;

                    case "NN_SUB_UNSUBSCRIBE":
                        NN_SUB_UNSUBSCRIBE = value;
                        break;

                    case "NN_REQ_RESEND_IVL":
                        NN_REQ_RESEND_IVL = value;
                        break;

                    case "NN_SURVEYOR_DEADLINE":
                        NN_SURVEYOR_DEADLINE = value;
                        break;

                    case "NN_TCP_NODELAY":
                        NN_TCP_NODELAY = value;
                        break;

                    case "NN_DONTWAIT":
                        NN_DONTWAIT = value;
                        break;

                    case "EADDRINUSE":
                        EADDRINUSE = value;
                        break;

                    case "EADDRNOTAVAIL":
                        EADDRNOTAVAIL = value;
                        break;

                    case "EAFNOSUPPORT":
                        EAFNOSUPPORT = value;
                        break;

                    case "EAGAIN":
                        EAGAIN = value;
                        break;

                    case "EBADF":
                        EBADF = value;
                        break;

                    case "ECONNREFUSED":
                        ECONNREFUSED = value;
                        break;

                    case "EFAULT":
                        EFAULT = value;
                        break;

                    case "EFSM":
                        EFSM = value;
                        break;

                    case "EINPROGRESS":
                        EINPROGRESS = value;
                        break;

                    case "EINTR":
                        EINTR = value;
                        break;

                    case "EINVAL":
                        EINVAL = value;
                        break;

                    case "EMFILE":
                        EMFILE = value;
                        break;

                    case "ENAMETOOLONG":
                        ENAMETOOLONG = value;
                        break;

                    case "ENETDOWN":
                        ENETDOWN = value;
                        break;

                    case "ENOBUFS":
                        ENOBUFS = value;
                        break;

                    case "ENODEV":
                        ENODEV = value;
                        break;

                    case "ENOMEM":
                        ENOMEM = value;
                        break;

                    case "ENOPROTOOPT":
                        ENOPROTOOPT = value;
                        break;

                    case "ENOTSOCK":
                        ENOTSOCK = value;
                        break;

                    case "ENOTSUP":
                        ENOTSUP = value;
                        break;

                    case "EPROTO":
                        EPROTO = value;
                        break;

                    case "EPROTONOSUPPORT":
                        EPROTONOSUPPORT = value;
                        break;

                    case "ETERM":
                        ETERM = value;
                        break;

                    case "ETIMEDOUT":
                        ETIMEDOUT = value;
                        break;

                    case "EACCES":
                        EACCES = value;
                        break;

                    case "ECONNABORTED":
                        ECONNABORTED = value;
                        break;

                    case "ECONNRESET":
                        ECONNRESET = value;
                        break;

                    case "EHOSTUNREACH":
                        EHOSTUNREACH = value;
                        break;

                    case "EMSGSIZE":
                        EMSGSIZE = value;
                        break;

                    case "ENETRESET":
                        ENETRESET = value;
                        break;

                    case "ENETUNREACH":
                        ENETUNREACH = value;
                        break;

                    case "ENOTCONN":
                        ENOTCONN = value;
                        break;
                }
            }
        }
    }
}