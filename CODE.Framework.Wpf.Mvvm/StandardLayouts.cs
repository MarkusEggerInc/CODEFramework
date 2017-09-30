using System.Collections.Generic;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// Defines all standard layouts supported by all themes
    /// </summary>
    public enum StandardLayouts
    {
        /// <summary>
        /// No layout assigned.
        /// </summary>
        None,
        /// <summary>
        /// Blade panels typically create a layout of blades that are arranged vertical left-to-right.
        /// If needed, the layout adds a scrollbar.
        /// </summary>
        Blades,
        /// <summary>
        /// Blade panels typically create a layout of blades that are arranged vertical left-to-right. 
        /// If needed, the layout adds a scrollbar.
        /// This variation fills the remaining horizontal space (of any) with the last element.
        /// </summary>
        BladesToFill,
        /// <summary>
        /// Blade panels typically create a layout of blades that are arranged vertical left-to-right.
        /// If needed, the layout adds a scrollbar.
        /// This version also has headers.
        /// </summary>
        BladesWithHeaders,
        /// <summary>
        /// Blade panels typically create a layout of blades that are arranged vertical left-to-right. 
        /// If needed, the layout adds a scrollbar.
        /// This variation fills the remaining horizontal space (of any) with the last element.
        /// This version also has headers.
        /// </summary>
        BladesWithHeadersToFill,
        /// <summary>
        /// Edit form layouts arrange controls in a fashion useful for complex data edit interfaces.
        /// The layout attempts to create groups of controls that have labels associated with them.
        /// Controls are arranged in groups and columns.
        /// </summary>
        EditForm,
        /// <summary>
        /// Flow form layouts arrange controls in a flowing fashion, left-to-right.
        /// This layout provides additional smarts in how controls are broken across lines
        /// to create a visually pleasing result.
        /// </summary>
        FlowForm,
        /// <summary>
        /// Multi-panel layouts arrange controls either horizontally or vertically, using up all available space.
        /// </summary>
        MultiPanel,
        /// <summary>
        /// Multi-panel layouts arrange controls either horizontally or vertically, using up all available space.
        /// This version includes headers.
        /// </summary>
        MultiPanelWithHeaders,
        /// <summary>
        /// Primary/Secondary layouts arrange a primary element that takes up most of the screen, and adorns
        /// the arrangement with a secondary/auxiliary element. This is similar to how Windows Explorer is organized,
        /// with a tree on the left (the "secondary" element) and a list of files using up the majority of the
        /// available UI space (the "primary element).
        /// </summary>
        PrimarySecondaryForm,
        /// <summary>
        /// Primary/Secondary layouts arrange a primary element that takes up most of the screen, and adorns
        /// the arrangement with a secondary/auxiliary element. This is similar to how Windows Explorer is organized,
        /// with a tree on the left (the "secondary" element) and a list of files using up the majority of the
        /// available UI space (the "primary element).
        /// This version is well suited for UIs where the main element is a list of items.
        /// </summary>
        PrimarySecondaryFormNoColor,
        /// <summary>
        /// Primary/Secondary layouts arrange a primary element that takes up most of the screen, and adorns
        /// the arrangement with a secondary/auxiliary element. This is similar to how Windows Explorer is organized,
        /// with a tree on the left (the "secondary" element) and a list of files using up the majority of the
        /// available UI space (the "primary element).
        /// This version does not create background colors.
        /// </summary>
        PrimarySecondaryListForm,
        /// <summary>
        /// Primary/Secondary layouts arrange a primary element that takes up most of the screen, and adorns
        /// the arrangement with a secondary/auxiliary element. This is similar to how Windows Explorer is organized,
        /// with a tree on the left (the "secondary" element) and a list of files using up the majority of the
        /// available UI space (the "primary element).
        /// This version is well suited for UIs where the main element is a list of items.
        /// This version does not create background colors.
        /// </summary>
        PrimarySecondaryListFormNoColor,
        /// <summary>
        /// Simple form layouts are basic arrangements of controls, typically in a vertical or horizontal stack.
        /// The layout is smart enough to create visually pleasing results by assigning meaningful spacing.
        /// </summary>
        SimpleForm,
        /// <summary>
        /// Simple form layouts are basic arrangements of controls, typically in a vertical or horizontal stack.
        /// The layout is smart enough to create visually pleasing results by assigning meaningful spacing.
        /// This version fills the remaining screen real-estate (if any) with the last element.
        /// </summary>
        SimpleFormToFill,
        /// <summary>
        /// Standard form layouts use WPF standards (typically a Grid) and leave layout details up to the developer.
        /// </summary>
        StandardForm,
        /// <summary>
        /// Creates a tab-control type of layout
        /// </summary>
        Tabs
    }

    /// <summary>
    /// This helper class provides functionality related to standard layouts.
    /// </summary>
    public static class StandardLayoutHelper
    {
        /// <summary>
        /// For internal use only
        /// </summary>
        private static readonly Dictionary<string, StandardLayouts> StandardLayoutMapsBackward = new Dictionary<string, StandardLayouts>();
        /// <summary>
        /// For internal use only
        /// </summary>
        private static readonly Dictionary<StandardLayouts, string> StandardLayoutMaps = new Dictionary<StandardLayouts, string>();
        /// <summary>
        /// For internal use only
        /// </summary>
        private static readonly Dictionary<StandardLayouts, string> StandardLayoutTitles = new Dictionary<StandardLayouts, string>();
        /// <summary>
        /// For internal use only
        /// </summary>
        private static readonly Dictionary<StandardLayouts, string> StandardLayoutDescriptions = new Dictionary<StandardLayouts, string>();

        /// <summary>
        /// Initializes static members of the <see cref="StandardLayoutHelper" /> class.
        /// </summary>
        static StandardLayoutHelper()
        {
            StandardLayoutMaps.Add(StandardLayouts.Blades, "CODE.Framework-Layout-BladePanelLayout");
            StandardLayoutMaps.Add(StandardLayouts.BladesToFill, "CODE.Framework-Layout-BladePanelToFillLayout");
            StandardLayoutMaps.Add(StandardLayouts.BladesWithHeaders, "CODE.Framework-Layout-BladePanelWithHeaderLayout");
            StandardLayoutMaps.Add(StandardLayouts.BladesWithHeadersToFill, "CODE.Framework-Layout-BladePanelWithHeaderToFillLayout");
            StandardLayoutMaps.Add(StandardLayouts.EditForm, "CODE.Framework-Layout-EditFormLayout");
            StandardLayoutMaps.Add(StandardLayouts.FlowForm, "CODE.Framework-Layout-FlowFormLayout");
            StandardLayoutMaps.Add(StandardLayouts.MultiPanel, "CODE.Framework-Layout-MultiPanelLayout");
            StandardLayoutMaps.Add(StandardLayouts.MultiPanelWithHeaders, "CODE.Framework-Layout-MultiPanelWithHeaderLayout");
            StandardLayoutMaps.Add(StandardLayouts.PrimarySecondaryForm, "CODE.Framework-Layout-PrimarySecondaryFormLayout");
            StandardLayoutMaps.Add(StandardLayouts.PrimarySecondaryFormNoColor, "CODE.Framework-Layout-PrimarySecondaryFormLayout-NoColor");
            StandardLayoutMaps.Add(StandardLayouts.PrimarySecondaryListForm, "CODE.Framework-Layout-ListPrimarySecondaryFormLayout");
            StandardLayoutMaps.Add(StandardLayouts.PrimarySecondaryListFormNoColor, "CODE.Framework-Layout-ListPrimarySecondaryFormLayout-NoColor");
            StandardLayoutMaps.Add(StandardLayouts.SimpleForm, "CODE.Framework-Layout-SimpleFormLayout");
            StandardLayoutMaps.Add(StandardLayouts.SimpleFormToFill, "CODE.Framework-Layout-SimpleFormToFillLayout");
            StandardLayoutMaps.Add(StandardLayouts.StandardForm, "CODE.Framework-Layout-StandardFormLayout");
            StandardLayoutMaps.Add(StandardLayouts.Tabs, "CODE.Framework-Layout-Tabs");

            StandardLayoutMapsBackward.Add("CODE.Framework-Layout-BladePanelLayout", StandardLayouts.Blades);
            StandardLayoutMapsBackward.Add("CODE.Framework-Layout-BladePanelToFillLayout", StandardLayouts.BladesToFill);
            StandardLayoutMapsBackward.Add("CODE.Framework-Layout-BladePanelWithHeaderLayout", StandardLayouts.BladesWithHeaders);
            StandardLayoutMapsBackward.Add("CODE.Framework-Layout-BladePanelWithHeaderToFillLayout", StandardLayouts.BladesWithHeadersToFill);
            StandardLayoutMapsBackward.Add("CODE.Framework-Layout-EditFormLayout", StandardLayouts.EditForm);
            StandardLayoutMapsBackward.Add("CODE.Framework-Layout-FlowFormLayout", StandardLayouts.FlowForm);
            StandardLayoutMapsBackward.Add("CODE.Framework-Layout-MultiPanelLayout", StandardLayouts.MultiPanel);
            StandardLayoutMapsBackward.Add("CODE.Framework-Layout-MultiPanelWithHeaderLayout", StandardLayouts.MultiPanelWithHeaders);
            StandardLayoutMapsBackward.Add("CODE.Framework-Layout-PrimarySecondaryFormLayout", StandardLayouts.PrimarySecondaryForm);
            StandardLayoutMapsBackward.Add("CODE.Framework-Layout-PrimarySecondaryFormLayout-NoColor", StandardLayouts.PrimarySecondaryFormNoColor);
            StandardLayoutMapsBackward.Add("CODE.Framework-Layout-ListPrimarySecondaryFormLayout", StandardLayouts.PrimarySecondaryListForm);
            StandardLayoutMapsBackward.Add("CODE.Framework-Layout-ListPrimarySecondaryFormLayout-NoColor", StandardLayouts.PrimarySecondaryListFormNoColor);
            StandardLayoutMapsBackward.Add("CODE.Framework-Layout-SimpleFormLayout", StandardLayouts.SimpleForm);
            StandardLayoutMapsBackward.Add("CODE.Framework-Layout-SimpleFormToFillLayout", StandardLayouts.SimpleFormToFill);
            StandardLayoutMapsBackward.Add("CODE.Framework-Layout-StandardFormLayout", StandardLayouts.StandardForm);
            StandardLayoutMapsBackward.Add("CODE.Framework-Layout-Tabs", StandardLayouts.Tabs);

            StandardLayoutTitles.Add(StandardLayouts.None, "(none)");
            StandardLayoutTitles.Add(StandardLayouts.Blades, "Blades Layout");
            StandardLayoutTitles.Add(StandardLayouts.BladesToFill, "Blades Layout (last item fills space)");
            StandardLayoutTitles.Add(StandardLayouts.BladesWithHeaders, "Blades Layout with Headers");
            StandardLayoutTitles.Add(StandardLayouts.BladesWithHeadersToFill, "Blades Layout with Headers (last item fills space)");
            StandardLayoutTitles.Add(StandardLayouts.EditForm, "Edit Form Layout");
            StandardLayoutTitles.Add(StandardLayouts.FlowForm, "Flow Form Layout");
            StandardLayoutTitles.Add(StandardLayouts.MultiPanel, "Multi-Panel Layout");
            StandardLayoutTitles.Add(StandardLayouts.MultiPanelWithHeaders, "Multi-Panel Layout with Headers");
            StandardLayoutTitles.Add(StandardLayouts.PrimarySecondaryForm, "Primary/Secondary Form Layout");
            StandardLayoutTitles.Add(StandardLayouts.PrimarySecondaryFormNoColor, "Primary/Secondary Form Layout (no colors)");
            StandardLayoutTitles.Add(StandardLayouts.PrimarySecondaryListForm, "Primary/Secondary Form Layout for Lists");
            StandardLayoutTitles.Add(StandardLayouts.PrimarySecondaryListFormNoColor, "Primary/Secondary Form Layout for Lists (no colors)");
            StandardLayoutTitles.Add(StandardLayouts.SimpleForm, "Simple Form Layout");
            StandardLayoutTitles.Add(StandardLayouts.SimpleFormToFill, "Simple Form Layout (last item fills space)");
            StandardLayoutTitles.Add(StandardLayouts.StandardForm, "Standard Form Layout (no automatic layout)");
            StandardLayoutTitles.Add(StandardLayouts.Tabs, "Tab-control layout)");

            StandardLayoutDescriptions.Add(StandardLayouts.Blades, "Blade panels typically create a layout of blades that are arranged vertical left-to-right.\r\nIf needed, the layout adds a scrollbar.");
            StandardLayoutDescriptions.Add(StandardLayouts.BladesToFill, "Blade panels typically create a layout of blades that are arranged vertical left-to-right. \r\nIf needed, the layout adds a scrollbar.\r\nThis variation fills the remaining horizontal space (of any) with the last element.");
            StandardLayoutDescriptions.Add(StandardLayouts.BladesWithHeaders, "Blade panels typically create a layout of blades that are arranged vertical left-to-right. \r\nIf needed, the layout adds a scrollbar.\r\nThis variation fills the remaining horizontal space (of any) with the last element.");
            StandardLayoutDescriptions.Add(StandardLayouts.BladesWithHeadersToFill, "Blade panels typically create a layout of blades that are arranged vertical left-to-right. \r\nIf needed, the layout adds a scrollbar.\r\nThis variation fills the remaining horizontal space (of any) with the last element.\r\nThis version also has headers.");
            StandardLayoutDescriptions.Add(StandardLayouts.EditForm, "Edit form layouts arrange controls in a fashion useful for complex data edit interfaces.\r\nThe layout attempts to create groups of controls that have labels associated with them.\r\nControls are arranged in groups and columns.");
            StandardLayoutDescriptions.Add(StandardLayouts.FlowForm, "Flow form layouts arrange controls in a flowing fashion, left-to-right.\r\nThis layout provides additional smarts in how controls are broken across lines to create a visually pleasing result.");
            StandardLayoutDescriptions.Add(StandardLayouts.MultiPanel, "Multi-panel layouts arrange controls either horizontally or vertically, using up all available space.");
            StandardLayoutDescriptions.Add(StandardLayouts.MultiPanelWithHeaders, "Multi-panel layouts arrange controls either horizontally or vertically, using up all available space.\r\nThis version includes headers.");
            StandardLayoutDescriptions.Add(StandardLayouts.PrimarySecondaryForm, "Primary/Secondary layouts arrange a primary element that takes up most of the screen, and adorns the arrangement with a secondary/auxiliary element. This is similar to how Windows Explorer is organized, with a tree on the left (the \"secondary\" element) and a list of files using up the majority of the available UI space (the \"primary\" element).");
            StandardLayoutDescriptions.Add(StandardLayouts.PrimarySecondaryFormNoColor, "Primary/Secondary layouts arrange a primary element that takes up most of the screen, and adorns the arrangement with a secondary/auxiliary element. This is similar to how Windows Explorer is organized, with a tree on the left (the \"secondary\" element) and a list of files using up the majority of the available UI space (the \"primary\" element).\r\nThis version is well suited for UIs where the main element is a list of items.");
            StandardLayoutDescriptions.Add(StandardLayouts.PrimarySecondaryListForm, "Primary/Secondary layouts arrange a primary element that takes up most of the screen, and adorns the arrangement with a secondary/auxiliary element. This is similar to how Windows Explorer is organized, with a tree on the left (the \"secondary\" element) and a list of files using up the majority of the available UI space (the \"primary\" element).\r\nThis version does not create background colors.");
            StandardLayoutDescriptions.Add(StandardLayouts.PrimarySecondaryListFormNoColor, "Primary/Secondary layouts arrange a primary element that takes up most of the screen, and adorns the arrangement with a secondary/auxiliary element. This is similar to how Windows Explorer is organized, with a tree on the left (the \"secondary\" element) and a list of files using up the majority of the available UI space (the \"primary\" element).\r\nThis version is well suited for UIs where the main element is a list of items.\r\nThis version does not create background colors.");
            StandardLayoutDescriptions.Add(StandardLayouts.SimpleForm, "Simple form layouts are basic arrangements of controls, typically in a vertical or horizontal stack.\r\nThe layout is smart enough to create visually pleasing results by assigning meaningful spacing.");
            StandardLayoutDescriptions.Add(StandardLayouts.SimpleFormToFill, "Simple form layouts are basic arrangements of controls, typically in a vertical or horizontal stack.\r\nThe layout is smart enough to create visually pleasing results by assigning meaningful spacing.\r\nThis version fills the remaining screen real-estate (if any) with the last element.");
            StandardLayoutDescriptions.Add(StandardLayouts.StandardForm, "Standard form layouts use WPF standards (typically a Grid) and leave layout details up to the developer.");
            StandardLayoutDescriptions.Add(StandardLayouts.Tabs, "Uses a tab-control type of approach to layout. View.Title is used for the tab header title.");
        }

        /// <summary>
        /// Returns a standard layout enum value from the provided key (or StandardLayouts.None, if the key is not valid)
        /// </summary>
        /// <param name="layoutKey">The layout key.</param>
        /// <returns>Standard Layout</returns>
        public static StandardLayouts GetStandardLayoutEnumFromKey(string layoutKey)
        {
            if (StandardLayoutMapsBackward.ContainsKey(layoutKey))
                return StandardLayoutMapsBackward[layoutKey];
            return StandardLayouts.None;
        }

        /// <summary>
        /// Returns a standard layout string/key from the provided enum value
        /// </summary>
        /// <param name="layout">The layout key.</param>
        /// <returns>Standard layout resource key name</returns>
        public static string GetStandardLayoutKeyFromEnum(StandardLayouts layout)
        {
            if (StandardLayoutMaps.ContainsKey(layout))
                return StandardLayoutMaps[layout];
            return string.Empty;
        }

        /// <summary>
        /// Returns a standard layout description from the provided enum value
        /// </summary>
        /// <param name="layout">The layout key.</param>
        /// <returns>Standard layout description</returns>
        public static string GetStandardLayoutDescription(StandardLayouts layout)
        {
            if (StandardLayoutDescriptions.ContainsKey(layout))
                return StandardLayoutDescriptions[layout];
            return string.Empty;
        }

        /// <summary>
        /// Returns a standard layout title from the provided enum value
        /// </summary>
        /// <param name="layout">The layout key.</param>
        /// <returns>Standard layout title</returns>
        public static string GetStandardLayoutTitle(StandardLayouts layout)
        {
            if (StandardLayoutTitles.ContainsKey(layout))
                return StandardLayoutTitles[layout];
            return string.Empty;
        }
    }
}
