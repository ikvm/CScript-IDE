using Roslyn.Compilers;
using Roslyn.Services;
using Roslyn.Services.Host;

namespace RoslynPad.RoslynExtensions
{
    internal class InteractiveWorkspace : TrackingWorkspace
    {
        private DocumentId _openDocumentId;
        private ITextContainer _openTextContainer;

        internal InteractiveWorkspace(IWorkspaceServiceProvider workspaceServices)
            : base(workspaceServices, true, true)
        {
        }

        protected override ISolution CreateNewSolution(ISolutionFactoryService solutionFactory, SolutionId id)
        {
            return solutionFactory.CreateSolution(id);
        }

        public override bool IsSupported(WorkspaceFeature feature)
        {
            switch (feature)
            {
                case WorkspaceFeature.OpenDocument:
                case WorkspaceFeature.UpdateDocument:
                    return true;
            }
            return false;
        }

        public void OpenDocument(DocumentId documentId, ITextContainer textContainer)
        {
            _openTextContainer = textContainer;
            _openDocumentId = documentId;
            OnDocumentOpened(documentId, textContainer);
        }

        public void SetCurrentSolution(ISolution solution)
        {
            SetLatestSolution(solution);
            RaiseWorkspaceChangedEventAsync(WorkspaceEventKind.SolutionChanged, solution);
        }

        public override void UpdateDocument(DocumentId document, IText newText)
        {
            if (_openDocumentId == document)
            {
            }
        }
    }
}
