//#define DEBUGMESSAGES

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MultiCoD_FoV_Changer
{
    using dword_ptr = UInt64;

    public static class Memory
    {
        #region Variables

        public static dword_ptr baseAddr;
        static IntPtr hProc;
        static int dvarPtrSize;
        static dword_ptr dvarValueOffset;
        static bool isGame64;
        static bool readFailureShown = false;

        public static Dictionary<string, dword_ptr> dvarAddresses = new Dictionary<string, dword_ptr>();

        #endregion

        #region Methods

        public static void Init(Process proc)
        {
            if (hProc != IntPtr.Zero)
            {
                PInvokeAPI.CloseHandle(hProc);
            }

            const uint dwDesiredAccess = 0x0008 | 0x0010 | 0x0020 | 0x0400; // PROCESS_VM_OPERATION | PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_QUERY_INFORMATION
            hProc = PInvokeAPI.OpenProcess(dwDesiredAccess, false, proc.Id);

            if (hProc == IntPtr.Zero)
            {
                if (!proc.HasExited)
                {
                    var w32e = new Win32Exception(Marshal.GetLastWin32Error());
                    throw new Exception($"OpenProcess failed.\n{w32e}");
                }

                Reset();
                return;
            }

            try
            {
                baseAddr = PInvokeAPI.GetBaseAddress(hProc); // (dword_ptr)proc.MainModule.BaseAddress
            }
            catch
            {
                if (!proc.HasExited)
                    throw;

                Reset();
                return;
            }

            isGame64 = PInvokeAPI.IsTargetProcess64Bit(hProc);
            dvarPtrSize = isGame64 ? sizeof(long) : sizeof(int);
        }

        public static void Reset()
        {
            if (hProc != IntPtr.Zero)
            {
                PInvokeAPI.CloseHandle(hProc);
            }

            hProc = IntPtr.Zero;
            dvarValueOffset = 0;
            dvarAddresses.Clear();
        }
        private static int ReadBytes(dword_ptr addr, ref byte[] buffer)
        {
            return PInvokeAPI.ReadMemory(hProc, addr, buffer, buffer.Length);
        }

        private static int ReadBytes(dword_ptr addr, int length, out byte[] buffer)
        {
            buffer = new byte[length];
            return PInvokeAPI.ReadMemory(hProc, addr, buffer, length);
        }

        private static int WriteBytes(dword_ptr addr, byte[] buffer)
        {
            return PInvokeAPI.WriteMemory(hProc, addr, buffer, buffer.Length);
        }

        public static bool FindDvarAddresses(string[] requiredNames, string[] optionalNames = null)
        {
            if (hProc == IntPtr.Zero)
            {
#if DEBUGMESSAGES
                MessageBox.Show("FindDvarAddresses failed: Process handle is null.", "Debug - Init");
#endif
                return false;
            }

            dvarAddresses.Clear();

            // Combine requiredNames and optionalNames into a single array to search
            string[] dvarNames = (optionalNames != null) ? requiredNames.Concat(optionalNames).ToArray() : requiredNames;

            // Track the index boundary for required names
            int requiredCount = requiredNames.Length;

            // Track if cg_fov needs to be added to resolve value offset
            bool cgFovNeeded = dvarValueOffset == 0 && !dvarNames.Contains("cg_fov");
            dvarNames = cgFovNeeded ? dvarNames.Concat(new[] { "cg_fov" }).ToArray() : dvarNames;

            int dvarCount = dvarNames.Length;

            if (dvarCount == 0)
            {
#if DEBUGMESSAGES
                MessageBox.Show("FindDvarAddresses failed: No Dvar names provided.", "Debug - Init");
#endif
                return false;
            }

#if DEBUGMESSAGES
            MessageBox.Show(
                $"Starting FindDvarAddresses:\n" +
                $"Targeting {dvarCount} total Dvars ({requiredCount} required).\n" +
                $"Dvars: {string.Join(", ", dvarNames)}\n" +
                $"Current dvarValueOffset: 0x{dvarValueOffset:X}\n" +
                $"Game x64: {isGame64}",
                "Debug - Step 1: Initial Setup"
            );
#endif

            int[] pendingStringIndexes = Enumerable.Range(0, dvarCount).ToArray();
            int pendingStringCount = dvarCount;

            int[] pendingStructIndexes = new int[dvarCount];
            int pendingStructCount = 0;

            var nameAddressesArr = new dword_ptr[dvarCount];
            dword_ptr nameAddressesMax = baseAddr;

            const int chunkSize = 0x1000000; // 16 MB
            const int chunkMargin = 0x100; // ensures dvar names and structs arent cut in half
            const int stepSize = chunkSize - chunkMargin;
            var buffer = new byte[chunkSize];
            int totalBytesRead = 0;

            // Search game memory to find addresses of dvar name strings

            dword_ptr maxAddr = baseAddr + 0x2000000; // limits name scan range to 32 MB
            int bytesRead, iBuf, iBufMax, iPending, iName;
            bool match;

            for (dword_ptr currAddr = baseAddr; currAddr < maxAddr && pendingStringCount > 0; currAddr += stepSize)
            {
                bytesRead = ReadBytes(currAddr, ref buffer);
                totalBytesRead += bytesRead;

                iBufMax = Math.Min(buffer.Length, bytesRead) - chunkMargin;

                for (iBuf = 0; iBuf < iBufMax && pendingStringCount > 0; iBuf++)
                {
                    for (iPending = pendingStringCount - 1; iPending >= 0; iPending--)
                    {
                        iName = pendingStringIndexes[iPending];
                        ref string name = ref dvarNames[iName];

                        match = true;
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
                            nameAddressesArr[iName] = currAddr + (dword_ptr)iBuf;
                            pendingStringIndexes[iPending] = pendingStringIndexes[--pendingStringCount];
                            pendingStructIndexes[pendingStructCount++] = iName;
                        }
                    }
                }
            }

            if (totalBytesRead == 0 && !readFailureShown)
            {
                MessageBox.Show("Failed to read process memory.", "FoV Changer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                readFailureShown = true;
                return false;
            }

#if DEBUGMESSAGES
            // Display string search results
            string stringResults = string.Join("\n", dvarNames.Select((n, idx) =>
                $"{n}: {(nameAddressesArr[idx] >= baseAddr ? $"0x{nameAddressesArr[idx]:X}" : "NOT FOUND")}"));

            MessageBox.Show($"String search complete:\nTotal bytes read: 0x{totalBytesRead:X}\n\nResults:\n{stringResults}", "Debug - Step 3: String Addresses Found");
#endif

            // Fail if any required name was not found
            for (iName = 0; iName < requiredCount; iName++)
            {
                if (nameAddressesArr[iName] < baseAddr)
                {
#if DEBUGMESSAGES
                    MessageBox.Show($"Failed: Required string '{dvarNames[iName]}' was not found in memory.", "Debug - Step 3 Error");
#endif
                    return false;
                }
            }

            // Search game memory to find the dvar structs via their name pointers
            unsafe
            {
                fixed (byte* pBuffer = buffer)
                {
                    dword_ptr ptrVal, structAddr;
                    maxAddr = baseAddr + 0x10000000; // first 256MB RAM
                    totalBytesRead = 0;

                    for (dword_ptr currAddr = nameAddressesArr.Max(); currAddr < maxAddr && pendingStructCount > 0; currAddr += stepSize)
                    {
                        bytesRead = ReadBytes(currAddr, ref buffer);
                        totalBytesRead += bytesRead;

                        iBufMax = Math.Min(buffer.Length, bytesRead) - chunkMargin;

                        for (iBuf = 0; iBuf < iBufMax && pendingStructCount > 0; iBuf += sizeof(int))
                        {
                            ptrVal = isGame64 ? *(ulong*)(pBuffer + iBuf) : *(uint*)(pBuffer + iBuf);
                            structAddr = currAddr + (dword_ptr)iBuf;

                            for (iPending = pendingStructCount - 1; iPending >= 0; iPending--)
                            {
                                iName = pendingStructIndexes[iPending];

                                if (ptrVal == nameAddressesArr[iName])
                                {
                                    dvarAddresses[dvarNames[iName]] = structAddr;
                                    pendingStructIndexes[iPending] = pendingStructIndexes[--pendingStructCount];
                                }
                            }
                        }
                    }
                }
            }

#if DEBUGMESSAGES
            string structResults = string.Join("\n", dvarAddresses.Select(kvp => $"{kvp.Key}: 0x{kvp.Value:X}"));
            MessageBox.Show($"Struct pointer search complete.\nTotal bytes read: 0x{totalBytesRead:X}\n\nResults:\n{structResults}", "Debug - Step 4: Struct Addresses Found");
#endif
            // Fail if any required name was not found
            foreach (var name in requiredNames)
            {
                if (!dvarAddresses.ContainsKey(name) || dvarAddresses[name] < baseAddr)
                {
#if DEBUGMESSAGES
                    MessageBox.Show($"Failed: Required dvar '{name}' was not found in memory.", "Debug - Step 4 Error");
#endif
                    return false;
                }
            }

            // Figure out the value offset via struct dvar_t's "DvarValue reset" from cg_fov

            if (dvarAddresses.TryGetValue("cg_fov", out var cgFovStructAddr))
            {
                int readLen = dvarPtrSize + 0x38;
                if (ReadBytes(cgFovStructAddr, readLen, out byte[] fovBuffer) >= readLen)
                {
                    for (int offset = dvarPtrSize + 0x34; offset >= 0x28; offset -= sizeof(int))
                    {
                        if (BitConverter.ToSingle(fovBuffer, offset) == 65f)
                        {
                            dvarValueOffset = (dword_ptr)(offset - 0x20);
#if DEBUGMESSAGES
                            MessageBox.Show($"Found default FOV value (65.0f) at offset 0x{offset:X}.\nInferred dvarValueOffset = 0x{dvarValueOffset:X}", "Debug - Step 5: Success");
#endif
                            break;
                        }
                    }
                }
            }

            if (dvarValueOffset == 0)
            {
#if DEBUGMESSAGES
                MessageBox.Show("Failed: Could not determine dvarValueOffset from 'cg_fov'.", "Debug - Step 5 Error");
#endif
                return false;
            }

            // Re-adjust all offsets
            foreach (var key in dvarAddresses.Keys.ToList())
            {
                dvarAddresses[key] += dvarValueOffset;
            }

#if DEBUGMESSAGES
            string finalAdjustedResults = string.Join("\n", dvarAddresses.Select(kvp => $"{kvp.Key}: 0x{kvp.Value:X}"));
            MessageBox.Show($"Adjusted addresses with offset 0x{dvarValueOffset:X}:\n\n{finalAdjustedResults}", "Debug - Step 5: Adjusted Struct Addresses");
#endif
            return true;
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
