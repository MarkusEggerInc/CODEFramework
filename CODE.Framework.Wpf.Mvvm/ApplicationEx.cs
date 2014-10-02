using System.Collections.Generic;
using System.Windows;
using System;
using CODE.Framework.Core.Utilities;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// Extended version of the windows application object
    /// </summary>
    public class ApplicationEx : Application
    {
        private bool _startupEventFired = false;
        /// <summary>
        /// Constructor
        /// </summary>
        public ApplicationEx()
        {
            Startup += (s, e) =>
                           {
                               _startupEventFired = true;
                               LoadTheme();
                           };
        }

        private string _theme = "";
        private string _oldTheme = "";

        /// <summary>Attached property to set application theme</summary>
        /// <remarks>
        /// Setting an application theme has a number of consequences. For one, the system tried to load built-in themes the CODE Framework is aware of. Currently supported are: "Metro", "Battleship".
        /// In addition, the system will try to load resource dictionaries in Theme folders. For instance, if the theme is set to "MyTheme", the system will try to load Themes/MyTheme/MyTheme.xaml resource dictionaries. It will also search in Theme/MyTheme, Style/MyTheme, and Styles/MyTheme for MyTheme.xaml.
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
        /// Forces a re-load of the current theme
        /// </summary>
        public void LoadTheme()
        {
            if (string.IsNullOrEmpty(Theme)) return;

            // Unloading resources we do not need anymore
            for (int counter = Resources.MergedDictionaries.Count - 1; counter >= 0; counter--)
            {
                var dictionary = Resources.MergedDictionaries[counter];
                if (dictionary != null && dictionary.Source != null)
                {
                    if (dictionary.Source.IsAbsoluteUri)
                    {
                        if (dictionary.Source.AbsolutePath.EndsWith("component/ThemeRoot.xaml"))
                            Resources.MergedDictionaries.RemoveAt(counter);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(_oldTheme) && !string.IsNullOrEmpty(dictionary.Source.OriginalString))
                        {
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
                }
            }

            // Loading standard supported themes
            switch (Theme.ToLower())
            {
                case "battleship":
                    Resources.MergedDictionaries.Add(new ResourceDictionary {Source = new Uri("pack://application:,,,/CODE.Framework.Wpf.Theme.Battleship;component/ThemeRoot.xaml", UriKind.Absolute)});
                    break;
                case "geek":
                    Resources.MergedDictionaries.Add(new ResourceDictionary {Source = new Uri("pack://application:,,,/CODE.Framework.Wpf.Theme.Geek;component/ThemeRoot.xaml", UriKind.Absolute)});
                    break;
                case "metro":
                    Resources.MergedDictionaries.Add(new ResourceDictionary {Source = new Uri("pack://application:,,,/CODE.Framework.Wpf.Theme.Metro;component/ThemeRoot.xaml", UriKind.Absolute)});
                    break;
                case "vapor":
                    Resources.MergedDictionaries.Add(new ResourceDictionary {Source = new Uri("pack://application:,,,/CODE.Framework.Wpf.Theme.Vapor;component/ThemeRoot.xaml", UriKind.Absolute)});
                    break;
                case "workplace":
                    Resources.MergedDictionaries.Add(new ResourceDictionary {Source = new Uri("pack://application:,,,/CODE.Framework.Wpf.Theme.Workplace;component/ThemeRoot.xaml", UriKind.Absolute)});
                    break;
                case "wildcat":
                    Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/CODE.Framework.Wpf.Theme.Wildcat;component/ThemeRoot.xaml", UriKind.Absolute) });
                    break;
            }

            // Loading potential secondary themes (and stop after we find the first one)
            TryLoadAndMergeResourceDictionary("/theme/" + Theme + "/" + Theme + ".xaml");
            TryLoadAndMergeResourceDictionary("/themes/" + Theme + "/" + Theme + ".xaml");
            TryLoadAndMergeResourceDictionary("/style/" + Theme + "/" + Theme + ".xaml");
            TryLoadAndMergeResourceDictionary("/styles/" + Theme + "/" + Theme + ".xaml");

            // We also go through all open views and change styles if need be
            foreach (var engine in Controller.RegisteredViewEngines)
            {
                var themeEngine = engine as IThemableViewEngine;
                if (themeEngine != null)
                {
                    var handlers = Controller.GetRegisteredViewHandlers();
                    foreach (var handler in handlers)
                    {
                        var shell = handler as Shell;
                        if (shell != null)
                        {
                            foreach (var result in shell.TopLevelViews)
                                themeEngine.ChangeViewTheme(result.View, Theme, _oldTheme);
                            foreach (var result in shell.NormalViews)
                                themeEngine.ChangeViewTheme(result.View, Theme, _oldTheme);
                        }
                    }
                    break;
                }
            }

            _oldTheme = Theme;
        }

        /// <summary>
        /// Attempts to load the specified resource dictionary from the specified location
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
            catch { }
            return false;
        }

        /// <summary>Returns a theme feature object for internal use</summary>
        /// <returns>IThemeStandardFeatures implementation specific to the provided theme</returns>
        public static IThemeStandardFeatures GetStandardThemeFeatures()
        {
            try
            {
                var appex = Current as ApplicationEx;
                if (appex == null) return null;
                var key = appex.Theme;

                if (!StandardThemeFeatures.ContainsKey(key))
                {
                    var implementationType = appex.TryFindResource("ThemeStandardFeaturesType").ToString();
                    var implementationAssembly = appex.TryFindResource("ThemeStandardFeaturesAssembly").ToString();

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

        private static readonly Dictionary<string, StandardThemeFeatureWrapper> StandardThemeFeatures = new Dictionary<string, StandardThemeFeatureWrapper>();

        private class StandardThemeFeatureWrapper
        {
            public bool ThemeFeaturesFound { get; set; }
            public IThemeStandardFeatures StandardFeatures { get; set; }
        }
    }
}
