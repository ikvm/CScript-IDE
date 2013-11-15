using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using Roslyn.Scripting.CSharp;
using Roslyn.Services;
using System.Data;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using CScriptIDE.DomainServices.GrammarDefinition;
using jinx.RoslynEditor.Runtime;

namespace jinx.RoslynEditor.RoslynExtensions
{
    public class InteractiveManager
    {
        #region Fields

        private static readonly Type[] _assemblyTypes = new[]{
                                                        typeof (object), 
                                                        typeof (Uri), 
                                                        typeof (Enumerable),
                                                        typeof(List<>),
                                                        typeof(DataSet),
                                                        typeof(Queryable),
                                                        typeof(ObservableCollection<>),
                                                        typeof (ObjectExtensions)
                                                    };

        private readonly InteractiveWorkspace _workspace;
        private readonly ParseOptions _parseOptions;
        private readonly CompilationOptions _compilationOptions;
        private readonly PortableExecutableReference[] _references;
        private readonly ICompletionService _completionService;

        private int _documentNumber;
        private ProjectId _previousProjectId;
        private DocumentId _currentDocumenId;

        #endregion

        public ISolution Solution
        {
            get { return _workspace.CurrentSolution; }
        }

        public InteractiveManager()
        {
            _workspace = new InteractiveWorkspace(DefaultServices.WorkspaceServicesFactory.CreateWorkspaceServiceProvider("RoslynEditor"));

            _parseOptions = new ParseOptions(CompatibilityMode.ECMA2, LanguageVersion.CSharp6, true, SourceCodeKind.Interactive);
            _compilationOptions = new CompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            var metadataFileProvider = _workspace.CurrentSolution.MetadataFileProvider;
            _references = _assemblyTypes.Select(t =>
                metadataFileProvider.GetReference(t.Assembly.Location, MetadataReferenceProperties.Assembly)).ToArray();

            _completionService = Solution.LanguageServicesFactory.CreateLanguageServiceProvider(LanguageNames.CSharp)
                .GetCompletionService();
        }


        #region SyntaxTree
   

        /// <summary>
        /// 得到当前文档的文法树
        /// </summary>
        public SyntaxTree CurrentDocumentSyntaxTree
        {
            get
            {
                var document = GetCurrentDocument();

                return (SyntaxTree)document.GetSyntaxTree();
            }
        }

        /// <summary>
        /// 得到类型定义树，指的是（类/接口 -> 方法），只包含一层结构。
        /// </summary>
        /// <returns></returns>
        public List<GrammarDefinition> GetGrammarDefinitionList()
        {
            SyntaxTree tree = this.CurrentDocumentSyntaxTree;
            var service = new GrammarDefinitionService();
            service.Visit(tree.GetRoot());
            return service.GrammarDefinitionList;
        }

   



        #endregion

        #region Documents

        public DocumentId CreateAndOpenDocument(ITextContainer textContainer)
        {
            IProject project;
            ISolution currentSolution = _workspace.CurrentSolution;
            //if (_previousProjectId == null)
            //{
            //    DocumentId id;
            //    project = CreateSubmissionProject(currentSolution);
            //    var usingText = CreateUsingText();
            //    currentSolution = project.Solution.AddDocument(project.Id, project.Name, usingText, out id);
            //    _previousProjectId = project.Id;
            //}
            project = CreateSubmissionProject(currentSolution);
            var currentDocument = SetSubmissionDocument(textContainer, project);
            _currentDocumenId = currentDocument.Id;

            return _currentDocumenId;
        }

    

        public void OpenDocument(DocumentId id , ITextContainer container)
        {
            _workspace.OpenDocument(id, container);
        }

        public IDocument GetDocumentByID(DocumentId id)
        {
            return _workspace.CurrentSolution.GetDocument(id);
        }

        private IDocument SetSubmissionDocument(ITextContainer textContainer, IProject project)
        {
            DocumentId id;
            ISolution solution = project.Solution.AddDocument(project.Id, project.Name, textContainer.CurrentText,
                                                              out id);
            _workspace.SetCurrentSolution(solution);
            _workspace.OpenDocument(id, textContainer);
            return solution.GetDocument(id);
        }

        private IProject CreateSubmissionProject(ISolution solution)
        {
            string name = "Submission#" + _documentNumber++;
            ProjectId id = ProjectId.CreateNewId(solution.Id, name);
            solution =
                solution.AddProject(new ProjectInfo(id, VersionStamp.Create(), name, name, LanguageNames.CSharp, null,
                                                    _compilationOptions.WithScriptClassName(name), _parseOptions, null,
                                                    null, _references, null, true));
            if (_previousProjectId != null)
            {
                solution = solution.AddProjectReference(id, _previousProjectId);
            }
            return solution.GetProject(id);
        }

        #endregion

        #region Completion

        public Task<IList<CompletionItem>> GetCompletionAsync(int position, string text = null)
        {
            return Task.Factory.StartNew<IList<CompletionItem>>(()=>GetCompletion(position,text));
        }


        public IList<CompletionItem> GetCompletion(int position,string text=null)
        {
            CompletionTriggerInfo triggerInfo;
            if(!string.IsNullOrEmpty(text))
                triggerInfo = CompletionTriggerInfo.CreateTypeCharTriggerInfo(char.Parse(text)); 
            else
                triggerInfo = CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo(); 

            var groups = _completionService.GetGroups(GetCurrentDocument(), position,
                                                 triggerInfo,
                                                 _completionService.GetDefaultCompletionProviders(),
                                                 CancellationToken.None);
            return (groups ?? Enumerable.Empty<CompletionItemGroup>()).SelectMany(t => t.Items).OrderByDescending(t=>t.SortText).ToArray();
        }

        public void SetCurrentDocumentByID(DocumentId id)
        {
            _currentDocumenId = id;
        }

        public IDocument GetCurrentDocument()
        {
            return _workspace.CurrentSolution.GetDocument(_currentDocumenId);
        }

        public bool IsCompletionTriggerCharacter(int position)
        {

            var text =GetCurrentDocument().GetText();

            return _completionService.IsTriggerCharacter(text, position,
                _completionService.GetDefaultCompletionProviders());
        }

        #endregion

        #region Symbol
        /// <summary>
        /// 得到当前文档的编译符号集合
        /// </summary>
        /// <returns></returns>
        public SemanticModel GetCurrentDocumentSymbol()
        {

            return (SemanticModel)this.GetCurrentDocument().GetSemanticModel();
        }



        #endregion


        #region Insight

        public List<IInsightItem> GetInsightTip(int postion)
        {
            List<IInsightItem> itemList = new List<IInsightItem>();
            var tree = this.CurrentDocumentSyntaxTree;
            var model = GetCurrentDocumentSymbol();

            var invocationSyntaxList = tree.GetRoot().DescendantNodes()
                .OfType<InvocationExpressionSyntax>();

            if (invocationSyntaxList.Count() > 0)
            {
                var invocationSyntaxQuery = from syntaxNode in invocationSyntaxList
                                            where postion >= syntaxNode.Span.Start && postion <= syntaxNode.Span.End
                                            select syntaxNode;
                if (invocationSyntaxQuery != null && invocationSyntaxQuery.Count() > 0)
                {
                    int maxPostion = invocationSyntaxQuery.Max(s => s.Span.Start);
                    var invocationSyntax = invocationSyntaxQuery.FirstOrDefault(s => s.Span.Start == maxPostion);

                    var symbolInfo = model.GetSymbolInfo(invocationSyntax);
                    var methodSymbol = (MethodSymbol)symbolInfo.Symbol;
                    if (methodSymbol != null)
                    {
                        foreach (MethodSymbol overload in methodSymbol.ContainingType.GetMembers(methodSymbol.Name))
                        {
                            itemList.Add(new InsightItemData(methodSymbol.Name, overload.ToDisplayString()));
                        }
                    }
                }
            }


            return itemList;
        }

        #endregion



        #region Script Engine

        public static ScriptEngine GetScriptEngine()
        {
            var scriptEngine = new ScriptEngine();

            foreach (var typeInAssembly in _assemblyTypes)
            {
                scriptEngine.AddReference(typeInAssembly.Assembly);
            }

            var namespaces = _assemblyTypes.Select(t => t.Namespace).Distinct();
            foreach (var ns in namespaces)
            {
                scriptEngine.ImportNamespace(ns);
            }

            return scriptEngine;
        }

        #endregion
    }
}
