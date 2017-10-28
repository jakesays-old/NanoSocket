using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using Std.NanoMsg.Internal;
using Std.NanoMsg.Native;

namespace Std.NanoMsg
{
    public class Listener
    {
        private int _socketCount;
        private nn_pollfd[] _pollFileDescriptors = new nn_pollfd[1];
        private int[] _results = new int[1];
        private int[] _sockets = new int[1];

        public delegate void ReceivedDelegate(int socketId);

        public void AddSocket(NanoSocket socket)
        {
            AddSocket(socket.SocketId);
        }

        public void AddSocket(int socket)
        {
            var capacity = _sockets.Length;
            if (_socketCount >= capacity)
            {
                var newCapacity = capacity * 2;
                var newSockets = new int[newCapacity];
                var newResults = new int[newCapacity];
                var newPollFds = new nn_pollfd[newCapacity];
                Array.Copy(_sockets, newSockets, _socketCount);
                Array.Copy(_results, newResults, _socketCount);
                Array.Copy(_pollFileDescriptors, newPollFds, _socketCount);
                _sockets = newSockets;
                _results = newResults;
                _pollFileDescriptors = newPollFds;
            }
            _sockets[_socketCount] = socket;
            ++_socketCount;
        }

        public void RemoveSocket(NanoSocket s)
        {
            RemoveSocket(s.SocketId);
        }

        public void RemoveSocket(int socket)
        {
            for (var i = 0; i < _socketCount; ++i)
            {
                if (_sockets[i] != socket)
                {
                    continue;
                }

                Array.Copy(_sockets, i + 1, _sockets, i, _socketCount - i - 1);
                Array.Copy(_results, i + 1, _results, i, _socketCount - i - 1);
                Array.Copy(_pollFileDescriptors, i + 1, _pollFileDescriptors, i, _socketCount - i - 1);
                --_socketCount;
                break;
            }
        }

        public event ReceivedDelegate ReceivedMessage;

        [HandleProcessCorruptedStateExceptions]
        public void Listen(TimeSpan? listenTimeout)
        {
            try
            {
                Utility.Poll(_sockets, _socketCount, _results, _pollFileDescriptors, listenTimeout);
            }
            catch (Exception e)
            {
                Console.WriteLine("DEBUG: Poll threw exception, ignoring: " + e);
                Thread.Sleep(TimeSpan
                    .FromSeconds(
                        1)); // This shouldn't ever happen, but when it does (!), this prevents a screen full of text.
                return;
            }

            for (var i = 0; i < _socketCount; ++i)
            {
                if (_results[i] == 0)
                {
                    continue;
                }

                ReceivedMessage?.Invoke(_sockets[i]);
            }
        }
    }
}