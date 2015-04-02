using System.Windows;
using CODE.Framework.Wpf.Mvvm;
using CODE.Framework.Wpf.Theme.Newsroom.StandardViews;

namespace CODE.Framework.Wpf.Theme.Newsroom.Classes
{
    /// <summary>
    /// Standard features supported by the Metro theme
    /// </summary>
    public class NewsroomStandardFeatures : IThemeStandardFeatures
    {
        /// <summary>Reference to the standard view factory (if supported)</summary>
        public IStandardViewFactory StandardViewFactory
        {
            get { return _standardViewFactory; }
        }

        private readonly IStandardViewFactory _standardViewFactory = new NewsroomStandardViewFactory();
    }

    /// <summary>Factory to create standard views supported by Metro</summary>
    public class NewsroomStandardViewFactory : IStandardViewFactory
    {
        /// <summary>Returns a standard view based on the view name as a string</summary>
        /// <param name="viewName">Standard view name</param>
        /// <returns>Standard view or null</returns>
        public FrameworkElement GetStandardView(string viewName)
        {
            if (string.IsNullOrEmpty(viewName) || viewName == "None") return null;

            switch (viewName)
            {
                case "Block": return new Block();
                case "Image": return new Image();
                case "LargeImage": return new LargeImage();
                case "LargeImageAndText01": return new LargeImageAndText01();
                case "LargeImageAndText02": return new LargeImageAndText02();
                case "LargeImageCollection": return new LargeImageCollection();
                case "PeekImageAndText01": return new PeekImageAndText01();
                case "PeekImageAndText02": return new PeekImageAndText02();
                case "PeekImageAndText03": return new PeekImageAndText03();
                case "PeekImageAndText04": return new PeekImageAndText04();
                case "PeekImageAndText05": return new PeekImageAndText05();
                case "Text01": return new Text01();
                case "Text02": return new Text02();
                case "Text03": return new Text03();
                case "Text04": return new Text04();
                case "Text05": return new Text05();
                case "LargeBlockAndText01": return new LargeBlockAndText01();
                case "LargeBlockAndText02": return new LargeBlockAndText02();
                case "LargeSmallImageAndText01": return new LargeSmallImageAndText01();
                case "LargeSmallImageAndText02": return new LargeSmallImageAndText02();
                case "LargeSmallImageAndText03": return new LargeSmallImageAndText03();
                case "LargeSmallImageAndText04": return new LargeSmallImageAndText04();
                case "LargeSmallImageAndText05": return new LargeSmallImageAndText05();
                case "LargeSmallImageAndText06": return new LargeSmallImageAndText06();
                case "LargeSmallImageAndText07": return new LargeSmallImageAndText07();
                case "LargeText01": return new LargeText01();
                case "LargeText02": return new LargeText02();
                case "LargeText03": return new LargeText03();
                case "LargeText04": return new LargeText04();
                case "LargeText05": return new LargeText05();
                case "LargeText06": return new LargeText06();
                case "LargeText07": return new LargeText07();
                case "LargeText08": return new LargeText08();
                case "LargeText09": return new LargeText09();
                case "LargeText10": return new LargeText10();
                case "LargeText11": return new LargeText11();
                case "LargePeekImageCollection01": return new LargePeekImageCollection01();
                case "LargePeekImageCollection02": return new LargePeekImageCollection02();
                case "LargePeekImageCollection03": return new LargePeekImageCollection03();
                case "LargePeekImageCollection04": return new LargePeekImageCollection04();
                case "LargePeekImageCollection05": return new LargePeekImageCollection05();
                case "LargePeekImageCollection06": return new LargePeekImageCollection06();
                case "LargePeekImageAndText01": return new LargePeekImageAndText01();
                case "LargePeekImageAndText02": return new LargePeekImageAndText02();
                case "LargePeekImageAndText03": return new LargePeekImageAndText03();
                case "LargePeekImageAndText04": return new LargePeekImageAndText04();
                case "LargePeekImageAndText05": return new LargePeekImageAndText05();
                case "LargePeekImageAndText06": return new LargePeekImageAndText06();
                case "Data01": return new Data01();
                case "Data02": return new Data02();
                case "Data03": return new Data03();
                case "DataAndImage01": return new DataAndImage01();
                case "DataAndImage02": return new DataAndImage02();
                case "DataAndImage03": return new DataAndImage03();
                case "DataRowAndImage01": return new DataRowAndImage01();
                case "DataSmall01": return new DataSmall01();
                case "DataSmall02": return new DataSmall02();
                case "DataSmall03": return new DataSmall03();
                case "Notification": return new Notifications();

                case "TileTiny": return new TileTiny(); // Metro only
                case "TileNarrow": return new TileNarrow(); // Metro only
                case "TileWide": return new TileWide(); // Metro only
                case "TileWideSquare": return new TileWideSquare(); // Metro only
            }

            return new Fallback();
        }

        /// <summary>Returns a standard view based on the standard view enumeration</summary>
        /// <param name="standardView">Standard view identifier</param>
        /// <returns>Standard view or null</returns>
        public FrameworkElement GetStandardView(Mvvm.StandardViews standardView)
        {
            if (standardView == Mvvm.StandardViews.None) return null;
            return GetStandardView(standardView.ToString());
        }
    }
}
