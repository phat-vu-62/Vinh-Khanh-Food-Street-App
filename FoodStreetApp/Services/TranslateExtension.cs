namespace FoodStreetApp.Services
{
    [ContentProperty(nameof(Key))]
    public class TranslateExtension : IMarkupExtension<string>
    {
        public string Key { get; set; } = string.Empty;

        public string ProvideValue(IServiceProvider serviceProvider)
        {
            return LocalizationResourceManager.Instance[Key];
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        {
            return ProvideValue(serviceProvider);
        }
    }
}
