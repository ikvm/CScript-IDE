using System;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using Roslyn.Compilers;

namespace jinx.RoslynEditor
{
    class AvalonEditTextContainer : ITextContainer
    {
        private readonly TextEditor _editor;

        private IText _before;
        private IText _current;

        public TextDocument Document
        {
            get { return _editor.Document; }
        }

        public AvalonEditTextContainer(TextEditor editor)
        {
            _editor = editor;
            SetCurrent();

            _editor.Document.Changing += DocumentOnChanging;
            _editor.Document.Changed += DocumentOnChanged;
        }

        private void SetCurrent()
        {
            _current = new StringText(_editor.Text);
        }

        private void DocumentOnChanging(object sender, DocumentChangeEventArgs e)
        {
            _before = CurrentText;
        }

        private void DocumentOnChanged(object sender, DocumentChangeEventArgs e)
        {
            SetCurrent();

            OnTextChanged(new TextChangeEventArgs(_before, CurrentText, new TextChangeRange[0]));
        }

        public IText CurrentText
        {
            get { return _current; }
        }

        public event EventHandler<TextChangeEventArgs> TextChanged;

        private void OnTextChanged(TextChangeEventArgs e)
        {
            var handler = TextChanged;
            if (handler != null) handler(this, e);
        }
    }
}
