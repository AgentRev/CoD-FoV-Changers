using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace MemoryMod
{
    class MemoryMod
    {
        const int READ = 0x10; // PROCESS_VM_READ
        const int WRITE = 0x28; // PROCESS_VM_OPERATION | PROCESS_VM_WRITE
        // Above values are explained at http://msdn.microsoft.com/library/windows/desktop/ms684880.aspx

        uint pFoV;

        byte[] cVar = Encoding.ASCII.GetBytes("cg_fov\0"); // C-style (null-terminated) string; in this example, I will show you how to locate that string


        // Here are the DLLImports

        [DllImport("kernel32.dll", EntryPoint = "OpenProcess")] // kernel32.dll contains Win32 API functions; it is roughly equivalent to "windows.h" in C / C++
        public static extern uint OpenProcess(uint accessRights, bool inheritHandle, uint procID);

        [DllImport("kernel32.dll", EntryPoint = "CloseHandle")]
        public static extern bool CloseHandle(uint procHandle);

        [DllImport("kernel32.dll", EntryPoint = "ReadProcessMemory")]
        public static extern bool ReadProcessMemory(uint procHandle, uint address, byte[] buffer, uint size, ref uint bytesRead);

        [DllImport("kernel32.dll", EntryPoint = "WriteProcessMemory")]
        public static extern bool WriteProcessMemory(uint procHandle, uint address, byte[] buffer, uint size);

        [DllImport("msvcrt.dll", EntryPoint = "memcmp")] // msvcrt.dll contains contains common C and C++ libraries; "memcmp" is located in the "string.h" C library
        public unsafe static extern int memcmp(byte* haystack, byte[] needle, int length); // All DLLImported functions with array parameters can receive them as an array or a pointer;
                                                                                           // To use pointers, the function must be declared "unsafe"

        public unsafe bool SearchMemory() // Any function that deals with pointers in any shape or form must be declared "unsafe"
        {
            bool isFound = false;

            Process[] procs = Process.GetProcessesByName("iw5mp"); // ".exe" must be omitted

            if (procs.Length != 0)
            {
                uint procID = (uint)procs[0].Id;
                uint procHandle = OpenProcess(READ, false, procID);

                if (procHandle != 0)
                {
                    uint bytesRead = 0;

                    uint start = 0x00400000; // For applications compiled with Visual Studio, as in the case of MW3, the main module always begins at 0x400000
                    uint end = 0x01000000;

                    uint buffSize = 0x1000; // (4096 bytes) This buffer size is optimal, since when combined with the bytesRead value, 
                                            // it permits skipping over empty memory areas, which are always 4096 bytes at minimum

                    byte[] buffer = new byte[buffSize];

                    for (uint i = start; i < end; i += buffSize)
                    {
                        ReadProcessMemory(procHandle, i, buffer, i, ref bytesRead); // The "ref" is because the method writes the value directly to the variable;
                                                                                    // It is not needed for the buffer, since C# arrays are references themselves

                        if (bytesRead == buffSize) // We check if the buffer has been fully filled; if it isn't, it means that this area of the memory contains no data
				        {
                            fixed (byte* pBuffer = buffer) // We declare the pointer, which can only be used within this scope
                            {
                                for (uint j = 0; j < buffSize; i += 4) // The "4" is because values are always stored in the memory at an offset 
                                                                       // with the last digit being a multiple of 4 (32-bit offsets)
                                {
                                    if (memcmp(pBuffer + i, cVar, cVar.Length) == 0) // memcmp returns 0 if memory values are equal, it's not a mistake;
                                                                                     // The "+i" is the whole reason behind the use of a pointer here,
                                                                                     // it allows direct offsets without having to manipulate the array
                                    {
                                        pFoV = i + j;
                                        isFound = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    CloseHandle(procHandle);
                }
            }

            return isFound;
        }


        public void WriteFloat(uint ptr, float val)
        {
            if (ptr != 0)
            {
                Process[] procs = Process.GetProcessesByName("iw5mp");

                if (procs.Length != 0)
                {
                    uint procID = (uint)procs[0].Id;
                    uint procHandle = OpenProcess(WRITE, false, procID);

                    if (procHandle != 0)
                    {
                        WriteProcessMemory(procHandle, ptr, BitConverter.GetBytes(val), sizeof(float));
                        CloseHandle(procHandle);
                    }
                }
            }
        }
    }
}
