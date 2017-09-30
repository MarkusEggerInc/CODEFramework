using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using CODE.Framework.Core.Utilities;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>Standard view model interface, which can be used with all the standard data templates</summary>
    public interface IStandardViewModel
    {
        /// <summary>Text Element 1</summary>
        string Text1 { get; set; }

        /// <summary>Text Element 2</summary>
        string Text2 { get; set; }

        /// <summary>Text Element 3</summary>
        string Text3 { get; set; }

        /// <summary>Text Element 4</summary>
        string Text4 { get; set; }

        /// <summary>Text Element 5</summary>
        string Text5 { get; set; }

        /// <summary>Text Element 6</summary>
        string Text6 { get; set; }

        /// <summary>Text Element 7</summary>
        string Text7 { get; set; }

        /// <summary>Text Element 8</summary>
        string Text8 { get; set; }

        /// <summary>Text Element 9</summary>
        string Text9 { get; set; }

        /// <summary>Text Element 10</summary>
        string Text10 { get; set; }

        /// <summary>Identifier Text Element 1</summary>
        string Identifier1 { get; set; }

        /// <summary>Identifier Text Element 2</summary>
        string Identifier2 { get; set; }

        /// <summary>Text Element representing a number (such as an item count)</summary>
        string Number1 { get; set; }

        /// <summary>Second Text Element representing a number (such as an item count)</summary>
        string Number2 { get; set; }

        ///<summary>The text to display on the tool tip when this item is hovered over with the mouse</summary>
        string ToolTipText { get; set; }

        /// <summary>Image Element 1</summary>
        Brush Image1 { get; set; }

        /// <summary>Image Element 2</summary>
        Brush Image2 { get; set; }

        /// <summary>Image Element 3</summary>
        Brush Image3 { get; set; }

        /// <summary>Image Element 4</summary>
        Brush Image4 { get; set; }

        /// <summary>Image Element 5</summary>
        Brush Image5 { get; set; }

        /// <summary>Logo Element 1</summary>
        Brush Logo1 { get; set; }

        /// <summary>Logo Element 2</summary>
        Brush Logo2 { get; set; }

        /// <summary>Checked or selected flag</summary>
        bool IsChecked { get; set; }

        /// <summary>Generic color setting (expressed as a brush)</summary>
        Brush Color1 { get; set; }

        /// <summary>Generic color setting (expressed as a brush)</summary>
        Brush Color2 { get; set; }
    }

    /// <summary>Standard view model class based on IStandardViewModel</summary>
    public class StandardViewModel : ViewModel, IStandardViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StandardViewModel"/> class.
        /// </summary>
        public StandardViewModel()
        {
            PropertyChanged += (s, e) =>
            {
                if (_inBrushUpdating) return;
                if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName.StartsWith("Image") || e.PropertyName.StartsWith("Logo"))
                    CheckAllBrushesForResources(e.PropertyName);
            };

            var appEx = Application.Current as ApplicationEx;
            if (appEx != null)
                appEx.ThemeSwitched += (s, e) =>
                {
                    if (DefaultBrushSharingContextCollection != null) DefaultBrushSharingContextCollection.Clear();

                    if (!string.IsNullOrEmpty(_image1LoadedFromBrushResource))
                        LoadSharedImage1FromBrushResource(_image1LoadedFromBrushResource);
                    if (!string.IsNullOrEmpty(_image2LoadedFromBrushResource))
                        LoadSharedImage2FromBrushResource(_image2LoadedFromBrushResource);
                    if (!string.IsNullOrEmpty(_image3LoadedFromBrushResource))
                        LoadSharedImage3FromBrushResource(_image3LoadedFromBrushResource);
                    if (!string.IsNullOrEmpty(_image4LoadedFromBrushResource))
                        LoadSharedImage4FromBrushResource(_image4LoadedFromBrushResource);
                    if (!string.IsNullOrEmpty(_image5LoadedFromBrushResource))
                        LoadSharedImage5FromBrushResource(_image5LoadedFromBrushResource);
                    if (!string.IsNullOrEmpty(_logo1LoadedFromBrushResource))
                        LoadSharedLogo1FromBrushResource(_logo1LoadedFromBrushResource);
                    if (!string.IsNullOrEmpty(_logo2LoadedFromBrushResource))
                        LoadSharedLogo2FromBrushResource(_logo2LoadedFromBrushResource);
                };
        }

        private string _text1 = string.Empty;
        private string _text2 = string.Empty;
        private string _text3 = string.Empty;
        private string _text4 = string.Empty;
        private string _text5 = string.Empty;
        private string _text6 = string.Empty;
        private string _text7 = string.Empty;
        private string _text8 = string.Empty;
        private string _text9 = string.Empty;
        private string _text10 = string.Empty;

        private string _identifier1 = string.Empty;
        private string _identifier2 = string.Empty;

        private string _number1 = string.Empty;
        private string _number2 = string.Empty;

        private string _toolTipText = null; //string.Empty shows an empty tool top box

        private Brush _image1;
        private Brush _image2;
        private Brush _image3;
        private Brush _image4;
        private Brush _image5;

        private bool _image1BrushChecked;
        private bool _image2BrushChecked;
        private bool _image3BrushChecked;
        private bool _image4BrushChecked;
        private bool _image5BrushChecked;

        private Brush _logo1;
        private Brush _logo2;

        private bool _logo1BrushChecked;
        private bool _logo2BrushChecked;

        /// <summary>Key 1 (not used for display but can be used as an internal identifier)</summary>
        public Guid Key1 { get; set; }

        /// <summary>Key 2 (not used for display but can be used as an internal identifier)</summary>
        public Guid Key2 { get; set; }

        /// <summary>Key 3 (not used for display but can be used as an internal identifier)</summary>
        public Guid Key3 { get; set; }

        /// <summary>Key 4 (not used for display but can be used as an internal identifier)</summary>
        public Guid Key4 { get; set; }

        /// <summary>Key 5 (not used for display but can be used as an internal identifier)</summary>
        public Guid Key5 { get; set; }

        /// <summary>Text Element 1</summary>
        public virtual string Text1
        {
            get { return _text1; }
            set
            {
                _text1 = value;
                NotifyChanged("Text1");
            }
        }

        /// <summary>Text Element 2</summary>
        public virtual string Text2
        {
            get { return _text2; }
            set
            {
                _text2 = value;
                NotifyChanged("Text2");
            }
        }

        /// <summary>Text Element 3</summary>
        public virtual string Text3
        {
            get { return _text3; }
            set
            {
                _text3 = value;
                NotifyChanged("Text3");
            }
        }

        /// <summary>Text Element 4</summary>
        public virtual string Text4
        {
            get { return _text4; }
            set
            {
                _text4 = value;
                NotifyChanged("Text4");
            }
        }

        /// <summary>Text Element 5</summary>
        public virtual string Text5
        {
            get { return _text5; }
            set
            {
                _text5 = value;
                NotifyChanged("Text5");
            }
        }

        /// <summary>Text Element 6</summary>
        public virtual string Text6
        {
            get { return _text6; }
            set
            {
                _text6 = value;
                NotifyChanged("Text6");
            }
        }

        /// <summary>Text Element 7</summary>
        public virtual string Text7
        {
            get { return _text7; }
            set
            {
                _text7 = value;
                NotifyChanged("Text7");
            }
        }

        /// <summary>Text Element 8</summary>
        public virtual string Text8
        {
            get { return _text8; }
            set
            {
                _text8 = value;
                NotifyChanged("Text8");
            }
        }

        /// <summary>Text Element 9</summary>
        public virtual string Text9
        {
            get { return _text9; }
            set
            {
                _text9 = value;
                NotifyChanged("Text9");
            }
        }

        /// <summary>Text Element 10</summary>
        public virtual string Text10
        {
            get { return _text10; }
            set
            {
                _text10 = value;
                NotifyChanged("Text10");
            }
        }

        /// <summary>Identifier Text Element 1</summary>
        public virtual string Identifier1
        {
            get { return _identifier1; }
            set
            {
                _identifier1 = value;
                NotifyChanged("Identifier1");
            }
        }

        /// <summary>Identifier Text Element 2</summary>
        public virtual string Identifier2
        {
            get { return _identifier2; }
            set
            {
                _identifier2 = value;
                NotifyChanged("Identifier2");
            }
        }

        /// <summary>Text Element representing a number (such as an item count)</summary>
        public virtual string Number1
        {
            get { return _number1; }
            set
            {
                _number1 = value;
                NotifyChanged("Number1");
            }
        }

        /// <summary>Second Text Element representing a number (such as an item count)</summary>
        public virtual string Number2
        {
            get { return _number2; }
            set
            {
                _number2 = value;
                NotifyChanged("Number2");
            }
        }

        ///<summary>The text to display on the tool tip when this item is hovered over with the mouse</summary>
        public virtual string ToolTipText
        {
            get { return _toolTipText; }
            set
            {
                _toolTipText = value;
                NotifyChanged("ToolTipText");
            }
        }

        /// <summary>Image Element 1</summary>
        public virtual Brush Image1
        {
            get { return _image1; }
            set
            {
                _image1 = value;
                _image1BrushChecked = false;
                NotifyChanged("Image1");
            }
        }

        /// <summary>
        /// Assigns a standard icon as a brush
        /// </summary>
        /// <param name="icon">The icon.</param>
        public virtual void AssignImage1Icon(StandardIcons icon)
        {
            Image1 = GetBrushFromResource(StandardIconHelper.GetStandardIconKeyFromEnum(icon));
        }

        /// <summary>Image Element 2</summary>
        public virtual Brush Image2
        {
            get { return _image2; }
            set
            {
                _image2 = value;
                _image2BrushChecked = false;
                NotifyChanged("Image2");
            }
        }

        /// <summary>
        /// Assigns a standard icon as a brush
        /// </summary>
        /// <param name="icon">The icon.</param>
        public virtual void AssignImage2Icon(StandardIcons icon)
        {
            Image2 = GetBrushFromResource(StandardIconHelper.GetStandardIconKeyFromEnum(icon));
        }

        /// <summary>Image Element 3</summary>
        public virtual Brush Image3
        {
            get { return _image3; }
            set
            {
                _image3 = value;
                _image3BrushChecked = false;
                NotifyChanged("Image3");
            }
        }

        /// <summary>
        /// Assigns a standard icon as a brush
        /// </summary>
        /// <param name="icon">The icon.</param>
        public virtual void AssignImage3Icon(StandardIcons icon)
        {
            Image3 = GetBrushFromResource(StandardIconHelper.GetStandardIconKeyFromEnum(icon));
        }

        /// <summary>Image Element 4</summary>
        public virtual Brush Image4
        {
            get { return _image4; }
            set
            {
                _image4 = value;
                _image4BrushChecked = false;
                NotifyChanged("Image4");
            }
        }

        /// <summary>
        /// Assigns a standard icon as a brush
        /// </summary>
        /// <param name="icon">The icon.</param>
        public virtual void AssignImage4Icon(StandardIcons icon)
        {
            Image4 = GetBrushFromResource(StandardIconHelper.GetStandardIconKeyFromEnum(icon));
        }

        /// <summary>Image Element 5</summary>
        public virtual Brush Image5
        {
            get { return _image5; }
            set
            {
                _image5 = value;
                _image5BrushChecked = false;
                NotifyChanged("Image5");
            }
        }

        /// <summary>
        /// Assigns a standard icon as a brush
        /// </summary>
        /// <param name="icon">The icon.</param>
        public virtual void AssignImage5Icon(StandardIcons icon)
        {
            Image5 = GetBrushFromResource(StandardIconHelper.GetStandardIconKeyFromEnum(icon));
        }

        /// <summary>Logo Element 1</summary>
        public virtual Brush Logo1
        {
            get { return _logo1; }
            set
            {
                _logo1 = value;
                _logo1BrushChecked = false;
                NotifyChanged("Logo1");
            }
        }

        /// <summary>
        /// Assigns a standard icon as a brush
        /// </summary>
        /// <param name="icon">The icon.</param>
        public virtual void AssignLogo1Icon(StandardIcons icon)
        {
            Logo1 = GetBrushFromResource(StandardIconHelper.GetStandardIconKeyFromEnum(icon));
        }

        /// <summary>Logo Element 2</summary>
        public virtual Brush Logo2
        {
            get { return _logo2; }
            set
            {
                _logo2 = value;
                _logo2BrushChecked = false;
                NotifyChanged("Logo2");
            }
        }

        /// <summary>
        /// Assigns a standard icon as a brush
        /// </summary>
        /// <param name="icon">The icon.</param>
        public virtual void AssignLogo2Icon(StandardIcons icon)
        {
            Logo2 = GetBrushFromResource(StandardIconHelper.GetStandardIconKeyFromEnum(icon));
        }

        /// <summary>
        /// Checked or selected flag
        /// </summary>
        /// <value><c>true</c> if this instance is checked; otherwise, <c>false</c>.</value>
        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                _isChecked = value; 
                NotifyChanged("IsChecked");
            }
        }

        /// <summary>Generic color setting (expressed as a brush)</summary>
        public Brush Color1
        {
            get { return _color1; }
            set
            {
                _color1 = value; 
                NotifyChanged("Color1");
            }
        }

        /// <summary>Generic color setting (expressed as a brush)</summary>
        public Brush Color2
        {
            get { return _color2; }
            set
            {
                _color2 = value; 
                NotifyChanged("Color2");
            }
        }

        /// <summary>
        /// Internal resource context (used to resolve XAML resources for icons)
        /// </summary>
        public FrameworkElement ResourceContextObject
        {
            get { return _resourceContextObject; }
            set
            {
                _resourceContextObject = value;
                NotifyChanged(); // Could change a lot, so we issue an object-global refresh
            }
        }

        private FrameworkElement _resourceContextObject;

        private bool _inBrushUpdating;

        private void CheckAllBrushesForResources(string propertyName = "")
        {
            if (ResourceContextObject == null) return;
            if (Image1 == null && Image2 == null && Image3 == null && Image4 == null && Image5 == null && Logo1 == null && Logo2 == null) return;

            var brushResources = ResourceHelper.GetBrushResources(ResourceContextObject);
            if (brushResources.Count == 0) return;

            _inBrushUpdating = true;

            if (!_image1BrushChecked && (string.IsNullOrEmpty(propertyName) || propertyName == "Image1"))
                If.Real<DrawingBrush>(Image1, drawing =>
                {
                    ResourceHelper.ReplaceDynamicDrawingBrushResources(drawing, brushResources);
                    _image1BrushChecked = true;
                    NotifyChanged("Image1");
                });
            if (!_image2BrushChecked && (string.IsNullOrEmpty(propertyName) || propertyName == "Image2"))
                If.Real<DrawingBrush>(Image2, drawing =>
                {
                    ResourceHelper.ReplaceDynamicDrawingBrushResources(drawing, brushResources);
                    _image2BrushChecked = true;
                    NotifyChanged("Image2");
                });
            if (!_image3BrushChecked && (string.IsNullOrEmpty(propertyName) || propertyName == "Image3"))
                If.Real<DrawingBrush>(Image3, drawing =>
                {
                    ResourceHelper.ReplaceDynamicDrawingBrushResources(drawing, brushResources);
                    _image3BrushChecked = true;
                    NotifyChanged("Image3");
                });
            if (!_image4BrushChecked && (string.IsNullOrEmpty(propertyName) || propertyName == "Image4"))
                If.Real<DrawingBrush>(Image4, drawing =>
                {
                    ResourceHelper.ReplaceDynamicDrawingBrushResources(drawing, brushResources);
                    _image4BrushChecked = true;
                    NotifyChanged("Image4");
                });
            if (!_image5BrushChecked && (string.IsNullOrEmpty(propertyName) || propertyName == "Image5"))
                If.Real<DrawingBrush>(Image5, drawing =>
                {
                    ResourceHelper.ReplaceDynamicDrawingBrushResources(drawing, brushResources);
                    _image5BrushChecked = true;
                    NotifyChanged("Image5");
                });
            if (!_logo1BrushChecked && (string.IsNullOrEmpty(propertyName) || propertyName == "Logo1"))
                If.Real<DrawingBrush>(Logo1, drawing =>
                {
                    ResourceHelper.ReplaceDynamicDrawingBrushResources(drawing, brushResources);
                    _logo1BrushChecked = true;
                    NotifyChanged("Logo1");
                });
            if (!_logo2BrushChecked && (string.IsNullOrEmpty(propertyName) || propertyName == "Logo2"))
                If.Real<DrawingBrush>(Logo2, drawing =>
                {
                    ResourceHelper.ReplaceDynamicDrawingBrushResources(drawing, brushResources);
                    _logo2BrushChecked = true;
                    NotifyChanged("Logo2");
                });

            _inBrushUpdating = false;
        }

        /// <summary>Tries to find a named XAML resource of type brush and returns it.</summary>
        /// <param name="resourceName">Name of the resource.</param>
        /// <returns>Brush or null</returns>
        /// <remarks>The returned brush is a clone, so it can be manipulated at will without impacting other users of the same brush.</remarks>
        public Brush GetBrushFromResource(string resourceName)
        {
            try
            {
                var resource = Application.Current.FindResource(resourceName);
                if (resource == null) return null;

                var brush = resource as Brush;
                if (brush == null) return null;

                return brush.Clone();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Loads a resource brush to be shared across all instances of this view model and assigns it to Image1</summary>
        /// <param name="resourceName">Name of the resource (brush).</param>
        /// <param name="sharingContextCollection">Sharing context collection (can be used to differentiate brush context between different subclasses of standard view models).</param>
        public void LoadSharedImage1FromBrushResource(string resourceName, Dictionary<string, Brush> sharingContextCollection = null)
        {
            if (sharingContextCollection == null)
                sharingContextCollection = DefaultBrushSharingContextCollection;

            if (!sharingContextCollection.ContainsKey(resourceName))
                sharingContextCollection.Add(resourceName, GetBrushFromResource(resourceName));
            else
                _image1BrushChecked = true; // We are reusing an existing brush, so we do not need to re-check it again
            _image1LoadedFromBrushResource = resourceName;

            Image1 = sharingContextCollection[resourceName];
        }

        /// <summary>Loads a resource brush to be shared across all instances of this view model and assigns it to Image1</summary>
        /// <param name="standardIcon">Standard icon to be used as the brush.</param>
        /// <param name="sharingContextCollection">Sharing context collection (can be used to differentiate brush context between different subclasses of standard view models).</param>
        public void LoadSharedImage1FromBrushResource(StandardIcons standardIcon, Dictionary<string, Brush> sharingContextCollection = null)
        {
            LoadSharedImage1FromBrushResource(StandardIconHelper.GetStandardIconKeyFromEnum(standardIcon), sharingContextCollection);
        }

        private string _image1LoadedFromBrushResource;

        /// <summary>Loads a resource brush to be shared across all instances of this view model and assigns it to Image2</summary>
        /// <param name="resourceName">Name of the resource (brush).</param>
        /// <param name="sharingContextCollection">Sharing context collection (can be used to differentiate brush context between different subclasses of standard view models).</param>
        public void LoadSharedImage2FromBrushResource(string resourceName, Dictionary<string, Brush> sharingContextCollection = null)
        {
            if (sharingContextCollection == null)
                sharingContextCollection = DefaultBrushSharingContextCollection;

            if (!sharingContextCollection.ContainsKey(resourceName))
                sharingContextCollection.Add(resourceName, GetBrushFromResource(resourceName));
            else
                _image2BrushChecked = true; // We are reusing an existing brush, so we do not need to re-check it again
            _image2LoadedFromBrushResource = resourceName;

            Image2 = sharingContextCollection[resourceName];
        }
        private string _image2LoadedFromBrushResource;

        /// <summary>Loads a resource brush to be shared across all instances of this view model and assigns it to Image2</summary>
        /// <param name="standardIcon">Standard icon to be used as the brush.</param>
        /// <param name="sharingContextCollection">Sharing context collection (can be used to differentiate brush context between different subclasses of standard view models).</param>
        public void LoadSharedImage2FromBrushResource(StandardIcons standardIcon, Dictionary<string, Brush> sharingContextCollection = null)
        {
            LoadSharedImage2FromBrushResource(StandardIconHelper.GetStandardIconKeyFromEnum(standardIcon), sharingContextCollection);
        }

        /// <summary>Loads a resource brush to be shared across all instances of this view model and assigns it to Image3</summary>
        /// <param name="resourceName">Name of the resource (brush).</param>
        /// <param name="sharingContextCollection">Sharing context collection (can be used to differentiate brush context between different subclasses of standard view models).</param>
        public void LoadSharedImage3FromBrushResource(string resourceName, Dictionary<string, Brush> sharingContextCollection = null)
        {
            if (sharingContextCollection == null)
                sharingContextCollection = DefaultBrushSharingContextCollection;

            if (!sharingContextCollection.ContainsKey(resourceName))
                sharingContextCollection.Add(resourceName, GetBrushFromResource(resourceName));
            else
                _image3BrushChecked = true; // We are reusing an existing brush, so we do not need to re-check it again
            _image3LoadedFromBrushResource = resourceName;

            Image3 = sharingContextCollection[resourceName];
        }
        private string _image3LoadedFromBrushResource;

        /// <summary>Loads a resource brush to be shared across all instances of this view model and assigns it to Image3</summary>
        /// <param name="standardIcon">Standard icon to be used as the brush.</param>
        /// <param name="sharingContextCollection">Sharing context collection (can be used to differentiate brush context between different subclasses of standard view models).</param>
        public void LoadSharedImage3FromBrushResource(StandardIcons standardIcon, Dictionary<string, Brush> sharingContextCollection = null)
        {
            LoadSharedImage3FromBrushResource(StandardIconHelper.GetStandardIconKeyFromEnum(standardIcon), sharingContextCollection);
        }

        /// <summary>Loads a resource brush to be shared across all instances of this view model and assigns it to Image4</summary>
        /// <param name="resourceName">Name of the resource (brush).</param>
        /// <param name="sharingContextCollection">Sharing context collection (can be used to differentiate brush context between different subclasses of standard view models).</param>
        public void LoadSharedImage4FromBrushResource(string resourceName, Dictionary<string, Brush> sharingContextCollection = null)
        {
            if (sharingContextCollection == null)
                sharingContextCollection = DefaultBrushSharingContextCollection;

            if (!sharingContextCollection.ContainsKey(resourceName))
                sharingContextCollection.Add(resourceName, GetBrushFromResource(resourceName));
            else
                _image4BrushChecked = true; // We are reusing an existing brush, so we do not need to re-check it again
            _image4LoadedFromBrushResource = resourceName;

            Image4 = sharingContextCollection[resourceName];
        }
        private string _image4LoadedFromBrushResource;

        /// <summary>Loads a resource brush to be shared across all instances of this view model and assigns it to Image4</summary>
        /// <param name="standardIcon">Standard icon to be used as the brush.</param>
        /// <param name="sharingContextCollection">Sharing context collection (can be used to differentiate brush context between different subclasses of standard view models).</param>
        public void LoadSharedImage4FromBrushResource(StandardIcons standardIcon, Dictionary<string, Brush> sharingContextCollection = null)
        {
            LoadSharedImage4FromBrushResource(StandardIconHelper.GetStandardIconKeyFromEnum(standardIcon), sharingContextCollection);
        }

        /// <summary>Loads a resource brush to be shared across all instances of this view model and assigns it to Image5</summary>
        /// <param name="resourceName">Name of the resource (brush).</param>
        /// <param name="sharingContextCollection">Sharing context collection (can be used to differentiate brush context between different subclasses of standard view models).</param>
        public void LoadSharedImage5FromBrushResource(string resourceName, Dictionary<string, Brush> sharingContextCollection = null)
        {
            if (sharingContextCollection == null)
                sharingContextCollection = DefaultBrushSharingContextCollection;

            if (!sharingContextCollection.ContainsKey(resourceName))
                sharingContextCollection.Add(resourceName, GetBrushFromResource(resourceName));
            else
                _image5BrushChecked = true; // We are reusing an existing brush, so we do not need to re-check it again
            _image5LoadedFromBrushResource = resourceName;

            Image5 = sharingContextCollection[resourceName];
        }
        private string _image5LoadedFromBrushResource;

        /// <summary>Loads a resource brush to be shared across all instances of this view model and assigns it to Image5</summary>
        /// <param name="standardIcon">Standard icon to be used as the brush.</param>
        /// <param name="sharingContextCollection">Sharing context collection (can be used to differentiate brush context between different subclasses of standard view models).</param>
        public void LoadSharedImage5FromBrushResource(StandardIcons standardIcon, Dictionary<string, Brush> sharingContextCollection = null)
        {
            LoadSharedImage5FromBrushResource(StandardIconHelper.GetStandardIconKeyFromEnum(standardIcon), sharingContextCollection);
        }

        /// <summary>Loads a resource brush to be shared across all instances of this view model and assigns it to Logo1</summary>
        /// <param name="resourceName">Name of the resource (brush).</param>
        /// <param name="sharingContextCollection">Sharing context collection (can be used to differentiate brush context between different subclasses of standard view models).</param>
        public void LoadSharedLogo1FromBrushResource(string resourceName, Dictionary<string, Brush> sharingContextCollection = null)
        {
            if (sharingContextCollection == null)
                sharingContextCollection = DefaultBrushSharingContextCollection;

            if (!sharingContextCollection.ContainsKey(resourceName))
                sharingContextCollection.Add(resourceName, GetBrushFromResource(resourceName));
            else
                _logo1BrushChecked = true; // We are reusing an existing brush, so we do not need to re-check it again
            _logo1LoadedFromBrushResource = resourceName;

            Logo1 = sharingContextCollection[resourceName];
        }
        private string _logo1LoadedFromBrushResource;

        /// <summary>Loads a resource brush to be shared across all instances of this view model and assigns it to Logo1</summary>
        /// <param name="standardIcon">Standard icon to be used as the brush.</param>
        /// <param name="sharingContextCollection">Sharing context collection (can be used to differentiate brush context between different subclasses of standard view models).</param>
        public void LoadSharedLogo1FromBrushResource(StandardIcons standardIcon, Dictionary<string, Brush> sharingContextCollection = null)
        {
            LoadSharedLogo1FromBrushResource(StandardIconHelper.GetStandardIconKeyFromEnum(standardIcon), sharingContextCollection);
        }

        /// <summary>Loads a resource brush to be shared across all instances of this view model and assigns it to Logo1</summary>
        /// <param name="resourceName">Name of the resource (brush).</param>
        /// <param name="sharingContextCollection">Sharing context collection (can be used to differentiate brush context between different subclasses of standard view models).</param>
        public void LoadSharedLogo2FromBrushResource(string resourceName, Dictionary<string, Brush> sharingContextCollection = null)
        {
            if (sharingContextCollection == null)
                sharingContextCollection = DefaultBrushSharingContextCollection;

            if (!sharingContextCollection.ContainsKey(resourceName))
                sharingContextCollection.Add(resourceName, GetBrushFromResource(resourceName));
            else
                _image2BrushChecked = true; // We are reusing an existing brush, so we do not need to re-check it again
            _logo2LoadedFromBrushResource = resourceName;

            Logo2 = sharingContextCollection[resourceName];
        }
        private string _logo2LoadedFromBrushResource;

        /// <summary>Loads a resource brush to be shared across all instances of this view model and assigns it to Logo2</summary>
        /// <param name="standardIcon">Standard icon to be used as the brush.</param>
        /// <param name="sharingContextCollection">Sharing context collection (can be used to differentiate brush context between different subclasses of standard view models).</param>
        public void LoadSharedLogo2FromBrushResource(StandardIcons standardIcon, Dictionary<string, Brush> sharingContextCollection = null)
        {
            LoadSharedLogo2FromBrushResource(StandardIconHelper.GetStandardIconKeyFromEnum(standardIcon), sharingContextCollection);
        }

        private bool _isChecked;
        private Brush _color1;
        private Brush _color2;

        /// <summary>Default sharing context collection</summary>
        private static readonly Dictionary<string, Brush> DefaultBrushSharingContextCollection = new Dictionary<string, Brush>();
    }

    /// <summary>
    /// Standard view model class with an additional Data property that can be used to attach any kind of additional source data object
    /// </summary>
    /// <typeparam name="TData">The type of the Data property.</typeparam>
    /// <seealso cref="CODE.Framework.Wpf.Mvvm.StandardViewModel" />
    public class StandardViewModel<TData> : StandardViewModel
    {
        private TData _data;

        /// <summary>
        /// Original data object
        /// </summary>
        /// <value>The data.</value>
        public TData Data
        {
            get { return _data; }
            set
            {
                _data = value; 
                NotifyChanged("Data");
            }
        }
    }
}