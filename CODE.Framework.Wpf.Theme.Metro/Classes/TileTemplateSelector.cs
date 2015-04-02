using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CODE.Framework.Wpf.Mvvm;

namespace CODE.Framework.Wpf.Theme.Metro.Classes
{
    /// <summary>
    /// Selects an appropriate tile template
    /// </summary>
    public class ViewActionTileTemplateSelector : DataTemplateSelector 
    {
        /// <summary>
        /// When overridden in a derived class, returns a <see cref="T:System.Windows.DataTemplate" /> based on custom logic.
        /// </summary>
        /// <param name="item">The data object for which to select the template.</param>
        /// <param name="container">The data-bound object.</param>
        /// <returns>Returns a <see cref="T:System.Windows.DataTemplate" /> or null. The default value is null.</returns>
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var viewAction = item as IViewAction;
            if (viewAction == null) return NormalTileTemplate;

            if (viewAction.ActionView != null) return CustomViewTileTemplate;

            var standardViewModel = viewAction as IStandardViewModel;
            switch (viewAction.Significance)
            {
                case ViewActionSignificance.Normal:
                    if (standardViewModel == null) return NormalTileTemplate;
                    if (standardViewModel.Image1 is ImageBrush) return NormalTile1ImageTemplate;
                    return NormalTileTemplate;
                case ViewActionSignificance.BelowNormal:
                case ViewActionSignificance.Lowest:
                    if (standardViewModel == null) return TinyTileTemplate;
                    if (standardViewModel.Image1 is ImageBrush) return TinyTile1ImageTemplate;
                    return TinyTileTemplate;
                case ViewActionSignificance.AboveNormal:
                    if (standardViewModel == null) return WideTileTemplate;
                    if (standardViewModel.Image1 != null && standardViewModel.Image2 != null && standardViewModel.Image3 != null && standardViewModel.Image4 != null && standardViewModel.Image5 != null) return WideTile5ImagesTemplate;
                    if (standardViewModel.Image1 is ImageBrush) return WideTile1ImageTemplate;
                    return WideTileTemplate;
                case ViewActionSignificance.Highest:
                    if (standardViewModel == null) return WideSquareTileTemplate;
                    if (standardViewModel.Image1 != null && standardViewModel.Image2 != null && standardViewModel.Image3 != null && standardViewModel.Image4 != null && standardViewModel.Image5 != null) return WideSquareTile5ImagesTemplate;
                    if (standardViewModel.Image1 is ImageBrush) return WideSquareTile1ImageTemplate;
                    return WideSquareTileTemplate;
            }

            return NormalTileTemplate;
        }

        /// <summary>
        /// Template for tiny tiles
        /// </summary>
        public DataTemplate TinyTileTemplate { get; set; }

        /// <summary>
        /// Template for tiny tiles with 1 image brush
        /// </summary>
        public DataTemplate TinyTile1ImageTemplate { get; set; }

        /// <summary>
        /// Template for normal tiles
        /// </summary>
        public DataTemplate NormalTileTemplate { get; set; }

        /// <summary>
        /// Template for normal tiles with 1 image brush
        /// </summary>
        public DataTemplate NormalTile1ImageTemplate { get; set; }

        /// <summary>
        /// Template for wide tiles
        /// </summary>
        public DataTemplate WideTileTemplate { get; set; }

        /// <summary>
        /// Template for wide tiles with 1 image brush
        /// </summary>
        public DataTemplate WideTile1ImageTemplate { get; set; }

        /// <summary>
        /// Template for wide tiles with 5 populated images
        /// </summary>
        public DataTemplate WideTile5ImagesTemplate { get; set; }

        /// <summary>
        /// Template for wide square tiles
        /// </summary>
        public DataTemplate WideSquareTileTemplate { get; set; }

        /// <summary>
        /// Template for wide square tiles
        /// </summary>
        public DataTemplate WideSquareTile1ImageTemplate { get; set; }

        /// <summary>
        /// Template for wide square tiles
        /// </summary>
        public DataTemplate WideSquareTile5ImagesTemplate { get; set; }

        /// <summary>
        /// Template for custom view tiles
        /// </summary>
        public DataTemplate CustomViewTileTemplate { get; set; }
    }
}
