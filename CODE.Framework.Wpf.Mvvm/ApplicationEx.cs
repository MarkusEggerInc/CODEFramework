using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using CODE.Framework.Core.Utilities;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    ///     Extended version of the windows application object
    /// </summary>
    public class ApplicationEx : Application
    {
        /// <summary>
        /// The standard theme features (internal use only)
        /// </summary>
        private static readonly Dictionary<string, StandardThemeFeatureWrapper> StandardThemeFeatures = new Dictionary<string, StandardThemeFeatureWrapper>();
        /// <summary>
        /// Internal list of registered themes
        /// </summary>
        private static readonly Dictionary<string, string> RegisteredThemes = new Dictionary<string, string>();

        /// <summary>
        ///     List of secondary assemblies the current app references.
        /// </summary>
        public static Dictionary<string, Assembly> SecondaryAssemblies = new Dictionary<string, Assembly>();

        private string _oldTheme = string.Empty;
        private bool _startupEventFired;
        private string _theme = string.Empty;

        /// <summary>
        ///     Constructor
        /// </summary>
        public ApplicationEx()
        {
            Startup += (s, e) =>
            {
                _startupEventFired = true;
                LoadTheme();
            };
        }

        /// <summary>
        /// Initializes static members of the <see cref="ApplicationEx"/> class.
        /// </summary>
        static ApplicationEx()
        {
            RegisterTheme("battleship", "pack://application:,,,/CODE.Framework.Wpf.Theme.Battleship;component/ThemeRoot.xaml");
            RegisterTheme("geek", "pack://application:,,,/CODE.Framework.Wpf.Theme.Geek;component/ThemeRoot.xaml");
            RegisterTheme("metro", "pack://application:,,,/CODE.Framework.Wpf.Theme.Metro;component/ThemeRoot.xaml");
            RegisterTheme("newsroom", "pack://application:,,,/CODE.Framework.Wpf.Theme.Newsroom;component/ThemeRoot.xaml");
            RegisterTheme("universe", "pack://application:,,,/CODE.Framework.Wpf.Theme.Universe;component/ThemeRoot.xaml");
            RegisterTheme("vapor", "pack://application:,,,/CODE.Framework.Wpf.Theme.Vapor;component/ThemeRoot.xaml");
            RegisterTheme("workplace", "pack://application:,,,/CODE.Framework.Wpf.Theme.Workplace;component/ThemeRoot.xaml");
            RegisterTheme("wildcat", "pack://application:,,,/CODE.Framework.Wpf.Theme.Wildcat;component/ThemeRoot.xaml");
        }

        /// <summary>Attached property to set application theme</summary>
        /// <remarks>
        ///     Setting an application theme has a number of consequences. For one, the system tried to load built-in themes the
        ///     CODE Framework is aware of. Currently supported are: "Metro", "Battleship".
        ///     In addition, the system will try to load resource dictionaries in Theme folders. For instance, if the theme is set
        ///     to "MyTheme", the system will try to load Themes/MyTheme/MyTheme.xaml resource dictionaries. It will also search in
        ///     Theme/MyTheme, Style/MyTheme, and Styles/MyTheme for MyTheme.xaml.
        /// </remarks>
        public string Theme
        {
            get { return _theme; }
            set
            {
                _theme = value;
                if (_startupEventFired) LoadTheme(); // It does not make sense to do this earlier, since it would likely get overridden anyway and just waste time
            }
        }

        /// <summary>
        ///     Forces a re-load of the current theme
        /// </summary>
        public void LoadTheme()
        {
            if (string.IsNullOrEmpty(Theme)) return;

            var del = BeforeThemeSwitch;
            if (del != null) del.Invoke(this, new ThemeSwitchEventArgs {NewTheme = _theme, OldTheme = _oldTheme});

            // Unloading resources we do not need anymore
            for (var counter = Resources.MergedDictionaries.Count - 1; counter >= 0; counter--)
            {
                var dictionary = Resources.MergedDictionaries[counter];
                if (dictionary == null || dictionary.Source == null) continue;
                if (dictionary.Source.IsAbsoluteUri)
                {
                    if (dictionary.Source.AbsolutePath.EndsWith("component/ThemeRoot.xaml"))
                        Resources.MergedDictionaries.RemoveAt(counter);
                }
                else
                {
                    if (string.IsNullOrEmpty(_oldTheme) || string.IsNullOrEmpty(dictionary.Source.OriginalString)) continue;
                    if (dictionary.Source.OriginalString == "/theme/" + _oldTheme + "/" + _oldTheme + ".xaml")
                        Resources.MergedDictionaries.RemoveAt(counter);
                    else if (dictionary.Source.OriginalString == "/themes/" + _oldTheme + "/" + _oldTheme + ".xaml")
                        Resources.MergedDictionaries.RemoveAt(counter);
                    else if (dictionary.Source.OriginalString == "/style/" + _oldTheme + "/" + _oldTheme + ".xaml")
                        Resources.MergedDictionaries.RemoveAt(counter);
                    else if (dictionary.Source.OriginalString == "/styles/" + _oldTheme + "/" + _oldTheme + ".xaml")
                        Resources.MergedDictionaries.RemoveAt(counter);
                }
            }

            // Loading standard supported themes
            var themeName = Theme.ToLowerInvariant();
            if (RegisteredThemes.ContainsKey(themeName))
                Resources.MergedDictionaries.Add(new ResourceDictionary {Source = new Uri(RegisteredThemes[themeName], UriKind.Absolute)});

            // Loading potential secondary themes (and stop after we find the first one)
            TryLoadAndMergeResourceDictionary("/theme/" + Theme + "/" + Theme + ".xaml");
            TryLoadAndMergeResourceDictionary("/themes/" + Theme + "/" + Theme + ".xaml");
            TryLoadAndMergeResourceDictionary("/style/" + Theme + "/" + Theme + ".xaml");
            TryLoadAndMergeResourceDictionary("/styles/" + Theme + "/" + Theme + ".xaml");

            // We also go through all open views and change styles if need be
            foreach (var engine in Controller.RegisteredViewEngines)
            {
                var themeEngine = engine as IThemableViewEngine;
                if (themeEngine == null) continue;
                var handlers = Controller.GetRegisteredViewHandlers();
                foreach (var handler in handlers)
                {
                    var shell = handler as Shell;
                    if (shell == null) continue;
                    foreach (var result in shell.TopLevelViews)
                        themeEngine.ChangeViewTheme(result.View, Theme, _oldTheme);
                    foreach (var result in shell.NormalViews)
                        themeEngine.ChangeViewTheme(result.View, Theme, _oldTheme);
                }
                break;
            }

            var del2 = ThemeSwitched;
            if (del2 != null) del2.Invoke(this, new ThemeSwitchEventArgs { NewTheme = _theme, OldTheme = _oldTheme });

            _oldTheme = Theme;
        }

        /// <summary>
        /// Registers a new theme.
        /// </summary>
        /// <param name="themeName">Name of the theme.</param>
        /// <param name="absoluteResourceDictionarySourceUrl">The absolute resource dictionary source URL.</param>
        /// <example>ApplicationEx.RegisterTheme("RedTheme", "pack://application:,,,/RedThemeAssembly;component/ThemeRoot.xaml");</example>
        public static void RegisterTheme(string themeName, string absoluteResourceDictionarySourceUrl)
        {
            themeName = themeName.ToLowerInvariant();
            if (RegisteredThemes.ContainsKey(themeName))
                RegisteredThemes[themeName] = absoluteResourceDictionarySourceUrl;
            else
                RegisteredThemes.Add(themeName, absoluteResourceDictionarySourceUrl);
        }

        /// <summary>
        ///     Attempts to load the specified resource dictionary from the specified location
        /// </summary>
        /// <param name="searchPath">Search path</param>
        /// <returns>True if found</returns>
        protected virtual bool TryLoadAndMergeResourceDictionary(string searchPath)
        {
            try
            {
                var source = new Uri(searchPath, UriKind.Relative);
                var dictionary = LoadComponent(source) as ResourceDictionary;
                if (dictionary != null)
                {
                    dictionary.Source = source;
                    Resources.MergedDictionaries.Add(dictionary);
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }

        /// <summary>Returns a theme feature object for internal use</summary>
        /// <returns>IThemeStandardFeatures implementation specific to the provided theme</returns>
        public static IThemeStandardFeatures GetStandardThemeFeatures()
        {
            try
            {
                var appEx = Current as ApplicationEx;
                if (appEx == null) return null;
                var key = appEx.Theme;

                if (!StandardThemeFeatures.ContainsKey(key))
                {
                    var implementationType = appEx.TryFindResource("ThemeStandardFeaturesType").ToString();
                    var implementationAssembly = appEx.TryFindResource("ThemeStandardFeaturesAssembly").ToString();

                    var wrapper = new StandardThemeFeatureWrapper();
                    try
                    {
                        wrapper.StandardFeatures = ObjectHelper.CreateObject(implementationType, implementationAssembly) as IThemeStandardFeatures;
                        wrapper.ThemeFeaturesFound = wrapper.StandardFeatures != null;
                    }
                    catch (Exception)
                    {
                        wrapper.ThemeFeaturesFound = false;
                    }
                    StandardThemeFeatures.Add(key, wrapper);
                }

                if (StandardThemeFeatures[key].ThemeFeaturesFound)
                    return StandardThemeFeatures[key].StandardFeatures;
            }
            catch (Exception)
            {
                return null;
            }
            return null;
        }

        /// <summary>
        ///     Registers a secondary assembly so the elements within that assembly are found as if they were in the local assembly
        /// </summary>
        /// <param name="assemblyString">The assembly string (assembly name).</param>
        public static void RegisterSecondaryAssembly(string assemblyString)
        {
            try
            {
                // We simply make sure the secondary assembly is being loaded into the current app domain.
                // Note that just adding a reference to the assembly is not enough, since likely, no types are referenced
                // from that assembly, and hence it would not get loaded due to optimizations .NET applies
                var existingAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == assemblyString);
                if (existingAssembly == null)
                    SecondaryAssemblies.Add(assemblyString, AppDomain.CurrentDomain.Load(assemblyString));
                else if (!SecondaryAssemblies.ContainsKey(assemblyString))
                    SecondaryAssemblies.Add(assemblyString, existingAssembly);
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to reference secondary assembly '" + assemblyString + "'. Make sure you specify a valid assembly name (compatible with AppDomain.Load(assemblyString).", ex);
            }
        }

        private class StandardThemeFeatureWrapper
        {
            public bool ThemeFeaturesFound { get; set; }
            public IThemeStandardFeatures StandardFeatures { get; set; }
        }

        /// <summary>
        /// Fires just before a theme switching operation is initialized
        /// </summary>
        public event EventHandler<ThemeSwitchEventArgs> BeforeThemeSwitch;

        /// <summary>
        /// Fires just after a theme switching operation is completed
        /// </summary>
        public event EventHandler<ThemeSwitchEventArgs> ThemeSwitched;
    }

    /// <summary>
    /// Event args for theme switching events
    /// </summary>
    public class ThemeSwitchEventArgs : EventArgs
    {
        /// <summary>
        /// Old theme name.
        /// </summary>
        /// <value>The old theme.</value>
        public string OldTheme { get; set; }
        /// <summary>
        /// New theme name.
        /// </summary>
        /// <value>The new theme.</value>
        public string NewTheme { get; set; }
    }
}