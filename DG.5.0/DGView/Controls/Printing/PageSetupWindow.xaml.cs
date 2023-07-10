using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using WpfSpLib.Helpers;

namespace DGView.Controls.Printing
{
    /// <summary>
    /// Interaction logic for PageSetupWindow.xaml
    /// </summary>
    public partial class PageSetupWindow : Window
    {
        public PageViewModel ViewModel { get; }
        public PageSetupWindow(PageViewModel pageViewModel)
        {
            InitializeComponent();
            ViewModel = pageViewModel.GetPageSetupModel();
            DataContext = ViewModel;
            Dispatcher.BeginInvoke(new Action(() => ViewModel.UpdateUI(MarginContainer.ActualHeight, PrintingArea.MinWidth, PrintingArea.MinHeight)));
        }

        private void OnPageSizeSelectorMouseEnter(object sender, MouseEventArgs e)
        {
            var btn = (ToggleButton) sender;
            var txtBlock = btn.GetVisualChildren().OfType<TextBlock>().FirstOrDefault();
            if (txtBlock != null)
            {
                if (!string.IsNullOrEmpty(txtBlock.Text) && WpfSpLib.Common.Tips.IsTextTrimmed(txtBlock))
                    ToolTipService.SetToolTip(btn, txtBlock.Text);
                else
                    ToolTipService.SetToolTip(btn, null);
            }
        }

        private void OnPrintingAreaSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (IsLoaded)
                ViewModel.UpdateUiBySlider(MarginContainer.ActualHeight, PrintingArea);
        }
    }
}
