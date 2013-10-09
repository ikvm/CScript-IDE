using jinx.RoslynEditor;
using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace jinxapp
{
    interface IMainView
    {
        ObjectFormatter Formatter { get; }

        IEditor CreateEditor(string text);

        void DisplayTree(SyntaxTree tree);

        void AddDocument(IEditor editor);
    }
}
