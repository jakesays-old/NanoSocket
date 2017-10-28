namespace Std.NanoMsg.Internal
{
    public unsafe struct ByteBuffer
    {
        public readonly byte* Data;
        public readonly int Length;

        public ByteBuffer(byte* data, int length)
        {
            Data = data;
            Length = length;
        }

        public bool NotNull
        {
            get => Data != null;
        }
    }
}