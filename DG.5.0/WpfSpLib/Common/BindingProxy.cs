// see https://stackoverflow.com/questions/52145711/how-can-you-set-a-dynamicresource-in-code-behind-if-the-target-is-not-a-framewor
// https://stackoverflow.com/questions/33816511/how-can-you-bind-to-a-dynamicresource-so-you-can-use-a-converter-or-stringformat

using System.ComponentModel;
using System.Windows;

namespace WpfSpLib.Common
{
    public class BindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore() => new BindingProxy();
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value), typeof(object), typeof(BindingProxy), new FrameworkPropertyMetadata(default));
        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
    }

    //===================================================================
    public class LocalizationProxy : Freezable, INotifyPropertyChanged
    {
        protected override Freezable CreateInstanceCore() => new LocalizationProxy();

        public static readonly DependencyProperty InputValueProperty = DependencyProperty.Register(nameof(InputValue),
            typeof(object), typeof(LocalizationProxy), new FrameworkPropertyMetadata(null, OnValueChanged));
        public object InputValue
        {
            get => GetValue(InputValueProperty);
            set => SetValue(InputValueProperty, value);
        }
        //==========
        public static readonly DependencyProperty Argument0Property = DependencyProperty.Register("Argument0",
            typeof(object), typeof(LocalizationProxy), new FrameworkPropertyMetadata(null, OnValueChanged));
        public object Argument0
        {
            get => GetValue(Argument0Property);
            set => SetValue(Argument0Property, value);
        }
        //==============
        public string Value
        {
            get
            {
                if (InputValue == null)
                    return null;
                if (Argument0 == null)
                    return InputValue.ToString();
                return string.Format(InputValue.ToString(), Argument0);
            }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var proxy = (LocalizationProxy)d;
            proxy.OnPropertiesChanged(nameof(Value));
        }

        #region ===========  INotifyPropertyChanged  ===============
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
