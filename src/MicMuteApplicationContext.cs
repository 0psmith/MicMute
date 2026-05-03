using System;
using System.Drawing;
using System.Windows.Forms;

namespace MicMute
{
    public sealed class MicMuteApplicationContext : ApplicationContext
    {
        private readonly SettingsStore _store;
        private readonly AudioEndpointController _audio;
        private readonly HotkeyManager _hotkeys;
        private readonly OverlayForm _overlay;
        private readonly NotifyIcon _notifyIcon;
        private readonly Icon _mutedIcon;
        private readonly Icon _unmutedIcon;
        private readonly Timer _pollTimer;
        private readonly ToolStripMenuItem _toggleItem;
        private readonly ToolStripMenuItem _settingsItem;
        private readonly ToolStripMenuItem _exitItem;

        private AppSettings _settings;
        private SettingsForm _settingsForm;
        private bool _hasMuteState;
        private bool _lastMuted;
        private bool _exiting;
        private bool _reportedAudioError;

        public MicMuteApplicationContext()
        {
            _store = new SettingsStore();
            bool firstRun = !_store.Exists;
            _settings = _store.Load();

            _audio = new AudioEndpointController();
            _audio.Configure(_settings.DeviceId);

            _hotkeys = new HotkeyManager();
            _hotkeys.HotkeyPressed += delegate { ToggleMute(); };

            _overlay = new OverlayForm();
            _mutedIcon = TrayIconFactory.Create(true);
            _unmutedIcon = TrayIconFactory.Create(false);

            _toggleItem = new ToolStripMenuItem("마이크 음소거 전환", null, delegate { ToggleMute(); });
            _settingsItem = new ToolStripMenuItem("설정", null, delegate { ShowSettings(); });
            _exitItem = new ToolStripMenuItem("종료", null, delegate { ExitApplication(); });

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add(_toggleItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_settingsItem);
            menu.Items.Add(_exitItem);

            _notifyIcon = new NotifyIcon();
            _notifyIcon.Text = "MicMute";
            _notifyIcon.Icon = _unmutedIcon;
            _notifyIcon.ContextMenuStrip = menu;
            _notifyIcon.Visible = true;
            _notifyIcon.MouseDoubleClick += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    ShowSettings();
                }
            };

            HotkeyRegistrationResult hotkeyResult = _hotkeys.Register(_settings.Hotkey);
            if (!hotkeyResult.Success)
            {
                ShowBalloon("단축키 등록 실패", hotkeyResult.ErrorMessage + Environment.NewLine + "설정에서 다른 단축키를 기록하세요.", ToolTipIcon.Warning);
            }

            _pollTimer = new Timer();
            _pollTimer.Interval = 500;
            _pollTimer.Tick += delegate { PollMuteState(); };
            _pollTimer.Start();

            PollMuteState();

            if (firstRun || !hotkeyResult.Success)
            {
                ShowSettings();
            }
        }

        private void ToggleMute()
        {
            try
            {
                bool muted = _audio.ToggleMute();
                _reportedAudioError = false;
                HandleMuteState(muted, true);
            }
            catch (Exception ex)
            {
                ShowBalloon("마이크를 사용할 수 없습니다", ex.Message, ToolTipIcon.Warning);
            }
        }

        private void PollMuteState()
        {
            try
            {
                bool muted = _audio.GetMute();
                _reportedAudioError = false;

                if (!_hasMuteState)
                {
                    _hasMuteState = true;
                    _lastMuted = muted;
                    UpdateTray();
                    return;
                }

                if (muted != _lastMuted)
                {
                    HandleMuteState(muted, true);
                }
                else
                {
                    UpdateTray();
                }
            }
            catch (Exception ex)
            {
                _hasMuteState = false;
                UpdateTrayNoDevice();

                if (!_reportedAudioError)
                {
                    _reportedAudioError = true;
                    ShowBalloon("마이크를 사용할 수 없습니다", ex.Message, ToolTipIcon.Warning);
                }
            }
        }

        private void HandleMuteState(bool muted, bool feedback)
        {
            _hasMuteState = true;
            _lastMuted = muted;
            UpdateTray();

            if (feedback)
            {
                _overlay.ShowStatus(muted, _audio.CurrentDeviceName, _settings.OverlayPosition, _settings.OverlayOpacityPercent);
            }
        }

        private void UpdateTray()
        {
            _notifyIcon.Icon = _lastMuted ? _mutedIcon : _unmutedIcon;
            _toggleItem.Text = _lastMuted ? "마이크 음소거 해제" : "마이크 음소거";

            string state = _lastMuted ? "음소거" : "사용 중";
            string device = string.IsNullOrEmpty(_audio.CurrentDeviceName) ? "기본 마이크" : _audio.CurrentDeviceName;
            SetNotifyText("MicMute - " + state + " - " + device);
        }

        private void UpdateTrayNoDevice()
        {
            _notifyIcon.Icon = _unmutedIcon;
            _toggleItem.Text = "마이크 음소거 전환";
            SetNotifyText("MicMute - 활성 마이크 없음");
        }

        private void SetNotifyText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                text = "MicMute";
            }

            if (text.Length > 63)
            {
                text = text.Substring(0, 60) + "...";
            }

            _notifyIcon.Text = text;
        }

        private void ShowSettings()
        {
            if (_settingsForm == null || _settingsForm.IsDisposed)
            {
                _settingsForm = new SettingsForm(_audio, _settings);
                _settingsForm.Icon = _unmutedIcon;
                _settingsForm.ApplySettings = ApplySettings;
                _settingsForm.FormClosing += SettingsForm_FormClosing;
            }
            else
            {
                _settingsForm.UpdateSettings(_settings);
            }

            _settingsForm.Show();
            if (_settingsForm.WindowState == FormWindowState.Minimized)
            {
                _settingsForm.WindowState = FormWindowState.Normal;
            }
            _settingsForm.Activate();
        }

        private void SettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_exiting)
            {
                return;
            }

            if (_settings.CloseToTray)
            {
                e.Cancel = true;
                _settingsForm.Hide();
            }
            else
            {
                _exiting = true;
                ExitThread();
            }
        }

        private string ApplySettings(AppSettings candidate)
        {
            if (candidate == null)
            {
                return "설정이 비어 있습니다.";
            }

            AppSettings previous = _settings.Clone();

            try
            {
                _audio.Configure(candidate.DeviceId);
                HotkeyRegistrationResult result = _hotkeys.Register(candidate.Hotkey);
                if (!result.Success)
                {
                    _audio.Configure(previous.DeviceId);
                    _hotkeys.Register(previous.Hotkey);
                    return result.ErrorMessage;
                }

                StartupManager.SetEnabled(candidate.StartWithWindows);
                _settings = candidate.Clone();
                _store.Save(_settings);
                PollMuteState();
                return null;
            }
            catch (Exception ex)
            {
                try
                {
                    _audio.Configure(previous.DeviceId);
                    _hotkeys.Register(previous.Hotkey);
                    StartupManager.SetEnabled(previous.StartWithWindows);
                }
                catch
                {
                }

                return ex.Message;
            }
        }

        private void ShowBalloon(string title, string message, ToolTipIcon icon)
        {
            try
            {
                _notifyIcon.ShowBalloonTip(2500, title, message, icon);
            }
            catch
            {
            }
        }

        private void ExitApplication()
        {
            _exiting = true;
            ExitThread();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_pollTimer != null)
                {
                    _pollTimer.Stop();
                    _pollTimer.Dispose();
                }

                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                }

                if (_settingsForm != null)
                {
                    _settingsForm.Dispose();
                }

                if (_overlay != null)
                {
                    _overlay.Dispose();
                }

                if (_hotkeys != null)
                {
                    _hotkeys.Dispose();
                }

                if (_audio != null)
                {
                    _audio.Dispose();
                }

                if (_mutedIcon != null)
                {
                    _mutedIcon.Dispose();
                }

                if (_unmutedIcon != null)
                {
                    _unmutedIcon.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}
