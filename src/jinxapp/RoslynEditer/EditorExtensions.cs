﻿using ICSharpCode.AvalonEdit;
using Roslyn.Compilers;

namespace RoslynPad.Editor
{
    public static class EditorExtensions
    {
        public static IText AsText(this TextEditor editor)
        {
            return new StringText(editor.Text);
        }

        public static ITextContainer AsTextContainer(this TextEditor editor)
        {
            return new AvalonEditTextContainer(editor);
        }
    }
}
