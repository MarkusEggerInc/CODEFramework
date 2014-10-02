using System.Windows;
using System.Windows.Media;

namespace CODE.Framework.Wpf.Utilities
{
    /// <summary>
    /// This class is a utility class used to render headers in edit forms
    /// </summary>
    public class AutoLayoutHeaderRenderer : DependencyObject
    {
        /// <summary>This method performs the actual render operation to show a text label</summary>
        /// <param name="dc">Drawing context</param>
        /// <param name="info">Render info</param>
        /// <param name="scale">The scale.</param>
        /// <param name="offset">The offset.</param>
        public virtual void RenderHeader(DrawingContext dc, AutoHeaderTextRenderInfo info, double scale, Point offset)
        {
            dc.PushTransform(new TranslateTransform(offset.X, offset.Y));
            dc.PushTransform(new ScaleTransform(scale, scale));
            dc.PushClip(new RectangleGeometry(info.RenderRect));
            info.FormattedText.SetMaxTextWidths(new[] { info.RenderRect.Width });
            info.FormattedText.MaxLineCount = 1;
            info.FormattedText.Trimming = TextTrimming.CharacterEllipsis;
            dc.DrawText(info.FormattedText, info.RenderRect.TopLeft);
            dc.Pop();
            dc.Pop();
            dc.Pop();
        }

        /// <summary>This method draws a rectangular background around a group of elements</summary>
        /// <param name="dc">The drawing context.</param>
        /// <param name="info">Render info</param>
        /// <param name="scale">The scale.</param>
        /// <param name="offset">The offset.</param>
        public virtual void RenderBackground(DrawingContext dc, GroupBackgroundRenderInfo info, double scale, Point offset)
        {
            dc.PushTransform(new TranslateTransform(offset.X, offset.Y));
            dc.PushTransform(new ScaleTransform(scale, scale));
            dc.DrawRectangle(info.Background, null, info.RenderRect);
            if (info.Border != null && info.BorderWidth > 0)
            {
                var rect2 = new Rect(info.RenderRect.X + (info.BorderWidth/2),
                                     info.RenderRect.Y + (info.BorderWidth/2),
                                     info.RenderRect.Width + info.BorderWidth,
                                     info.RenderRect.Height + info.BorderWidth);
                dc.DrawRectangle(null, new Pen(info.Border, info.BorderWidth), rect2);
            }
            dc.Pop();
            dc.Pop();
        }
    }

    /// <summary>Internal utility class used to convey header rendering information</summary>
    public class AutoHeaderTextRenderInfo
    {
        /// <summary>Gets or sets the text.</summary>
        /// <value>The text.</value>
        public string Text { get; set; }
        /// <summary>Gets or sets the render rectangle.</summary>
        /// <value>The render rectangle.</value>
        public Rect RenderRect { get; set; }
        /// <summary>Gets or sets the formatted text.</summary>
        /// <value>The formatted text.</value>
        public FormattedText FormattedText { get; set; }
    }

    /// <summary>Internal utility class used to convey group background render information</summary>
    public class GroupBackgroundRenderInfo
    {
        /// <summary>Gets or sets the render rectangle.</summary>
        /// <value>The render rectangle.</value>
        public Rect RenderRect { get; set; }
        /// <summary>Background brush</summary>
        /// <value>The background.</value>
        public Brush Background { get; set; }
        /// <summary>Border brush</summary>
        /// <value>The border.</value>
        public Brush Border { get; set; }
        /// <summary>Border width</summary>
        /// <value>The width of the border.</value>
        public double BorderWidth { get; set; }
    }
}   