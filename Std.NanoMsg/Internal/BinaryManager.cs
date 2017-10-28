using System;
using System.IO;

namespace Std.NanoMsg.Internal
{
    public static class BinaryManager
    {
        public static unsafe void Initialize()
        {
            var dllPath = Path.Combine(ProcessHelpers.HostProcessDirectory, "nanomsg.dll");
            var pdbPath = Path.Combine(ProcessHelpers.HostProcessDirectory, "nanomsg.pdb");

            if (!File.Exists(dllPath))
            {
                File.WriteAllBytes(dllPath, sizeof(IntPtr) == 8
                    ? Binaries.X64Dll
                    : Binaries.X32Dll);
            }
            if (!File.Exists(pdbPath))
            {
                File.WriteAllBytes(pdbPath, sizeof(IntPtr) == 8
                    ? Binaries.X64Pdb
                    : Binaries.X32Pdb);
            }
        }
    }
}