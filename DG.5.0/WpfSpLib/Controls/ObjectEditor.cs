using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WpfSpLib.Common;
using WpfSpLib.Helpers;

namespace WpfSpLib.Controls
{
    public class ObjectEditor : Control, INotifyPropertyChanged
    {
        static ObjectEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ObjectEditor), new FrameworkPropertyMetadata(typeof(ObjectEditor)));
            EventManager.RegisterClassHandler(typeof(ObjectEditor), GotFocusEvent, new RoutedEventHandler((sender, args) => ControlHelper.OnGotFocusOfControl(sender, args, ((ObjectEditor)sender).GetEditControl())));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(ObjectEditor), new FrameworkPropertyMetadata(KeyboardNavigationMode.Once));
            KeyboardNavigation.IsTabStopProperty.OverrideMetadata(typeof(ObjectEditor), new FrameworkPropertyMetadata(false));
        }
        public ObjectEditor()
        {
            if (Equals(ValueDataType, ValueDataTypeProperty.DefaultMetadata.DefaultValue))
                OnValueDataTypeChanged(this, new DependencyPropertyChangedEventArgs(ValueDataTypeProperty, ValueDataType, ValueDataType));
        }

        public decimal NumericMinValue => Metadata.Template == DataTypeMetadata.DataTemplate.NumericBox ? Convert.ToDecimal(Metadata.MinValue) : decimal.MinValue;
        public decimal NumericMaxValue => Metadata.Template == DataTypeMetadata.DataTemplate.NumericBox ? Convert.ToDecimal(Metadata.MaxValue) : decimal.MaxValue;
        public DataTypeMetadata Metadata => DataTypeMetadata.MetadataList[ValueDataType];

        private bool _isTemplateChanging;

        //=============================
        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<object>), typeof(ObjectEditor));

        public static readonly DependencyProperty ValueDataTypeProperty = DependencyProperty.Register("ValueDataType", 
            typeof(DataTypeMetadata.DataType), typeof(ObjectEditor), 
            new FrameworkPropertyMetadata(DataTypeMetadata.DataType.String, OnValueDataTypeChanged));

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object), typeof(ObjectEditor), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged, CoerceValue));
        public static readonly DependencyProperty IsNullableProperty = DependencyProperty.Register("IsNullable", typeof(bool), typeof(ObjectEditor), new FrameworkPropertyMetadata(false, OnIsNullableChanged));

        //=============================
        public event RoutedPropertyChangedEventHandler<object> ValueChanged
        {
            add => AddHandler(ValueChangedEvent, value);
            remove => RemoveHandler(ValueChangedEvent, value);
        }

        public DataTypeMetadata.DataType ValueDataType
        {
            get => (DataTypeMetadata.DataType)GetValue(ValueDataTypeProperty);
            set => SetValue(ValueDataTypeProperty, value);
        }
        public object Value
        {
            get => (object)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>
        /// Can the value be null?
        /// </summary>
        public bool IsNullable
        {
            get => (bool)GetValue(IsNullableProperty);
            set => SetValue(IsNullableProperty, value);
        }

        //=================================
        private static void OnValueDataTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var editor = (ObjectEditor)d;
            var metadata = DataTypeMetadata.MetadataList[(DataTypeMetadata.DataType)e.NewValue];
            
            editor.Dispatcher.InvokeAsync(() =>
            {
                editor.PreviewMouseLeftButtonDown -= EditorBoolean_PreviewMouseLeftButtonDown;

                editor._isTemplateChanging = true;
                editor.Template = editor.FindResource(metadata.Template.ToString()) as ControlTemplate;
                editor.Value = Tips.GetDefaultOfType(metadata.Type);
                editor._isTemplateChanging = false;
                
                var newValue = editor.IsNullable ? null : Tips.GetDefaultOfType(metadata.Type);
                if (Equals(newValue, editor.Value))
                    editor.OnValueChanged(null, newValue);
                else
                    editor.Value = newValue;

                editor.UpdateUI();

                if ((DataTypeMetadata.DataType) e.NewValue == DataTypeMetadata.DataType.Bool)
                    editor.PreviewMouseLeftButtonDown += EditorBoolean_PreviewMouseLeftButtonDown;
            }, DispatcherPriority.Normal);

        }

        private static void EditorBoolean_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var editor = (ObjectEditor)sender;
            if (editor.IsEnabled && editor.ValueDataType == DataTypeMetadata.DataType.Bool)
            {
                if (Equals(editor.Value, false))
                    editor.Value = true;
                else if (Equals(editor.Value, null) || !editor.IsNullable)
                    editor.Value = false;
                else
                    editor.Value = null;

                Keyboard.Focus(editor);
                e.Handled = true;
            }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var objectEditor = (ObjectEditor)d;
            objectEditor.OnValueChanged(e.OldValue, e.NewValue);
        }

        private static object CoerceValue(DependencyObject d, object value)
        {
            var editor = (ObjectEditor)d;
            if (editor._isTemplateChanging)
                return value;
            if (value == null)
                return editor.IsNullable ? null : Tips.GetDefaultOfType(editor.Metadata.Type);

            if (editor.Metadata.MinValue != null)
            {
                var min = ChangeTypeConverter.Instance.Convert(editor.Metadata.MinValue, value.GetType(), null, null);
                if (((IComparable)min).CompareTo(value) == 1)
                    value = min;
            }
            if (editor.Metadata.MaxValue != null)
            {
                var max = ChangeTypeConverter.Instance.Convert(editor.Metadata.MaxValue, value.GetType(), null, null);
                if (((IComparable)max).CompareTo(value) == -1)
                    value = max;
            }
            return ChangeTypeConverter.Instance.Convert(value, editor.Metadata.Type, null, null);
        }

        //=========================================================
        protected virtual void OnValueChanged(object oldValue, object newValue)
        {
            if (!_isTemplateChanging)
                RaiseEvent(new RoutedPropertyChangedEventArgs<object>(oldValue, newValue, ValueChangedEvent));
        }

        private static void OnIsNullableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != e.OldValue)
            {
                var objectEditor = (ObjectEditor)d;
                objectEditor.Value = objectEditor.Value; // Coerce value
            }
        }

        #region ===========  INotifyPropertyChanged  ===============
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        //================================
        private void UpdateUI() =>
            OnPropertiesChanged(nameof(Metadata), nameof(NumericMinValue), nameof(NumericMaxValue));

        private Control GetEditControl()
        {
            var child = VisualTreeHelper.GetChild(this, 0);
            return child as Control ?? (Control) VisualTreeHelper.GetChild(child, 0);
        }
    }
}
