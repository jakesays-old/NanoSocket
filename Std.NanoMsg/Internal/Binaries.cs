using System.Globalization;
using System.Resources;

namespace Std.NanoMsg.Internal
{
    internal class Binaries
    {
        private static ResourceManager _resourceMan;

        internal static ResourceManager ResourceManager
        {
            get
            {
                if (ReferenceEquals(_resourceMan, null))
                {
#if DEBUG
                    var temp = new ResourceManager("Std.Network.Native.Binaries.DebugBinaries",
                        typeof(Binaries).Assembly);
#else
                    ResourceManager temp =
                        new ResourceManager("Std.Network.Native.Binaries.ReleaseBinaries", typeof(Binaries).Assembly);
#endif
                    _resourceMan = temp;
                }
                return _resourceMan;
            }
        }

        internal static CultureInfo Culture { get; set; }
#if DEBUG
        /// <summary>
        ///     Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] X32Dll
        {
            get
            {
                var obj = ResourceManager.GetObject("x32_debug_dll", Culture);
                return (byte[]) obj;
            }
        }

        /// <summary>
        ///     Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] X32Pdb
        {
            get
            {
                var obj = ResourceManager.GetObject("x32_debug_pdb", Culture);
                return (byte[]) obj;
            }
        }
#else
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] X32Dll {
            get {
                object obj = ResourceManager.GetObject("x32_release_dll", Culture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] X32Pdb {
            get {
                object obj = ResourceManager.GetObject("x32_release_pdb", Culture);
                return ((byte[])(obj));
            }
        }
#endif
#if DEBUG
        /// <summary>
        ///     Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] X64Dll
        {
            get
            {
                var obj = ResourceManager.GetObject("x64_debug_dll", Culture);
                return (byte[]) obj;
            }
        }

        /// <summary>
        ///     Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] X64Pdb
        {
            get
            {
                var obj = ResourceManager.GetObject("x64_debug_pdb", Culture);
                return (byte[]) obj;
            }
        }
#else
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] X64Dll {
            get {
                object obj = ResourceManager.GetObject("x64_release_dll", Culture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] X64Pdb {
            get {
                object obj = ResourceManager.GetObject("x64_release_pdb", Culture);
                return ((byte[])(obj));
            }
        }
#endif
    }
}