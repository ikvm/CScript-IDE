using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtendPropertyLib;
using ExtendPropertyLib.WPF;
using System.ComponentModel.Composition;
using MaxZhang.EasyEntities.Dynamic.Aop;
using System.Windows.Input;
using Roslyn.Scripting;
using jinx;
using Microsoft.Win32;
using System.IO;
using Roslyn.Scripting.CSharp;
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using System.Reflection;
using System.Windows;
using jinx.RoslynEditor.RoslynExtensions;

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

        public static ExtendProperty OpenContentProperty = RegisterProperty<MainViewModel>(v => v.OpenContent);
        public string OpenContent { set { SetValue(OpenContentProperty, value); } get { return (string)GetValue(OpenContentProperty); } }

        private Session session;


        public override string GetViewTitle()
        {
            return "RoslynEditor - C#";
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
     
        //打开文件
        public void Open()
        {
            OpenFileDialog odf = new OpenFileDialog();
            odf.Filter = "csharp files (*.cs)|*.cs|All files (*.*)|*.*";
            if (odf.ShowDialog().Value)
            {
                string fileName = odf.FileName;
                string content = File.ReadAllText(fileName, Encoding.UTF8);
                OpenContent = content;
            }
        }

        //新建文件
        public void New()
        {
            if (MessageBox.Show("新建文件会覆盖现有内容，要继续操作吗？", "提示"
                                    , MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                string content = File.ReadAllText("NewFile.cs", Encoding.UTF8);
                OpenContent = content;
            }
        }





    }
}
