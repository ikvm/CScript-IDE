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
using Roslyn.Scripting;
using jinx;

namespace jinxapp
{
    [Export(typeof(IShell))]
    public class MainViewModel : ViewModelBase<MainViewFormModel>, IShell
    {

        public override void OnDoCreate(ExtendPropertyLib.ExtendObject item, params object[] args)
        {
            base.OnDoCreate(item, args);

        }

        //JS脚本内容
        public static ExtendProperty JSharpContentProperty = RegisterProperty<MainViewModel>(v => v.JSharpContent);
        public string JSharpContent { set { SetValue(JSharpContentProperty, value); } get { return (string)GetValue(JSharpContentProperty); } }

        //C#语言内容
        public static ExtendProperty CSharpContentProperty = RegisterProperty<MainViewModel>(v => v.CSharpContent);
        public string CSharpContent { set { SetValue(CSharpContentProperty, value); } get { return (string)GetValue(CSharpContentProperty); } }
        private Session session;


        public override string GetViewTitle()
        {
            return "CSharp to Javascript Compiler base on Roslyn";
        }

        //编译运行
        public void MakeAndRun()
        {
            var mv = this.View as IMainView;
            //mv.Formatter.Clear();
            try
            {
                var scriptEngine = InteractiveManager.GetScriptEngine();
                session = scriptEngine.CreateSession();
                session.Execute(this.CSharpContent);
                mv.Formatter.WriteObject("Make all success." + DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                mv.Formatter.WriteError(ex);
            }
        }

        //构建Javascript
        public void Build()
        {
            string js = JavaScriptCompiler.EmitJs(this.CSharpContent);

            this.JSharpContent = js;

        }


    }
}
