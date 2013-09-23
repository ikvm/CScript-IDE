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
using RoslynPad.Runtime;


namespace RoslynPad.RoslynExtensions
{
    public class InteractiveManager
    {
        #region Fields

        private static readonly Type[] _assemblyTypes = new[]
                                                            {
                                                                typeof (object), 
                                                                typeof (Uri), 
                                                                typeof (Enumerable),
                                                                typeof(List<>),
                                                                typeof(DataSet),
                                                                typeof(Queryable),
                                                                typeof(ObservableCollection<>),
                                                                typeof(BlockingCollection<>),
                                                                typeof (ObjectExtensions)
                                                                //,typeof (ObjectExtensions)
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
            _workspace = new InteractiveWorkspace(DefaultServices.WorkspaceServicesFactory.CreateWorkspaceServiceProvider("RoslynPad"));

            _parseOptions = new ParseOptions(CompatibilityMode.None, LanguageVersion.CSharp6, true, SourceCodeKind.Interactive);
            _compilationOptions = new CompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            var metadataFileProvider = _workspace.CurrentSolution.MetadataFileProvider;
            _references = _assemblyTypes.Select(t =>
                metadataFileProvider.GetReference(t.Assembly.Location, MetadataReferenceProperties.Assembly)).ToArray();

            _completionService = Solution.LanguageServicesFactory.CreateLanguageServiceProvider(LanguageNames.CSharp)
                .GetCompletionService();
        }

        #region Documents

        public void SetDocument(ITextContainer textContainer)
        {
            IProject project;
            ISolution currentSolution = _workspace.CurrentSolution;
            if (_previousProjectId == null)
            {
                DocumentId id;
                project = CreateSubmissionProject(currentSolution);
                var usingText = CreateUsingText();
                currentSolution = project.Solution.AddDocument(project.Id, project.Name, usingText, out id);
                _previousProjectId = project.Id;
            }
            project = CreateSubmissionProject(currentSolution);
            var currentDocument = SetSubmissionDocument(textContainer, project);
            _currentDocumenId = currentDocument.Id;
        }

        private static IText CreateUsingText()
        {
            return
                new StringText(string.Join(Environment.NewLine,
                                           _assemblyTypes.Select(t => string.Format("using {0};", t.Namespace))));
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

        public IList<CompletionItem> GetCompletion(int position)
        {
            var groups = _completionService.GetGroups(GetCurrentDocument(), position,
                                                 CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo(),
                                                 _completionService.GetDefaultCompletionProviders(),
                                                 CancellationToken.None);
            return (groups ?? Enumerable.Empty<CompletionItemGroup>()).SelectMany(t => t.Items).OrderBy(t=>t.SortText).ToArray();
        }

        private IDocument GetCurrentDocument()
        {
            return _workspace.CurrentSolution.GetDocument(_currentDocumenId);
        }

        public bool IsCompletionTriggerCharacter(int position)
        {
            return _completionService.IsTriggerCharacter(GetCurrentDocument().GetText(), position,
                _completionService.GetDefaultCompletionProviders());
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
