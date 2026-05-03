using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MicMute
{
    public sealed class HotkeyRegistrationResult
    {
        public bool Success { get; private set; }
        public string ErrorMessage { get; private set; }

        public static HotkeyRegistrationResult Ok()
        {
            return new HotkeyRegistrationResult { Success = true, ErrorMessage = string.Empty };
        }

        public static HotkeyRegistrationResult Error(string message)
        {
            return new HotkeyRegistrationResult { Success = false, ErrorMessage = message };
        }
    }

    public sealed class HotkeyManager : IDisposable
    {
        private const int HOTKEY_ID = 0x4D4D;
        private const int WM_HOTKEY = 0x0312;
        private const int MOD_ALT = 0x0001;
        private const int MOD_CONTROL = 0x0002;
        private const int MOD_SHIFT = 0x0004;
        private const int MOD_WIN = 0x0008;
        private const int MOD_NOREPEAT = 0x4000;

        private const int WH_MOUSE_LL = 14;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_XBUTTONDOWN = 0x020B;
        private const int XBUTTON1 = 0x0001;
        private const int XBUTTON2 = 0x0002;

        private readonly HotkeyWindow _window;
        private readonly LowLevelMouseProc _mouseProc;
        private HotkeyGesture _gesture;
        private IntPtr _mouseHook;
        private DateTime _lastMouseTriggerUtc;
        private bool _keyboardRegistered;

        public event EventHandler HotkeyPressed;

        public HotkeyManager()
        {
            _window = new HotkeyWindow(this);
            _mouseProc = MouseHookCallback;
            _lastMouseTriggerUtc = DateTime.MinValue;
        }

        public HotkeyRegistrationResult Register(HotkeyGesture gesture)
        {
            Unregister();

            if (gesture == null || !gesture.IsValid())
            {
                return HotkeyRegistrationResult.Error("Record a hotkey to use.");
            }

            _gesture = gesture.Clone();

            if (_gesture.TriggerType == HotkeyTriggerType.Keyboard)
            {
                int modifiers = MOD_NOREPEAT;
                if (_gesture.Control)
                {
                    modifiers |= MOD_CONTROL;
                }
                if (_gesture.Alt)
                {
                    modifiers |= MOD_ALT;
                }
                if (_gesture.Shift)
                {
                    modifiers |= MOD_SHIFT;
                }
                if (_gesture.Win)
                {
                    modifiers |= MOD_WIN;
                }

                if (!RegisterHotKey(_window.Handle, HOTKEY_ID, modifiers, _gesture.KeyCode))
                {
                    int error = Marshal.GetLastWin32Error();
                    _gesture = null;
                    return HotkeyRegistrationResult.Error("Could not register hotkey: " + gesture.ToDisplayString() + Environment.NewLine + new Win32Exception(error).Message);
                }

                _keyboardRegistered = true;
                return HotkeyRegistrationResult.Ok();
            }

            _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, GetModuleHandle(null), 0);
            if (_mouseHook == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                _gesture = null;
                return HotkeyRegistrationResult.Error("Could not register hotkey: " + gesture.ToDisplayString() + Environment.NewLine + new Win32Exception(error).Message);
            }

            return HotkeyRegistrationResult.Ok();
        }

        public void Unregister()
        {
            if (_keyboardRegistered)
            {
                UnregisterHotKey(_window.Handle, HOTKEY_ID);
                _keyboardRegistered = false;
            }

            if (_mouseHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }

            _gesture = null;
        }

        public void Dispose()
        {
            Unregister();
            _window.DestroyHandle();
        }

        private void RaiseHotkeyPressed()
        {
            EventHandler handler = HotkeyPressed;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _gesture != null && _gesture.TriggerType == HotkeyTriggerType.MouseButton)
            {
                int message = wParam.ToInt32();
                MouseHotkeyButton pressed = MouseHotkeyButton.None;

                if (message == WM_MBUTTONDOWN)
                {
                    pressed = MouseHotkeyButton.Middle;
                }
                else if (message == WM_XBUTTONDOWN)
                {
                    MSLLHOOKSTRUCT hook = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                    int button = (int)((hook.mouseData >> 16) & 0xffff);
                    if (button == XBUTTON1)
                    {
                        pressed = MouseHotkeyButton.XButton1;
                    }
                    else if (button == XBUTTON2)
                    {
                        pressed = MouseHotkeyButton.XButton2;
                    }
                }

                if (pressed == _gesture.MouseButton && ModifiersMatch(_gesture) && MouseDebounceElapsed())
                {
                    RaiseHotkeyPressed();
                    return new IntPtr(1);
                }
            }

            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        private bool MouseDebounceElapsed()
        {
            DateTime now = DateTime.UtcNow;
            if ((now - _lastMouseTriggerUtc).TotalMilliseconds < 250)
            {
                return false;
            }

            _lastMouseTriggerUtc = now;
            return true;
        }

        private static bool ModifiersMatch(HotkeyGesture gesture)
        {
            return ModifierMatches(gesture.Control, Keys.ControlKey, Keys.LControlKey, Keys.RControlKey) &&
                   ModifierMatches(gesture.Alt, Keys.Menu, Keys.LMenu, Keys.RMenu) &&
                   ModifierMatches(gesture.Shift, Keys.ShiftKey, Keys.LShiftKey, Keys.RShiftKey) &&
                   ModifierMatches(gesture.Win, Keys.LWin, Keys.RWin);
        }

        private static bool ModifierMatches(bool expected, params Keys[] keys)
        {
            bool down = false;
            for (int i = 0; i < keys.Length; i++)
            {
                if ((GetAsyncKeyState((int)keys[i]) & 0x8000) != 0)
                {
                    down = true;
                    break;
                }
            }

            return expected ? down : !down;
        }

        private sealed class HotkeyWindow : NativeWindow
        {
            private readonly HotkeyManager _owner;

            public HotkeyWindow(HotkeyManager owner)
            {
                _owner = owner;
                CreateParams cp = new CreateParams();
                cp.Caption = "MicMuteHotkeyWindow";
                CreateHandle(cp);
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
                {
                    _owner.RaiseHotkeyPressed();
                    return;
                }

                base.WndProc(ref m);
            }
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
    }
}
