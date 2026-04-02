namespace FoodStreetApp.CMS.Services;

public class ToastService
{
    public event Action<ToastMessage>? OnShow;

    public void Success(string message) => OnShow?.Invoke(new ToastMessage("Success", message, "text-bg-success"));
    public void Error(string message) => OnShow?.Invoke(new ToastMessage("Error", message, "text-bg-danger"));
    public void Info(string message) => OnShow?.Invoke(new ToastMessage("Info", message, "text-bg-primary"));
}

public record ToastMessage(string Title, string Message, string CssClass);
