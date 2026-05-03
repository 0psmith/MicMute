using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MicMute
{
    internal static class StartupManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "MicMute";

        public static void SetEnabled(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true) ??
                                     Registry.CurrentUser.CreateSubKey(RunKeyPath))
            {
                if (key == null)
                {
                    throw new InvalidOperationException("Windows 시작 프로그램 레지스트리를 열 수 없습니다.");
                }

                if (enabled)
                {
                    key.SetValue(ValueName, Quote(Application.ExecutablePath), RegistryValueKind.String);
                }
                else
                {
                    key.DeleteValue(ValueName, false);
                }
            }
        }

        private static string Quote(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            return "\"" + path + "\"";
        }
    }
}
