using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using jinx.Roslyn.SyntaxVisualizer.Debugger;
using jinxapp.DomainServices.GrammarDefinition;
using jinxapp.RoslynEditer.RoslynExtensions;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;
using RoslynPad.Editor;
using RoslynPad.Formatting;
using RoslynPad.RoslynExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace jinxapp.RoslynEditer
{
    public enum EditerType
    {
        CSharp,
        Javascript
    }
    public interface IEditer
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
    }

    /// <summary>
    /// Interaction logic for RoslynEditer.xaml
    /// </summary>
    public partial class RoslynEditer : UserControl,IEditer
    {
    
        private readonly InteractiveManager _interactiveManager;
        private RoslynEditorInsightWindow _insightWindow;
        private CompletionWindow _completionWindow;

        public RoslynEditer()
        {
            InitializeComponent();

            ConfigureEditor();

            _interactiveManager = new InteractiveManager();
            _interactiveManager.SetDocument(Editor.AsTextContainer());
        }

        private void ConfigureEditor()
        {
            if (EditerType == EditerType.CSharp)
            {

                Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
                Editor.TextArea.TextEntering += OnTextEntering;
                Editor.TextArea.TextEntered += OnTextEntered;
                Editor.TextChanged += Editor_TextChanged;
                Editor.TextArea.KeyDown += TextArea_KeyDown;
                Editor.TextArea.KeyUp += TextArea_KeyUp;
                Editor.MouseHover += Editor_MouseHover;
                Editor.MouseHoverStopped += Editor_MouseHoverStopped;

                syntaxVisualizer.SyntaxNodeNavigationToSourceRequested += node => NavigateToSource(node.Span);
                syntaxVisualizer.SyntaxTokenNavigationToSourceRequested += token => NavigateToSource(token.Span);
                syntaxVisualizer.SyntaxTriviaNavigationToSourceRequested += trivia => NavigateToSource(trivia.Span);

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


    

        #region SyntaxVisualizer

        private void NavigateToSource(TextSpan span)
        {
            if(span.Start>= 0 && span.Length>0)
                SelectText(span.Start, span.Length);
        }

        private async void SelectText(int spanStart, int spanLength)
        {
            Editor.Select(spanStart, spanLength);

            var docLine = Editor.TextArea.Document.GetLineByOffset(spanStart);
       
            Editor.ScrollToLine(docLine.LineNumber);
        
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
                    var tree = _interactiveManager.CurrentDocumentSyntaxTree;
                    SyntaxTransporter transporter = new SyntaxTransporter(tree);
                    
                    syntaxVisualizer.DisplaySyntaxTree(tree, transporter.SourceLanguage);

                    if (!syntaxVisualizer.NavigateToBestMatch(transporter.ItemSpan,
                                                                transporter.ItemKind,
                                                                transporter.ItemCategory,
                                                                highlightMatch: true,
                                                                highlightLegendDescription: "Under Inspection")) ;

                    var definitions = _interactiveManager.GetGrammarDefinitionList();
                    this.childrenDropDown.ItemsSource = null;
                    this.definitionDropDown.ItemsSource = definitions;
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

                    var comp = completions.FirstOrDefault(c => c.DisplayText == msg);
                    if (comp == null)
                    {
                        completions = _interactiveManager.GetCompletion(position);
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
                    _insightWindow.Foreground = Brushes.LightSkyBlue;
                    _insightWindow.Background = Brushes.LightSlateGray;

                    var items = _interactiveManager.GetInsightTip(Editor.CaretOffset);

                    foreach(var item in items)
                        _insightWindow.Items.Add(item);

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

        public static DependencyProperty EditerTypeProperty = DependencyProperty.Register("EditerType", typeof(EditerType), typeof(RoslynEditer), new UIPropertyMetadata(new PropertyChangedCallback(EditerTypeChanged)));
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
            var that = d as RoslynEditer;
            that.Editor.Text= e.NewValue.ToString();
        }

        static void TextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var that = d as RoslynEditer;
            if (e.OldValue == null && e.OldValue != e.NewValue && !that.istypeset)
                that.Editor.AppendText(e.NewValue.ToString());
        }

        static void EditerTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var that = d as RoslynEditer;
            that.Editor.TextArea.TextEntering -= that.OnTextEntering;
            that.Editor.TextArea.TextEntered -= that.OnTextEntered;
            that.Editor.TextChanged -= that.Editor_TextChanged;
            that.Editor.TextArea.KeyDown -= that.TextArea_KeyDown;
            that.Editor.MouseHover -= that.Editor_MouseHover;
            that.Editor.MouseHoverStopped -= that.Editor_MouseHoverStopped;
            that.syntaxVisualizer.SyntaxNodeNavigationToSourceRequested -= node => that.NavigateToSource(node.Span);
            that.syntaxVisualizer.SyntaxTokenNavigationToSourceRequested -= token => that.NavigateToSource(token.Span);
            that.syntaxVisualizer.SyntaxTriviaNavigationToSourceRequested -= trivia => that.NavigateToSource(trivia.Span);



            if ((EditerType)(e.NewValue) == EditerType.CSharp)
            {

                that.Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
                that.Editor.TextArea.TextEntering -= that.OnTextEntering;
                that.Editor.TextArea.TextEntered -= that.OnTextEntered;
                that.Editor.TextArea.TextEntering += that.OnTextEntering;
                that.Editor.TextArea.TextEntered += that.OnTextEntered;
                that.Editor.TextChanged += that.Editor_TextChanged;
                that.Editor.TextArea.KeyDown += that.TextArea_KeyDown;
                that.Editor.MouseHover += that.Editor_MouseHover;
                that.Editor.MouseHoverStopped += that.Editor_MouseHoverStopped;
                that.syntaxVisualizer.SyntaxNodeNavigationToSourceRequested += node => that.NavigateToSource(node.Span);
                that.syntaxVisualizer.SyntaxTokenNavigationToSourceRequested += token => that.NavigateToSource(token.Span);
                that.syntaxVisualizer.SyntaxTriviaNavigationToSourceRequested += trivia => that.NavigateToSource(trivia.Span);
            }
            else if ((EditerType)(e.NewValue) == EditerType.Javascript)
            {
                that.Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("JavaScript");
                that.grid1.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel);
                that.grid1.RowDefinitions[0].Height = new GridLength(0, GridUnitType.Pixel);
            }
        }

        public static DependencyProperty InitTextProperty = DependencyProperty.Register("InitText", typeof(string), typeof(RoslynEditer), new UIPropertyMetadata(new PropertyChangedCallback(InitTextChanged)));

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

        public static DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(RoslynEditer), new UIPropertyMetadata(new PropertyChangedCallback(TextChanged)));

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

     

    }
}
