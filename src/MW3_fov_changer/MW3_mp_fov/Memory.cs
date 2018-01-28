using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace MW3_fov_changer
{
    public class Memory
    {
        #region DLLImports

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProc, int lpBaseAddress, [Out] byte[] buffer, int size, [Out] int lpNumberOfBytesRead);
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProc, int lpBaseAddress, out int buffer, int size, [Out] int lpNumberOfBytesRead);
        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(IntPtr hProc, int lpBaseAddress, [In] byte[] buffer, int size, [Out] int lpNumberOfBytesWritten);
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int strcmp(byte* b1, byte[] b2);

        #endregion

        #region Constants

        const int READ = 0x10; // PROCESS_VM_READ
        const int WRITE = 0x28; // PROCESS_VM_OPERATION | PROCESS_VM_WRITE

        const int searchTextRegion = 0x600000;
        const int searchRegion = 0xC00000;
        const int searchRegionBefore = 0x400000;

        #endregion

        #region Variables

        byte pOffset;
        byte searchRange;
        int pid;
        int baseAddr;
        int varAddr;
        byte[] cVar;

        #endregion

        public Memory(string cVar, int pid, int baseAddr, byte searchRange, byte pOffset)
        {
            this.cVar = Encoding.ASCII.GetBytes(cVar + '\0');
            this.pid = pid;
            this.baseAddr = baseAddr;
            this.searchRange = searchRange;
            this.pOffset = pOffset;
        }

        #region Methods

        public bool FindFoVOffset(ref int pFoV, ref byte step)
        {
            IntPtr hProc;
            bool isFound = false;

            try { hProc = OpenProcess(READ, false, pid); step = 0; }
            catch (Exception e) { throw new Exception(String.Format("Failed to open the process handle during a FindFoVOffset statement;\n{0}", e.Message)); }

            if (hProc != IntPtr.Zero)
            {
                int start = pFoV - searchRegionBefore;

                if (varAddr == 0)
                {
                    step = 2;
                    byte[] buffer = new byte[searchTextRegion];

                    try { ReadProcessMemory(hProc, baseAddr, buffer, searchTextRegion, 0); }
                    catch (Exception e) { step = 3; throw new Exception(String.Format("Failed to read process memory during the first iteration of a FindFoVOffset statement\nWin32 Error: {1}", e.Message)); }

                    unsafe
                    {
                        fixed (byte* pBuffer = buffer)
                        {
                            for (int i = 0; i < searchTextRegion; i += sizeof(int))
                            {
                                step = 4;

                                if (strcmp(pBuffer + i, cVar) == 0)
                                {
                                    step = 5;
                                    varAddr = baseAddr + i;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (varAddr > 0)
                {
                    int testCurrentPtr;

                    try { ReadProcessMemory(hProc, pFoV - pOffset, out testCurrentPtr, sizeof(int), 0); step = 14; }
                    catch (Exception e) { step = 1; throw new Exception(String.Format("Failed to read process memory during the test FindFoVOffset statement; \nWin32 Error: {0}", e.Message)); }

                    if (testCurrentPtr == varAddr)
                    {
                        step = 15;
                        isFound = true;
                    }
                    else
                    {
                        step = 6;
                        byte[] buffer = new byte[searchRegion];

                        try { ReadProcessMemory(hProc, start, buffer, searchRegion, 0); step = 7; }
                        catch (Exception e) { step = 8; throw new Exception(String.Format("Failed to read process memory during the second iteration of a FindFoVOffset statement; Address = {0:X}\nWin32 Error: {1}", start, e.Message)); }

                        step = 9;

                        unsafe
                        {
                            fixed (byte* pBuffer = buffer)
                            {
                                for (int i = 0; i < searchRegion; i += sizeof(int))
                                {
                                    step = 10;

                                    if (*(int*)(pBuffer + i) == varAddr)
                                    {
                                        step = 11;
                                        pFoV += i - searchRegionBefore + MainForm.c_pOffset;
                                        isFound = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                CloseHandle(hProc);
            }

            return isFound;
        }

        public float ReadFloat(int ptr)
        {
            byte[] buffer = new byte[sizeof(float)];
            IntPtr hProc;

            try { hProc = OpenProcess(READ, false, pid); }
            catch (Exception e) { throw new Exception(String.Format("Failed to open the process handle during a ReadFloat statement;\n{0}", e.Message)); }

            if (hProc != IntPtr.Zero)
            {
                try { ReadProcessMemory(hProc, ptr, buffer, sizeof(float), 0); }
                catch (Exception e) { throw new Exception(String.Format("Failed to read process memory during a ReadFloat statement; Address = {0:X}\nWin32 Error: {1}", ptr, e.Message)); }

                CloseHandle(hProc);
            }

            return BitConverter.ToSingle(buffer, 0); //* 65.0f;
        }

        public void WriteFloat(int ptr, float val)
        {
            //val /= 65.0f;
            IntPtr hProc;

            try { hProc = OpenProcess(WRITE, false, pid); }
            catch (Exception e) { throw new Exception(String.Format("Failed to open the process handle during a WriteFloat statement;\n{0}", e.Message)); }

            if (hProc != IntPtr.Zero)
            {
                try { WriteProcessMemory(hProc, ptr, BitConverter.GetBytes(val), sizeof(float), 0); }
                catch (Exception e) { throw new Exception(String.Format("Failed to write to process memory during a WriteFloat statement; Address = {0:X}\nWin32 Error: {1}", ptr, e.Message)); }

                CloseHandle(hProc);
            }
        }

        #endregion
    }
}
