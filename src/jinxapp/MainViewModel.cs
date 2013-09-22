using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtendPropertyLib;
using ExtendPropertyLib.WPF;
using System.ComponentModel.Composition;
using MaxZhang.EasyEntities.Dynamic.Aop;
using System.Windows.Input;

namespace jinxapp
{
    [Export(typeof(IShell))]
    public class MainViewModel : ViewModelBase<MainViewFormModel>, IShell
    {

        public override void OnDoCreate(ExtendPropertyLib.ExtendObject item, params object[] args)
        {
            base.OnDoCreate(item, args);

        }



        public override string GetViewTitle()
        {
            return "jinx - CSharp and Javascript Compiler base on Roslyn";
        }



    }
}
