using System;
using System.Threading.Tasks;
using MyUnityPackage.Toolkit;
using UnityEngine;

namespace MyUnityPackage.Requests
{
    public sealed class AccountManager : MonoBehaviour
    {
        public FirebaseAuthService AuthService { get; private set; }
        public FirestoreUserDataService FirestoreService { get; private set; }

        public bool IsInitialized { get; private set; }
        public bool IsSignedIn => AuthService?.IsSignedIn ?? false;
        public string UserId => AuthService?.UserId;
        public string Email => AuthService?.Email;
        public bool IsEmailVerified => AuthService?.IsEmailVerified ?? false;

        public event Action OnSignIn;
        public event Action OnSignOut;
        public event Action OnInitialized;

        // Nouveaux événements pour le UIBlocker
        public event Action OnOperationStarted;
        public event Action OnOperationEnded;

        [Header("Google Sign-In")]
        public string WebClientId;

        private void Awake()
        {
            ServiceLocator.AddService<AccountManager>(gameObject);
        }

        private async void Start()
        {
            AuthService = new FirebaseAuthService();
            FirestoreService = new FirestoreUserDataService();

            AuthService.AuthStateChanged += OnAuthStateChanged;

            try
            {
                await Task.Yield();

                await FirebaseBootstrap.InitializeAsync();
                await FirestoreService.InitializeAsync();

                AuthService.SetWebClientId(WebClientId);
                await AuthService.InitializeAsync();

                IsInitialized = true;
                OnInitialized?.Invoke();
            }
            catch (Exception ex)
            {
                MUPLogger.Error("[AccountManager] Initialization failed: " + ex.Message);
            }
        }

        private void OnDestroy()
        {
            if (AuthService != null)
            {
                AuthService.AuthStateChanged -= OnAuthStateChanged;
                AuthService.Dispose();
            }
        }

        private void OnAuthStateChanged(bool signedIn)
        {
            if (signedIn)
                OnSignIn?.Invoke();
            else
                OnSignOut?.Invoke();
        }

        // ---------------------------------------------------------
        // AUTH WRAPPERS
        // ---------------------------------------------------------

        public Task SignInEmailAsync(string email, string password)
            => AuthService.SignInEmailAsync(email, password);

        public Task SignUpEmailAsync(string email, string password)
            => AuthService.SignUpEmailAsync(email, password);

        public Task SendVerificationEmailAsync()
            => AuthService.SendEmailVerificationAsync();

        public Task SendPasswordResetEmailAsync(string email)
            => AuthService.SendPasswordResetEmailAsync(email);

        public Task ReloadUserAsync()
            => AuthService.ReloadUserAsync();

        public void SignOut()
        {
            AuthService.SignOut();
        }

        public Task DeleteAccountAsync()
            => AuthService.DeleteAccountAsync();

        // ---------------------------------------------------------
        // GOOGLE AUTH WRAPPERS
        // ---------------------------------------------------------

        public Task SignInGoogleAsync()
            => AuthService.SignInWithGoogleAsync();

        public void SignOutGoogle()
        {
            AuthService.SignOutGoogle();
        }

        public void DisconnectGoogle()
        {
            AuthService.DisconnectGoogle();
        }

        // ---------------------------------------------------------
        // FIRESTORE WRAPPERS
        // ---------------------------------------------------------

        public Task SaveUserDataAsync<T>(T data) where T : class, IFirestoreData
            => FirestoreService.SaveDataAsync("Users", UserId, data);

        public Task<T> LoadUserDataAsync<T>() where T : class, IFirestoreData
            => FirestoreService.LoadDataAsync<T>("Users", UserId);

        public Task SaveDataAsync<T>(string collection, string documentId, T data)
            where T : class, IFirestoreData
            => FirestoreService.SaveDataAsync(collection, documentId, data);

        public Task<T> LoadDataAsync<T>(string collection, string documentId)
            where T : class, IFirestoreData
            => FirestoreService.LoadDataAsync<T>(collection, documentId);

        public string GetIDByCollection(string collection)
        {
            return FirestoreService.GetIDByCollection(collection);
        }

        public async Task<bool> CheckAdminState()
        {
           /* var user = AuthService.GetCurrentUser();

            if (user == null)
                return false;

            // Force refresh du token pour récupérer les claims à jour
            var idTokenResult = await user.GetIdTokenResultAsync(true);
            string idToken = idTokenResult.Token;

            // Un JWT = header.payload.signature
            string[] parts = idToken.Split('.');
            if (parts.Length < 2)
                return false;

            string payload = parts[1];

            // Correction du padding Base64
            payload = payload.Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            // Décodage JSON
            string json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));

            // Parse en dictionnaire
            var dict = MiniJson.Deserialize(json) as Dictionary<string, object>;

            if (dict != null && dict.ContainsKey("admin"))
            {
                return Convert.ToBoolean(dict["admin"]);
            }
*/
            return false;
        }
    }
}
