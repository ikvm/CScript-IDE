using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Services;
using System.Linq;
using RoslynPad.Utilities;

namespace RoslynPad.RoslynExtensions
{
    class CompletionServiceWrapper : ICompletionService
    {
        private readonly object _inner;
        private readonly ConcurrentDictionary<MethodBase, MethodBase> _methods;
        private readonly Type _innerType;

        public CompletionServiceWrapper(ILanguageService inner)
        {
            _inner = inner;
            _innerType = _inner.GetType();
            _methods = new ConcurrentDictionary<MethodBase, MethodBase>();
        }

        public IEnumerable<ICompletionProvider> GetDefaultCompletionProviders()
        {
            return (IEnumerable<ICompletionProvider>)Invoke(MethodBase.GetCurrentMethod());
        }

        public TextSpan GetDefaultTrackingSpan(IDocument document, int position, CancellationToken cancellationToken)
        {
            return (TextSpan)Invoke(MethodBase.GetCurrentMethod(), document, position, cancellationToken);
        }

        public IEnumerable<CompletionItemGroup> GetGroups(IDocument document, int position, CompletionTriggerInfo triggerInfo, IEnumerable<ICompletionProvider> completionProviders, CancellationToken cancellationToken)
        {
            return (IEnumerable<CompletionItemGroup>)Invoke(MethodBase.GetCurrentMethod(), document, position, triggerInfo, completionProviders, cancellationToken);
        }

        public bool IsTriggerCharacter(IText text, int characterPosition, IEnumerable<ICompletionProvider> completionProviders)
        {
            return (bool)Invoke(MethodBase.GetCurrentMethod(), text, characterPosition, completionProviders);
        }

        [DebuggerNonUserCode]
        private object Invoke(MethodBase method, params object[] parameters)
        {
            return _methods.GetOrAdd(method, GetMethod).Invoke<object>(_inner, parameters);
        }

        private MethodBase GetMethod(MethodBase method)
        {
            return _innerType.GetMethod(method.Name, method.GetParameters().Select(t => t.ParameterType).ToArray());
        }
    }
}
