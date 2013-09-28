using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jinxapp.RoslynEditer.RoslynExtensions
{
    public class RoslynEditorInsightWindow : OverloadInsightWindow
    {
        sealed class SDItemProvider : IOverloadProvider
        {
            readonly RoslynEditorInsightWindow insightWindow;
            int selectedIndex;

            public SDItemProvider(RoslynEditorInsightWindow insightWindow)
            {
                this.insightWindow = insightWindow;
                insightWindow.items.CollectionChanged += insightWindow_items_CollectionChanged;
            }

            void insightWindow_items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                OnPropertyChanged("Count");
                OnPropertyChanged("CurrentHeader");
                OnPropertyChanged("CurrentContent");
                OnPropertyChanged("CurrentIndexText");
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public int SelectedIndex
            {
                get
                {
                    return selectedIndex;
                }
                set
                {
                    if (selectedIndex != value)
                    {
                        selectedIndex = value;
                        OnPropertyChanged("SelectedIndex");
                        OnPropertyChanged("CurrentHeader");
                        OnPropertyChanged("CurrentContent");
                        OnPropertyChanged("CurrentIndexText");
                    }
                }
            }

            public int Count
            {
                get { return insightWindow.Items.Count; }
            }

            public string CurrentIndexText
            {
                get { return (selectedIndex + 1).ToString() + " of " + this.Count.ToString(); }
            }

            public object CurrentHeader
            {
                get
                {
                    IInsightItem item = insightWindow.SelectedItem;
                    return item != null ? item.Header : null;
                }
            }

            public object CurrentContent
            {
                get
                {
                    IInsightItem item = insightWindow.SelectedItem;
                    return item != null ? item.Content : null;
                }
            }

            void OnPropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }

        readonly ObservableCollection<IInsightItem> items = new ObservableCollection<IInsightItem>();

        public RoslynEditorInsightWindow(TextArea textArea): base(textArea)
        {
            this.Provider = new SDItemProvider(this);
            this.Provider.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == "SelectedIndex")
                    OnSelectedItemChanged(EventArgs.Empty);
            };
            AttachEvents();
        }

        public IList<IInsightItem> Items
        {
            get { return items; }
        }

        public IInsightItem SelectedItem
        {
            get
            {
                int index = this.Provider.SelectedIndex;
                if (index < 0 || index >= items.Count)
                    return null;
                else
                    return items[index];
            }
            set
            {
                this.Provider.SelectedIndex = items.IndexOf(value);
                OnSelectedItemChanged(EventArgs.Empty);
            }
        }

        TextDocument document;
        Caret caret;

        void AttachEvents()
        {
            document = this.TextArea.Document;
            caret = this.TextArea.Caret;
            //if (document != null)
            //    document.Changed += document_Changed;
            //if (caret != null)
            //    caret.PositionChanged += caret_PositionChanged;
        }

        void caret_PositionChanged(object sender, EventArgs e)
        {
            OnCaretPositionChanged(e);
        }

        /// <inheritdoc/>
        protected override void DetachEvents()
        {
            //if (document != null)
            //    document.Changed -= document_Changed;
            if (caret != null)
                caret.PositionChanged -= caret_PositionChanged;
            base.DetachEvents();
        }

        //void document_Changed(object sender, DocumentChangeEventArgs e)
        //{
        //    if (DocumentChanged != null)
        //        DocumentChanged(this, new TextChangeEventArgs(e.Offset, e.RemovedText, e.InsertedText));
        //}

        //public event EventHandler<TextChangeEventArgs> DocumentChanged;

        public event EventHandler SelectedItemChanged;

        protected virtual void OnSelectedItemChanged(EventArgs e)
        {
            if (SelectedItemChanged != null)
            {
                SelectedItemChanged(this, e);
            }
        }

        public event EventHandler CaretPositionChanged;

        protected virtual void OnCaretPositionChanged(EventArgs e)
        {
            if (CaretPositionChanged != null)
            {
                CaretPositionChanged(this, e);
            }
        }
    }
}
