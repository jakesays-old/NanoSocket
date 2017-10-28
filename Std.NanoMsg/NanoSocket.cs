using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using Std.NanoMsg.Internal;

namespace Std.NanoMsg
{
    public sealed class NanoSocket : IDisposable
    {
        private const int InvalidSocketHandle = -1;
        private ReadStream _recycledReadStream;

        internal UnmanagedBufferManager BufferManager { get; }

        private int _socket;

        public const int DefaultBufferPoolSize = 128;
        public const int DefaultBufferSize = 2048;

        private NanoSocket(Domain domain, Protocol protocol, int maxBufferPoolSize, int maxBufferSize)
        {
            Bindable = protocol != Protocol.Bus && protocol != Protocol.Request;
            CanWrite = protocol == Protocol.Publish || protocol == Protocol.Push || protocol == Protocol.Pair;
            CanRead = protocol == Protocol.Subscribe || protocol == Protocol.Pull || protocol == Protocol.Pair;
            Connectable = protocol != Protocol.Bus && protocol != Protocol.Reply && protocol != Protocol.Publish;

            Domain = domain;
            Protocol = protocol;

            _socket = Library.nn_socket((int) Domain, (int) Protocol);
            if (_socket >= 0)
            {
                Options = new SocketOptions(_socket);
            }
            else
            {
                throw new NanoException($"nn_socket {domain} {protocol}", _socket);
            }

            BufferManager = UnmanagedBufferManager.Create(maxBufferPoolSize, maxBufferSize);
        }

        public bool Bindable { get; }
        public bool CanRead { get; }
        public bool CanWrite { get; }
        public bool Connectable { get; }

        public Domain Domain { get; }
        public Protocol Protocol { get; }
        public SocketOptions Options { get; }

        public int SocketId
        {
            get => _socket;
        }

        public void Close()
        {
            Dispose(false);
        }

        /// <summary>
        ///     Closes the socket, releasing its resources and making it invalid for future use.
        /// </summary>
        /// <exception cref="NanoException">
        ///     Thrown if the socket is invalid or the close attempt was interrupted and should be
        ///     reattempted.
        /// </exception>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public static void Terminate()
        {
            Library.nn_term();
        }

        public static NanoSocket CreateBusSocket(Domain domain = Domain.Sp,
            int bufferPoolSize = DefaultBufferPoolSize,
            int bufferSize = DefaultBufferSize)
        {
            return new NanoSocket(domain, Protocol.Bus, bufferPoolSize, bufferSize);
        }

        public static NanoSocket CreatePairSocket(int bufferPoolSize = DefaultBufferPoolSize,
            int bufferSize = DefaultBufferSize)
        {
            return new NanoSocket(Domain.Sp, Protocol.Pair, bufferPoolSize, bufferSize);
        }

        public static NanoSocket CreatePublishSocket(int bufferPoolSize = DefaultBufferPoolSize,
            int bufferSize = DefaultBufferSize)
        {
            return new NanoSocket(Domain.Sp, Protocol.Publish, bufferPoolSize, bufferSize);
        }

        public static NanoSocket CreatePullSocket(int bufferPoolSize = DefaultBufferPoolSize,
            int bufferSize = DefaultBufferSize)
        {
            return new NanoSocket(Domain.Sp, Protocol.Pull, bufferPoolSize, bufferSize);
        }

        public static NanoSocket CreatePushSocket(int bufferPoolSize = DefaultBufferPoolSize,
            int bufferSize = DefaultBufferSize)
        {
            return new NanoSocket(Domain.Sp, Protocol.Push, bufferPoolSize, bufferSize);
        }

        public static NanoSocket CreateReplySocket(int bufferPoolSize = DefaultBufferPoolSize,
            int bufferSize = DefaultBufferSize)
        {
            return new NanoSocket(Domain.Sp, Protocol.Reply, bufferPoolSize, bufferSize);
        }

        public static NanoSocket CreateRequestSocket(int bufferPoolSize = DefaultBufferPoolSize,
            int bufferSize = DefaultBufferSize)
        {
            return new NanoSocket(Domain.Sp, Protocol.Request, bufferPoolSize, bufferSize);
        }

        public static NanoSocket CreateRespondentSocket(int bufferPoolSize = DefaultBufferPoolSize,
            int bufferSize = DefaultBufferSize)
        {
            return new NanoSocket(Domain.Sp, Protocol.Respondent, bufferPoolSize, bufferSize);
        }

        public static NanoSocket CreateSubscribeSocket(int bufferPoolSize = DefaultBufferPoolSize,
            int bufferSize = DefaultBufferSize)
        {
            return new NanoSocket(Domain.Sp, Protocol.Subscribe, bufferPoolSize, bufferSize);
        }

        public static NanoSocket CreateSurveyorSocket(int bufferPoolSize = DefaultBufferPoolSize,
            int bufferSize = DefaultBufferSize)
        {
            return new NanoSocket(Domain.Sp, Protocol.Surveyor, bufferPoolSize, bufferSize);
        }

        private void CheckConnect()
        {
            if (!Connectable)
            {
                throw new NotSupportedException("Connect not supported");
            }
        }

        private void CheckRead()
        {
            if (!CanRead)
            {
                throw new NotSupportedException("Read not supported");
            }
        }

        private void CheckWrite()
        {
            if (!CanWrite)
            {
                throw new NotSupportedException("Write not supported");
            }
        }

        private void CheckBind()
        {
            if (!Bindable)
            {
                throw new NotSupportedException("Bind not supported");
            }
        }

        /// <summary>
        ///     Connects the socket to the remote address.  This can be called multiple times per socket.
        /// </summary>
        /// <param name="address">
        ///     The addr argument consists of two parts as follows: transport://address. The transport specifies
        ///     the underlying transport protocol to use. The meaning of the address part is specific to the underlying transport
        ///     protocol.
        /// </param>
        /// <returns>An endpoint identifier which can be used to reference the connected endpoint in the future</returns>
        /// <exception cref="NanoException">Thrown if the address is invalid</exception>
        public Endpoint Connect(string address)
        {
            CheckConnect();

            var endpoint = Library.nn_connect(_socket, address);

            if (endpoint > 0)
            {
                return new Endpoint(endpoint);
            }

            throw new NanoException("nn_connect " + address, endpoint);
        }

        /// <summary>
        ///     Connects the socket to the remote address.  This can be called multiple times per socket.
        /// </summary>
        /// <param name="address">The IP address to which this client is connecting</param>
        /// <param name="port">The port number to which this client is connecting</param>
        /// <exception cref="NanoException">Thrown if the address is invalid</exception>
        public Endpoint Connect(IPAddress address, int port)
        {
            var endpoint = Library.nn_connect(_socket, $"tcp://{address}:{port}");

            if (endpoint > 0)
            {
                return new Endpoint(endpoint);
            }

            throw new NanoException("nn_connect " + address, endpoint);
        }

        /// <summary>
        ///     Binds the socket to the local address.  This can be called multiple times per socket.
        /// </summary>
        /// <param name="address">
        ///     The addr argument consists of two parts as follows: transport://address. The transport specifies
        ///     the underlying transport protocol to use. The meaning of the address part is specific to the underlying transport
        ///     protocol.
        /// </param>
        /// <returns>An endpoint identifier which can be used to reference the bound endpoint in the future</returns>
        /// <exception cref="NanoException">Thrown if the address is invalid</exception>
        public Endpoint Bind(string address)
        {
            CheckBind();

            var endpoint = Library.nn_bind(_socket, address);

            if (endpoint > 0)
            {
                return new Endpoint(endpoint);
            }

            throw new NanoException("nn_bind " + address, endpoint);
        }

        /// <summary>
        ///     Shuts down a specific endpoint of this socket.
        /// </summary>
        /// <param name="endpoint">The endpoint created by Connect or Bind which is being shut down.</param>
        /// <returns>True if the endpoint was shut down, false if the shutdown attempt was interrupted and should be reattempted.</returns>
        /// <exception cref="NanoException">
        ///     Thrown if the socket is in an invalid state or the endpoint's shutdown attempt was
        ///     interrupted and should be reattempted.
        /// </exception>
        public bool Shutdown(Endpoint endpoint)
        {
            const int validShutdownResult = 0;
            const int maxShutdownAttemptCount = 5;
            var attemptCount = 0;

            while (true)
            {
                if (Library.nn_shutdown(_socket, endpoint.Id) == validShutdownResult)
                {
                    return true;
                }

                var error = Library.nn_errno();

                // if we were interrupted by a signal, reattempt is allowed by the native library
                if (error != Symbols.EINTR)
                {
                    throw new NanoException("nn_shutdown " + endpoint.Id, error);
                }

                if (attemptCount++ >= maxShutdownAttemptCount)
                {
                    return false;
                }

                Thread.SpinWait(1);
            }
        }

        public void Dispose(bool disposing)
        {
            const int validCloseResult = 0;
            const int maxCloseAttemptCount = 5;

            // ensure that cleanup is only ever called once
            var socket = Interlocked.Exchange(ref _socket, InvalidSocketHandle);

            if (socket == InvalidSocketHandle)
            {
                return;
            }

            var attemptCount = 0;
            while (true)
            {
                if (Library.nn_close(socket) == validCloseResult)
                {
                    break;
                }

                var error = Library.nn_errno();

                // if we were interrupted by a signal, reattempt is allowed by the native library
                if (error != Symbols.EINTR)
                {
                    Debug.Assert(error == Symbols.EBADF);

                    // currently the only non-interrupt errors are for invalid sockets, which can't be closed
                    if (disposing)
                    {
                        throw new NanoException("nn_close " + socket, error);
                    }

                    return;
                }

                if (attemptCount++ >= maxCloseAttemptCount)
                {
                    if (disposing)
                    {
                        throw new NanoException("nn_close " + socket, error);
                    }

#if DEBUG
                    // if we couldn't close the socket and we're on a finalizer thread, an exception would usually kill the process
                    var errorText =
                        $"nn_close was repeatedly interrupted for socket {socket}, which has not been successfully closed and may be leaked";
                    Trace.TraceError(errorText);
                    Debug.Fail(errorText);
#endif //DEBUG
                }

                // reattempt the close
                Thread.SpinWait(1);
            }

            BufferManager.Clear();
        }

        internal unsafe void SendMessage(nn_msghdr* messageHeader)
        {
            var sentBytes = Library.nn_sendmsg(_socket, messageHeader, (int) SendRecvFlags.None);
            if (sentBytes < 0)
            {
                throw new NanoException("nn_send", sentBytes);
            }
        }

        /// <summary>
        ///     Sends the data.  If a send buffer cannot be immediately acquired, this method returns false and no send is
        ///     performed.
        /// </summary>
        /// <param name="buffer">The data to send.</param>
        /// <param name="flags"></param>
        /// <returns>True if the data was sent, false if the data couldn't be sent at this time, and should be reattempted.</returns>
        /// <exception cref="NanoException">
        ///     Thrown if the socket is in an invalid state, the send was interrupted, or the send
        ///     timeout has expired
        /// </exception>
        public bool Send(byte[] buffer, SendRecvFlags flags = SendRecvFlags.None)
        {
            CheckWrite();

            var sentBytes = Library.nn_send(_socket, buffer, buffer.Length, (int) flags);
            if (sentBytes >= 0)
            {
                Debug.Assert(sentBytes == buffer.Length);
                return true;
            }

            var error = Library.nn_errno();
            if (error == Symbols.EAGAIN)
            {
                return false;
            }

            throw new NanoException("nn_send", error);
        }

        public WriteStream CreateSendStream()
        {
            return new WriteStream(this);
        }

        const int MaxIoRetries = 10;

        private static unsafe (ByteBuffer Next, int Count, int ByteCount) FillIoVecs(WriteStream stream, 
            ByteBuffer next, nn_iovec* iovecs, int iovecCount)
        {
            if (next.Data == null)
            {
                return default;
            }

            var count = 0;
            var byteCount = 0;
            var current = next;
            var vecPtr = iovecs;
            var iovecEnd = iovecs + sizeof(nn_iovec) * iovecCount;
            while (current.Data != null &&
                vecPtr < iovecEnd)
            {
                vecPtr->iov_len = current.Length;
                vecPtr->iov_base = current.Data;
                current = stream.NextPage(current);
                vecPtr++;
                count += 1;
                byteCount += current.Length;
            }

            return (current, count, byteCount);
        }

        public int Send(WriteStream stream, SendRecvFlags flags = SendRecvFlags.None)
        {
            CheckWrite();

            unsafe
            {
                const int maxVectors = 20;

                nn_iovec* iovec = stackalloc nn_iovec[maxVectors];
                nn_msghdr* hdr = stackalloc nn_msghdr[1];

                var bytesSent = 0;
                var chunk = stream.FirstPage();

                while (true)
                {
                    var result = FillIoVecs(stream, chunk, iovec, maxVectors);
                    if (result.Count == 0)
                    {
                        break;
                    }

                    hdr->msg_control = null;
                    hdr->msg_controllen = 0;
                    hdr->msg_iov = iovec;
                    hdr->msg_iovlen = result.Count;

                    var retryCount = 0;
                    int byteCount;
                    do
                    {
                        byteCount = Library.nn_sendmsg(SocketId, hdr, (int) flags);
                        var error = Library.nn_errno();
                        if (error == Symbols.EAGAIN)
                        {
                            Thread.SpinWait(1);
                            continue;
                        }

                        if (byteCount <= 0)
                        {
                            return byteCount;
                        }

                        break;
                    } while (retryCount++ < MaxIoRetries);

                    bytesSent += byteCount;
                }

                return bytesSent;
            }
        }

        private static readonly byte[] _emptyBytes = new byte[0];

        /// <summary>
        ///     Blocks until a message is received, and then returns a buffer containing its contents.  Note that this intermediate
        ///     byte array can be avoided using the ReceiveStream method.
        /// </summary>
        /// <returns>A buffer containing the received message.</returns>
        /// <exception cref="NanoException">
        ///     Thrown if the socket is in an invalid state, the receive was interrupted, or the
        ///     receive timeout has expired
        /// </exception>
        public byte[] Receive(SendRecvFlags flags = SendRecvFlags.None)
        {
            CheckRead();

            unsafe
            {
                void* buffer = null;

                int receiveCount;
                var retryCount = 0;
                do
                {
                    receiveCount = Library.nn_recv(_socket, ref buffer, Constants.NN_MSG, (int) flags);
                    var error = Library.nn_errno();
                    if (error == Symbols.EAGAIN)
                    {
                        if (flags == SendRecvFlags.DontWait)
                        {
                            return null;
                        }

                        Thread.SpinWait(1);
                        continue;
                    }

                    if (receiveCount == 0)
                    {
                        return _emptyBytes;
                    }

                    if (receiveCount < 0 ||
                        buffer == null)
                    {
                        throw new NanoException("nn_recv", receiveCount);
                    }
                } while (retryCount++ < MaxIoRetries);

                var output = new byte[receiveCount];
                try
                {
                    fixed (byte* bits = output)
                    {
                        Unsafe.CopyBlockUnaligned(bits, buffer, (uint)receiveCount);
                    }
                }
                finally
                {
                    receiveCount = Library.nn_freemsg(buffer);
                    if (receiveCount != 0)
                    {
                        throw new NanoException("nn_freemsg");
                    }
                }
                return output;
            }
        }

        /// <summary>
        ///     Blocks until a message is received, and then returns a stream containing its contents.
        /// </summary>
        /// <returns>
        ///     The stream containing the message data.  This stream should be disposed in order to free the message
        ///     resources.
        /// </returns>
        /// <exception cref="NanoException">
        ///     Thrown if the socket is in an invalid state, the receive was interrupted, or the
        ///     receive timeout has expired
        /// </exception>
        public ReadStream ReceiveStream()
        {
            CheckRead();

            var stream = InternalReceiveStream(SendRecvFlags.None);
            if (stream == null)
            {
                throw new NanoException("nn_recv");
            }

            return stream;
        }

        private ReadStream InternalReceiveStream(SendRecvFlags flags)
        {
            unsafe
            {
                void* buffer = null;

                int receiveCount;
                var retryCount = 0;

                do
                {
                    receiveCount = Library.nn_recv(_socket, ref buffer, Constants.NN_MSG, (int) flags);
                    var error = Library.nn_errno();
                    if (error == Symbols.EAGAIN)
                    {
                        Thread.SpinWait(1);
                        continue;
                    }
                    if (receiveCount < 0 ||
                        buffer == null)
                    {
                        return null;
                    }

                    break;
                } while (retryCount++ < MaxIoRetries);

                /*
                 * In order to prevent managed allocations per receive, we attempt to recycle stream objects.  This
                 * will work optimally if the stream is disposed before the next receive call, as in this case each
                 * socket class will always reuse the same stream.
                 * 
                 * Disposing the stream will both release its nanomsg-allocated native buffer and return it to its
                 * socket class for reuse.  
                 */

                var stream = Interlocked.Exchange(ref _recycledReadStream, null);

                if (stream != null)
                {
                    stream.Reinitialize(buffer, receiveCount);
                }
                else
                {
                    stream = new ReadStream(new ByteBuffer((byte*) buffer, receiveCount),
                        arg =>
                        {
                            Library.nn_freemsg(arg.Buffer.Data);
                            RecycleStream(arg.Stream);
                        });
                }

                return stream;
            }
        }

        private void RecycleStream(ReadStream messageStream)
        {
            _recycledReadStream = messageStream;
        }

        ~NanoSocket()
        {
            Dispose(false);
        }
    }
}