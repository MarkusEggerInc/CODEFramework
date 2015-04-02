using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using CODE.Framework.Core.Utilities;
using CODE.Framework.Wpf.Interfaces;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// Interface suppoted by all document engines
    /// </summary>
    public interface IViewEngine
    {
        /// <summary>
        /// Finds and instantiates the specified document 
        /// </summary>
        /// <param name="viewName">Name of the document to find</param>
        /// <param name="controllerName">Name of the controller that requested the document</param>
        /// <returns>Document result indicating the success of the operation</returns>
        ViewEngineResult GetView(string viewName, string controllerName);
   }

    /// <summary>
    /// Interface supported by all document-capable document engines
    /// </summary>
    public interface IDocumentEngine
    {
        /// <summary>
        /// Finds and instantiates the specified document (document)
        /// </summary>
        /// <param name="documentName">Name of the document to find</param>
        /// <param name="controllerName">Name of the controller that requested the document</param>
        /// <returns>Document result indicating the success of the operation</returns>
        DocumentEngineResult GetDocument(string documentName, string controllerName);
    }

    /// <summary>
    /// Interface based on IViewEngine that also supports themeing of document (re-theming on the fly)
    /// </summary>
    public interface IThemableViewEngine : IViewEngine
    {
        /// <summary>Changes the theme of the provided document from the old theme name to the new theme name</summary>
        /// <param name="view">The document.</param>
        /// <param name="newTheme">The new theme.</param>
        /// <param name="oldTheme">The old theme.</param>
        void ChangeViewTheme(FrameworkElement view, string newTheme, string oldTheme);
    }

    /// <summary>
    /// Object indicating the result of an attempt to find a document
    /// </summary>
    public class ViewEngineResult
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ViewEngineResult()
        {
            LocationsSearched = new List<string>();
        }

        /// <summary>
        /// Indicates whether the document was found
        /// </summary>
        /// <remarks>
        /// If true, Document and DocumentSource will be populated. Otherwise, LocationsSearched will be populated
        /// </remarks>
        public bool FoundView { get; set; }
        /// <summary>
        /// Actual document object
        /// </summary>
        /// <remarks>
        /// Only available of FoundDocument = true
        /// </remarks>
        public FrameworkElement View { get; set; }
        /// <summary>
        /// Original source of the document (for information purposes)
        /// </summary>
        /// <remarks>
        /// Only available of FoundDocument = true
        /// </remarks>
        public string ViewSource { get; set; }
        /// <summary>
        /// List of locations unsuccessfully searched
        /// </summary>
        /// <remarks>
        /// Only available of FoundDocument = false
        /// </remarks>
        public List<string> LocationsSearched { get; set; }
    }

    /// <summary>
    /// Object indicating the result of an attempt to find a document (document)
    /// </summary>
    public class DocumentEngineResult
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public DocumentEngineResult()
        {
            LocationsSearched = new List<string>();
        }

        /// <summary>
        /// Indicates whether the document was found
        /// </summary>
        /// <remarks>
        /// If true, Document and DocumentSource will be populated. Otherwise, LocationsSearched will be populated
        /// </remarks>
        public bool FoundDocument { get; set; }
        /// <summary>
        /// Actual document object
        /// </summary>
        /// <remarks>
        /// Only available of FoundDocument = true
        /// </remarks>
        public FrameworkContentElement Document { get; set; }
        /// <summary>
        /// Original source of the document (for information purposes)
        /// </summary>
        /// <remarks>
        /// Only available of FoundDocument = true
        /// </remarks>
        public string DocumentSource { get; set; }
        /// <summary>
        /// List of locations unsuccessfully searched
        /// </summary>
        /// <remarks>
        /// Only available of FoundDocument = false
        /// </remarks>
        public List<string> LocationsSearched { get; set; }
    }

    /// <summary>
    /// This document engine searches namespaces for matching views
    /// </summary>
    /// <remarks>
    /// For this engine to return a valid document, any of the application's assemblies
    /// must have a document (class) of the desired name (case-insensitive!) in a namespace that 
    /// ends in "Views.[controller]" or "Views.Shared"
    /// </remarks>
    public class CompiledXamlViewEngine : IViewEngine
    {
        /// <summary>
        /// Finds and instantiates the specified document
        /// </summary>
        /// <param name="viewName">Name of the document to find</param>
        /// <param name="controllerName">Name of the controller that requested the document</param>
        /// <returns>
        /// Document result indicating the success of the operation
        /// </returns>
        public ViewEngineResult GetView(string viewName, string controllerName)
        {
            if (_views == null) PopulateViews();

            viewName = viewName.ToLower();
            if (_views != null)
                foreach (var key in _views.Keys)
                    if (key == viewName)
                    {
                        var viewEntry = _views[key];
                        if (viewEntry.ViewHierarchy == controllerName || viewEntry.ViewHierarchy == "shared")
                            return new ViewEngineResult
                                {
                                    View = Activator.CreateInstance(_views[key].ViewType) as FrameworkElement,
                                    FoundView = true,
                                    ViewSource = "Class: " + _views[key].ViewType.FullName
                                };
                    }

            // TODO: Add information about all the locations searched for this document
            return new ViewEngineResult {FoundView = false};
        }

        private static void PopulateViews()
        {
            _views = new Dictionary<string, ViewInformation>();
            lock (_views) // When we do this, everyone else needs to wait so we do not end up populating this multiple times
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    string scopeName = assembly.ManifestModule.ScopeName;
                    if (scopeName != "CommonLanguageRuntimeLibrary" && !scopeName.StartsWith("Microsoft.") && !scopeName.StartsWith("System."))
                    {
                        var assemblyTypes = assembly.GetTypes();
                        foreach (var assemblyType in assemblyTypes)
                            if (assemblyType != null && assemblyType.Namespace != null)
                                if (assemblyType.Namespace.ToLower().Contains(".views.")) // By naming convention, views must be inside a namespace that contains ".Views."
                                    if (assemblyType.IsSubclassOf(typeof(FrameworkElement)))
                                    {
                                        if (assemblyType.Namespace != null)
                                        {
                                            var hierarchy = assemblyType.Namespace.ToLower();
                                            hierarchy = hierarchy.Substring(hierarchy.IndexOf(".views.", StringComparison.Ordinal) + 7);
                                            if (hierarchy.IndexOf(".", StringComparison.Ordinal) < 0) // There must be no . left in the namespace for this to qualify as a document
                                                _views.Add(assemblyType.Name.ToLower(), new ViewInformation {ViewType = assemblyType, ViewHierarchy = hierarchy});
                                        }
                                    }
                    }
                }
            }
        }
        private static Dictionary<string, ViewInformation> _views;

        private class ViewInformation
        {
            public Type ViewType { get; set; }
            public string ViewHierarchy { get; set; }
        }
    }
        
    /// <summary>
    /// This document engine searches an application's resources for matching views programmed as XAML pages.
    /// </summary>
    /// <remarks>
    /// For this engine to return a valid document, any of the application's assemblies
    /// must have a document (loose XAML) of the desired name (case-insensitive!) in a namespace that 
    /// ends in "Views.[controller]" or "Views.Shared"
    /// </remarks>
    public class LooseXamlViewEngine : IThemableViewEngine, IDocumentEngine
    {
        /// <summary>
        /// Finds and instantiates the specified document
        /// </summary>
        /// <param name="viewName">Name of the document to find</param>
        /// <param name="controllerName">Name of the controller that requested the document</param>
        /// <returns>
        /// Document result indicating the success of the operation
        /// </returns>
        public ViewEngineResult GetView(string viewName, string controllerName)
        {
            viewName = viewName.ToLower();

            var failureResult = new ViewEngineResult { FoundView = false };

            var searchPath = "/views/" + controllerName + "/" + viewName + ".xaml";
            var viewResult = TryGetView(searchPath);
            if (viewResult != null) return viewResult;
            failureResult.LocationsSearched.Add(searchPath);

            searchPath = "/views/shared/" + viewName + ".xaml";
            viewResult = TryGetView(searchPath);
            if (viewResult != null) return viewResult;
            failureResult.LocationsSearched.Add(searchPath);

            foreach (var assemblyName in ApplicationEx.SecondaryAssemblies.Keys)
            {
                var secondaryAssembly = ApplicationEx.SecondaryAssemblies[assemblyName];

                searchPath = "/" + secondaryAssembly.GetName().Name + ";component/views/" + controllerName + "/" + viewName + ".xaml";
                viewResult = TryGetView(searchPath);
                if (viewResult != null) return viewResult;
                failureResult.LocationsSearched.Add(searchPath);

                searchPath = "/" + secondaryAssembly.GetName().Name + ";component/views/shared/" + viewName + ".xaml";
                viewResult = TryGetView(searchPath);
                if (viewResult != null) return viewResult;
                failureResult.LocationsSearched.Add(searchPath);
            }

            return failureResult;
        }

        /// <summary>Changes the theme of the provided document from the old theme name to the new theme name</summary>
        /// <param name="view">The document.</param>
        /// <param name="newTheme">The new theme.</param>
        /// <param name="oldTheme">The old theme.</param>
        public void ChangeViewTheme(FrameworkElement view, string newTheme, string oldTheme)
        {
            if (view == null) return;
            var realView = view as View;
            if (realView == null) return;

            var location = realView.OriginalViewLoadLocation;
            if (string.IsNullOrEmpty(location)) return; // No chance to find themes at this point
            var location2 = location;
            if (location2.ToLower().EndsWith(".xaml")) location2 = location2.Substring(0, location2.Length - 5);

            for (int counter = view.Resources.MergedDictionaries.Count - 1; counter >= 0; counter--)
            {
                var dictionary = view.Resources.MergedDictionaries[counter];
                if (dictionary != null && dictionary.Source != null)
                    if (!dictionary.Source.IsAbsoluteUri)
                        if (!string.IsNullOrEmpty(dictionary.Source.OriginalString))
                            if (dictionary.Source.OriginalString == location2 + "." + oldTheme + ".xaml")
                                view.Resources.MergedDictionaries.RemoveAt(counter);
            }
            if (!ApplyResource(view, location2 + "." + newTheme + ".xaml"))
                ApplyResource(view, location2 + ".Default.xaml");
        }

        /// <summary>
        /// Tries the get the document.
        /// </summary>
        /// <param name="searchPath">The search path.</param>
        /// <returns>ViewEngineResult.</returns>
        protected virtual ViewEngineResult TryGetView(string searchPath)
        {
            try
            {
                var uri = searchPath.StartsWith("pack://") ? new Uri(searchPath) : new Uri(searchPath, UriKind.Relative);
                var view = Application.LoadComponent(uri) as FrameworkElement;
                if (view != null)
                {
                    var viewInfo = view as IViewInformation;
                    if (viewInfo != null)
                        viewInfo.OriginalViewLoadLocation = searchPath;

                    var viewResult = new ViewEngineResult
                    {
                        View = view,
                        FoundView = true,
                        ViewSource = "XAML Resource: " + searchPath
                    };

                    TryAttachingLayoutResources(view, searchPath);

                    return viewResult;
                }
            }
            catch
            {
            }
            return null;
        }

        private static bool _searchedForAppEx;
        private static ApplicationEx _appEx;

        /// <summary>
        /// Attempts to load additional layout resource dictionaries if present.
        /// </summary>
        /// <param name="view">Document to attach these layouts to</param>
        /// <param name="searchPath">Search path to look in</param>
        protected virtual void TryAttachingLayoutResources(FrameworkElement view, string searchPath)
        {
            if (searchPath.EndsWith(".xaml")) searchPath = searchPath.Substring(0, searchPath.Length - 5);

            if (!_searchedForAppEx)
            {
                _searchedForAppEx = true;
                if (Application.Current is ApplicationEx)
                    _appEx = Application.Current as ApplicationEx;
            }

            if (_appEx != null && !string.IsNullOrEmpty(_appEx.Theme))
            {
                ApplyResource(view, searchPath + ".AllThemes.xaml");
                if (!ApplyResource(view, searchPath + "." + _appEx.Theme + ".xaml"))
                    ApplyResource(view, searchPath + ".Default.xaml");
            }

            ApplyResource(view, searchPath + ".layout.xaml");
        }

        /// <summary>
        /// Attempts to load additional layout resource dictionaries if present.
        /// </summary>
        /// <param name="document">Document to attach these layouts to</param>
        /// <param name="searchPath">Search path to look in</param>
        protected virtual void TryAttachingDocumentLayoutResources(FrameworkContentElement document, string searchPath)
        {
            if (searchPath.EndsWith(".xaml")) searchPath = searchPath.Substring(0, searchPath.Length - 5);

            if (!_searchedForAppEx)
            {
                _searchedForAppEx = true;
                if (Application.Current is ApplicationEx)
                    _appEx = Application.Current as ApplicationEx;
            }

            if (_appEx != null && !string.IsNullOrEmpty(_appEx.Theme))
            {
                ApplyDocumentResource(document, searchPath + ".AllThemes.xaml");
                if (!ApplyDocumentResource(document, searchPath + "." + _appEx.Theme + ".xaml"))
                    ApplyDocumentResource(document, searchPath + ".Default.xaml");
            }

            ApplyDocumentResource(document, searchPath + ".layout.xaml");
        }

        /// <summary>
        /// Attempts to apply a resource to a framework element
        /// </summary>
        /// <param name="view"></param>
        /// <param name="searchPath"></param>
        /// <returns></returns>
        private static bool ApplyResource(FrameworkElement view, string searchPath)
        {
            var retVal = false;

            var searchCounter = -1;
            while (true)
            {
                var currentSearchPath = searchPath;
                if (searchCounter != -1)
                    currentSearchPath = currentSearchPath.Replace(".xaml", "." + searchCounter + ".xaml");
                searchCounter++;

                var source = new Uri(currentSearchPath, UriKind.Relative);
                try
                {
                    var resources = Application.LoadComponent(source) as ResourceDictionary;
                    if (resources != null)
                    {
                        resources.Source = source;
                        view.Resources.MergedDictionaries.Add(resources);
                        retVal = true;
                    }
                }
                catch
                {
                    // Up to .xaml, .0.xaml, .1.xaml, we try anyway. If we haven't found anything by then, we are done
                    if (searchCounter > 1)
                        break;
                }
            }
            return retVal;
        }

        /// <summary>
        /// Attempts to apply a resource to a framework element
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="searchPath">The search path.</param>
        /// <remarks>This method searches for the specified resource dictionary and keeps searching for additional files of the same name with a numberd sequence</remarks>
        /// <returns>True or false</returns>
        private static bool ApplyDocumentResource(FrameworkContentElement document, string searchPath)
        {
            var retVal = false;

            var searchCounter = -1;
            while (true)
            {
                var currentSearchPath = searchPath;
                if (searchCounter != -1)
                    currentSearchPath = currentSearchPath.Replace(".xaml", "." + searchCounter + ".xaml");
                searchCounter++;

                var source = new Uri(currentSearchPath, UriKind.Relative);
                try
                {
                    var resources = Application.LoadComponent(source) as ResourceDictionary;
                    if (resources != null)
                    {
                        resources.Source = source;
                        document.Resources.MergedDictionaries.Add(resources);
                        retVal = true;
                    }
                }
                catch
                {
                    // Up to .xaml, .0.xaml, .1.xaml, we try anyway. If we haven't found anything by then, we are done
                    if (searchCounter > 1)
                        break;
                }
            }
            return retVal;
        }

        /// <summary>
        /// Finds and instantiates the specified document (document)
        /// </summary>
        /// <param name="documentName">Name of the document to find</param>
        /// <param name="controllerName">Name of the controller that requested the document</param>
        /// <returns>Document result indicating the success of the operation</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public DocumentEngineResult GetDocument(string documentName, string controllerName)
        {
            documentName = documentName.ToLower();

            var failureResult = new DocumentEngineResult { FoundDocument = false };

            var searchPath = "/views/" + controllerName + "/" + documentName + ".xaml";
            var documentResult = TryGetDocument(searchPath);
            if (documentResult != null) return documentResult;
            failureResult.LocationsSearched.Add(searchPath);

            searchPath = "/views/shared/" + documentName + ".xaml";
            documentResult = TryGetDocument(searchPath);
            if (documentResult != null) return documentResult;
            failureResult.LocationsSearched.Add(searchPath);

            searchPath = "/views/shareddocuments/" + documentName + ".xaml";
            documentResult = TryGetDocument(searchPath);
            if (documentResult != null) return documentResult;
            failureResult.LocationsSearched.Add(searchPath);

            foreach (var assemblyName in ApplicationEx.SecondaryAssemblies.Keys)
            {
                var secondaryAssembly = ApplicationEx.SecondaryAssemblies[assemblyName];

                searchPath = "/" + secondaryAssembly.GetName().Name + ";component/views/" + controllerName + "/" + documentName + ".xaml";
                documentResult = TryGetDocument(searchPath);
                if (documentResult != null) return documentResult;
                failureResult.LocationsSearched.Add(searchPath);

                searchPath = "/" + secondaryAssembly.GetName().Name + ";component/views/shared/" + documentName + ".xaml";
                documentResult = TryGetDocument(searchPath);
                if (documentResult != null) return documentResult;
                failureResult.LocationsSearched.Add(searchPath);

                searchPath = "/" + secondaryAssembly.GetName().Name + ";component/views/shareddocuments/" + documentName + ".xaml";
                documentResult = TryGetDocument(searchPath);
                if (documentResult != null) return documentResult;
                failureResult.LocationsSearched.Add(searchPath);
            }
            
            return failureResult;
        }

        /// <summary>
        /// Tries the get the document.
        /// </summary>
        /// <param name="searchPath">The search path.</param>
        /// <returns>ViewEngineResult.</returns>
        protected virtual DocumentEngineResult TryGetDocument(string searchPath)
        {
            try
            {
                var document = Application.LoadComponent(new Uri(searchPath, UriKind.Relative)) as FrameworkContentElement;
                if (document != null)
                {
                    var documentInfo = document as ISourceInformation;
                    if (documentInfo != null)
                        documentInfo.OriginalLoadLocation = searchPath;

                    var documentResult = new DocumentEngineResult
                        {
                            Document = document,
                            FoundDocument = true,
                            DocumentSource = "XAML Resource: " + searchPath
                        };

                    TryAttachingDocumentLayoutResources(document, searchPath);

                    return documentResult;
                }
            }
            catch
            {
            }
            return null;
        }
    }

    /// <summary>
    /// This document engine can serve up standard views for the current theme
    /// </summary>
    public class StandardViewEngine : IThemableViewEngine
    {
        /// <summary>Finds and instantiates the specified document</summary>
        /// <param name="viewName">Name of the document to find</param>
        /// <param name="controllerName">Name of the controller that requested the document</param>
        /// <returns>Document result indicating the success of the operation</returns>
        public ViewEngineResult GetView(string viewName, string controllerName)
        {
            if (!viewName.StartsWith("CODEFrameworkStandardView")) return new ViewEngineResult {FoundView = false};
            if (viewName.StartsWith("CODEFrameworkStandardViewNone")) return new ViewEngineResult {FoundView = false};

            viewName = viewName.Replace("CODEFrameworkStandardView", string.Empty);

            var result = new ViewEngineResult();

            var view = new StandardViewGrid {ViewName = viewName};
            view.Children.Add(GetViewInternal(viewName));

            result.FoundView = true;
            result.View = view;
            result.ViewSource = "Standard Document: " + viewName;

            return result;
        }

        private FrameworkElement GetViewInternal(string viewName)
        {
            var features = ApplicationEx.GetStandardThemeFeatures();
            if (features == null) return null;

            var factory = features.StandardViewFactory;
            if (factory == null) return null;

            return factory.GetStandardView(viewName);
        }

        /// <summary>Changes the theme of the provided document from the old theme name to the new theme name</summary>
        /// <param name="view">The document.</param>
        /// <param name="newTheme">The new theme.</param>
        /// <param name="oldTheme">The old theme.</param>
        public void ChangeViewTheme(FrameworkElement view, string newTheme, string oldTheme)
        {
            var grid = view as StandardViewGrid;
            if (grid != null)
            {
                grid.Children.Clear();
                grid.Children.Add(GetViewInternal(grid.ViewName));
            }
        }
    }

    /// <summary>
    /// For internal use only
    /// </summary>
    public class StandardViewGrid : Grid
    {
        /// <summary>
        /// Document name
        /// </summary>
        public string ViewName { get; set; }
    }
}
