using jinxapp;
using System.Windows;

namespace RoslynPad.Runtime
{
    public static class ObjectExtensions
    {
        //[UsedImplicitly]
        public static T Dump<T>(this T o)
        {
            ((MainView)Application.Current.MainWindow).Formatter.WriteObject(o);
            return o;
        }
    }
}
