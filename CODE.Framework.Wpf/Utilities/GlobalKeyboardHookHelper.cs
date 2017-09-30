using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;

namespace CODE.Framework.Wpf.Utilities
{
    /// <summary>
    /// Helper class used for handling global keyboard hooks
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class GlobalKeyboardHookHelper : IDisposable
    {
        /// <summary>
        /// Initializes static members of the <see cref="GlobalKeyboardHookHelper"/> class.
        /// </summary>
        public GlobalKeyboardHookHelper()
        {
            _hookId = InterceptKeys.SetHook(HookCallback);
        }

        private readonly IntPtr _hookId;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code < 0) return InterceptKeys.CallNextHookEx(_hookId, code, wParam, lParam);
            const int WM_KEYDOWN = 0x0100;
            if (wParam != (IntPtr) WM_KEYDOWN) return InterceptKeys.CallNextHookEx(_hookId, code, wParam, lParam);

            var vkCode = Marshal.ReadInt32(lParam);

            var keyDownEvent = KeyDown;
            if (keyDownEvent != null)
                keyDownEvent(null, new RawKeyEventArgs(vkCode, false));
            return InterceptKeys.CallNextHookEx(_hookId, code, wParam, lParam);
        }

        /// <summary>
        /// Occurs when a key is pressed.
        /// </summary>
        public event RawKeyEventHandler KeyDown;

        /// <summary>
        /// Finalizes an instance of the <see cref="GlobalKeyboardHookHelper"/> class.
        /// </summary>
        ~GlobalKeyboardHookHelper()
        {
            Dispose();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            InterceptKeys.UnhookWindowsHookEx(_hookId);
        }
    }

    /// <summary>
    /// This class provides functionality related to keyboard features
    /// </summary>
    public static class KeyboardHelper
    {
        [DllImport("user32.dll")]
        private static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)] StringBuilder pwszBuff, int cchBuff, uint wFlags);

        [DllImport("user32.dll")]
        private static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, MapType uMapType);

        private enum MapType : uint
        {
            MAPVK_VK_TO_VSC = 0x0,
            MAPVK_VSC_TO_VK = 0x1,
            MAPVK_VK_TO_CHAR = 0x2,
            MAPVK_VSC_TO_VK_EX = 0x3,
        }

        /// <summary>
        /// Returns a char value from a Key structure
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>System.Char.</returns>
        public static char GetCharFromKey(Key key)
        {
            var ch = ' ';
            var virtualKey = KeyInterop.VirtualKeyFromKey(key);
            var keyboardState = new byte[256];
            GetKeyboardState(keyboardState);

            var scanCode = MapVirtualKey((uint)virtualKey, MapType.MAPVK_VK_TO_VSC);
            var stringBuilder = new StringBuilder(2);

            var result = ToUnicode((uint)virtualKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);
            switch (result)
            {
                case -1:
                    break;
                case 0:
                    break;
                case 1:
                    {
                        ch = stringBuilder[0];
                        break;
                    }
                default:
                    {
                        ch = stringBuilder[0];
                        break;
                    }
            }
            return ch;
        }
    }

    internal static class InterceptKeys
    {
        private static LowLevelKeyboardProc _proc;

        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            _proc = proc;
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
                return SetWindowsHookEx(13, _proc, GetModuleHandle(curModule.ModuleName), 0);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
    }

    /// <summary>
    /// Raw Keyboard Event Args
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class RawKeyEventArgs : EventArgs
    {
        /// <summary>
        /// Raw Key Code
        /// </summary>
        /// <value>The vk code.</value>
        public int VKCode { get; }
        /// <summary>
        /// Key.
        /// </summary>
        /// <value>The key.</value>
        public Key Key { get; }
        /// <summary>
        /// Gets a value indicating whether this instance is system key.
        /// </summary>
        /// <value><c>true</c> if this instance is system key; otherwise, <c>false</c>.</value>
        public bool IsSystemKey { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RawKeyEventArgs"/> class.
        /// </summary>
        /// <param name="vkCode">The vk code.</param>
        /// <param name="isSystemKey">if set to <c>true</c> [is system key].</param>
        public RawKeyEventArgs(int vkCode, bool isSystemKey)
        {
            VKCode = vkCode;
            IsSystemKey = isSystemKey;
            Key = KeyInterop.KeyFromVirtualKey(vkCode);
        }
    }

    /// <summary>
    /// Event handler signature for global key hook event
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">The <see cref="RawKeyEventArgs"/> instance containing the event data.</param>
    public delegate void RawKeyEventHandler(object sender, RawKeyEventArgs args);
}
