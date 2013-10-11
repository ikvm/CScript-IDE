using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using jinx.RoslynEditor.SyntaxVisualizer;
using jinx.RoslynEditor.RoslynExtensions;
using jinxapp.DomainServices.GrammarDefinition;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using jinxapp;
using Roslyn.Services;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit;
using System.IO;
using System.Xml;
using jinxapp.RoslynEditor;

namespace jinx.RoslynEditor
{
    public enum EditerType
    {
        CSharp,
        Javascript
    }
    public interface IEditor
    {
        /// <summary>
        /// 设置文字 
        /// </summary>
        /// <param name="text"></param>
        void SetText(string text);
        /// <summary>
        /// 得到编辑器中的文字 
        /// </summary>
        /// <param name="text"></param>
        string GetText();
        /// <summary>
        /// 编辑器内容属性
        /// </summary>
        string Text { get; }
        /// <summary>
        /// 选择编辑器中的内容
        /// </summary>
        /// <param name="spanStart">开始位置</param>
        /// <param name="spanLength">长度</param>
        void SelectText(int spanStart, int spanLength);
        /// <summary>
        /// 编辑器容器
        /// </summary>
        ITextContainer TextContainer {  get; }
        /// <summary>
        /// 编辑器内容改变事件
        /// </summary>
        event EventHandler EditorTextChanged;
        /// <summary>
        /// 编辑器对应文档ID
        /// </summary>
        DocumentId DocumentID { set; get; }
        /// <summary>
        /// 标题
        /// </summary>
        string Title { set; get; }
    }

    /// <summary>
    ///
    /// </summary>
    public partial class RoslynEditor : UserControl,IEditor ,IDisposable
    {

        private InteractiveManager _interactiveManager;
        private RoslynEditorInsightWindow _insightWindow;
        private CompletionWindow _completionWindow;

        public RoslynEditor()
        {
            InitializeComponent();

            _interactiveManager = ApplicationService.Services.Take<InteractiveManager>();
            ConfigureEditor();
         
        }

        private void ConfigureEditor()
        {
            if (EditerType == EditerType.CSharp)
            {

                //Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
                //Editor.TextArea.LeftMargins.Insert(0, new BreakPointMargin());
                setSyntaxHightlight(Editor);
                Editor.TextArea.TextEntering += OnTextEntering;
                Editor.TextArea.TextEntered += OnTextEntered;
                Editor.TextChanged += Editor_TextChanged;
                Editor.TextArea.KeyDown += TextArea_KeyDown;
                Editor.TextArea.KeyUp += TextArea_KeyUp;
                Editor.MouseHover += Editor_MouseHover;
                Editor.MouseHoverStopped += Editor_MouseHoverStopped;
            }
            else if(EditerType == EditerType.Javascript)
            {
                Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("JavaScript");
            }

            DispatcherTimer foldingUpdateTimer = new DispatcherTimer(DispatcherPriority.Normal, this.Dispatcher);
            foldingUpdateTimer.Interval = TimeSpan.FromSeconds(2);
            foldingUpdateTimer.Tick += foldingUpdateTimer_Tick;
            foldingUpdateTimer.Start();
            
            Editor.TextArea.IndentationStrategy = new ICSharpCode.AvalonEdit.Indentation.CSharp.CSharpIndentationStrategy(Editor.Options);
            foldingStrategy = new BraceFoldingStrategy();

            if (foldingStrategy != null)
            {
                if (foldingManager == null)
                    foldingManager = FoldingManager.Install(Editor.TextArea);
                foldingStrategy.UpdateFoldings(foldingManager, Editor.Document);
            }
            else
            {
                if (foldingManager != null)
                {
                    FoldingManager.Uninstall(foldingManager);
                    foldingManager = null;
                }
            }
            _completionWindow = new CompletionWindow(Editor.TextArea);
            _completionWindow = null;
        }

        private void setSyntaxHightlight(TextEditor editor)
        {
            IHighlightingDefinition customHighlighting;
            using (Stream s = typeof(RoslynEditor).Assembly.GetManifestResourceStream("jinxapp.RoslynEditor.Resources.CSharp-Mode.xshd"))
            {
                if (s == null)
                    throw new InvalidOperationException("Could not find embedded resource");
                using (XmlReader reader = new XmlTextReader(s))
                {
                    customHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.
                        HighlightingLoader.Load(reader, HighlightingManager.Instance);
                }
            }

            editor.SyntaxHighlighting = customHighlighting;
        }
    

        #region SyntaxVisualizer

        private void NavigateToSource(TextSpan span)
        {
            if(span.Start>= 0 && span.Length>0)
                SelectText(span.Start, span.Length);
        }

        public async void SelectText(int spanStart, int spanLength)
        {
            if (spanStart >= 0 && spanLength > 0)
            {
                Editor.Select(spanStart, spanLength);

                var docLine = Editor.TextArea.Document.GetLineByOffset(spanStart);

                Editor.ScrollToLine(docLine.LineNumber);
            }
        
        }
        #endregion


        #region editor event

        bool istypeset = false;

        async void Editor_TextChanged(object sender, EventArgs e)
        {
            if (!istypeset)
                istypeset = true;
            SetValue(TextProperty, Editor.Text);
            if (istypeset)
                istypeset = false;

            if (this.EditerType == EditerType.CSharp)
            {

                if (!string.IsNullOrEmpty(Editor.Text))
                {
                    if (EditorTextChanged != null)
                        EditorTextChanged(this, null);

                    var definitions = _interactiveManager.GetGrammarDefinitionList();
                    if (definitions != null)
                    {
                        this.childrenDropDown.ItemsSource = null;
                        this.definitionDropDown.ItemsSource = definitions;
                    }
                }
            }
        }

        ToolTip toolTip = new ToolTip();

        void Editor_MouseHoverStopped(object sender, MouseEventArgs e)
        {
            toolTip.IsOpen = false;
        }

        void Editor_MouseHover(object sender, MouseEventArgs e)
        {
            var pos = Editor.GetPositionFromPoint(e.GetPosition(Editor));
            if (pos != null)
            {
                toolTip.PlacementTarget = this; // required for property inheritance
                var docLine = Editor.TextArea.Document.Lines[pos.Value.Line - 1];
                int startOfWord = TextUtilities.GetNextCaretPosition(Editor.TextArea.Document, docLine.Offset + pos.Value.Column, LogicalDirection.Backward, CaretPositioningMode.WordBorderOrSymbol);

                int endOfWord = TextUtilities.GetNextCaretPosition(Editor.TextArea.Document, docLine.Offset + pos.Value.Column, LogicalDirection.Forward,
                                                                   CaretPositioningMode.WordBorder);
                string msg = null;
                if (startOfWord < endOfWord && startOfWord >= 0)
                    msg = Editor.TextArea.Document.GetText(startOfWord, endOfWord - startOfWord).Replace(".","").Trim();

                if (!string.IsNullOrEmpty(msg))
                {
                    var position = Editor.CaretOffset;
                    var completions = _interactiveManager.GetCompletion(endOfWord - 1);
                    if (completions != null)
                    {
                        var comp = completions.FirstOrDefault(c => c.DisplayText == msg);
                        if (comp == null)
                        {
                            completions = _interactiveManager.GetCompletion(position);
                            if(completions!=null)
                                comp = completions.FirstOrDefault(c => c.DisplayText == msg);
                        }


                        if (comp != null)
                        {
                            CompletionDescription cd = new CompletionDescription();

                            cd.DataContext = Roslyn.Compilers.SymbolDisplayExtensions.ToDisplayString(comp.GetDescription());
                            toolTip.Content = cd;
                            toolTip.IsOpen = true;
                        }
                    }
                }
               
                e.Handled = true;
            }
        }

        void TextArea_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyStates == Keyboard.GetKeyStates(Key.J) && Keyboard.Modifiers == ModifierKeys.Control)
            {
                OnTextEntered(this, null);
                e.Handled = false;

            }
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (_completionWindow != null)
                {
                    _completionWindow.Opacity = 0.5;
                    e.Handled = false;
                }
            }

           
            if (e.KeyStates == Keyboard.GetKeyStates(Key.M) && Keyboard.Modifiers == ModifierKeys.Control)
            {
                foreach (var foldingItem in foldingManager.AllFoldings)
                {
                    foldingItem.IsFolded = !foldingItem.IsFolded;
                }
            }
        }

        void TextArea_KeyUp(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (_completionWindow != null)
                {
                    _completionWindow.Opacity = 0.9;
                }
            }
        }


        private async void OnTextEntered(object sender, TextCompositionEventArgs e)
        {
            string keystring = string.Empty;
            keystring = e == null ? "" : e.Text;  
            var position = Editor.CaretOffset;
            if (position > 0 && _interactiveManager.IsCompletionTriggerCharacter(position-1))
            {
                if (keystring != "(")
                {
                    _completionWindow = new CompletionWindow(Editor.TextArea);
                    _completionWindow.Opacity = 0.9;
                    _completionWindow.WindowStyle = WindowStyle.None;
                    _completionWindow.Width = 300;
                    _completionWindow.Background = Brushes.Black;
                    _completionWindow.BorderThickness = new Thickness(0);
                    var data = _completionWindow.CompletionList.CompletionData;

                    var completionDataList = await _interactiveManager.GetCompletionAsync(position, keystring);

                    foreach (var completionData in completionDataList)
                    {
                        data.Add(new AvalonEditCompletionData(completionData));
                    }

                    _completionWindow.Show();
                    _completionWindow.Closed += delegate
                    {
                        _completionWindow = null;
                    };
                }
                else if(keystring == "(" || keystring == ",")
                {
                    _insightWindow = new RoslynEditorInsightWindow(Editor.TextArea);
                    _insightWindow.Foreground = Brushes.WhiteSmoke;
                    _insightWindow.Background = Brushes.Black;

                    var items = _interactiveManager.GetInsightTip(Editor.CaretOffset);
                    if(items!=null && items.Count > 0)
                        _insightWindow.AddRangeItems(items);

                    _insightWindow.Show();
                    _insightWindow.Closed += delegate
                    {
                        _insightWindow = null;
                    };
                }
            }

            istypeset = false;

        }



        private void OnTextEntering(object sender, TextCompositionEventArgs e)
        {
            istypeset = true;
            if (e.Text.Length > 0 && _completionWindow != null)
            {
                if (!char.IsLetterOrDigit(e.Text[0]))
                {
                    _completionWindow.CompletionList.RequestInsertion(e);
                }
            }
        }

        private void definitionDropDown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                var definition = e.AddedItems[0] as GrammarDefinition;
                childrenDropDown.ItemsSource = null;
                childrenDropDown.ItemsSource = definition.Children;
            }
        }

        private void childrenDropDown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                var definition = e.AddedItems[0] as GrammarDefinition;
                this.NavigateToSource(definition.SyntaxNode.Span);
            }
        }


        #endregion


        #region DependencyProperty

        public static DependencyProperty EditerTypeProperty = DependencyProperty.Register("EditerType", typeof(EditerType), typeof(RoslynEditor), new UIPropertyMetadata(new PropertyChangedCallback(EditerTypeChanged)));
        public EditerType EditerType
        {
            set
            {
                SetValue(EditerTypeProperty, value);
            }
            get
            {
                return (EditerType)GetValue(EditerTypeProperty);
            }

        }

        static void InitTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var that = d as RoslynEditor;
            that.Editor.Text= e.NewValue.ToString();
        }

        static void TextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var that = d as RoslynEditor;
            if (e.OldValue == null && e.OldValue != e.NewValue && !that.istypeset)
                that.Editor.AppendText(e.NewValue.ToString());
        }

        static void EditerTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var that = d as RoslynEditor;
            that.Editor.TextArea.TextEntering -= that.OnTextEntering;
            that.Editor.TextArea.TextEntered -= that.OnTextEntered;
            that.Editor.TextChanged -= that.Editor_TextChanged;
            that.Editor.TextArea.KeyDown -= that.TextArea_KeyDown;
            that.Editor.MouseHover -= that.Editor_MouseHover;
            that.Editor.MouseHoverStopped -= that.Editor_MouseHoverStopped;
           

            if ((EditerType)(e.NewValue) == EditerType.CSharp)
            {

                //that.Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
                that.setSyntaxHightlight(that.Editor);
                that.Editor.TextArea.TextEntering -= that.OnTextEntering;
                that.Editor.TextArea.TextEntered -= that.OnTextEntered;
                that.Editor.TextArea.TextEntering += that.OnTextEntering;
                that.Editor.TextArea.TextEntered += that.OnTextEntered;
                that.Editor.TextChanged += that.Editor_TextChanged;
                that.Editor.TextArea.KeyDown += that.TextArea_KeyDown;
                that.Editor.MouseHover += that.Editor_MouseHover;
                that.Editor.MouseHoverStopped += that.Editor_MouseHoverStopped;
         
            }
            else if ((EditerType)(e.NewValue) == EditerType.Javascript)
            {
                that.Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("JavaScript");
                that.grid1.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel);
                that.grid1.RowDefinitions[0].Height = new GridLength(0, GridUnitType.Pixel);
            }
        }

        public static DependencyProperty InitTextProperty = DependencyProperty.Register("InitText", typeof(string), typeof(RoslynEditor), new UIPropertyMetadata(new PropertyChangedCallback(InitTextChanged)));

        public string InitText
        {
            set
            {
                SetValue(InitTextProperty, value);
            }
            get
            {
                return Editor.Text;
            }
        }

        public static DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(RoslynEditor), new UIPropertyMetadata(new PropertyChangedCallback(TextChanged)));

        public string Text
        {
            set
            {
                SetValue(TextProperty, value);
            }
            get
            {
                return Editor.Text;
            }
        }

        public static DependencyProperty DocumentIdProperty = DependencyProperty.Register("DocumentID", typeof(DocumentId), typeof(RoslynEditor));

        public DocumentId DocumentID
        {
            set
            {
                SetValue(DocumentIdProperty, value);
            }
            get
            {
                return (DocumentId)GetValue(DocumentIdProperty);
            }

        }

 




        public void SetText(string text)
        {
            Editor.Text = text;
        }

        public string GetText()
        {
            string text = string.Empty;

            text = Editor.Text;

            return text;
        }

        public ITextContainer TextContainer
        {
            get
            {

                return Editor.AsTextContainer();
            }
        }

        public event EventHandler EditorTextChanged;

        public string Title
        {
            set;
            get;
        }

        #endregion

        #region Folding
        FoldingManager foldingManager;
        AbstractFoldingStrategy foldingStrategy;



        void foldingUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (foldingStrategy != null)
            {
                foldingStrategy.UpdateFoldings(foldingManager, Editor.Document);
            }
        }
        #endregion

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Editor.TextArea.Focus();
        }











        public void Dispose()
        {
            Editor.TextArea.TextEntering -= OnTextEntering;
            Editor.TextArea.TextEntered -= OnTextEntered;
            Editor.TextChanged -= Editor_TextChanged;
            Editor.TextArea.KeyDown -= TextArea_KeyDown;
            Editor.TextArea.KeyUp -= TextArea_KeyUp;
            Editor.MouseHover -= Editor_MouseHover;
            Editor.MouseHoverStopped -= Editor_MouseHoverStopped;
        }


       
    }
}
