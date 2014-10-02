using System.Windows;
using CODE.Framework.Wpf.Controls;
using CODE.Framework.Wpf.Mvvm;
using CODE.Framework.Wpf.Theme.Workplace.StandardViews;

namespace CODE.Framework.Wpf.Theme.Workplace.Classes
{
    /// <summary>
    /// Standard features supported by the Workplace theme
    /// </summary>
    public class WorkplaceStandardFeatures : IThemeStandardFeatures
    {
        /// <summary>Reference to the standard view factory (if supported)</summary>
        public IStandardViewFactory StandardViewFactory
        {
            get { return _standardViewFactory; }
        }

        private readonly IStandardViewFactory _standardViewFactory = new WorkplaceStandardViewFactory();
    }

    /// <summary>Factory to create standard views supported by Workplace</summary>
    public class WorkplaceStandardViewFactory : IStandardViewFactory
    {
        /// <summary>Returns a standard view based on the view name as a string</summary>
        /// <param name="viewName">Standard view name</param>
        /// <returns>Standard view or null</returns>
        public FrameworkElement GetStandardView(string viewName)
        {
            if (string.IsNullOrEmpty(viewName) || viewName == "None") return null;

            switch (viewName)
            {
                case "Block": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Text1, Text2, Number1" };
                case "Image": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Image1, Number1" };
                case "LargeImage": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Image1, Number1" };
                case "LargeImageAndText01": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Image1, Text1, Number1" };
                case "LargeImageAndText02": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Image1, Text1, Text2, Number1" };
                case "LargeImageCollection": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Image1, Image2, Image3, Image4, Image5" };
                case "PeekImageAndText01": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Image1, Text1, Text2, Text3, Text4" };
                case "PeekImageAndText02": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Image1, Text1, Text2" };
                case "PeekImageAndText03": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Image1, Text1, Text2, Text3" };
                case "PeekImageAndText04": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Image1, Text1" };
                case "PeekImageAndText05": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Image1, Text1" }; // TODO: Doc
                case "Text01": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Text1, Text2, Text3, Text4" };
                case "Text02": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Text1, Text2, Text3" };
                case "Text03": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Text1, Text2" };
                case "Text04": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Text1" };
                case "Text05": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Text1" }; // TODO: Doc
                case "LargeBlockAndText01": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Text1, Text2, Text3, Text4, Text5, Text6" };
                case "LargeBlockAndText02": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Text1, Text2, Text3" };
                case "LargeSmallImageAndText01": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Image1, Text1" };
                case "LargeSmallImageAndText02": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Image1, Text1, Text2, Text3, Text4" };
                case "LargeSmallImageAndText03": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Image1, Text1" };
                case "LargeSmallImageAndText04": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Image1, Text1, Text2" };
                case "LargeSmallImageAndText05": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Image1, Text1, Text2" };
                case "LargeSmallImageAndText06": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Image1, Text1" }; // TODO: Doc
                case "LargeSmallImageAndText07": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Image1, Text1" }; // TODO: Doc
                case "LargeText01": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Text1, Text2, Text3, Text4" };
                case "LargeText02": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Text1, Text2, Text3, Text4, Text5, Text6, Text7, Text8, Text9" };
                case "LargeText03": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Text1" };
                case "LargeText04": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Text1" };
                case "LargeText05": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Text1, Text2, Text3, Text4, Text5" };
                case "LargeText06": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Text1, Text2, Text3, Text4, Text5, Text6, Text7, Text8, Text9, Text10" };
                case "LargeText07": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Text1, Text2, Text3, Text4, Text5, Text6, Text7, Text8, Text9" };
                case "LargeText08": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Text1, Text2, Text3, Text4, Text5, Text6, Text7, Text8, Text9, Text10" };
                case "LargeText09": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Text1, Text2" };
                case "LargeText10": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Text1, Text2, Text3, Text4, Text5, Text6, Text7, Text8, Text9" };
                case "LargeText11": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Text1, Text2, Text3, Text4, Text5, Text6, Text7, Text8, Text9, Text10" };
                case "LargePeekImageCollection01": return new ListBoxSmartDataTemplate { DefaultColumns = "Image1, Image2, Image3, Image4, Image5, Text1, Text2" };
                case "LargePeekImageCollection02": return new ListBoxSmartDataTemplate { DefaultColumns = "Image1, Image2, Image3, Image4, Image5, Text1, Text2, Text3, Text4, Text5" };
                case "LargePeekImageCollection03": return new ListBoxSmartDataTemplate { DefaultColumns = "Image1, Image2, Image3, Image4, Image5, Text1" };
                case "LargePeekImageCollection04": return new ListBoxSmartDataTemplate { DefaultColumns = "Image1, Image2, Image3, Image4, Image5, Text1" };
                case "LargePeekImageCollection05": return new ListBoxSmartDataTemplate { DefaultColumns = "Image1, Image2, Image3, Image4, Image5, Text1, Text2" };
                case "LargePeekImageCollection06": return new ListBoxSmartDataTemplate { DefaultColumns = "Image1, Image2, Image3, Image4, Image5, Text1" };
                case "LargePeekImageAndText01": return new ListBoxSmartDataTemplate { DefaultColumns = "Image1, Text1, Text2" };
                case "LargePeekImageAndText02": return new ListBoxSmartDataTemplate { DefaultColumns = "Image1, Text1, Text2, Text3, Text4, Text5" };
                case "LargePeekImageAndText03": return new ListBoxSmartDataTemplate { DefaultColumns = "Image1, Text1" };
                case "LargePeekImageAndText04": return new ListBoxSmartDataTemplate { DefaultColumns = "Image1, Text1" };
                case "LargePeekImageAndText05": return new ListBoxSmartDataTemplate { DefaultColumns = "Image1, Text1, Text2" };
                case "LargePeekImageAndText06": return new LargePeekImageAndText06();
                case "Data01": return new ListBoxSmartDataTemplate { DefaultColumns = "Text1, Text2, Text3" };
                case "Data02": return new ListBoxSmartDataTemplate { DefaultColumns = "Text1, Text2" };
                case "Data03": return new ListBoxSmartDataTemplate { DefaultColumns = "Text1, Text2, Text3, Text4, Text5" };
                case "DataAndImage01": return new ListBoxSmartDataTemplate { DefaultColumns = "Image1, Image2, Text1, Text2, Text3" };
                case "DataAndImage02": return new ListBoxSmartDataTemplate { DefaultColumns = "Image1, Image2, Text1, Text2" };
                case "DataAndImage03": return new ListBoxSmartDataTemplate { DefaultColumns = "Image1, Image2, Text1, Text2, Text3, Text4, Text5" };
                case "DataRowAndImage01": return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Logo2, Image1, Image2, Image3, Image4, Image5, Text1, Text2" }; // TODO: Doc
                case "DataSmall01": return new ListBoxSmartDataTemplate { DefaultColumns = "Text1" };
                case "DataSmall02": return new ListBoxSmartDataTemplate { DefaultColumns = "Image1, Text1" };
                case "DataSmall03": return new ListBoxSmartDataTemplate { DefaultColumns = "Image1, Text1, Text2" };
                case "Notification": return new Notification();
            }

            return new ListBoxSmartDataTemplate { DefaultColumns = "Logo1, Logo2, Image1, Image2, Image3, Image4, Image5, Text1, Text2, Text3, Text4, Text5, Text6, Text7, Text8, Text9, Text10, Number1, Number2" };
        }

        /// <summary>
        /// Returns a standard view based on the standard view enumeration
        /// </summary>
        /// <param name="standardView">Standard view identifier</param>
        /// <returns>Standard view or null</returns>
        public FrameworkElement GetStandardView(Mvvm.StandardViews standardView)
        {
            return standardView == Mvvm.StandardViews.None ? null : GetStandardView(standardView.ToString());
        }
    }
}
