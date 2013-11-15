using System;
using System.Reflection;
using Roslyn.Services;
using CScriptIDE.RoslynEditor.Utilities;


namespace CScriptIDE.RoslynEditor.RoslynExtensions
{
    internal static class RoslynExtensions
    {
        private static readonly MethodInfo GetServiceMethod =
            StaticReflection.GetMethodInfo<ILanguageServiceProvider>(t => t.GetService<ILanguageService>())
                            .GetGenericMethodDefinition();

        private static readonly Type LanguageServiceType = typeof(ILanguageService);

        private static readonly MethodInfo GetCompletionServiceMethod =
            GetServiceMethod.MakeGenericMethod(LanguageServiceType.Assembly.GetType(LanguageServiceType.Namespace + "." + typeof(ICompletionService).Name));

        public static ICompletionService GetCompletionService(this ILanguageServiceProvider provider)
        {
            return new CompletionServiceWrapper(GetCompletionServiceMethod.Invoke<ILanguageService>(provider));
        }
    }
}
