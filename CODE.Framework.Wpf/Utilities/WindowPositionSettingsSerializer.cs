using System;
using System.Text;
using System.Windows;
using CODE.Framework.Core.Newtonsoft;
using CODE.Framework.Core.Utilities;

namespace CODE.Framework.Wpf.Utilities
{
    /// <summary>
    /// This serializer handles storing window position information
    /// </summary>
    /// <seealso cref="CODE.Framework.Core.Utilities.ISettingsSerializer" />
    public class WindowPositionSettingsSerializer : ISettingsSerializer
    {
        /// <summary>
        /// Serializes the state object and returns the state as JSON
        /// </summary>
        /// <param name="stateObject">Object to serialize</param>
        /// <returns>State JSON</returns>
        public virtual string SerializeToJson(object stateObject)
        {
            try
            {
                var window = stateObject as Window;
                if (window == null) return string.Empty;

                var jsonBuilder = new JsonBuilder();
                jsonBuilder.Append("Top", window.Top);
                jsonBuilder.Append("Left", window.Left);
                jsonBuilder.Append("Height", window.Height);
                jsonBuilder.Append("Width", window.Width);
                jsonBuilder.Append("WindowState", window.WindowState);
                jsonBuilder.Append("Timestamp", DateTime.Now);
                var json = jsonBuilder.ToString();
                return json;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Deserializes the provides JSON state and updates the state object with the settings
        /// </summary>
        /// <param name="stateObject">Object to set the persisted state on.</param>
        /// <param name="state">State information (JSON)</param>
        public virtual void DeserializeFromJson(object stateObject, string state)
        {
            try
            {
                var window = stateObject as Window;
                if (window == null) return;
                if (string.IsNullOrEmpty(state)) return;
                JsonHelper.QuickParse(state, (n, v) =>
                {
                    switch (n)
                    {
                        case "Top":
                            SafeDoubleParse(v, top => { window.Top = top; });
                            break;
                        case "Left":
                            SafeDoubleParse(v, left => { window.Left = left; });
                            break;
                        case "Width":
                            SafeDoubleParse(v, width =>
                            {
                                if (!double.IsNaN(window.MinWidth) && window.MinWidth > width)
                                    width = window.MinWidth;
                                window.Width = width;
                            });
                            break;
                        case "Height":
                            SafeDoubleParse(v, height =>
                            {
                                if (!double.IsNaN(window.MinHeight) && window.MinHeight > height)
                                    height = window.MinHeight;
                                window.Height = height;
                            });
                            break;
                    }
                });

                // Making sure the window state is set last so it doesn't maximize in an odd location
                JsonHelper.QuickParse(state, (n, v) =>
                {
                    switch (n)
                    {
                        case "WindowState":
                            switch (v)
                            {
                                case "Normal":
                                case "0":
                                    window.WindowState = WindowState.Normal;
                                    break;
                                case "Minimized":
                                case "1":
                                    window.WindowState = WindowState.Minimized;
                                    break;
                                case "Maximized":
                                case "2":
                                    window.WindowState = WindowState.Maximized;
                                    break;
                            }
                            break;
                    }
                });

                VerifyWindowIsInVisibleRange(window);
            }
            catch
            {
                // Bummer, but nothing we can do
            }
        }

        /// <summary>
        /// Verifies the window is in visible range.
        /// </summary>
        /// <param name="window">The window.</param>
        /// <remarks>
        /// This method performs a simple check to make sure the window isn't completely off screen.
        /// This method works reasonably well, but it is not designed to work with odd irregular screen setups.
        /// </remarks>
        protected virtual void VerifyWindowIsInVisibleRange(Window window)
        {
            if (window.Left + window.Width <= SystemParameters.VirtualScreenLeft)
                // Window is off screen to the left
                window.Left = SystemParameters.VirtualScreenLeft;

            if (window.Top <= SystemParameters.VirtualScreenTop)
                // Window title is off screen to the top
                window.Top = SystemParameters.VirtualScreenTop;

            if (SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth <= window.Left)
                // Window is off screen to the right
                window.Left = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - window.Width;

            if (SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight <= window.Top)
                // Window is off screen to the bottom
                window.Top = SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - window.Height;
        }

        private static void SafeDoubleParse(string value, Action<double> callback)
        {
            double realValue;
            if (double.TryParse(value, out realValue))
                callback(realValue);
        }

        /// <summary>
        /// Returns true if the provided serializer can handle the object in question
        /// </summary>
        /// <param name="stateObject">Object containing the state</param>
        /// <param name="id">ID of the setting that is to be persisted</param>
        /// <param name="scope">Scope of the setting</param>
        /// <returns>True if the serializer can handle the provided object</returns>
        public virtual bool CanHandle(object stateObject, string id, SettingScope scope)
        {
            return stateObject is Window;
        }

        /// <summary>
        /// Can be used to suggest a file name for the setting, in case the handler is file-based
        /// </summary>
        /// <param name="stateObject">Object containing the state</param>
        /// <param name="id">ID of the setting that is to be persisted</param>
        /// <param name="scope">Scope of the setting</param>
        /// <returns>File name, or string.Empty if no default is suggested</returns>
        public virtual string GetSuggestedFileName(object stateObject, string id, SettingScope scope)
        {
            return id + ".WindowPosition.json";
        }

        /// <summary>
        /// If set to true, this serializer will be invoked, even if other serializers have already
        /// handled the process
        /// </summary>
        /// <value>True or False</value>
        public virtual bool UseInAdditionToOtherAppliedSerializers
        {
            get { return true; }
        }
    }
}
