using System;
using System.Runtime.InteropServices;

namespace Std.NanoMsg.Internal
{
    internal static class Utility
    {
        [Flags]
        private enum Events
        {
            PollIn = 0x01,
            PollOut = 0x02
        }

        internal static void Poll(int[] s, int ct, int[] result, nn_pollfd[] info, TimeSpan? timeout)
        {
            var milliseconds = -1;
            if (timeout != null)
            {
                milliseconds = (int) timeout.Value.TotalMilliseconds;
            }
            else
            {
                milliseconds = int.MaxValue;
            }

            unsafe
            {
                for (var i = 0; i < ct; ++i)
                {
                    info[i] = new nn_pollfd
                    {
                        fd = s[i],
                        events = (short) Events.PollIn,
                        revents = 0
                    };
                }

                fixed (nn_pollfd* pInfo = info)
                {
                    Library.nn_poll(pInfo, ct, milliseconds);
                }
            }

            for (var i = 0; i < ct; ++i)
            {
                result[i] = (info[i]
                            .revents &
                        (short) Events.PollIn) !=
                    0
                        ? 1
                        : 0;
            }
        }
    }
}