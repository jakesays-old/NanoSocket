using System;

namespace Std.NanoMsg
{
    public static class NanoSocketExtensions
    {
        public static void Subscribe(this NanoSocket socket, string topic)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            topic = topic ?? "";

            SocketOptions.SetString(socket, SocketOptionLevel.Subscribe, SocketOption.SubSubscribe, topic);
        }

        private static readonly byte[] _noTopic = {0};

        public static void Subscribe(this NanoSocket socket, byte[] topic)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            topic = topic ?? _noTopic;

            SocketOptions.SetBytes(socket, SocketOptionLevel.Subscribe, SocketOption.SubSubscribe, topic);
        }

        public static void Unsubscribe(this NanoSocket socket, string topic)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            topic = topic ?? "";

            SocketOptions.SetString(socket, SocketOptionLevel.Subscribe, SocketOption.SubUnsubscribe, topic);
        }

        public static void Unsubscribe(this NanoSocket socket, byte[] topic)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            topic = topic ?? _noTopic;

            SocketOptions.SetBytes(socket, SocketOptionLevel.Subscribe, SocketOption.SubUnsubscribe, topic);
        }

        public static void SetSurveyorDeadline(this NanoSocket socket, TimeSpan deadline)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            SocketOptions.SetTimespan(socket.SocketId, SocketOptionLevel.Surveyor, SocketOption.SurveyorDeadline, deadline);
        }

        public static TimeSpan? GetSurveyorDeadline(this NanoSocket socket)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            return SocketOptions.GetTimespan(socket.SocketId,
                SocketOptionLevel.Surveyor,
                SocketOption.SurveyorDeadline);
        }
    }
}