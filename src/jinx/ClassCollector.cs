using System.Collections.Generic;
using System.Linq;
using Roslyn.Compilers.CSharp;
using jinx.types.Attributes;

namespace jinx
{
    class ClassCollector : SyntaxWalker
    {
        public readonly List<ClassDeclarationSyntax> Classes = new List<ClassDeclarationSyntax>();

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (node.AttributeLists.Any(a => a.DescendantNodes().OfType<AttributeSyntax>().Any(s => s.Name.GetText().ToString() == typeof(JavaScript).Name)))
                Classes.Add(node);

            base.VisitClassDeclaration(node);
        }
    }
}
