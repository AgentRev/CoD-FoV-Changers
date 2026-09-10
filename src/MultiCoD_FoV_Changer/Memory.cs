using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace MultiCoD_FoV_Changer
{
    using dword_ptr = UInt64;
    public static class Memory
    {
        #region DLLImports

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        #endregion

        #region Variables

        static dword_ptr baseAddr;
        static IntPtr hProc;
        static int dvarPtrSize;
        static dword_ptr dvarValueOffset;
        static bool isGameX64;

        public static Dictionary<string, dword_ptr> dvarAddresses = new Dictionary<string, dword_ptr>();

        #endregion

        #region Methods

        public static void Init(Process proc)
        {
            if (hProc != IntPtr.Zero)
            {
                CloseHandle(hProc);
            }

            try
            {
                const uint dwDesiredAccess = 0x0008 | 0x0010 | 0x0020 | 0x0400; // PROCESS_VM_OPERATION | PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_QUERY_INFORMATION
                hProc = OpenProcess(dwDesiredAccess, false, proc.Id);
            }
            catch (Exception e)
            {
                throw new Exception($"OpenProcess failed.\n{e.Message}", e);
            }

            if (hProc == IntPtr.Zero)
            {
                throw new Exception("OpenProcess failed.");
            }

#if !DEBUG
            try
            {
#endif
                baseAddr = MemoryAPI.GetBaseAddress(proc, hProc);
                isGameX64 = MemoryAPI.IsTarget64Bit(hProc);
                dvarPtrSize = isGameX64 ? sizeof(Int64) : sizeof(Int32);
#if !DEBUG
            }
            catch (Exception e)
            {
                throw new Exception($"IsWow64Process failed.\n{e.Message}", e);
            }
#endif
        }

        public static void Reset()
        {
            if (hProc != IntPtr.Zero)
            {
                CloseHandle(hProc);
            }

            hProc = IntPtr.Zero;
            dvarValueOffset = 0;
            dvarAddresses.Clear();
        }

        private static bool ReadBytes(dword_ptr addr, ulong length, out byte[] buffer)
        {
            buffer = new byte[length];

            try
            {
                return MemoryAPI.ReadMemory(hProc, addr, buffer);
            }
            catch (Exception e)
            {
                throw new Exception($"ReadProcessMemory failed for address: 0x{addr:X}\n{e.Message}", e);
            }
        }

        private static bool WriteBytes(dword_ptr addr, byte[] buffer)
        {
            try
            {
                return MemoryAPI.WriteMemory(hProc, addr, buffer);
            }
            catch (Exception e)
            {
                throw new Exception($"WRiteProcessMemory failed for address: 0x{addr:X}\n{e.Message}", e);
            }
        }

        public static bool FindDvarAddresses(string[] requiredNames, string[] optionalNames = null)
        {
            if (hProc == IntPtr.Zero) return false;

            // Combine requiredNames and optionalNames into a single array to search
            string[] dvarNames = (optionalNames != null) ? requiredNames.Concat(optionalNames).ToArray() : requiredNames;

            // Track if cg_fov needs to be added to resolve value offset
            bool cgFovNeeded = dvarValueOffset == 0 && !dvarNames.Contains("cg_fov");
            dvarNames = cgFovNeeded ? dvarNames.Concat(new[] { "cg_fov" }).ToArray() : dvarNames;

            int dvarCount = dvarNames.Length;

            if (dvarCount == 0)
                return false;

            var nameAddressesArr = new dword_ptr[dvarCount];
            var pendingNameIndexes = Enumerable.Range(0, dvarCount).ToArray();

            ReadBytes(baseAddr, Constants.c_memReadRange, out byte[] buffer);

            int iBufMax = 0x2000000; // first 32MB
            int pendingCount = dvarCount;

            // Search game memory to find addresses of dvar name strings
            for (int iBuf = 0; iBuf <= iBufMax && pendingCount > 0; iBuf += sizeof(int))
            {
                for (int iPending = pendingCount - 1; iPending >= 0; iPending--)
                {
                    int iName = pendingNameIndexes[iPending];
                    ref var name = ref dvarNames[iName];

                    // Equivalent to "if (strcmp(name, &buffer[iBuf]) == 0)"

                    bool match = true;
                    for (int iChar = 0; iChar < name.Length; iChar++)
                    {
                        if (buffer[iBuf + iChar] != name[iChar])
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match && buffer[iBuf + name.Length] == '\0')
                    {
                        nameAddressesArr[iName] = baseAddr + (dword_ptr)iBuf;
                        pendingNameIndexes[iPending] = pendingNameIndexes[--pendingCount]; // since iPending found, overwrite iPending element with last element, decrementing element count
                    }
                }
            }

            // Abort if any address is invalid
            if (nameAddressesArr.Any(addr => addr < baseAddr))
                return false;

            pendingCount = dvarCount;
            pendingNameIndexes = Enumerable.Range(0, dvarCount).ToArray();

            iBufMax = buffer.Length - dvarPtrSize - 1;
            bool isGameX64 = dvarPtrSize == sizeof(Int64);

            // Search game memory to find the dvar structs via their name pointers
            unsafe
            {
                fixed (byte* pBuffer = buffer)
                {
                    for (int iBuf = (int)(nameAddressesArr.Max() - baseAddr); iBuf <= iBufMax && pendingCount > 0; iBuf += sizeof(int))
                    {
                        dword_ptr ptrVal = isGameX64 ? *(UInt64*)(pBuffer + iBuf) : *(UInt32*)(pBuffer + iBuf);

                        for (int iPending = pendingCount - 1; iPending >= 0; iPending--)
                        {
                            int iName = pendingNameIndexes[iPending];

                            if (ptrVal == nameAddressesArr[iName])
                            {
                                dvarAddresses[dvarNames[iName]] = baseAddr + (dword_ptr)iBuf + dvarValueOffset;
                                pendingNameIndexes[iPending] = pendingNameIndexes[--pendingCount];
                            }
                        }
                    }
                }
            }

            // Figure out the value offset via struct dvar_t's "DvarValue reset" from cg_fov
            if (dvarValueOffset == 0)
            {
                if (dvarAddresses.TryGetValue("cg_fov", out var cgFovStructAddr))
                {
                    int iBufCgFov = (int)(cgFovStructAddr - baseAddr);

                    // "DvarValue reset" should normally always be within this range for IW3/4/5/6 and T4/5/6 engines, hopefully S1 as well
                    for (int offset = dvarPtrSize + 0x34; offset >= 0x28; offset -= sizeof(int))
                    {
                        if (BitConverter.ToSingle(buffer, iBufCgFov + offset) == 65f)
                        {
                            dvarValueOffset = (dword_ptr)(offset - 0x20); // infer "DvarValue current" offset based on "DvarValue reset", their gap is always 0x20 for these engines
                            break;
                        }
                    }
                }

                if (dvarValueOffset == 0)
                    return false;

                // Re-adjust all offsets
                foreach (var key in dvarAddresses.Keys.ToList())
                {
                    dvarAddresses[key] += dvarValueOffset;
                }
            }

            // Ensure all requiredNames were found. optionalNames missing won't fail this.
            bool success = requiredNames.All(n => dvarAddresses.ContainsKey(n));
            return success;
        }

        // Type-specific wrappers

        public static float ReadFloat(string name)
        {
            ReadBytes(dvarAddresses[name], sizeof(float), out byte[] buffer);
            return BitConverter.ToSingle(buffer, 0);
        }

        public static void WriteFloat(string name, float val)
        {
            WriteBytes(dvarAddresses[name], BitConverter.GetBytes(val));
        }

        public static int ReadInt(string name)
        {
            ReadBytes(dvarAddresses[name], sizeof(int), out byte[] buffer);
            return BitConverter.ToInt32(buffer, 0);
        }

        public static void WriteInt(string name, int val)
        {
            WriteBytes(dvarAddresses[name], BitConverter.GetBytes(val));
        }

        public static void ResetDvar(string name)
        {
            ReadBytes(dvarAddresses[name], 0x30, out byte[] buffer);
            WriteBytes(dvarAddresses[name], BitConverter.GetBytes(BitConverter.ToInt32(buffer, 0x20))); // "DvarValue reset" is always 0x20 after the value
        }

#endregion
    }
}
