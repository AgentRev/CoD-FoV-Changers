using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace MultiCoD_FoV_Changer
{
#if WIN64
    using dword_ptr = UInt64;
#else
    using dword_ptr = UInt32;
#endif

    public static class Memory
    {
        #region DLLImports

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWow64Process(IntPtr hProc, out bool bIsX86);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadProcessMemory(IntPtr hProc, dword_ptr lpBaseAddress, [Out] byte[] lpBuffer, int nSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadProcessMemory(IntPtr hProc, dword_ptr lpBaseAddress, out dword_ptr dwBuffer, int nSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WriteProcessMemory(IntPtr hProc, dword_ptr lpBaseAddress, [In] byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);

        #endregion

        #region Variables

        static dword_ptr baseAddr;
        static IntPtr hProc;
        static int dvarPtrSize;
        static dword_ptr dvarValueOffset;

        public static Dictionary<string, dword_ptr> dvarAddresses = new Dictionary<string, dword_ptr>();

        #endregion

        #region Methods

        public static void Init(int pid, dword_ptr baseAddr)
        {
            Memory.baseAddr = baseAddr;

            if (hProc != IntPtr.Zero)
            {
                CloseHandle(hProc);
            }

            try
            {
                const uint dwDesiredAccess = 0x0008 | 0x0010 | 0x0020; // PROCESS_VM_OPERATION | PROCESS_VM_READ | PROCESS_VM_WRITE
                hProc = OpenProcess(dwDesiredAccess, false, pid);
            }
            catch (Exception e)
            {
                throw new Exception($"OpenProcess failed.\n{e.Message}", e);
            }

            if (hProc == IntPtr.Zero)
            {
                throw new Exception("OpenProcess failed.");
            }

            try
            {
                IsWow64Process(hProc, out bool isX86);
                dvarPtrSize = isX86 ? sizeof(Int32) : sizeof(Int64);
            }
            catch (Exception e)
            {
                throw new Exception($"IsWow64Process failed.\n{e.Message}", e);
            }
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

        private static int ReadBytes(dword_ptr addr, int length, out byte[] buffer)
        {
            buffer = new byte[length];

            try
            {
                ReadProcessMemory(hProc, addr, buffer, length, out int lpNumberOfBytesRead);
                return lpNumberOfBytesRead;
            }
            catch (Exception e)
            {
                throw new Exception($"ReadProcessMemory failed for address: 0x{addr:X}\n{e.Message}", e);
            }
        }

        private static int WriteBytes(dword_ptr addr, byte[] bytes)
        {
            try
            {
                WriteProcessMemory(hProc, addr, bytes, bytes.Length, out int lpNumberOfBytesWritten);
                return lpNumberOfBytesWritten;
            }
            catch (Exception e)
            {
                throw new Exception($"WRiteProcessMemory failed for address: 0x{addr:X}\n{e.Message}", e);
            }
        }
        public static bool FindDvarAddresses(string[] requiredNames, string[] optionalNames = null)
        {
            if (hProc == IntPtr.Zero) return false;

            // Combine required and optional requiredNames into a single list to search
            string[] allNames = (optionalNames != null) ? requiredNames.Concat(optionalNames).ToArray() : requiredNames;

            // Track if cg_fov needs to be added as a required bootstrap dvar
            bool addedCgFov = dvarValueOffset == 0 && !allNames.Contains("cg_fov");
            string[] dvarNames = addedCgFov ? allNames.Concat(new[] { "cg_fov" }).ToArray() : allNames;

            int dvarCount = dvarNames.Length;
            var nameAddressesArr = new dword_ptr[dvarCount];
            var nameIndexesLeft = Enumerable.Range(0, dvarCount).ToArray();

            ReadBytes(baseAddr, Constants.c_memReadRange, out byte[] buffer);

            int iBufMax = buffer.Length - dvarNames.Select(w => w?.Length ?? 0).DefaultIfEmpty(0).Max() - 1;
            int dvarsLeft = dvarCount;

            // Search game memory to find addresses of dvar name strings
            for (int iBuf = 0; iBuf <= iBufMax && dvarsLeft > 0; iBuf += sizeof(int))
            {
                for (int iNameIdx = dvarsLeft - 1; iNameIdx >= 0; iNameIdx--)
                {
                    int iDvar = nameIndexesLeft[iNameIdx];
                    ref var name = ref dvarNames[iDvar];

                    // Equivalent to "if (strcmp(name, &buffer[iBuf]) == 0)"

                    bool match = true;
                    for (int iName = 0; iName < name.Length; iName++)
                    {
                        if (buffer[iBuf + iName] != name[iName])
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match && buffer[iBuf + name.Length] == '\0')
                    {
                        nameAddressesArr[iDvar] = baseAddr + (dword_ptr)iBuf;
                        nameIndexesLeft[iNameIdx] = nameIndexesLeft[--dvarsLeft];
                    }
                }
            }

            // Abort if any address is invalid
            if (nameAddressesArr.Any(addr => addr < baseAddr))
                return false;

            dvarsLeft = dvarCount;
            nameIndexesLeft = Enumerable.Range(0, dvarCount).ToArray();

            iBufMax = buffer.Length - dvarPtrSize - 1;
            bool isX86 = dvarPtrSize == sizeof(int);

            // Search game memory to find the dvar structs via their name pointers
            for (int iBuf = (int)(nameAddressesArr.Max() - baseAddr); iBuf <= iBufMax && dvarsLeft > 0; iBuf += sizeof(int))
            {
                dword_ptr ptrVal = isX86 ? BitConverter.ToUInt32(buffer, iBuf) : BitConverter.ToUInt64(buffer, iBuf);

                for (int n = dvarsLeft - 1; n >= 0; n--)
                {
                    int iDvar = nameIndexesLeft[n];
                    if (ptrVal == nameAddressesArr[iDvar])
                    {
                        dvarAddresses[dvarNames[iDvar]] = baseAddr + (dword_ptr)iBuf + dvarValueOffset;
                        nameIndexesLeft[n] = nameIndexesLeft[--dvarsLeft];
                    }
                }
            }

            // Figure out the value offset via struct dvar_t's "DvarValue reset" from cg_fov
            if (dvarValueOffset == 0 && dvarAddresses.TryGetValue("cg_fov", out var cgFovStructAddr) && cgFovStructAddr > baseAddr)
            {
                int iDvarStruct = (int)(cgFovStructAddr - baseAddr);

                // "DvarValue reset" should normally always be within this range for IW3/4/5/6 and T4/5/6 engines, hopefully S1 as well
                for (int offset = dvarPtrSize + 0x34; offset >= 0x28; offset -= sizeof(int))
                {
                    if (BitConverter.ToSingle(buffer, iDvarStruct + offset) == 65f)
                    {
                        dvarValueOffset = (dword_ptr)(offset - 0x20); // infer "DvarValue current" offset based on "DvarValue reset", their gap is always 0x20 for these engines
                        break;
                    }
                }

                if (dvarValueOffset > 0)
                {
                    // Re-adjust all offsets
                    foreach (var key in dvarAddresses.Keys.ToList())
                    {
                        dvarAddresses[key] += dvarValueOffset;
                    }
                }
            }

            // Ensure all required requiredNames (and cg_fov if added) were found. Optional requiredNames missing won't fail this.
            bool success = dvarValueOffset > 0 && requiredNames.All(n => dvarAddresses.ContainsKey(n)) && (!addedCgFov || dvarAddresses.ContainsKey("cg_fov"));
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
