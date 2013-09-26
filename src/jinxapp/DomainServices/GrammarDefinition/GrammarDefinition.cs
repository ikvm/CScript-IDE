using Roslyn.Compilers.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jinxapp.DomainServices.GrammarDefinition
{
    public sealed class GrammarDefinition
    {
        public GrammarDefinition()
        {
            Children = new List<GrammarDefinition>();
        }

        public GrammarDefinition(string name, GrammarType type, CommonSyntaxNode node):this()
        {
            this.Name = name;
            this.GrammarDefineType = type;
            this.SyntaxNode = node;
        }


        public string Name { set; get; }

        public GrammarType GrammarDefineType { set; get; }

        public CommonSyntaxNode SyntaxNode { set; get; }

        public List<GrammarDefinition> Children { set; get; }

        public override string ToString()
        {
            return this.Name;
        }

    }
}
