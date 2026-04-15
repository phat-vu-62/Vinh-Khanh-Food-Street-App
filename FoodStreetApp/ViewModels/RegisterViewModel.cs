using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FoodStreetApp.Services;

namespace FoodStreetApp.ViewModels
{
    public class RegisterViewModel : INotifyPropertyChanged
    {
        private readonly IAuthService _authService;

        public event PropertyChangedEventHandler? PropertyChanged;

        private string _username = string.Empty;
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        private string _confirmPassword = string.Empty;
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set { _confirmPassword = value; OnPropertyChanged(); }
        }

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        private string _fullName = string.Empty;
        public string FullName
        {
            get => _fullName;
            set { _fullName = value; OnPropertyChanged(); }
        }

        private string _phoneNumber = string.Empty;
        public string PhoneNumber
        {
            get => _phoneNumber;
            set { _phoneNumber = value; OnPropertyChanged(); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotBusy)); }
        }
        public bool IsNotBusy => !_isBusy;

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
        }
        public bool HasError => !string.IsNullOrEmpty(_errorMessage);

        private string? _successMessage;
        public string? SuccessMessage
        {
            get => _successMessage;
            set { _successMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSuccess)); }
        }
        public bool HasSuccess => !string.IsNullOrEmpty(_successMessage);

        public ICommand RegisterCommand { get; }
        public ICommand GoToLoginCommand { get; }

        public RegisterViewModel(IAuthService authService)
        {
            _authService = authService;
            RegisterCommand = new Command(async () => await RegisterAsync());
            GoToLoginCommand = new Command(async () =>
            {
                await Shell.Current.GoToAsync("..");
            });
        }

        private async Task RegisterAsync()
        {
            ErrorMessage = null;
            SuccessMessage = null;

            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(PhoneNumber))
            {
                ErrorMessage = "Vui lòng nhập đầy đủ Họ tên, SĐT, tài khoản và mật khẩu.";
                return;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Mật khẩu xác nhận không khớp.";
                return;
            }

            if (Password.Length < 6)
            {
                ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.";
                return;
            }

            IsBusy = true;

            var (success, message) = await _authService.RegisterAsync(
                Username, Password,
                string.IsNullOrWhiteSpace(Email) ? null : Email,
                string.IsNullOrWhiteSpace(FullName) ? null : FullName,
                string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber);

            IsBusy = false;

            if (success)
            {
                SuccessMessage = message;
                // Auto-navigate back to login after 1.5 seconds
                await Task.Delay(1500);
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                ErrorMessage = message;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
