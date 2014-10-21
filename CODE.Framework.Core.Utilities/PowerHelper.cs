using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CODE.Framework.Core.Utilities
{
    /// <summary>
    /// This class provides a number of power-related features
    /// </summary>
    public static class PowerHelper
    {
        /// <summary>
        /// Dummy form use to receive messages
        /// </summary>
        private static MessageSinkWindow _dummyMessageReceiver;

        /// <summary>
        /// Handle for power source changed
        /// </summary>
        private static IntPtr _handlePowerSource;
        /// <summary>
        /// Handle for battery capacity changes
        /// </summary>
        private static IntPtr _handleBatteryCapacity;
        /// <summary>
        /// Handle for monitor power status changes
        /// </summary>
        private static IntPtr _handleMonitorOn;
        /// <summary>
        /// Handle for power scheme changes
        /// </summary>
        private static IntPtr _handlePowerScheme;
        /// <summary>
        /// Indicates whether the class currently listens for power events
        /// </summary>
        private static bool _listeningForPowerEvents;

        /// <summary>
        /// Current power status of the system
        /// </summary>
        /// <value>The current status.</value>
        public static PowerInformation CurrentStatus
        {
            get
            {
                var information = new PowerInformation();

                var powerStatus = new SYSTEM_POWER_STATUS();
                var result = NativeMethods.GetSystemPowerStatus(powerStatus);
                if (!result) return information;
                // Power source
                if (powerStatus.ACLineStatus == (byte)ACLineStatus.Battery) { information.Source = PowerSource.Battery; }
                else if (powerStatus.ACLineStatus == (byte)ACLineStatus.AC) { information.Source = PowerSource.PluggedIn; }
                else { information.Source = PowerSource.Unknown; }

                // Battery charge status
                information.BatteryIsCharging = (powerStatus.BatteryFlag & (byte)BatteryFlag.Charging) == (int)BatteryFlag.Charging;
                information.BatteryAvailable = (powerStatus.BatteryFlag & (byte)BatteryFlag.NoSystemBattery) != (int)BatteryFlag.NoSystemBattery;
                if ((powerStatus.BatteryFlag & (byte)BatteryFlag.Critical) == (int)BatteryFlag.Critical) { information.BatteryChargeState = BatteryChargeState.Critical; }
                else if ((powerStatus.BatteryFlag & (byte)BatteryFlag.High) == (int)BatteryFlag.High) { information.BatteryChargeState = BatteryChargeState.High; }
                else if ((powerStatus.BatteryFlag & (byte)BatteryFlag.Low) == (int)BatteryFlag.Low) { information.BatteryChargeState = BatteryChargeState.Low; }
                else { information.BatteryChargeState = BatteryChargeState.Normal; }

                // Battery life precentage
                if (powerStatus.BatteryLifePercent == 255) { information.BatteryPercentLeft = -1f; }
                else { information.BatteryPercentLeft = (100 / powerStatus.BatteryLifePercent); }

                // Battery lifetime
                if (powerStatus.BatteryLifeTime > -1) { information.RemainingBatteryLifeTime = new TimeSpan(0, 0, powerStatus.BatteryLifeTime); }
                if (powerStatus.BatteryFullLifeTime > -1) { information.TotalBatteryLifeTime = new TimeSpan(0, 0, powerStatus.BatteryFullLifeTime); }

                // Power scheme (Vista Only)
                if (IsVistaOrLater())
                {
                    // This is Vista or later
                    IntPtr schemaPointer;
                    var result2 = NativeMethods.PowerGetActiveScheme(IntPtr.Zero, out schemaPointer);
                    if (result2 == 0)
                    {
                        var schemaGuid = (Guid)Marshal.PtrToStructure(schemaPointer, typeof(Guid));
                        if (schemaGuid == PowerSettings.GUID_MAX_POWER_SAVINGS)
                            information.Plan = PowerPlan.PowerSaver;
                        else if (schemaGuid == PowerSettings.GUID_MIN_POWER_SAVINGS)
                            information.Plan = PowerPlan.HighPerformance;
                        else if (schemaGuid == PowerSettings.GUID_TYPICAL_POWER_SAVINGS)
                            information.Plan = PowerPlan.Balanced;
                        else
                            information.Plan = PowerPlan.Unknown;
                    }
                }

                // Disk power state
                var files = Assembly.GetExecutingAssembly().GetFiles();
                if (files.Length > 0)
                {
                    var fileHandle = files[0].Handle;
                    bool deviceOn;
                    if (NativeMethods.GetDevicePowerState(fileHandle, out deviceOn))
                        information.DiskDriveState = deviceOn ? DeviceState.On : DeviceState.Off;
                    else
                        information.DiskDriveState = DeviceState.Unknown;
                }

                // Monitor power state (Vista only)
                if (IsVistaOrLater())
                {
                    // TODO: Implement this
                    //bool monitorOn = false;

                    //System.Windows.Forms.Screen

                    //IntPtr monitorPointer = IntPtr.Zero;
                    //Marshal.StructureToPtr(PowerSettings.GUID_MONITOR_POWER_ON, monitorPointer, false);
                    //if (PowerHelper.GetDevicePowerState(monitorPointer, out monitorOn))
                    //{
                    //}
                }

                return information;
            }
        }

        /// <summary>
        /// Window handle for windows power messages
        /// </summary>
        private static IntPtr _windowHandle = IntPtr.Zero;

        /// <summary>
        /// Returns true if the current OS is at least Vista
        /// </summary>
        /// <returns></returns>
        private static bool IsVistaOrLater()
        {
            if (Environment.OSVersion.Platform != PlatformID.WinCE && Environment.OSVersion.Platform != PlatformID.Unix)
                if (Environment.OSVersion.Version.Major >= 6)
                    return true;
            return false;
        }

        /// <summary>
        /// Registers for power notifications.
        /// </summary>
        private static void RegisterForPowerNotifications()
        {
            if (_listeningForPowerEvents) return;
            _dummyMessageReceiver = new MessageSinkWindow();
            _windowHandle = _dummyMessageReceiver.Handle;

            _handlePowerSource = NativeMethods.RegisterPowerSettingNotification(_windowHandle, ref PowerSettings.GUID_ACDC_POWER_SOURCE, DEVICE_NOTIFY_WINDOW_HANDLE);
            _handleBatteryCapacity = NativeMethods.RegisterPowerSettingNotification(_windowHandle, ref PowerSettings.GUID_BATTERY_PERCENTAGE_REMAINING, DEVICE_NOTIFY_WINDOW_HANDLE);
            _handleMonitorOn = NativeMethods.RegisterPowerSettingNotification(_windowHandle, ref PowerSettings.GUID_MONITOR_POWER_ON, DEVICE_NOTIFY_WINDOW_HANDLE);
            _handlePowerScheme = NativeMethods.RegisterPowerSettingNotification(_windowHandle, ref PowerSettings.GUID_POWERSCHEME_PERSONALITY, DEVICE_NOTIFY_WINDOW_HANDLE);

            _listeningForPowerEvents = true;
        }

        /// <summary>
        /// Unregisters for power notifications.
        /// </summary>
        private static void UnregisterForPowerNotifications()
        {
            NativeMethods.UnregisterPowerSettingNotification(_handleBatteryCapacity);
            NativeMethods.UnregisterPowerSettingNotification(_handleMonitorOn);
            NativeMethods.UnregisterPowerSettingNotification(_handlePowerScheme);
            NativeMethods.UnregisterPowerSettingNotification(_handlePowerSource);

            _listeningForPowerEvents = false;
        }

        /// <summary>
        /// Message hook for power notifications
        /// </summary>
        /// <param name="windowHandle">The window handle.</param>
        /// <param name="message">The message.</param>
        /// <param name="parameterW">The parameter W.</param>
        /// <param name="parameterL">The parameter L.</param>
        /// <param name="handled">if set to <c>true</c> [handled].</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "parameterW", Justification = "We have no choice, because the OS passes the parameter."),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "parameterL", Justification = "We have no choice, because the OS passes the parameter."),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "handled", Justification = "We have no choice, because the OS passes the parameter.")]
        internal static IntPtr MessageHook(IntPtr windowHandle, int message, IntPtr parameterW, IntPtr parameterL, ref bool handled)
        {
            if (windowHandle == _windowHandle && message == WM_POWERBROADCAST)
                RaisePowerChanged();
            return IntPtr.Zero;
        }

        /// <summary>
        /// Defines whether the screen should be allowed to go to sleep.
        /// </summary>
        /// <param name="keepAlive">If true, the screen will not go into sleep mode</param>
        /// <returns>True if successfully set</returns>
        /// <remarks>This method is specific to the thread that calls it. When the thread goes away, so does this setting.</remarks>
        public static bool KeepScreenAlive(bool keepAlive)
        {
            if (keepAlive)
                NativeMethods.SetThreadExecutionState(EXECUTION_STATE.ES_DISPLAY_REQUIRED | EXECUTION_STATE.ES_CONTINUOUS);
            else
                NativeMethods.SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
            return true;
        }

        /// <summary>
        /// Occurs when power settings change
        /// </summary>
        private static event EventHandler<PowerChangeEventArgs> _powerChanged;

        /// <summary>
        /// Occurs when power settings change
        /// </summary>
        public static event EventHandler<PowerChangeEventArgs> PowerChanged
        {
            add
            {
                _powerChanged += value;
                RegisterForPowerNotifications();
            }
            remove
            {
                _powerChanged -= value;
                if (_powerChanged == null)
                    UnregisterForPowerNotifications();
            }
        }

        /// <summary>
        /// Raises the power changed event.
        /// </summary>
        private static void RaisePowerChanged()
        {
            var arguments = new PowerChangeEventArgs {PowerInformation = CurrentStatus};
            if (_listeningForPowerEvents)
                _powerChanged(null, arguments);
        }

        /// <summary>
        /// Power broadcase
        /// </summary>
        private static int WM_POWERBROADCAST = 0x0218;
        ///// <summary>
        ///// Query suspend
        ///// </summary>
        //private static int PBT_APMQUERYSUSPEND = 0x0000;
        ///// <summary>
        ///// Query standby
        ///// </summary>
        //private static int PBT_APMQUERYSTANDBY = 0x0001;
        ///// <summary>
        ///// Query suspend failed
        ///// </summary>
        //private static int PBT_APMQUERYSUSPENDFAILED = 0x0002;
        ///// <summary>
        ///// Query standby failed
        ///// </summary>
        //private static int PBT_APMQUERYSTANDBYFAILED = 0x0003;
        ///// <summary>
        ///// Suspend
        ///// </summary>
        //private static int PBT_APMSUSPEND = 0x0004;
        ///// <summary>
        ///// Standby
        ///// </summary>
        //private static int PBT_APMSTANDBY = 0x0005;
        ///// <summary>
        ///// Resume crititical
        ///// </summary>
        //private static int PBT_APMRESUMECRITICAL = 0x0006;
        ///// <summary>
        ///// Resume suspend
        ///// </summary>
        //private static int PBT_APMRESUMESUSPEND = 0x0007;
        ///// <summary>
        ///// Resume standby
        ///// </summary>
        //private static int PBT_APMRESUMESTANDBY = 0x0008;
        ///// <summary>
        ///// Battery low
        ///// </summary>
        //private static int PBT_APMBATTERYLOW = 0x0009;
        ///// <summary>
        ///// Power status changed
        ///// </summary>
        //private static int PBT_APMPOWERSTATUSCHANGE = 0x000A;
        ///// <summary>
        ///// OEM Event
        ///// </summary>
        //private static int PBT_APMOEMEVENT = 0x000B;
        ///// <summary>
        ///// Resume automatic
        ///// </summary>
        //private static int PBT_APMRESUMEAUTOMATIC = 0x0012;
        ///// <summary>
        ///// Power setting change
        ///// </summary>
        //private static int PBT_POWERSETTINGCHANGE = 0x8013; // DPPE
        /// <summary>
        /// Device notify window handle
        /// </summary>
        private static int DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;
        ///// <summary>
        ///// Device notify service handle
        ///// </summary>
        //private static int DEVICE_NOTIFY_SERVICE_HANDLE = 0x00000001;

        /// <summary>
        /// Native (interop) methods used by the PowerHelper class
        /// </summary>
        private static class NativeMethods
        {
            /// <summary>
            /// Gets the system power status.
            /// </summary>
            /// <param name="SystemPowerStatus">The system power status.</param>
            /// <returns>True of false, to indicate success</returns>
            [DllImport("Kernel32.DLL", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal extern static bool GetSystemPowerStatus(SYSTEM_POWER_STATUS SystemPowerStatus);

            /// <summary>
            /// Gets the power state of the device.
            /// </summary>
            /// <param name="device">The device.</param>
            /// <param name="on">if set to <c>true</c>, the device is on.</param>
            /// <returns></returns>
            [DllImport("Kernel32.DLL", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal extern static bool GetDevicePowerState(IntPtr device, [MarshalAs(UnmanagedType.Bool)] out bool on);

            /// <summary>
            /// Powers the get ative scheme (Vista).
            /// </summary>
            /// <param name="userRootPowerKey">The user root power key.</param>
            /// <param name="activePolicyGuid">The active policy GUID.</param>
            /// <returns></returns>
            [DllImport("PowrProf.dll", CharSet = CharSet.Ansi, SetLastError = true)]
            internal extern static uint PowerGetActiveScheme(IntPtr userRootPowerKey, out IntPtr activePolicyGuid);

            /// <summary>
            /// Registers for power setting notifications.
            /// </summary>
            /// <param name="handle">The handle.</param>
            /// <param name="powerSettingGuid">The power setting GUID.</param>
            /// <param name="flags">The flags.</param>
            /// <returns></returns>
            [DllImport(@"User32", EntryPoint = "RegisterPowerSettingNotification", CallingConvention = CallingConvention.StdCall)]
            internal static extern IntPtr RegisterPowerSettingNotification(IntPtr handle, ref Guid powerSettingGuid, Int32 flags);

            /// <summary>
            /// Unregisters for power setting notification.
            /// </summary>
            /// <param name="handle">The handle.</param>
            /// <returns></returns>
            [DllImport(@"User32", EntryPoint = "UnregisterPowerSettingNotification", CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool UnregisterPowerSettingNotification(IntPtr handle);

            /// <summary>
            /// Sets the state of the thread execution.
            /// </summary>
            /// <param name="state">The state.</param>
            /// <returns></returns>
            [DllImport("Kernel32.DLL", CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE state);
        }

        /// <summary>
        /// Dummy class used as a message sink for windows messages
        /// sent to receive power notifications
        /// </summary>
        private class MessageSinkWindow : Form
        {
            /// <summary>
            /// Message handler
            /// </summary>
            /// <param name="message">The message.</param>
            protected override void WndProc(ref Message message)
            {
                base.WndProc(ref message);
                if (message.Msg == WM_POWERBROADCAST)
                {
                    bool handled = false;
                    message.Result = MessageHook(message.HWnd, message.Msg, message.WParam, message.LParam, ref handled);
                }
            }
        }

        /// <summary>
        /// System Power Status
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private class SYSTEM_POWER_STATUS
        {
            /// <summary>
            /// AC Line Status
            /// </summary>
            public byte ACLineStatus;
            /// <summary>
            /// Battery Flag
            /// </summary>
            public byte BatteryFlag;
            /// <summary>
            /// Battery life precent
            /// </summary>
            public byte BatteryLifePercent;
            /// <summary>
            /// Reserved
            /// </summary>
            public byte Reserved1;
            /// <summary>
            /// Battery life time remaining
            /// </summary>
            public Int32 BatteryLifeTime;
            /// <summary>
            /// Battery full lifetime
            /// </summary>
            public Int32 BatteryFullLifeTime;
        }

        /// <summary>
        /// AC Line Status
        /// </summary>
        private enum ACLineStatus : byte
        {
            /// <summary>
            /// Battery 
            /// </summary>
            Battery = 0,
            /// <summary>
            /// AC
            /// </summary>
            AC = 1,
            /// <summary>
            /// Unknown
            /// </summary>
            Unknown = 255
        }

        /// <summary>
        /// Battery flag
        /// </summary>
        [Flags]
        private enum BatteryFlag : byte
        {
            /// <summary>
            /// High
            /// </summary>
            High = 1,
            /// <summary>
            /// Low
            /// </summary>
            Low = 2,
            /// <summary>
            /// Critical
            /// </summary>
            Critical = 4,
            /// <summary>
            /// Charging
            /// </summary>
            Charging = 8,
            /// <summary>
            /// No systme battery
            /// </summary>
            NoSystemBattery = 128,
            /// <summary>
            /// Unknown
            /// </summary>
            Unknown = 255
        }

        /// <summary>
        /// Defines various constants for power settings
        /// </summary>
        private static class PowerSettings
        {
            /// <summary>
            /// High Performance - The scheme is designed to deliver maximum performance 
            /// at the expense of power consumption savings.
            /// </summary>
            public static readonly Guid GUID_MIN_POWER_SAVINGS = new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
            /// <summary>
            /// Power Saver - The scheme is designed to deliver maximum power consumption 
            /// savings at the expense of system performance and responsiveness.
            /// </summary>
            public static readonly Guid GUID_MAX_POWER_SAVINGS = new Guid("a1841308-3541-4fab-bc81-f71556f20b4a");

            /// <summary>
            /// Automatic (Balanced) - The scheme is designed to automatically balance 
            /// performance and power consumption savings.
            /// </summary>
            public static readonly Guid GUID_TYPICAL_POWER_SAVINGS = new Guid("381b4222-f694-41f0-9685-ff5bb260df2e");

            /// <summary>
            /// Monitor on switch
            /// </summary>
            public static Guid GUID_MONITOR_POWER_ON = new Guid(0x02731015, 0x4510, 0x4526, 0x99, 0xE6, 0xE5, 0xA1, 0x7E, 0xBD, 0x1A, 0xEA);

            /// <summary>
            /// Power source
            /// </summary>
            public static Guid GUID_ACDC_POWER_SOURCE = new Guid(0x5D3E9A59, 0xE9D5, 0x4B00, 0xA6, 0xBD, 0xFF, 0x34, 0xFF, 0x51, 0x65, 0x48);

            /// <summary>
            /// Power scheme personality
            /// </summary>
            public static Guid GUID_POWERSCHEME_PERSONALITY = new Guid(0x245D8541, 0x3943, 0x4422, 0xB0, 0x25, 0x13, 0xA7, 0x84, 0xF6, 0x79, 0xB7);

            /// <summary>
            /// Battery percentage remaining
            /// </summary>
            public static Guid GUID_BATTERY_PERCENTAGE_REMAINING = new Guid("A7AD8041-B45A-4CAE-87A3-EECBB468A9E1");
        }

        /// <summary>
        /// Thread execution state
        /// </summary>
        [Flags]
        private enum EXECUTION_STATE : uint
        {
            /// <summary>
            /// No keep alive required
            /// </summary>
            None = 0x00,
            /// <summary>
            /// System required
            /// </summary>
            ES_SYSTEM_REQUIRED = 0x00000001,
            /// <summary>
            /// Display required
            /// </summary>
            ES_DISPLAY_REQUIRED = 0x00000002,
            // Legacy flag, should not be used.
            // ES_USER_PRESENT   = 0x00000004,
            /// <summary>
            /// Continuous
            /// </summary>
            ES_CONTINUOUS = 0x80000000,
        }
    }

    /// <summary>
    /// Power plan (Windows Vista)
    /// </summary>
    public enum PowerPlan
    {
        /// <summary>
        /// High performance
        /// </summary>
        HighPerformance,
        /// <summary>
        /// PowerSaver
        /// </summary>
        PowerSaver,
        /// <summary>
        /// Balanced
        /// </summary>
        Balanced,
        /// <summary>
        /// Unknown
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Power source
    /// </summary>
    public enum PowerSource
    {
        /// <summary>
        /// Battery (DC)
        /// </summary>
        Battery,
        /// <summary>
        /// Plugged in (AC)
        /// </summary>
        PluggedIn,
        /// <summary>
        /// Unknown
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Battery charge state
    /// </summary>
    public enum BatteryChargeState
    {
        /// <summary>
        /// Normal
        /// </summary>
        Normal,
        /// <summary>
        /// High
        /// </summary>
        High,
        /// <summary>
        /// Low
        /// </summary>
        Low,
        /// <summary>
        /// Critical
        /// </summary>
        Critical,
        /// <summary>
        /// Unknown
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Device state
    /// </summary>
    public enum DeviceState
    {
        /// <summary>
        /// Unknown
        /// </summary>
        Unknown,
        /// <summary>
        /// On
        /// </summary>
        On,
        /// <summary>
        /// Off
        /// </summary>
        Off
    }

    /// <summary>
    /// Provides information about system power status
    /// </summary>
    public class PowerInformation
    {
        /// <summary>
        /// Indicates whether the battery is currently charging.
        /// </summary>
        /// <value><c>true</c> if [battery is charging]; otherwise, <c>false</c>.</value>
        public bool BatteryIsCharging { get; internal set; }

        /// <summary>
        /// Indicates whether a battery is available.
        /// </summary>
        /// <value><c>true</c> if [battery available]; otherwise, <c>false</c>.</value>
        public bool BatteryAvailable { get; internal set; }

        /// <summary>
        /// The state of the battery charge.
        /// </summary>
        /// <value>The state of the battery charge.</value>
        public BatteryChargeState BatteryChargeState { get; internal set; }

        /// <summary>
        /// Power source
        /// </summary>
        /// <value>Source</value>
        public PowerSource Source { get; internal set; }

        /// <summary>
        /// Current power plan (Vista only)
        /// </summary>
        /// <value>The plan.</value>
        public PowerPlan Plan { get; internal set; }

        /// <summary>
        /// Battery percent left.
        /// </summary>
        /// <value>The battery percent left.</value>
        public float BatteryPercentLeft { get; internal set; }

        /// <summary>
        /// Remaining battery lifetime.
        /// </summary>
        /// <value>The remaining battery lifetime.</value>
        /// <remarks>
        /// Depending on the system's hardware, this information may not be available.
        /// In that case, the value of this property is TimeSpan.MinValue.
        /// </remarks>
        public TimeSpan RemainingBatteryLifeTime { get; internal set; }

        /// <summary>
        /// Total battery lifetime.
        /// </summary>
        /// <value>The remaining battery lifetime.</value>
        /// <remarks>
        /// Depending on the system's hardware, this information may not be available.
        /// In that case, the value of this property is TimeSpan.MinValue.
        /// </remarks>
        public TimeSpan TotalBatteryLifeTime { get; internal set; }

        /// <summary>
        /// State of the monitor.
        /// </summary>
        /// <value>The state of the monitor.</value>
        public DeviceState MonitorState { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerInformation"/> class.
        /// </summary>
        public PowerInformation()
        {
            DiskDriveState = DeviceState.Unknown;
            MonitorState = DeviceState.Unknown;
            TotalBatteryLifeTime = TimeSpan.MinValue;
            RemainingBatteryLifeTime = TimeSpan.MinValue;
            BatteryPercentLeft = 1f;
            Plan = PowerPlan.Unknown;
            Source = PowerSource.Unknown;
            BatteryChargeState = BatteryChargeState.Unknown;
        }

        /// <summary>
        /// State of the disk drive.
        /// </summary>
        /// <value>The state of the disk drive.</value>
        public DeviceState DiskDriveState { get; internal set; }
    }

    /// <summary>
    /// Power settings changed
    /// </summary>
    public class PowerChangeEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the power information.
        /// </summary>
        /// <value>The power information.</value>
        public PowerInformation PowerInformation { get; set; }
    }
}
