using ICSharpCode.AvalonEdit;
using Roslyn.Compilers;

namespace RoslynPad.Editor
{
    class AvalonEditText : TextBase
    {
        private readonly TextEditor _editor;

        public AvalonEditText(TextEditor editor)
        {
            _editor = editor;
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                destination[i + destinationIndex] = _editor.Document.GetCharAt(sourceIndex);
            }
        }

        public override int Length
        {
            get { return _editor.Document.TextLength; }
        }

        public override char this[int position]
        {
            get { return _editor.Document.GetCharAt(position); }
        }
    }
}
