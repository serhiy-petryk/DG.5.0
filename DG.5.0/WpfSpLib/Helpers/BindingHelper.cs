using System.Windows.Data;

namespace WpfSpLib.Helpers
{
    public static class BindingHelper
    {
        public static Binding CloneBinding(Binding source) =>
          new Binding()
          {
            UpdateSourceTrigger = source.UpdateSourceTrigger,
            ValidatesOnDataErrors = source.ValidatesOnDataErrors,
            Mode = source.Mode,
            Path = source.Path,
            AsyncState = source.AsyncState,
            BindingGroupName = source.BindingGroupName,
            BindsDirectlyToSource = source.BindsDirectlyToSource,
            Converter = source.Converter,
            ConverterCulture = source.ConverterCulture,
            ConverterParameter = source.ConverterParameter,
            ElementName = source.ElementName,
            FallbackValue = source.FallbackValue,
            IsAsync = source.IsAsync,
            NotifyOnSourceUpdated = source.NotifyOnSourceUpdated,
            NotifyOnTargetUpdated = source.NotifyOnTargetUpdated,
            NotifyOnValidationError = source.NotifyOnValidationError,
            StringFormat = source.StringFormat,
            TargetNullValue = source.TargetNullValue,
            UpdateSourceExceptionFilter = source.UpdateSourceExceptionFilter,
            ValidatesOnExceptions = source.ValidatesOnExceptions,
            XPath = source.XPath,
            Delay = source.Delay,
            ValidatesOnNotifyDataErrors = source.ValidatesOnNotifyDataErrors
          };
    }
}
