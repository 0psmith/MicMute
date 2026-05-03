using System;
using System.IO;
using System.Xml.Serialization;

namespace MicMute
{
    public sealed class SettingsStore
    {
        private readonly string _filePath;

        public SettingsStore()
        {
            string root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MicMute");
            _filePath = Path.Combine(root, "settings.xml");
        }

        public bool Exists
        {
            get { return File.Exists(_filePath); }
        }

        public string FilePath
        {
            get { return _filePath; }
        }

        public AppSettings Load()
        {
            if (!File.Exists(_filePath))
            {
                return AppSettings.Default();
            }

            try
            {
                using (FileStream stream = File.OpenRead(_filePath))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
                    AppSettings settings = serializer.Deserialize(stream) as AppSettings;
                    return Normalize(settings);
                }
            }
            catch
            {
                return AppSettings.Default();
            }
        }

        public void Save(AppSettings settings)
        {
            string directory = Path.GetDirectoryName(_filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (FileStream stream = File.Create(_filePath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
                serializer.Serialize(stream, Normalize(settings));
            }
        }

        private static AppSettings Normalize(AppSettings settings)
        {
            if (settings == null)
            {
                settings = AppSettings.Default();
            }

            if (settings.DeviceId == null)
            {
                settings.DeviceId = string.Empty;
            }

            if (settings.Hotkey == null || !settings.Hotkey.IsValid())
            {
                settings.Hotkey = HotkeyGesture.Default();
            }

            settings.OverlayOpacityPercent = AppSettings.NormalizeOverlayOpacityPercent(settings.OverlayOpacityPercent);

            return settings;
        }
    }
}
