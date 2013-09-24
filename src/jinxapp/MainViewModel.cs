using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtendPropertyLib;
using ExtendPropertyLib.WPF;
using System.ComponentModel.Composition;
using MaxZhang.EasyEntities.Dynamic.Aop;
using System.Windows.Input;
using RoslynPad.RoslynExtensions;

namespace jinxapp
{
    [Export(typeof(IShell))]
    public class MainViewModel : ViewModelBase<MainViewFormModel>, IShell
    {

        public override void OnDoCreate(ExtendPropertyLib.ExtendObject item, params object[] args)
        {
            base.OnDoCreate(item, args);

        }

        public static ExtendProperty CSharpContentProperty = RegisterProperty<MainViewModel>(v => v.CSharpContent);
        public string CSharpContent { set { SetValue(CSharpContentProperty, value); } get { return (string)GetValue(CSharpContentProperty); } }

        public override string GetViewTitle()
        {
            return "CSharp and Javascript Compiler base on Roslyn";
        }

        public void MakeAndRun()
        {
            var mv = this.View as IMainView;
            //mv.Formatter.Clear();

            try
            {
                var scriptEngine = InteractiveManager.GetScriptEngine();
                var session = scriptEngine.CreateSession();
                session.Execute(CSharpContent);
            }
            catch (Exception ex)
            {
                mv.Formatter.WriteError(ex);
            }


        }

    }
}
