using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using CODE.Framework.Core.Utilities;

namespace CODE.Framework.Wpf.Documents
{
    /// <summary>
    /// A section of repeatable and templatable document content
    /// </summary>
    public class RepeatingSection : Section
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RepeatingSection" /> class.
        /// </summary>
        public RepeatingSection()
        {
            Loaded += (sender, e) => GenerateContent();
        }

        /// <summary>
        /// Items source (data source)
        /// </summary>
        /// <value>The items source.</value>
        public IEnumerable ItemsSource
        {
            get { return (IEnumerable) GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        /// <summary>
        /// Items source (data source)
        /// </summary>
        private static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof (IEnumerable), typeof (RepeatingSection), new PropertyMetadata((d, e) => If.Real<RepeatingSection>(d, i => i.ItemsSourceChanged())));

        /// <summary>
        /// Document template for each item
        /// </summary>
        /// <value>The item template.</value>
        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate) GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        /// <summary>
        /// Document template for each item
        /// </summary>
        private static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(RepeatingSection), new PropertyMetadata((d, e) => If.Real<RepeatingSection>(d, i => i.GenerateContent())));

        /// <summary>Item Template Selector.</summary>
        /// <value>The item template selector.</value>
        public DataTemplateSelector ItemTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(ItemTemplateSelectorProperty); }
            set { SetValue(ItemTemplateSelectorProperty, value); }
        }

        /// <summary>Item Template Selector.</summary>
        public static readonly DependencyProperty ItemTemplateSelectorProperty = DependencyProperty.Register("ItemTemplateSelector", typeof(DataTemplateSelector), typeof(RepeatingSection), new PropertyMetadata(null));

        /// <summary>
        /// Items panel template (defines the arrangement of each item relative to each other)
        /// </summary>
        /// <value>The items panel.</value>
        public DataTemplate ItemsPanel
        {
            get { return (DataTemplate) GetValue(ItemsPanelProperty); }
            set { SetValue(ItemsPanelProperty, value); }
        }

        /// <summary>
        /// Items panel template (defines the arrangement of each item relative to each other)
        /// </summary>
        private static readonly DependencyProperty ItemsPanelProperty = DependencyProperty.Register("ItemsPanel", typeof(DataTemplate), typeof(RepeatingSection), new PropertyMetadata((d, e) => If.Real<RepeatingSection>(d, i => i.GenerateContent())));

        /// <summary>
        /// Fires when the items souce changes
        /// </summary>
        private void ItemsSourceChanged()
        {
            if (ItemsSource != null)
            {
                try
                {
                    dynamic observable = ItemsSource;
                    if (ItemsSource is CollectionView)
                    {
                        var view = ItemsSource as CollectionView;
                        observable = view.SourceCollection;
                    }
                    // Using dynamic here, because without it, hooking the event generically on an 
                    // observable collection of unknown generic type is not trivial
                    observable.CollectionChanged += new NotifyCollectionChangedEventHandler(ItemsCollectionChanged);
                }
                catch (Exception)
                {
                }
            }

            GenerateContent();
        }

        /// <summary>
        /// Fires when the items collection changes
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="NotifyCollectionChangedEventArgs" /> instance containing the event data.</param>
        public void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            GenerateContent();
        }

        /// <summary>
        /// Generates content based on the template information
        /// </summary>
        /// <exception cref="System.Exception">ItemsPanel must be a block element</exception>
        private void GenerateContent()
        {
            if (!IsLoaded) return;

            Blocks.Clear();
            if ((ItemTemplate == null && ItemTemplateSelector == null) || ItemsSource == null) return;

            FrameworkContentElement panel = null;

            foreach (var data in ItemsSource)
            {
                if (panel == null)
                    if (ItemsPanel == null)
                        panel = this;
                    else
                    {
                        var itemsPanelTemplate = LoadDataTemplate(ItemsPanel);
                        foreach (var templatePart in itemsPanelTemplate)
                        {
                            var block = templatePart as Block;
                            if (block == null) throw new Exception("ItemsPanel must be a block element");
                            Blocks.Add(block);
                            panel = DocEx.GetItemsHost(block);
                            if (panel == null) throw new Exception("ItemsHost not found. Did you forget to specify DocEx.IsItemsHost?");
                        }
                    }

                var itemTemplate = ItemTemplateSelector != null ? ItemTemplateSelector.SelectTemplate(data, this) : ItemTemplate;
                var elements = LoadDataTemplate(itemTemplate);
                foreach (var element in elements)
                {
                    element.DataContext = data;

                    var expressions = element.GetValue(DocEx.ForceRefreshExpressionProperty);
                    if (expressions != null)
                    {
                        var bindings = expressions as List<BindingToSet>;
                        if (bindings != null)
                            foreach (var binding in bindings)
                            {
                                if (binding.Binding.Source == null && binding.Binding.RelativeSource == null)
                                    binding.Binding.Source = data;
                                var frameworkElement = binding.DependencyObject as FrameworkElement;
                                if (frameworkElement != null)
                                    frameworkElement.SetBinding(binding.Property, binding.Binding);
                                else
                                {
                                    var frameworkContentElement = binding.DependencyObject as FrameworkContentElement;
                                    if (frameworkContentElement != null)
                                        frameworkContentElement.SetBinding(binding.Property, binding.Binding);
                                }
                            }
                    }

                    var section = panel as Section;
                    var paragraph = panel as Paragraph;
                    var inline = element as Inline;
                    var tableRowGroup = panel as TableRowGroup;
                    var tableRow = element as TableRow;
                    var list = panel as List;
                    var listItem = element as ListItem;

                    if (section != null)
                        section.Blocks.Add(ConvertToBlock(data, element));
                    else if (paragraph != null && inline != null)
                    {
                        if (inline.Parent == null)
                            paragraph.Inlines.Add(inline);
                    }
                    else if (tableRowGroup != null && tableRow != null)
                    {
                        if (tableRow.Parent == null)
                            tableRowGroup.Rows.Add(tableRow);
                    }
                    else if (list != null && listItem != null)
                    {
                        if (listItem.Parent == null)
                            list.ListItems.Add(listItem);
                    }
                    else if (panel != null)
                        throw new Exception(String.Format("Can't add an instance of {0} to an instance of {1}", element.GetType(), panel.GetType()));
                    else
                        throw new Exception("Unable to add child elements.");
                }
            }
        }

        /// <summary>
        /// Loads the data template.
        /// </summary>
        /// <param name="dataTemplate">The data template.</param>
        /// <returns>List{FrameworkContentElement}.</returns>
        public static List<FrameworkContentElement> LoadDataTemplate(DataTemplate dataTemplate)
        {
            var elements = new List<FrameworkContentElement>();

            var documentDataTemplate = dataTemplate as DocumentDataTemplate;
            DependencyObject content;
            if (documentDataTemplate != null)
                content = documentDataTemplate.LoadDocumentTemplate();
            else 
                content = dataTemplate.LoadContent();

            var sp = content as StackPanel;
            if (sp != null)
                foreach (var child in sp.Children)
                {
                    var childDependencyObject = child as DependencyObject;
                    if (childDependencyObject != null)
                        elements.AddRange(ProcessDataTemplateItems(childDependencyObject));
                }
            else 
                elements.AddRange(ProcessDataTemplateItems(content));
            return elements;
        }

        /// <summary>
        /// Processes the data template items.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>FrameworkContentElement.</returns>
        /// <exception cref="System.Exception">Data template needs to contain a DocumentFragment, MultiFragment, or TextBlock.</exception>
        private static IEnumerable<FrameworkContentElement> ProcessDataTemplateItems(DependencyObject content)
        {
            var fragment = content as DocumentFragment;
            if (fragment != null)
                return new List<FrameworkContentElement> {fragment.Content};

            var multiFragment = content as DocumentMultiFragment;
            if (multiFragment != null)
            {
                var fragments = new List<FrameworkContentElement>();
                foreach (var itemFragment in multiFragment.Items)
                    fragments.Add(itemFragment);
                return fragments;
            }

            var textBlock = content as TextBlock;
            if (textBlock != null)
            {
                var inlines = textBlock.Inlines;
                if (inlines.Count == 1)
                    return new List<FrameworkContentElement> {inlines.FirstInline};
                var paragraph = new Paragraph();
                // we can't use an enumerator, since adding an inline removes it from its collection
                while (inlines.FirstInline != null)
                    paragraph.Inlines.Add(inlines.FirstInline);
                return new List<FrameworkContentElement> {paragraph};
            }
            throw new Exception("Data template needs to contain a <DocumentFragment>, <MultiFragment>, or <TextBlock>.");
        }

        /// <summary>
        /// Convert "data" to a flow document block object. If data is already a block, the return value is data recast.
        /// </summary>
        /// <param name="dataContext">only used when bindable content needs to be created</param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Block ConvertToBlock(object dataContext, object data)
        {
            var block = data as Block;
            if (block != null) return block;
            var inline = data as Inline;
            if (inline != null) return new Paragraph(inline);

            var bindingBase = data as BindingBase;
            if (bindingBase != null)
            {
                var run = new Run();
                var dataContextBindingBase = dataContext as BindingBase;
                if (dataContextBindingBase != null)
                    run.SetBinding(DataContextProperty, dataContextBindingBase);
                else
                    run.DataContext = dataContext;
                run.SetBinding(Run.TextProperty, bindingBase);
                return new Paragraph(run);
            }
            return new Paragraph(new Run { Text = (data == null) ? string.Empty : data.ToString() });
        }
    }
}