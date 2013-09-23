using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel.Composition;
using ExtendPropertyLib;
using System.Windows.Media.Animation;
using ExtendPropertyLib.WPF;
using RoslynPad.Formatting;

namespace jinxapp
{

    /// <summary>
    /// Interaction logic for WindowTestView.xaml
    /// </summary>
    public partial class MainView : MahApps.Metro.Controls.MetroWindow,IMainView
    {
        private readonly ObjectFormatter _formatter;
        public ObjectFormatter Formatter
        {
            get { return _formatter; }
        }
        public MainView()
        {
          
            InitializeComponent();
            width = new GridLength(260, GridUnitType.Pixel);
            this.Closed += MainView_Closed;

            var document = new FlowDocument { FontFamily = new FontFamily("Consolas"), FontSize = 14 };
            _formatter = new ObjectFormatter(document);
            log.Document = document;


        }

        void MainView_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private bool toMinsize = false;
        GridLength width;
        public void ShowExplorer()
        {
            var grid = this.FindName("LeftBar") as Grid;

            GridLengthAnimation animation = new GridLengthAnimation();
            animation.From = new GridLength(this.toMinsize ? grid.ColumnDefinitions[0].Width.Value : 0.5, GridUnitType.Pixel);
            if (this.toMinsize)
            {
                width = grid.ColumnDefinitions[0].Width;
                animation.To = new GridLength(0.5, GridUnitType.Pixel);
            }
            else
                animation.To = width;

            animation.FillBehavior = FillBehavior.Stop;
            animation.Duration = new Duration(TimeSpan.FromSeconds(0.2));
            grid.ColumnDefinitions[0].Width = animation.To;
            grid.ColumnDefinitions[0].BeginAnimation(ColumnDefinition.WidthProperty, animation);

            this.toMinsize = !this.toMinsize;

            DoubleAnimation oLabelAngleAnimation = new DoubleAnimation();

            oLabelAngleAnimation.From = toMinsize ? 180 : 0;
            oLabelAngleAnimation.To = toMinsize ? 0 : 180;

            oLabelAngleAnimation.Duration
              = new Duration(new TimeSpan(0, 0, 0, 0, 500));

            RotateTransform oTransform
              = (RotateTransform)rotate.LayoutTransform;
            oTransform.BeginAnimation(RotateTransform.AngleProperty,
              oLabelAngleAnimation);


        }



        private void AppBarButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ShowExplorer();





        }

     

        private void log_TextChanged(object sender, TextChangedEventArgs e)
        {
            var txtBox = e.OriginalSource as RichTextBox;
            txtBox.ScrollToEnd();
        }

    
     


    }
}
