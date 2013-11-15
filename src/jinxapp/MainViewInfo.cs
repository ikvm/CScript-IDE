using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using ExtendPropertyLib;
using ExtendPropertyLib.WPF;

namespace CScriptIDE
{
    public class MainViewFormModel : BusinessInfoBase<MainViewFormModel>
    {

        public static readonly ExtendProperty NameProperty = RegisterProperty<MainViewFormModel>(p => p.Name);

        public string Name
        {
            set
            {
                SetValue(NameProperty, value);
            }
            get
            {
                return (string)GetValue(NameProperty);
            }
        }


        protected override void AddValidationRules()
        {

            ValiationRules.Add(NameProperty, () =>
            {
                string result = null;

                if (string.IsNullOrWhiteSpace(Name))
                {
                    result = "名字不能为空！";
                }
                return result;
            });


        }

    }
}
