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
using Roslyn.Services;
using Roslyn.Compilers.Common;
using System.Reflection;
using System.Windows;
using jinx.RoslynEditor.RoslynExtensions;
using Editor = jinx.RoslynEditor;
namespace jinxapp
{
    [Export(typeof(IShell))]
    public class MainViewModel : ViewModelBase<MainViewFormModel>, IShell
    {
        private InteractiveManager InteractiveManager = new InteractiveManager();
        private Editor.ObjectFormatter Formatter;

        public override void OnDoCreate(ExtendPropertyLib.ExtendObject item, params object[] args)
        {
            base.OnDoCreate(item, args);
            ApplicationService.Services.Add<InteractiveManager>(InteractiveManager);
        }

        //当前文档
        public static ExtendProperty CurrentDocumentIDProperty = RegisterProperty<MainViewModel>(v => v.CurrentDocumentID);
        public DocumentId CurrentDocumentID { set { SetValue(CurrentDocumentIDProperty, value); } get { return (DocumentId)GetValue(CurrentDocumentIDProperty); } }



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

        public override void OnLoad()
        {
            var docID = InteractiveManager.DocumentList[0];
            InteractiveManager.SetCurrentDocumentByID(docID);
            var mv = this.View as IMainView;
            Formatter = mv.Formatter;
            ApplicationService.Services.Add<Editor.ObjectFormatter>(Formatter);
        }

        private bool CompileCode(ref string error)
        {
            bool compileSuccess = true;
            var model = InteractiveManager.GetCurrentDocumentSymbol();
            Diagnostic[] dg = model.GetDiagnostics().ToArray();
            
            if (dg.Length > 0)
            {
                error = "The following compile error occured:\r\n";
                foreach (Diagnostic d in dg)
                    error += "Info: " + d.Info + "\n";
                compileSuccess = false;
            }

            return compileSuccess;
        }

        //编译运行
        public void MakeAndRun()
        {
            string errorMsg = string.Empty;
            if (CompileCode(ref errorMsg))
            {
                try
                {
                    var scriptEngine = InteractiveManager.GetScriptEngine();
                    session = scriptEngine.CreateSession();
                    session.Execute(this.CSharpContent);
                    Formatter.WriteObject("Make all success." + DateTime.Now.ToString());
                }
                catch (Exception ex)
                {
                    Formatter.WriteError(ex);
                }
            }
            else
            {
                Formatter.Clear();
                Formatter.WriteObject(errorMsg);
                MessageBox.Show("Compile Failed !", "Tips", MessageBoxButton.OK, MessageBoxImage.Error);
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
