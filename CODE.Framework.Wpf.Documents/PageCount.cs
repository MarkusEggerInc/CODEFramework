using System.Windows.Documents;

namespace CODE.Framework.Wpf.Documents
{
    /// <summary>Whenever this type of Run object is used within a text element, it shows the total page count</summary>
    public class PageCount : Run
    {
    }

    /// <summary>Whenever this type of Run object is used within a text element, it shows the current page</summary>
    public class CurrentPage : Run
    {
    }
}
