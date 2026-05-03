using System;
using System.Windows.Forms;

namespace MicMute
{
    public enum HotkeyTriggerType
    {
        Keyboard,
        MouseButton
    }

    public enum MouseHotkeyButton
    {
        None,
        Middle,
        XButton1,
        XButton2
    }

    public enum OverlayPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        Center,
        BottomLeft,
        BottomCenter,
        BottomRight
    }

    [Serializable]
    public class HotkeyGesture
    {
        public HotkeyTriggerType TriggerType { get; set; }
        public int KeyCode { get; set; }
        public MouseHotkeyButton MouseButton { get; set; }
        public bool Control { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public bool Win { get; set; }

        public HotkeyGesture()
        {
            TriggerType = HotkeyTriggerType.Keyboard;
            KeyCode = (int)Keys.M;
            MouseButton = MouseHotkeyButton.None;
            Control = true;
            Alt = true;
            Shift = true;
            Win = false;
        }

        public static HotkeyGesture Default()
        {
            return new HotkeyGesture();
        }

        public HotkeyGesture Clone()
        {
            return new HotkeyGesture
            {
                TriggerType = TriggerType,
                KeyCode = KeyCode,
                MouseButton = MouseButton,
                Control = Control,
                Alt = Alt,
                Shift = Shift,
                Win = Win
            };
        }

        public bool IsValid()
        {
            if (TriggerType == HotkeyTriggerType.Keyboard)
            {
                return KeyCode != 0 && !IsModifierOnlyKey((Keys)KeyCode);
            }

            return MouseButton != MouseHotkeyButton.None;
        }

        public string ToDisplayString()
        {
            string result = string.Empty;
            AppendModifier(ref result, Control, "Ctrl");
            AppendModifier(ref result, Alt, "Alt");
            AppendModifier(ref result, Shift, "Shift");
            AppendModifier(ref result, Win, "Win");

            string trigger;
            if (TriggerType == HotkeyTriggerType.Keyboard)
            {
                trigger = KeyName((Keys)KeyCode);
            }
            else
            {
                trigger = MouseButtonName(MouseButton);
            }

            if (result.Length > 0)
            {
                result += " + ";
            }

            return result + trigger;
        }

        private static void AppendModifier(ref string value, bool enabled, string name)
        {
            if (!enabled)
            {
                return;
            }

            if (value.Length > 0)
            {
                value += " + ";
            }

            value += name;
        }

        private static bool IsModifierOnlyKey(Keys key)
        {
            return key == Keys.ControlKey ||
                   key == Keys.ShiftKey ||
                   key == Keys.Menu ||
                   key == Keys.LWin ||
                   key == Keys.RWin ||
                   key == Keys.Control ||
                   key == Keys.Shift ||
                   key == Keys.Alt;
        }

        public static string KeyName(Keys key)
        {
            if (key >= Keys.A && key <= Keys.Z)
            {
                return key.ToString();
            }

            if (key >= Keys.D0 && key <= Keys.D9)
            {
                return ((int)(key - Keys.D0)).ToString();
            }

            if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
            {
                return "Num " + ((int)(key - Keys.NumPad0)).ToString();
            }

            if (key >= Keys.F1 && key <= Keys.F24)
            {
                return key.ToString();
            }

            switch (key)
            {
                case Keys.Space:
                    return "Space";
                case Keys.Return:
                    return "Enter";
                case Keys.Escape:
                    return "Esc";
                case Keys.Insert:
                    return "Insert";
                case Keys.Delete:
                    return "Delete";
                case Keys.Home:
                    return "Home";
                case Keys.End:
                    return "End";
                case Keys.PageUp:
                    return "Page Up";
                case Keys.PageDown:
                    return "Page Down";
                case Keys.Up:
                    return "Up";
                case Keys.Down:
                    return "Down";
                case Keys.Left:
                    return "Left";
                case Keys.Right:
                    return "Right";
                case Keys.Oemtilde:
                    return "`";
                case Keys.OemMinus:
                    return "-";
                case Keys.Oemplus:
                    return "=";
                case Keys.OemOpenBrackets:
                    return "[";
                case Keys.OemCloseBrackets:
                    return "]";
                case Keys.OemPipe:
                    return "\\";
                case Keys.OemSemicolon:
                    return ";";
                case Keys.OemQuotes:
                    return "'";
                case Keys.Oemcomma:
                    return ",";
                case Keys.OemPeriod:
                    return ".";
                case Keys.OemQuestion:
                    return "/";
                case Keys.MediaPlayPause:
                    return "Media Play/Pause";
                case Keys.MediaNextTrack:
                    return "Media Next";
                case Keys.MediaPreviousTrack:
                    return "Media Previous";
                case Keys.VolumeMute:
                    return "Volume Mute";
                default:
                    return key.ToString();
            }
        }

        public static string MouseButtonName(MouseHotkeyButton button)
        {
            switch (button)
            {
                case MouseHotkeyButton.Middle:
                    return "마우스 가운데 버튼";
                case MouseHotkeyButton.XButton1:
                    return "마우스 XButton1";
                case MouseHotkeyButton.XButton2:
                    return "마우스 XButton2";
                default:
                    return "마우스";
            }
        }
    }

    [Serializable]
    public class AppSettings
    {
        public string DeviceId { get; set; }
        public HotkeyGesture Hotkey { get; set; }
        public bool CloseToTray { get; set; }
        public bool StartWithWindows { get; set; }
        public OverlayPosition OverlayPosition { get; set; }
        public int OverlayOpacityPercent { get; set; }

        public AppSettings()
        {
            DeviceId = string.Empty;
            Hotkey = HotkeyGesture.Default();
            CloseToTray = true;
            StartWithWindows = false;
            OverlayPosition = OverlayPosition.BottomCenter;
            OverlayOpacityPercent = 90;
        }

        public static AppSettings Default()
        {
            return new AppSettings();
        }

        public AppSettings Clone()
        {
            return new AppSettings
            {
                DeviceId = DeviceId ?? string.Empty,
                Hotkey = Hotkey == null ? HotkeyGesture.Default() : Hotkey.Clone(),
                CloseToTray = CloseToTray,
                StartWithWindows = StartWithWindows,
                OverlayPosition = OverlayPosition,
                OverlayOpacityPercent = NormalizeOverlayOpacityPercent(OverlayOpacityPercent)
            };
        }

        public static int NormalizeOverlayOpacityPercent(int value)
        {
            if (value < 30)
            {
                return 90;
            }

            if (value > 100)
            {
                return 100;
            }

            return value;
        }
    }
}
