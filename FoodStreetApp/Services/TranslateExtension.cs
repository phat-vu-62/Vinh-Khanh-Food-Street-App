namespace FoodStreetApp.Services
{
    /// <summary>
    /// Markup extension that creates a binding to LocalizationResourceManager
    /// using a value converter. Binds to CurrentCulture property so that when
    /// the language changes, all translated texts update automatically.
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
                Path = nameof(LocalizationResourceManager.CurrentCulture),
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
    /// The 'value' parameter receives the CurrentCulture string (triggers re-evaluation),
    /// and the 'parameter' contains the translation key.
    /// </summary>
    public class TranslateConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (parameter is string key)
            {
                return LocalizationResourceManager.Instance[key];
            }
            return parameter?.ToString() ?? string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
