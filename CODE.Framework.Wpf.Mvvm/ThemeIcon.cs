using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// Displays an icon control and is aware of theme specific icons (and switches icons as themes switch)
    /// </summary>
    public class ThemeIcon : Panel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThemeIcon"/> class.
        /// </summary>
        public ThemeIcon()
        {
            var appEx = Application.Current as ApplicationEx;
            if (appEx != null)
                appEx.ThemeSwitched += (s, e) => LoadIcon();

            Loaded += (s, e) => LoadIcon();
        }

        /// <summary>
        /// Loads (or re-loads) the icon from resources
        /// </summary>
        private void LoadIcon()
        {
            if (string.IsNullOrEmpty(IconResourceKey) && string.IsNullOrEmpty(FallbackIconResourceKey))
            {
                Background = null;
                return;
            }

            if (IconResourceKey == null) IconResourceKey = string.Empty;
            var resourceNameUsed = IconResourceKey;
            var rawResource = TryFindResource(IconResourceKey);
            if (rawResource == null && UseFallbackIcon && !string.IsNullOrEmpty(FallbackIconResourceKey))
            {
                resourceNameUsed = FallbackIconResourceKey;
                rawResource = TryFindResource(FallbackIconResourceKey);
                if (rawResource == null)
                    rawResource = Application.Current.TryFindResource(FallbackIconResourceKey);
            }
            if (rawResource == null)
            {
                Background = null;
                return;
            }

            var brushResource = rawResource as Brush;
            if (brushResource == null)
                throw new ArgumentException("Icon resource '" + resourceNameUsed + "' is not a Brush resource.");

            if (ReplacementBrushes != null)
            {
                var drawingBrush = brushResource as DrawingBrush;
                if (drawingBrush != null)
                {
                    var replacementBrushes = new Dictionary<object, Brush>();
                    foreach (var resourceKey in ReplacementBrushes.Keys)
                    {
                        var resourceBrush = ReplacementBrushes[resourceKey] as Brush;
                        if (resourceBrush != null)
                            replacementBrushes.Add(resourceKey, resourceBrush);
                    }
                    if (replacementBrushes.Count > 0)
                    {
                        var brush2 = drawingBrush.Clone();
                        ResourceHelper.ReplaceDynamicDrawingBrushResources(brush2, replacementBrushes);
                        brushResource = brush2;
                    }
                }
            }

            Background = brushResource;
        }

        /// <summary>
        /// Defines the resource key (name) of the icon that is to be loaded
        /// </summary>
        /// <value>The icon resource key.</value>
        /// <remarks>
        /// While this isn't strictly a requirement, the resource name of the icon is designed to be theme specific, and the icon will be reloaded when the theme switches.
        /// </remarks>
        public string IconResourceKey
        {
            get { return (string)GetValue(IconResourceKeyProperty); }
            set { SetValue(IconResourceKeyProperty, value); }
        }
        /// <summary>
        /// Defines the resource key (name) of the icon that is to be loaded
        /// </summary>
        public static readonly DependencyProperty IconResourceKeyProperty = DependencyProperty.Register("IconResourceKey", typeof(string), typeof(ThemeIcon), new PropertyMetadata("", OnIconResourceKeyChanged));

        /// <summary>
        /// Fires when the icon resource changes
        /// </summary>
        /// <param name="d">The icon object</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnIconResourceKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var icon = d as ThemeIcon;
            if (icon == null) return;
            icon.LoadIcon();
        }

        /// <summary>
        /// Defines a standard icon to be used
        /// </summary>
        /// <value>The standard icon.</value>
        /// <remarks>This automatically sets the associated icon resource key</remarks>
        public StandardIcons StandardIcon
        {
            get { return (StandardIcons)GetValue(StandardIconProperty); }
            set { SetValue(StandardIconProperty, value); }
        }
        /// <summary>
        /// Defines a standard icon to be used
        /// </summary>
        /// <value>The standard icon.</value>
        /// <remarks>This automatically sets the associated icon resource key</remarks>
        public static readonly DependencyProperty StandardIconProperty = DependencyProperty.Register("StandardIcon", typeof(StandardIcons), typeof(ThemeIcon), new PropertyMetadata(StandardIcons.None, OnStandardIconChanged));

        /// <summary>
        /// Handles the StandardIconChanged event event.
        /// </summary>
        /// <param name="d">The object the icon is set on.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnStandardIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var icon = d as ThemeIcon;
            if (icon == null) return;
            icon.IconResourceKey = StandardIconHelper.GetStandardIconKeyFromEnum(icon.StandardIcon);
        }

        /// <summary>
        /// The fallback icon resource key is used when the references icon is not found (perhaps because it isn't available in the current theme)
        /// </summary>
        /// <value>The fallback icon resource key.</value>
        public string FallbackIconResourceKey
        {
            get { return (string)GetValue(FallbackIconResourceKeyProperty); }
            set { SetValue(FallbackIconResourceKeyProperty, value); }
        }
        /// <summary>
        /// The fallback icon resource key is used when the references icon is not found (perhaps because it isn't available in the current theme)
        /// </summary>
        public static readonly DependencyProperty FallbackIconResourceKeyProperty = DependencyProperty.Register("FallbackIconResourceKey", typeof(string), typeof(ThemeIcon), new PropertyMetadata("CODE.Framework-Icon-MissingIcon", OnFallbackIconResourceKeyChanged));

        /// <summary>
        /// Fires when the fallback icon resource key changes.
        /// </summary>
        /// <param name="d">The icon object</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnFallbackIconResourceKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var icon = d as ThemeIcon;
            if (icon == null) return;
            icon.LoadIcon();
        }

        /// <summary>
        /// Defines whether the fallback icon setting should be used (true) or ignored (false)
        /// </summary>
        /// <value>True if fallback is to be used</value>
        public bool UseFallbackIcon
        {
            get { return (bool)GetValue(UseFallbackIconProperty); }
            set { SetValue(UseFallbackIconProperty, value); }
        }
        /// <summary>
        /// Defines whether the fallback icon setting should be used (true) or ignored (false)
        /// </summary>
        public static readonly DependencyProperty UseFallbackIconProperty = DependencyProperty.Register("UseFallbackIcon", typeof(bool), typeof(ThemeIcon), new PropertyMetadata(true, OnUseFallbackIconChanged));

        /// <summary>
        /// Handles the UseFallbackIconChanged event
        /// </summary>
        /// <param name="d">The object the setting was set on</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnUseFallbackIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var icon = d as ThemeIcon;
            if (icon == null) return;
            icon.LoadIcon();
        }

        /// <summary>
        /// If set, this can be a dictionary of brushes that replace brushes of the same resource name within the icon
        /// </summary>
        /// <value>The replacement brushes.</value>
        public ObservableResourceDictionary ReplacementBrushes
        {
            get { return (ObservableResourceDictionary)GetValue(ReplacementBrushesProperty); }
            set { SetValue(ReplacementBrushesProperty, value); }
        }
        /// <summary>
        /// If set, this can be a dictionary of brushes that replace brushes of the same resource name within the icon
        /// </summary>
        public static readonly DependencyProperty ReplacementBrushesProperty = DependencyProperty.Register("ReplacementBrushes", typeof(ObservableResourceDictionary), typeof(ThemeIcon), new PropertyMetadata(null, OnReplacementBrushesChanged));

        /// <summary>
        /// Handles the OnReplacementBrushesChanged event
        /// </summary>
        /// <param name="d">The object the setting was set on</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnReplacementBrushesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var icon = d as ThemeIcon;
            if (icon == null) return;

            var dictionary = e.NewValue as ObservableResourceDictionary;
            if (dictionary != null)
                dictionary.CollectionChanged += (s, e2) => icon.LoadIcon();

            icon.LoadIcon();
        }
    }

    /// <summary>
    /// Helper features related to standard icons
    /// </summary>
    public static class StandardIconHelper
    {
        /// <summary>
        /// For internal use only
        /// </summary>
        private static readonly Dictionary<string, StandardIcons> StandardIconMapsBackward = new Dictionary<string, StandardIcons>();
        /// <summary>
        /// For internal use only
        /// </summary>
        private static readonly Dictionary<StandardIcons, string> StandardIconMaps = new Dictionary<StandardIcons, string>();

        /// <summary>
        /// Initializes static members of the <see cref="StandardIconHelper"/> class.
        /// </summary>
        static StandardIconHelper()
        {
            StandardIconMaps.Add(StandardIcons.Accounts, "CODE.Framework-Icon-Accounts");
            StandardIconMaps.Add(StandardIcons.Add, "CODE.Framework-Icon-Add");
            StandardIconMaps.Add(StandardIcons.Admin, "CODE.Framework-Icon-Admin");
            StandardIconMaps.Add(StandardIcons.AlignCenter, "CODE.Framework-Icon-AlignCenter");
            StandardIconMaps.Add(StandardIcons.AlignLeft, "CODE.Framework-Icon-AlignLeft");
            StandardIconMaps.Add(StandardIcons.AlignRight, "CODE.Framework-Icon-AlignRight");
            StandardIconMaps.Add(StandardIcons.ArrowDown, "CODE.Framework-Icon-ArrowDown");
            StandardIconMaps.Add(StandardIcons.ArrowDownLeft, "CODE.Framework-Icon-ArrowDownLeft");
            StandardIconMaps.Add(StandardIcons.ArrowDownRight, "CODE.Framework-Icon-ArrowDownRight");
            StandardIconMaps.Add(StandardIcons.ArrowLeft, "CODE.Framework-Icon-ArrowLeft");
            StandardIconMaps.Add(StandardIcons.ArrowRight, "CODE.Framework-Icon-ArrowRight");
            StandardIconMaps.Add(StandardIcons.ArrowUp, "CODE.Framework-Icon-ArrowUp");
            StandardIconMaps.Add(StandardIcons.ArrowUpLeft, "CODE.Framework-Icon-ArrowUpLeft");
            StandardIconMaps.Add(StandardIcons.ArrowUpRight, "CODE.Framework-Icon-ArrowUpRight");
            StandardIconMaps.Add(StandardIcons.Attach, "CODE.Framework-Icon-Attach");
            StandardIconMaps.Add(StandardIcons.AttachCamera, "CODE.Framework-Icon-AttachCamera");
            StandardIconMaps.Add(StandardIcons.Audio, "CODE.Framework-Icon-Audio");
            StandardIconMaps.Add(StandardIcons.Bold, "CODE.Framework-Icon-Bold");
            StandardIconMaps.Add(StandardIcons.Bookmarks, "CODE.Framework-Icon-Bookmarks");
            StandardIconMaps.Add(StandardIcons.BrowsePhotos, "CODE.Framework-Icon-BrowsePhotos");
            StandardIconMaps.Add(StandardIcons.Bullets, "CODE.Framework-Icon-Bullets");
            StandardIconMaps.Add(StandardIcons.Calendar, "CODE.Framework-Icon-Calendar");
            StandardIconMaps.Add(StandardIcons.Caption, "CODE.Framework-Icon-Caption");
            StandardIconMaps.Add(StandardIcons.Cc, "CODE.Framework-Icon-Cc");
            StandardIconMaps.Add(StandardIcons.Characters, "CODE.Framework-Icon-Characters");
            StandardIconMaps.Add(StandardIcons.Clock, "CODE.Framework-Icon-Clock");
            StandardIconMaps.Add(StandardIcons.ClosePane, "CODE.Framework-Icon-ClosePane");
            StandardIconMaps.Add(StandardIcons.Collapsed, "CODE.Framework-Icon-Collapsed");
            StandardIconMaps.Add(StandardIcons.Comment, "CODE.Framework-Icon-Comment");
            StandardIconMaps.Add(StandardIcons.Contact, "CODE.Framework-Icon-Contact");
            StandardIconMaps.Add(StandardIcons.Contact2, "CODE.Framework-Icon-Contact2");
            StandardIconMaps.Add(StandardIcons.ContactInfo, "CODE.Framework-Icon-ContactInfo");
            StandardIconMaps.Add(StandardIcons.Copy, "CODE.Framework-Icon-Copy");
            StandardIconMaps.Add(StandardIcons.Crop, "CODE.Framework-Icon-Crop");
            StandardIconMaps.Add(StandardIcons.Cut, "CODE.Framework-Icon-Cut");
            StandardIconMaps.Add(StandardIcons.Data, "CODE.Framework-Icon-Data");
            StandardIconMaps.Add(StandardIcons.Data2, "CODE.Framework-Icon-Data2");
            StandardIconMaps.Add(StandardIcons.Data3, "CODE.Framework-Icon-Data3");
            StandardIconMaps.Add(StandardIcons.Day, "CODE.Framework-Icon-Day");
            StandardIconMaps.Add(StandardIcons.DisableUpdates, "CODE.Framework-Icon-DisableUpdates");
            StandardIconMaps.Add(StandardIcons.Discard, "CODE.Framework-Icon-Discard");
            StandardIconMaps.Add(StandardIcons.Dislike, "CODE.Framework-Icon-Dislike");
            StandardIconMaps.Add(StandardIcons.DockBottom, "CODE.Framework-Icon-DockBottom");
            StandardIconMaps.Add(StandardIcons.DockLeft, "CODE.Framework-Icon-DockLeft");
            StandardIconMaps.Add(StandardIcons.DockRight, "CODE.Framework-Icon-DockRight");
            StandardIconMaps.Add(StandardIcons.Document, "CODE.Framework-Icon-Document");
            StandardIconMaps.Add(StandardIcons.Download, "CODE.Framework-Icon-Download");
            StandardIconMaps.Add(StandardIcons.Edit, "CODE.Framework-Icon-Edit");
            StandardIconMaps.Add(StandardIcons.Emoji, "CODE.Framework-Icon-Emoji");
            StandardIconMaps.Add(StandardIcons.Emoji2, "CODE.Framework-Icon-Emoji2");
            StandardIconMaps.Add(StandardIcons.Expanded, "CODE.Framework-Icon-Expanded");
            StandardIconMaps.Add(StandardIcons.Favorite, "CODE.Framework-Icon-Favorite");
            StandardIconMaps.Add(StandardIcons.Filter, "CODE.Framework-Icon-Filter");
            StandardIconMaps.Add(StandardIcons.Flag, "CODE.Framework-Icon-Flag");
            StandardIconMaps.Add(StandardIcons.Folder, "CODE.Framework-Icon-Folder");
            StandardIconMaps.Add(StandardIcons.Font, "CODE.Framework-Icon-Font");
            StandardIconMaps.Add(StandardIcons.FontColor, "CODE.Framework-Icon-FontColor");
            StandardIconMaps.Add(StandardIcons.Globe, "CODE.Framework-Icon-Globe");
            StandardIconMaps.Add(StandardIcons.Go, "CODE.Framework-Icon-Go");
            StandardIconMaps.Add(StandardIcons.HangUp, "CODE.Framework-Icon-HangUp");
            StandardIconMaps.Add(StandardIcons.Help, "CODE.Framework-Icon-Help");
            StandardIconMaps.Add(StandardIcons.HideBcc, "CODE.Framework-Icon-HideBcc");
            StandardIconMaps.Add(StandardIcons.Highlight, "CODE.Framework-Icon-Highlight");
            StandardIconMaps.Add(StandardIcons.Home, "CODE.Framework-Icon-Home");
            StandardIconMaps.Add(StandardIcons.Import, "CODE.Framework-Icon-Import");
            StandardIconMaps.Add(StandardIcons.ImportAll, "CODE.Framework-Icon-ImportAll");
            StandardIconMaps.Add(StandardIcons.Important, "CODE.Framework-Icon-Important");
            StandardIconMaps.Add(StandardIcons.Italic, "CODE.Framework-Icon-Italic");
            StandardIconMaps.Add(StandardIcons.Keyboard, "CODE.Framework-Icon-Keyboard");
            StandardIconMaps.Add(StandardIcons.Like, "CODE.Framework-Icon-Like");
            StandardIconMaps.Add(StandardIcons.LikeDislike, "CODE.Framework-Icon-LikeDislike");
            StandardIconMaps.Add(StandardIcons.Link, "CODE.Framework-Icon-Link");
            StandardIconMaps.Add(StandardIcons.List, "CODE.Framework-Icon-List");
            StandardIconMaps.Add(StandardIcons.Login, "CODE.Framework-Icon-Login");
            StandardIconMaps.Add(StandardIcons.Mail, "CODE.Framework-Icon-Mail");
            StandardIconMaps.Add(StandardIcons.Mail2, "CODE.Framework-Icon-Mail2");
            StandardIconMaps.Add(StandardIcons.MailForward, "CODE.Framework-Icon-MailForward");
            StandardIconMaps.Add(StandardIcons.MailReply, "CODE.Framework-Icon-MailReply");
            StandardIconMaps.Add(StandardIcons.MailReplyAll, "CODE.Framework-Icon-MailReplyAll");
            StandardIconMaps.Add(StandardIcons.MapPin, "CODE.Framework-Icon-MapPin");
            StandardIconMaps.Add(StandardIcons.Menu, "CODE.Framework-Icon-Menu");
            StandardIconMaps.Add(StandardIcons.Message, "CODE.Framework-Icon-Message");
            StandardIconMaps.Add(StandardIcons.MissingIcon, "CODE.Framework-Icon-MissingIcon");
            StandardIconMaps.Add(StandardIcons.Money, "CODE.Framework-Icon-Money");
            StandardIconMaps.Add(StandardIcons.More, "CODE.Framework-Icon-More");
            StandardIconMaps.Add(StandardIcons.MoveToFolder, "CODE.Framework-Icon-MoveToFolder");
            StandardIconMaps.Add(StandardIcons.MusicInfo, "CODE.Framework-Icon-MusicInfo");
            StandardIconMaps.Add(StandardIcons.Mute, "CODE.Framework-Icon-Mute");
            StandardIconMaps.Add(StandardIcons.Next, "CODE.Framework-Icon-Next");
            StandardIconMaps.Add(StandardIcons.No, "CODE.Framework-Icon-No");
            StandardIconMaps.Add(StandardIcons.OpenFile, "CODE.Framework-Icon-OpenFile");
            StandardIconMaps.Add(StandardIcons.OpenLocal, "CODE.Framework-Icon-OpenLocal");
            StandardIconMaps.Add(StandardIcons.Orientation, "CODE.Framework-Icon-Orientation");
            StandardIconMaps.Add(StandardIcons.OtherUser, "CODE.Framework-Icon-OtherUser");
            StandardIconMaps.Add(StandardIcons.Out, "CODE.Framework-Icon-Out");
            StandardIconMaps.Add(StandardIcons.Page, "CODE.Framework-Icon-Page");
            StandardIconMaps.Add(StandardIcons.Page2, "CODE.Framework-Icon-Page2");
            StandardIconMaps.Add(StandardIcons.Paste, "CODE.Framework-Icon-Paste");
            StandardIconMaps.Add(StandardIcons.Pause, "CODE.Framework-Icon-Pause");
            StandardIconMaps.Add(StandardIcons.People, "CODE.Framework-Icon-People");
            StandardIconMaps.Add(StandardIcons.Permissions, "CODE.Framework-Icon-Permissions");
            StandardIconMaps.Add(StandardIcons.Phone, "CODE.Framework-Icon-Phone");
            StandardIconMaps.Add(StandardIcons.Photo, "CODE.Framework-Icon-Photo");
            StandardIconMaps.Add(StandardIcons.Pictures, "CODE.Framework-Icon-Pictures");
            StandardIconMaps.Add(StandardIcons.Pin, "CODE.Framework-Icon-Pin");
            StandardIconMaps.Add(StandardIcons.Placeholder, "CODE.Framework-Icon-Placeholder");
            StandardIconMaps.Add(StandardIcons.Play, "CODE.Framework-Icon-Play");
            StandardIconMaps.Add(StandardIcons.Presence, "CODE.Framework-Icon-Presence");
            StandardIconMaps.Add(StandardIcons.Preview, "CODE.Framework-Icon-Preview");
            StandardIconMaps.Add(StandardIcons.PreviewLink, "CODE.Framework-Icon-PreviewLink");
            StandardIconMaps.Add(StandardIcons.Previous, "CODE.Framework-Icon-Previous");
            StandardIconMaps.Add(StandardIcons.Print, "CODE.Framework-Icon-Print");
            StandardIconMaps.Add(StandardIcons.Priority, "CODE.Framework-Icon-Priority");
            StandardIconMaps.Add(StandardIcons.ProtectedDocument, "CODE.Framework-Icon-ProtectedDocument");
            StandardIconMaps.Add(StandardIcons.Read, "CODE.Framework-Icon-Read");
            StandardIconMaps.Add(StandardIcons.Redo, "CODE.Framework-Icon-Redo");
            StandardIconMaps.Add(StandardIcons.Refresh, "CODE.Framework-Icon-Refresh");
            StandardIconMaps.Add(StandardIcons.Remote, "CODE.Framework-Icon-Remote");
            StandardIconMaps.Add(StandardIcons.Remove, "CODE.Framework-Icon-Remove");
            StandardIconMaps.Add(StandardIcons.Rename, "CODE.Framework-Icon-Rename");
            StandardIconMaps.Add(StandardIcons.Repair, "CODE.Framework-Icon-Repair");
            StandardIconMaps.Add(StandardIcons.RotateCamera, "CODE.Framework-Icon-RotateCamera");
            StandardIconMaps.Add(StandardIcons.Save, "CODE.Framework-Icon-Save");
            StandardIconMaps.Add(StandardIcons.SaveLocal, "CODE.Framework-Icon-SaveLocal");
            StandardIconMaps.Add(StandardIcons.Search, "CODE.Framework-Icon-Search");
            StandardIconMaps.Add(StandardIcons.SelectAll, "CODE.Framework-Icon-SelectAll");
            StandardIconMaps.Add(StandardIcons.Send, "CODE.Framework-Icon-Send");
            StandardIconMaps.Add(StandardIcons.SetLockscreen, "CODE.Framework-Icon-SetLockscreen");
            StandardIconMaps.Add(StandardIcons.Settings, "CODE.Framework-Icon-Settings");
            StandardIconMaps.Add(StandardIcons.SetTitle, "CODE.Framework-Icon-SetTitle");
            StandardIconMaps.Add(StandardIcons.Shop, "CODE.Framework-Icon-Shop");
            StandardIconMaps.Add(StandardIcons.ShowBcc, "CODE.Framework-Icon-ShowBcc");
            StandardIconMaps.Add(StandardIcons.ShowResults, "CODE.Framework-Icon-ShowResults");
            StandardIconMaps.Add(StandardIcons.Shuffle, "CODE.Framework-Icon-Shuffle");
            StandardIconMaps.Add(StandardIcons.SkipAhead, "CODE.Framework-Icon-SkipAhead");
            StandardIconMaps.Add(StandardIcons.SkipBack, "CODE.Framework-Icon-SkipBack");
            StandardIconMaps.Add(StandardIcons.Skydrive, "CODE.Framework-Icon-Skydrive");
            StandardIconMaps.Add(StandardIcons.Slideshow, "CODE.Framework-Icon-Slideshow");
            StandardIconMaps.Add(StandardIcons.Sort, "CODE.Framework-Icon-Sort");
            StandardIconMaps.Add(StandardIcons.SortAscending, "CODE.Framework-Icon-SortAscending");
            StandardIconMaps.Add(StandardIcons.SortDescending, "CODE.Framework-Icon-SortDescending");
            StandardIconMaps.Add(StandardIcons.Stop, "CODE.Framework-Icon-Stop");
            StandardIconMaps.Add(StandardIcons.StopSlideshow, "CODE.Framework-Icon-StopSlideshow");
            StandardIconMaps.Add(StandardIcons.Switch, "CODE.Framework-Icon-Switch");
            StandardIconMaps.Add(StandardIcons.Sync, "CODE.Framework-Icon-Sync");
            StandardIconMaps.Add(StandardIcons.Today, "CODE.Framework-Icon-Today");
            StandardIconMaps.Add(StandardIcons.Trim, "CODE.Framework-Icon-Trim");
            StandardIconMaps.Add(StandardIcons.TwoPage, "CODE.Framework-Icon-TwoPage");
            StandardIconMaps.Add(StandardIcons.Underline, "CODE.Framework-Icon-Underline");
            StandardIconMaps.Add(StandardIcons.Undo, "CODE.Framework-Icon-Undo");
            StandardIconMaps.Add(StandardIcons.Unfavorite, "CODE.Framework-Icon-Unfavorite");
            StandardIconMaps.Add(StandardIcons.UnPin, "CODE.Framework-Icon-UnPin");
            StandardIconMaps.Add(StandardIcons.Upload, "CODE.Framework-Icon-Upload");
            StandardIconMaps.Add(StandardIcons.User, "CODE.Framework-Icon-User");
            StandardIconMaps.Add(StandardIcons.UserRights, "CODE.Framework-Icon-UserRights");
            StandardIconMaps.Add(StandardIcons.UserRoles, "CODE.Framework-Icon-UserRoles");
            StandardIconMaps.Add(StandardIcons.Users, "CODE.Framework-Icon-Users");
            StandardIconMaps.Add(StandardIcons.Video, "CODE.Framework-Icon-Video");
            StandardIconMaps.Add(StandardIcons.VideoChat, "CODE.Framework-Icon-VideoChat");
            StandardIconMaps.Add(StandardIcons.View, "CODE.Framework-Icon-View");
            StandardIconMaps.Add(StandardIcons.ViewAll, "CODE.Framework-Icon-ViewAll");
            StandardIconMaps.Add(StandardIcons.Volume, "CODE.Framework-Icon-Volume");
            StandardIconMaps.Add(StandardIcons.Webcam, "CODE.Framework-Icon-Webcam");
            StandardIconMaps.Add(StandardIcons.Week, "CODE.Framework-Icon-Week");
            StandardIconMaps.Add(StandardIcons.World, "CODE.Framework-Icon-World");
            StandardIconMaps.Add(StandardIcons.Yes, "CODE.Framework-Icon-Yes");
            StandardIconMaps.Add(StandardIcons.Zoom, "CODE.Framework-Icon-Zoom");
            StandardIconMaps.Add(StandardIcons.ZoomIn, "CODE.Framework-Icon-ZoomIn");
            StandardIconMaps.Add(StandardIcons.ZoomOut, "CODE.Framework-Icon-ZoomOut");

            StandardIconMapsBackward.Add("", StandardIcons.None);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Accounts", StandardIcons.Accounts);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Add", StandardIcons.Add);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Admin", StandardIcons.Admin);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-AlignCenter", StandardIcons.AlignCenter);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-AlignLeft", StandardIcons.AlignLeft);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-AlignRight", StandardIcons.AlignRight);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-ArrowDown", StandardIcons.ArrowDown);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-ArrowDownLeft", StandardIcons.ArrowDownLeft);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-ArrowDownRight", StandardIcons.ArrowDownRight);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-ArrowLeft", StandardIcons.ArrowLeft);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-ArrowRight", StandardIcons.ArrowRight);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-ArrowUp", StandardIcons.ArrowUp);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-ArrowUpLeft", StandardIcons.ArrowUpLeft);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-ArrowUpRight", StandardIcons.ArrowUpRight);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Attach", StandardIcons.Attach);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-AttachCamera", StandardIcons.AttachCamera);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Audio", StandardIcons.Audio);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Bold", StandardIcons.Bold);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Bookmarks", StandardIcons.Bookmarks);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-BrowsePhotos", StandardIcons.BrowsePhotos);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Bullets", StandardIcons.Bullets);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Calendar", StandardIcons.Calendar);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Caption", StandardIcons.Caption);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Cc", StandardIcons.Cc);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Characters", StandardIcons.Characters);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Clock", StandardIcons.Clock);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-ClosePane", StandardIcons.ClosePane);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Collapsed", StandardIcons.Collapsed);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Comment", StandardIcons.Comment);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Contact", StandardIcons.Contact);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Contact2", StandardIcons.Contact2);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-ContactInfo", StandardIcons.ContactInfo);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Copy", StandardIcons.Copy);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Crop", StandardIcons.Crop);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Cut", StandardIcons.Cut);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Data", StandardIcons.Data);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Data2", StandardIcons.Data2);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Data3", StandardIcons.Data3);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Day", StandardIcons.Day);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-DisableUpdates", StandardIcons.DisableUpdates);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Discard", StandardIcons.Discard);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Dislike", StandardIcons.Dislike);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-DockBottom", StandardIcons.DockBottom);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-DockLeft", StandardIcons.DockLeft);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-DockRight", StandardIcons.DockRight);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Document", StandardIcons.Document);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Download", StandardIcons.Download);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Edit", StandardIcons.Edit);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Emoji", StandardIcons.Emoji);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Emoji2", StandardIcons.Emoji2);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Expanded", StandardIcons.Expanded);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Favorite", StandardIcons.Favorite);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Filter", StandardIcons.Filter);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Flag", StandardIcons.Flag);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Folder", StandardIcons.Folder);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Font", StandardIcons.Font);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-FontColor", StandardIcons.FontColor);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Globe", StandardIcons.Globe);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Go", StandardIcons.Go);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-HangUp", StandardIcons.HangUp);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Help", StandardIcons.Help);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-HideBcc", StandardIcons.HideBcc);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Highlight", StandardIcons.Highlight);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Home", StandardIcons.Home);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Import", StandardIcons.Import);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-ImportAll", StandardIcons.ImportAll);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Important", StandardIcons.Important);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Italic", StandardIcons.Italic);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Keyboard", StandardIcons.Keyboard);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Like", StandardIcons.Like);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-LikeDislike", StandardIcons.LikeDislike);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Link", StandardIcons.Link);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-List", StandardIcons.List);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Login", StandardIcons.Login);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Mail", StandardIcons.Mail);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Mail2", StandardIcons.Mail2);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-MailForward", StandardIcons.MailForward);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-MailReply", StandardIcons.MailReply);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-MailReplyAll", StandardIcons.MailReplyAll);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-MapPin", StandardIcons.MapPin);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Menu", StandardIcons.Menu);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Message", StandardIcons.Message);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-MissingIcon", StandardIcons.MissingIcon);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Money", StandardIcons.Money);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-More", StandardIcons.More);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-MoveToFolder", StandardIcons.MoveToFolder);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-MusicInfo", StandardIcons.MusicInfo);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Mute", StandardIcons.Mute);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Next", StandardIcons.Next);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-No", StandardIcons.No);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-OpenFile", StandardIcons.OpenFile);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-OpenLocal", StandardIcons.OpenLocal);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Orientation", StandardIcons.Orientation);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-OtherUser", StandardIcons.OtherUser);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Out", StandardIcons.Out);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Page", StandardIcons.Page);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Page2", StandardIcons.Page2);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Paste", StandardIcons.Paste);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Pause", StandardIcons.Pause);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-People", StandardIcons.People);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Permissions", StandardIcons.Permissions);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Phone", StandardIcons.Phone);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Photo", StandardIcons.Photo);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Pictures", StandardIcons.Pictures);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Pin", StandardIcons.Pin);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Placeholder", StandardIcons.Placeholder);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Play", StandardIcons.Play);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Presence", StandardIcons.Presence);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Preview", StandardIcons.Preview);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-PreviewLink", StandardIcons.PreviewLink);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Previous", StandardIcons.Previous);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Print", StandardIcons.Print);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Priority", StandardIcons.Priority);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-ProtectedDocument", StandardIcons.ProtectedDocument);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Read", StandardIcons.Read);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Redo", StandardIcons.Redo);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Refresh", StandardIcons.Refresh);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Remote", StandardIcons.Remote);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Remove", StandardIcons.Remove);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Rename", StandardIcons.Rename);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Repair", StandardIcons.Repair);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-RotateCamera", StandardIcons.RotateCamera);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Save", StandardIcons.Save);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-SaveLocal", StandardIcons.SaveLocal);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Search", StandardIcons.Search);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-SelectAll", StandardIcons.SelectAll);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Send", StandardIcons.Send);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-SetLockscreen", StandardIcons.SetLockscreen);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Settings", StandardIcons.Settings);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-SetTitle", StandardIcons.SetTitle);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Shop", StandardIcons.Shop);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-ShowBcc", StandardIcons.ShowBcc);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-ShowResults", StandardIcons.ShowResults);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Shuffle", StandardIcons.Shuffle);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-SkipAhead", StandardIcons.SkipAhead);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-SkipBack", StandardIcons.SkipBack);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Skydrive", StandardIcons.Skydrive);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Slideshow", StandardIcons.Slideshow);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Sort", StandardIcons.Sort);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-SortAscending", StandardIcons.SortAscending);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-SortDescending", StandardIcons.SortDescending);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Stop", StandardIcons.Stop);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-StopSlideshow", StandardIcons.StopSlideshow);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Switch", StandardIcons.Switch);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Sync", StandardIcons.Sync);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Today", StandardIcons.Today);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Trim", StandardIcons.Trim);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-TwoPage", StandardIcons.TwoPage);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Underline", StandardIcons.Underline);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Undo", StandardIcons.Undo);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Unfavorite", StandardIcons.Unfavorite);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-UnPin", StandardIcons.UnPin);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Upload", StandardIcons.Upload);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-User", StandardIcons.User);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-UserRights", StandardIcons.UserRights);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-UserRoles", StandardIcons.UserRoles);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Users", StandardIcons.Users);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Video", StandardIcons.Video);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-VideoChat", StandardIcons.VideoChat);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-View", StandardIcons.View);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-ViewAll", StandardIcons.ViewAll);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Volume", StandardIcons.Volume);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Webcam", StandardIcons.Webcam);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Week", StandardIcons.Week);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-World", StandardIcons.World);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Yes", StandardIcons.Yes);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-Zoom", StandardIcons.Zoom);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-ZoomIn", StandardIcons.ZoomIn);
            StandardIconMapsBackward.Add("CODE.Framework-Icon-ZoomOut", StandardIcons.ZoomOut);
        }

        /// <summary>
        /// Returns a standard icon enum value from the provided key (or StandardIcons.None, if the key is not valid)
        /// </summary>
        /// <param name="iconKey">The icon key.</param>
        /// <returns>Standard Icon</returns>
        public static StandardIcons GetStandardIconEnumFromKey(string iconKey)
        {
            if (StandardIconMapsBackward.ContainsKey(iconKey))
                return StandardIconMapsBackward[iconKey];
            return StandardIcons.None;
        }

        /// <summary>
        /// Returns a standard icon string/key from the provided enum value
        /// </summary>
        /// <param name="icon">The icon key.</param>
        /// <returns>Standard icon resource key name</returns>
        public static string GetStandardIconKeyFromEnum(StandardIcons icon)
        {
            if (StandardIconMaps.ContainsKey(icon))
                return StandardIconMaps[icon];
            return string.Empty;
        }
    }

    /// <summary>
    /// List defining all standard CODE Framework icons
    /// </summary>
    public enum StandardIcons
    {
        /// <summary>No standard icon</summary>
        None,
        /// Accounts icon
        Accounts,
        /// Add icon
        Add,
        /// Admin icon
        Admin,
        /// AlignCenter icon
        AlignCenter,
        /// AlignLeft icon
        AlignLeft,
        /// AlignRight icon
        AlignRight,
        /// ArrowDown icon
        ArrowDown,
        /// ArrowDownLeft icon
        ArrowDownLeft,
        /// ArrowDownRight icon
        ArrowDownRight,
        /// ArrowLeft icon
        ArrowLeft,
        /// ArrowRight icon
        ArrowRight,
        /// ArrowUp icon
        ArrowUp,
        /// ArrowUpLeft icon
        ArrowUpLeft,
        /// ArrowUpRight icon
        ArrowUpRight,
        /// Attach icon
        Attach,
        /// AttachCamera icon
        AttachCamera,
        /// Audio icon
        Audio,
        /// Bold icon
        Bold,
        /// Bookmarks icon
        Bookmarks,
        /// BrowsePhotos icon
        BrowsePhotos,
        /// Bullets icon
        Bullets,
        /// Calendar icon
        Calendar,
        /// Caption icon
        Caption,
        /// Cc icon
        Cc,
        /// Characters icon
        Characters,
        /// Clock icon
        Clock,
        /// ClosePane icon
        ClosePane,
        /// Collapsed icon
        Collapsed,
        /// Comment icon
        Comment,
        /// Contact icon
        Contact,
        /// Contact2 icon
        Contact2,
        /// ContactInfo icon
        ContactInfo,
        /// Copy icon
        Copy,
        /// Crop icon
        Crop,
        /// Cut icon
        Cut,
        /// Data icon
        Data,
        /// Data2 icon
        Data2,
        /// Data3 icon
        Data3,
        /// Day icon
        Day,
        /// DisableUpdates icon
        DisableUpdates,
        /// Discard icon
        Discard,
        /// Dislike icon
        Dislike,
        /// DockBottom icon
        DockBottom,
        /// DockLeft icon
        DockLeft,
        /// DockRight icon
        DockRight,
        /// Document icon
        Document,
        /// Download icon
        Download,
        /// Edit icon
        Edit,
        /// Emoji icon
        Emoji,
        /// Emoji2 icon
        Emoji2,
        /// Expanded icon
        Expanded,
        /// Favorite icon
        Favorite,
        /// Filter icon
        Filter,
        /// Flag icon
        Flag,
        /// Folder icon
        Folder,
        /// Font icon
        Font,
        /// FontColor icon
        FontColor,
        /// Globe icon
        Globe,
        /// Go icon
        Go,
        /// HangUp icon
        HangUp,
        /// Help icon
        Help,
        /// HideBcc icon
        HideBcc,
        /// Highlight icon
        Highlight,
        /// Home icon
        Home,
        /// Import icon
        Import,
        /// ImportAll icon
        ImportAll,
        /// Important icon
        Important,
        /// Italic icon
        Italic,
        /// Keyboard icon
        Keyboard,
        /// Like icon
        Like,
        /// LikeDislike icon
        LikeDislike,
        /// Link icon
        Link,
        /// List icon
        List,
        /// Login icon
        Login,
        /// Mail icon
        Mail,
        /// Mail2 icon
        Mail2,
        /// MailForward icon
        MailForward,
        /// MailReply icon
        MailReply,
        /// MailReplyAll icon
        MailReplyAll,
        /// MapPin icon
        MapPin,
        /// Menu icon
        Menu,
        /// Message icon
        Message,
        /// MissingIcon icon
        MissingIcon,
        /// Money icon
        Money,
        /// More icon
        More,
        /// MoveToFolder icon
        MoveToFolder,
        /// MusicInfo icon
        MusicInfo,
        /// Mute icon
        Mute,
        /// Next icon
        Next,
        /// No icon
        No,
        /// OpenFile icon
        OpenFile,
        /// OpenLocal icon
        OpenLocal,
        /// Orientation icon
        Orientation,
        /// OtherUser icon
        OtherUser,
        /// Out icon
        Out,
        /// Page icon
        Page,
        /// Page2 icon
        Page2,
        /// Paste icon
        Paste,
        /// Pause icon
        Pause,
        /// People icon
        People,
        /// Permissions icon
        Permissions,
        /// Phone icon
        Phone,
        /// Photo icon
        Photo,
        /// Pictures icon
        Pictures,
        /// Pin icon
        Pin,
        /// Placeholder icon
        Placeholder,
        /// Play icon
        Play,
        /// Presence icon
        Presence,
        /// Preview icon
        Preview,
        /// PreviewLink icon
        PreviewLink,
        /// Previous icon
        Previous,
        /// Print icon
        Print,
        /// Priority icon
        Priority,
        /// ProtectedDocument icon
        ProtectedDocument,
        /// Read icon
        Read,
        /// Redo icon
        Redo,
        /// Refresh icon
        Refresh,
        /// Remote icon
        Remote,
        /// Remove icon
        Remove,
        /// Rename icon
        Rename,
        /// Repair icon
        Repair,
        /// RotateCamera icon
        RotateCamera,
        /// Save icon
        Save,
        /// SaveLocal icon
        SaveLocal,
        /// Search icon
        Search,
        /// SelectAll icon
        SelectAll,
        /// Send icon
        Send,
        /// SetLockscreen icon
        SetLockscreen,
        /// Settings icon
        Settings,
        /// SetTitle icon
        SetTitle,
        /// Shop icon
        Shop,
        /// ShowBcc icon
        ShowBcc,
        /// ShowResults icon
        ShowResults,
        /// Shuffle icon
        Shuffle,
        /// SkipAhead icon
        SkipAhead,
        /// SkipBack icon
        SkipBack,
        /// Skydrive icon
        Skydrive,
        /// Slideshow icon
        Slideshow,
        /// Sort icon
        Sort,
        /// SortAscending icon
        SortAscending,
        /// SortDescending icon
        SortDescending,
        /// Stop icon
        Stop,
        /// StopSlideshow icon
        StopSlideshow,
        /// Switch icon
        Switch,
        /// Sync icon
        Sync,
        /// Today icon
        Today,
        /// Trim icon
        Trim,
        /// TwoPage icon
        TwoPage,
        /// Underline icon
        Underline,
        /// Undo icon
        Undo,
        /// Unfavorite icon
        Unfavorite,
        /// UnPin icon
        UnPin,
        /// Upload icon
        Upload,
        /// User icon
        User,
        /// UserRights icon
        UserRights,
        /// UserRoles icon
        UserRoles,
        /// Users icon
        Users,
        /// Video icon
        Video,
        /// VideoChat icon
        VideoChat,
        /// View icon
        View,
        /// ViewAll icon
        ViewAll,
        /// Volume icon
        Volume,
        /// Webcam icon
        Webcam,
        /// Week icon
        Week,
        /// World icon
        World,
        /// Yes icon
        Yes,
        /// Zoom icon
        Zoom,
        /// ZoomIn icon
        ZoomIn,
        /// ZoomOut icon
        ZoomOut,
    }
}
