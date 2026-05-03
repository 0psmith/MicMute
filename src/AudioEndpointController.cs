using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MicMute
{
    public sealed class AudioDeviceInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public sealed class AudioEndpointController : IDisposable
    {
        private const int DEVICE_STATE_ACTIVE = 0x00000001;
        private static readonly Guid IID_IAudioEndpointVolume = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");

        private string _selectedDeviceId;
        private IMMDevice _currentDevice;
        private IAudioEndpointVolume _volume;
        private string _currentDeviceId;
        private string _currentDeviceName;

        public AudioEndpointController()
        {
            _selectedDeviceId = string.Empty;
            _currentDeviceId = string.Empty;
            _currentDeviceName = string.Empty;
        }

        public string SelectedDeviceId
        {
            get { return _selectedDeviceId; }
        }

        public string CurrentDeviceId
        {
            get { return _currentDeviceId; }
        }

        public string CurrentDeviceName
        {
            get { return _currentDeviceName; }
        }

        public void Configure(string selectedDeviceId)
        {
            _selectedDeviceId = selectedDeviceId ?? string.Empty;
            RefreshEndpoint();
        }

        public List<AudioDeviceInfo> GetCaptureDevices()
        {
            List<AudioDeviceInfo> devices = new List<AudioDeviceInfo>();
            IMMDeviceEnumerator enumerator = null;
            IMMDeviceCollection collection = null;

            try
            {
                enumerator = CreateEnumerator();
                int hr = enumerator.EnumAudioEndpoints(EDataFlow.eCapture, DEVICE_STATE_ACTIVE, out collection);
                Marshal.ThrowExceptionForHR(hr);

                int count;
                Marshal.ThrowExceptionForHR(collection.GetCount(out count));

                for (int i = 0; i < count; i++)
                {
                    IMMDevice device = null;
                    try
                    {
                        Marshal.ThrowExceptionForHR(collection.Item(i, out device));
                        string id;
                        Marshal.ThrowExceptionForHR(device.GetId(out id));
                        string name = ReadFriendlyName(device);
                        devices.Add(new AudioDeviceInfo
                        {
                            Id = id,
                            Name = string.IsNullOrEmpty(name) ? "Microphone" : name
                        });
                    }
                    finally
                    {
                        ReleaseCom(device);
                    }
                }
            }
            finally
            {
                ReleaseCom(collection);
                ReleaseCom(enumerator);
            }

            devices.Sort(delegate(AudioDeviceInfo left, AudioDeviceInfo right)
            {
                return string.Compare(left.Name, right.Name, StringComparison.CurrentCultureIgnoreCase);
            });

            return devices;
        }

        public bool HasEndpoint()
        {
            EnsureEndpoint();
            return _volume != null;
        }

        public bool GetMute()
        {
            EnsureEndpoint();
            if (_volume == null)
            {
                throw new InvalidOperationException("No active microphone endpoint is available.");
            }

            bool muted;
            int hr = _volume.GetMute(out muted);
            if (hr < 0)
            {
                RefreshEndpoint();
                if (_volume == null)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                Marshal.ThrowExceptionForHR(_volume.GetMute(out muted));
            }

            return muted;
        }

        public bool SetMute(bool muted)
        {
            EnsureEndpoint();
            if (_volume == null)
            {
                throw new InvalidOperationException("No active microphone endpoint is available.");
            }

            Guid eventContext = Guid.Empty;
            int hr = _volume.SetMute(muted, ref eventContext);
            if (hr < 0)
            {
                RefreshEndpoint();
                if (_volume == null)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                Marshal.ThrowExceptionForHR(_volume.SetMute(muted, ref eventContext));
            }

            return GetMute();
        }

        public bool ToggleMute()
        {
            return SetMute(!GetMute());
        }

        public void Dispose()
        {
            ReleaseEndpoint();
        }

        private void EnsureEndpoint()
        {
            if (_volume == null)
            {
                RefreshEndpoint();
                return;
            }

            if (string.IsNullOrEmpty(_selectedDeviceId))
            {
                string defaultId = TryGetDefaultDeviceId();
                if (!string.IsNullOrEmpty(defaultId) &&
                    !string.Equals(defaultId, _currentDeviceId, StringComparison.OrdinalIgnoreCase))
                {
                    RefreshEndpoint();
                }
            }
        }

        private void RefreshEndpoint()
        {
            ReleaseEndpoint();

            IMMDevice device = null;
            try
            {
                if (!string.IsNullOrEmpty(_selectedDeviceId))
                {
                    device = TryGetDeviceById(_selectedDeviceId);
                }

                if (device == null)
                {
                    device = TryGetDefaultDevice();
                }

                if (device == null)
                {
                    _currentDeviceId = string.Empty;
                    _currentDeviceName = string.Empty;
                    return;
                }

                object volumeObject;
                Guid iid = IID_IAudioEndpointVolume;
                Marshal.ThrowExceptionForHR(device.Activate(ref iid, CLSCTX.CLSCTX_ALL, IntPtr.Zero, out volumeObject));

                _currentDevice = device;
                _volume = (IAudioEndpointVolume)volumeObject;
                Marshal.ThrowExceptionForHR(device.GetId(out _currentDeviceId));
                _currentDeviceName = ReadFriendlyName(device);
                if (string.IsNullOrEmpty(_currentDeviceName))
                {
                    _currentDeviceName = "Microphone";
                }

                device = null;
            }
            finally
            {
                ReleaseCom(device);
            }
        }

        private IMMDevice TryGetDefaultDevice()
        {
            IMMDeviceEnumerator enumerator = null;
            IMMDevice device = null;
            try
            {
                enumerator = CreateEnumerator();
                int hr = enumerator.GetDefaultAudioEndpoint(EDataFlow.eCapture, ERole.eConsole, out device);
                if (hr < 0 || device == null)
                {
                    ReleaseCom(device);
                    device = null;
                    hr = enumerator.GetDefaultAudioEndpoint(EDataFlow.eCapture, ERole.eCommunications, out device);
                }

                if (hr < 0)
                {
                    ReleaseCom(device);
                    return null;
                }

                IMMDevice result = device;
                device = null;
                return result;
            }
            finally
            {
                ReleaseCom(device);
                ReleaseCom(enumerator);
            }
        }

        private string TryGetDefaultDeviceId()
        {
            IMMDevice device = null;
            try
            {
                device = TryGetDefaultDevice();
                if (device == null)
                {
                    return string.Empty;
                }

                string id;
                int hr = device.GetId(out id);
                return hr < 0 ? string.Empty : id;
            }
            finally
            {
                ReleaseCom(device);
            }
        }

        private IMMDevice TryGetDeviceById(string deviceId)
        {
            IMMDeviceEnumerator enumerator = null;
            IMMDevice device = null;
            try
            {
                enumerator = CreateEnumerator();
                int hr = enumerator.GetDevice(deviceId, out device);
                if (hr < 0)
                {
                    ReleaseCom(device);
                    return null;
                }

                IMMDevice result = device;
                device = null;
                return result;
            }
            finally
            {
                ReleaseCom(device);
                ReleaseCom(enumerator);
            }
        }

        private static IMMDeviceEnumerator CreateEnumerator()
        {
            return (IMMDeviceEnumerator)(new MMDeviceEnumerator());
        }

        private static string ReadFriendlyName(IMMDevice device)
        {
            if (device == null)
            {
                return string.Empty;
            }

            IPropertyStore store = null;
            PropVariant value = new PropVariant();
            try
            {
                int hr = device.OpenPropertyStore(STGM.STGM_READ, out store);
                if (hr < 0 || store == null)
                {
                    return string.Empty;
                }

                PropertyKey key = PropertyKeys.PKEY_Device_FriendlyName;
                hr = store.GetValue(ref key, out value);
                if (hr < 0)
                {
                    return string.Empty;
                }

                return value.GetString();
            }
            finally
            {
                PropVariantClear(ref value);
                ReleaseCom(store);
            }
        }

        private void ReleaseEndpoint()
        {
            ReleaseCom(_volume);
            ReleaseCom(_currentDevice);
            _volume = null;
            _currentDevice = null;
            _currentDeviceId = string.Empty;
            _currentDeviceName = string.Empty;
        }

        private static void ReleaseCom(object value)
        {
            if (value != null && Marshal.IsComObject(value))
            {
                Marshal.ReleaseComObject(value);
            }
        }

        [DllImport("ole32.dll")]
        private static extern int PropVariantClear(ref PropVariant pvar);
    }

    internal enum EDataFlow
    {
        eRender = 0,
        eCapture = 1,
        eAll = 2
    }

    internal enum ERole
    {
        eConsole = 0,
        eMultimedia = 1,
        eCommunications = 2
    }

    [Flags]
    internal enum CLSCTX
    {
        CLSCTX_INPROC_SERVER = 0x1,
        CLSCTX_INPROC_HANDLER = 0x2,
        CLSCTX_LOCAL_SERVER = 0x4,
        CLSCTX_REMOTE_SERVER = 0x10,
        CLSCTX_ALL = CLSCTX_INPROC_SERVER | CLSCTX_INPROC_HANDLER | CLSCTX_LOCAL_SERVER | CLSCTX_REMOTE_SERVER
    }

    internal enum STGM
    {
        STGM_READ = 0
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    internal class MMDeviceEnumerator
    {
    }

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDeviceEnumerator
    {
        [PreserveSig]
        int EnumAudioEndpoints(EDataFlow dataFlow, int dwStateMask, out IMMDeviceCollection ppDevices);

        [PreserveSig]
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppEndpoint);

        [PreserveSig]
        int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId, out IMMDevice ppDevice);

        [PreserveSig]
        int RegisterEndpointNotificationCallback(IntPtr pClient);

        [PreserveSig]
        int UnregisterEndpointNotificationCallback(IntPtr pClient);
    }

    [ComImport]
    [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDeviceCollection
    {
        [PreserveSig]
        int GetCount(out int pcDevices);

        [PreserveSig]
        int Item(int nDevice, out IMMDevice ppDevice);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDevice
    {
        [PreserveSig]
        int Activate(ref Guid iid, CLSCTX dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);

        [PreserveSig]
        int OpenPropertyStore(STGM stgmAccess, out IPropertyStore ppProperties);

        [PreserveSig]
        int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);

        [PreserveSig]
        int GetState(out int pdwState);
    }

    [ComImport]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPropertyStore
    {
        [PreserveSig]
        int GetCount(out int cProps);

        [PreserveSig]
        int GetAt(int iProp, out PropertyKey pkey);

        [PreserveSig]
        int GetValue(ref PropertyKey key, out PropVariant pv);

        [PreserveSig]
        int SetValue(ref PropertyKey key, ref PropVariant pv);

        [PreserveSig]
        int Commit();
    }

    [ComImport]
    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioEndpointVolume
    {
        [PreserveSig]
        int RegisterControlChangeNotify(IntPtr pNotify);

        [PreserveSig]
        int UnregisterControlChangeNotify(IntPtr pNotify);

        [PreserveSig]
        int GetChannelCount(out uint pnChannelCount);

        [PreserveSig]
        int SetMasterVolumeLevel(float fLevelDB, ref Guid pguidEventContext);

        [PreserveSig]
        int SetMasterVolumeLevelScalar(float fLevel, ref Guid pguidEventContext);

        [PreserveSig]
        int GetMasterVolumeLevel(out float pfLevelDB);

        [PreserveSig]
        int GetMasterVolumeLevelScalar(out float pfLevel);

        [PreserveSig]
        int SetChannelVolumeLevel(uint nChannel, float fLevelDB, ref Guid pguidEventContext);

        [PreserveSig]
        int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, ref Guid pguidEventContext);

        [PreserveSig]
        int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);

        [PreserveSig]
        int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);

        [PreserveSig]
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, ref Guid pguidEventContext);

        [PreserveSig]
        int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);

        [PreserveSig]
        int GetVolumeStepInfo(out uint pnStep, out uint pnStepCount);

        [PreserveSig]
        int VolumeStepUp(ref Guid pguidEventContext);

        [PreserveSig]
        int VolumeStepDown(ref Guid pguidEventContext);

        [PreserveSig]
        int QueryHardwareSupport(out uint pdwHardwareSupportMask);

        [PreserveSig]
        int GetVolumeRange(out float pflVolumeMindB, out float pflVolumeMaxdB, out float pflVolumeIncrementdB);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PropertyKey
    {
        public Guid fmtid;
        public int pid;

        public PropertyKey(Guid fmtid, int pid)
        {
            this.fmtid = fmtid;
            this.pid = pid;
        }
    }

    internal static class PropertyKeys
    {
        public static readonly PropertyKey PKEY_Device_FriendlyName =
            new PropertyKey(new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"), 14);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PropVariant
    {
        public ushort vt;
        public ushort wReserved1;
        public ushort wReserved2;
        public ushort wReserved3;
        public IntPtr p;

        public string GetString()
        {
            if (vt == 31 && p != IntPtr.Zero)
            {
                return Marshal.PtrToStringUni(p);
            }

            if (vt == 30 && p != IntPtr.Zero)
            {
                return Marshal.PtrToStringAnsi(p);
            }

            return string.Empty;
        }
    }
}
