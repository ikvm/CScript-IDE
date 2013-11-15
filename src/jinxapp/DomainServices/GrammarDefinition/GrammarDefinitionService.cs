using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CScriptIDE.DomainServices.GrammarDefinition
{
    /// <summary>
    /// 得到所有语法树中关于类的定义
    /// </summary>
    public class GrammarDefinitionService : SyntaxWalker
    {
        public GrammarDefinitionService()
        {
            GrammarDefinitionList = new List<GrammarDefinition>();
        }
        private GrammarDefinition currentGrammarDefinition;
        public List<GrammarDefinition> GrammarDefinitionList { set; get; }
        public int NoteCount { set; get; }
        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            string className = node.Identifier.ValueText;

            if (!GrammarDefinitionList.Any(gd => gd.Name == className) && !string.IsNullOrEmpty(className))
            {
                currentGrammarDefinition = new GrammarDefinition(className, GrammarType.Class, node);
                GrammarDefinitionList.Add(currentGrammarDefinition);
                NoteCount++;
            }
          
            base.VisitClassDeclaration(node);
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            string interfaceName = node.Identifier.ValueText;

            if (!GrammarDefinitionList.Any(gd => gd.Name == interfaceName) && !string.IsNullOrEmpty(interfaceName))
            {
                currentGrammarDefinition = new GrammarDefinition(interfaceName, GrammarType.Class, node);
                GrammarDefinitionList.Add(currentGrammarDefinition);
                NoteCount++;
            }
          
            base.VisitInterfaceDeclaration(node);
        }

        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            var declaration = node.Declaration;
            if (declaration != null && currentGrammarDefinition!=null)
            {
                if (declaration.Variables.Count > 0)
                {
                    string fieldName = declaration.Variables[0].Identifier.ValueText;
                    currentGrammarDefinition.Children.Add(new GrammarDefinition(fieldName, GrammarType.Field, node));
                    NoteCount++;
                }
            }

            //string className = node.Identifier.ValueText;
            base.VisitFieldDeclaration(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            string methodName = node.Identifier.ValueText;
            if (!string.IsNullOrEmpty(methodName) && currentGrammarDefinition != null)
            {
                currentGrammarDefinition.Children.Add(new GrammarDefinition(methodName, GrammarType.Method, node));
                NoteCount++;
            }
            base.VisitMethodDeclaration(node);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            string propertyName = node.Identifier.ValueText;
            if (!string.IsNullOrEmpty(propertyName) && currentGrammarDefinition != null)
            {
                currentGrammarDefinition.Children.Add(new GrammarDefinition(propertyName, GrammarType.Property, node));
                NoteCount++;
            }
            base.VisitPropertyDeclaration(node);
        }


        


    }
}
