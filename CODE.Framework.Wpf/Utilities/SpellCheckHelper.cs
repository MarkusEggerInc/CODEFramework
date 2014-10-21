using System;
using System.IO;
using CODE.Framework.Core.Configuration;

namespace CODE.Framework.Wpf.Utilities
{
    /// <summary>
    /// Spell check helper class
    /// </summary>
    public static class SpellCheckHelper
    {
        ///<summary>Typically \My Documents\Custom Dictionaries\. Can be overridden in app.Config with CustomDictionaryPath appSetting</summary>
        public static string GetCustomDictionaryPath()
        {
            return ConfigurationSettings.Settings.IsSettingSupported("CustomDictionaryPath") ? ConfigurationSettings.Settings["CustomDictionaryPath"] : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Custom Dictionaries");
        }

        ///<summary>Returns the fully qualified file name of the UserDictionary.lex file</summary>
        public static string GetCustomDictionaryFile(string customDictionaryPath)
        {
            return Path.Combine(customDictionaryPath, "UserDictionary.lex");
        }

        ///<summary>Returns the fully qualified file name of the IgnoreAllDictionary.lex file</summary>
        public static string GetIgnoreAllDictionaryFile(string customDictionaryPath)
        {
            return Path.Combine(customDictionaryPath, "IgnoreAllDictionary.lex");
        }
    }
}
