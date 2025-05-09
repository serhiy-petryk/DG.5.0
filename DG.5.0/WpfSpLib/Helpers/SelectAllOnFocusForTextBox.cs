using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfSpLib.Helpers
{
    public static class SelectAllOnFocusForTextBox
    {
        public static void ActivateGlobally()
        {
            // Select the text in a TextBox when it receives focus.
            // var type = typeof(TextBox).Assembly.GetType("System.Windows.Controls.TextBoxView");
            EventManager.RegisterClassHandler(typeof(TextBox), UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(SelectivelyIgnoreMouseButton));
            EventManager.RegisterClassHandler(typeof(TextBox), UIElement.GotKeyboardFocusEvent, new RoutedEventHandler(SelectAllText));
            EventManager.RegisterClassHandler(typeof(TextBox), Control.MouseDoubleClickEvent, new RoutedEventHandler(SelectAllText));
        }

        private static void SelectivelyIgnoreMouseButton(object sender, MouseButtonEventArgs e)
        {
            // Find the TextBox
            DependencyObject parent = e.OriginalSource as UIElement;
            while (parent != null && !(parent is TextBox))
                parent = VisualTreeHelper.GetParent(parent);

            if (parent != null)
            {
                var textBox = (TextBox)parent;
                if (!textBox.IsKeyboardFocusWithin && !textBox.IsReadOnly && textBox.Focusable)
                {
                    // If the text box is not yet focused, give it the focus and
                    textBox.Focus();

                    // if textbox has a button under mouse click: don't set e.Handled = true because button may has mouse handler
                    // see TextBoxEffects.Buttons attached property
                    foreach (var element in (Mouse.DirectlyOver as DependencyObject).GetVisualParents<FrameworkElement>())
                    {
                        if (element is ButtonBase)
                            return;
                        if (element is TextBox || element.GetType().Name == "TextBoxView")
                            break;
                    }

                    // stop further processing of this click event.
                    e.Handled = true;
                }
            }
        }

        private static void SelectAllText(object sender, RoutedEventArgs e)
        {
            var textBox = e.OriginalSource as TextBox;
            if (textBox != null && !textBox.IsReadOnly && textBox.Focusable)
                textBox.SelectAll();
        }

    }
}
