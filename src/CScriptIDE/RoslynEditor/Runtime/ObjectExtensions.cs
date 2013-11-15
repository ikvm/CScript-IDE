using CScriptIDE;
using System.Windows;

namespace CScriptIDE.RoslynEditor.Runtime
{
    public static class ObjectExtensions
    {
        //[UsedImplicitly]
        public static void Dump(this object o)
        {
            ((MainView)Application.Current.MainWindow).Formatter.WriteObject(o);
        }
    }
}
