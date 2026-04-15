namespace FoodStreetApp.Services
{
    /// <summary>
    /// Markup extension that creates a binding to LocalizationResourceManager
    /// using a value converter instead of indexer syntax to avoid XAML parser issues.
    /// </summary>
    [ContentProperty(nameof(Key))]
    public class TranslateExtension : IMarkupExtension<BindingBase>
    {
        public string Key { get; set; } = string.Empty;

        public BindingBase ProvideValue(IServiceProvider serviceProvider)
        {
            return new Binding
            {
                Source = LocalizationResourceManager.Instance,
                Path = ".",
                Mode = BindingMode.OneWay,
                Converter = new TranslateConverter(),
                ConverterParameter = Key
            };
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        {
            return ProvideValue(serviceProvider);
        }
    }

    /// <summary>
    /// Value converter that translates a key using the LocalizationResourceManager.
    /// Triggered when PropertyChanged fires (language change).
    /// </summary>
    public class TranslateConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (parameter is string key && value is LocalizationResourceManager manager)
            {
                return manager[key];
            }
            return parameter?.ToString() ?? string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
