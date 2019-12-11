using AwsCognitoExample.Services;
using GalaSoft.MvvmLight;
using System.Threading.Tasks;

namespace AwsCognitoExample.ViewModel
{
    public class TestViewModel : ViewModelBase
    {
        private readonly AuthenticationServiceOnBlazor authenticationService;

        #region Properties
        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set => Set(ref _isBusy, value); }

        private string _username;
        public string Username { get => _username; set => Set(ref _username, value); }

        private string _password;
        public string Password { get => _password; set => Set(ref _password, value); }

        private bool _wasLoginSuccessful;
        public bool WasLoginSuccessful { get => _wasLoginSuccessful; set => Set(ref _wasLoginSuccessful, value); }
        #endregion

        public TestViewModel(AuthenticationServiceOnBlazor authentication)
        {
            authenticationService = authentication;
        }

        public async Task TryLoginAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            await Task.Delay(100);
            WasLoginSuccessful = await authenticationService.TryLoginAsync(Username, Password);
            
            IsBusy = false;
        }
    }
}
