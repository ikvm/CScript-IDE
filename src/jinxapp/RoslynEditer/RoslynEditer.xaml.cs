using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using RoslynPad.Editor;
using RoslynPad.Formatting;
using RoslynPad.RoslynExtensions;
using System;
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
                Editor.TextArea.KeyDown += TextArea_KeyDown;
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
                
                var position = Editor.CaretOffset;
                var completions = _interactiveManager.GetCompletion(1);

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
               
                e.Handled = true;
            }
        }

        void TextArea_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyStates == Keyboard.GetKeyStates(Key.J) && Keyboard.Modifiers == ModifierKeys.Control)
            {
                OnTextEntered(this, null);
            }
        }

        private void OnTextEntered(object sender, TextCompositionEventArgs e)
        {
            var position = Editor.CaretOffset;
            if (position > 0 && _interactiveManager.IsCompletionTriggerCharacter(position - 1))
            {
                _completionWindow = new CompletionWindow(Editor.TextArea);
                _completionWindow.Width = 300;
                _completionWindow.Background = Brushes.Black;
                _completionWindow.BorderThickness = new Thickness(0);

                var data = _completionWindow.CompletionList.CompletionData;
                foreach (var completionData in _interactiveManager.GetCompletion(position))
                {
                    data.Add(new AvalonEditCompletionData(completionData));
                }

                _completionWindow.Show();
                _completionWindow.Closed += delegate
                {
                    _completionWindow = null;
                };
            }

            istypeset = true;
            SetValue(TextProperty, Editor.Text);
            

        }

        bool istypeset = false;


        private void OnTextEntering(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && _completionWindow != null)
            {
                if (!char.IsLetterOrDigit(e.Text[0]))
                {
                    _completionWindow.CompletionList.RequestInsertion(e);
                }
            }
        }


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

        static void TextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var that = d as RoslynEditer;
            if (e.OldValue == null && !that.istypeset)
                that.Editor.AppendText(e.NewValue.ToString());
           
        }

        static void EditerTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var that = d as RoslynEditer;
            that.Editor.TextArea.TextEntering -= that.OnTextEntering;
            that.Editor.TextArea.TextEntered -= that.OnTextEntered;
     
            if ((EditerType)(e.NewValue) == EditerType.CSharp)
            {

                that.Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
                that.Editor.TextArea.TextEntering += that.OnTextEntering;
                that.Editor.TextArea.TextEntered += that.OnTextEntered;
            }
            else if ((EditerType)(e.NewValue) == EditerType.Javascript)
            {
                that.Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("JavaScript");
             
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


    }
}
