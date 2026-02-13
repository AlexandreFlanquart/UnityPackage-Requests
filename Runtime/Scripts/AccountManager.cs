using System;
using System.Threading.Tasks;
using MyUnityPackage.Toolkit;
using UnityEngine;
using UnityEngine.Events;

namespace MyUnityPackage.Requests
{
    /// <summary>
    /// Manages user authentication and Firestore data operations.
    /// Acts as a facade for FirebaseAuthService and FirestoreUserDataService.
    /// </summary>
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

        private async void Awake()
        {
            ServiceLocator.AddService<AccountManager>(gameObject);

            AuthService = new FirebaseAuthService();
            FirestoreService = new FirestoreUserDataService();
            AuthService.AuthStateChanged += OnAuthStateChanged;
           
            try
            {
                await FirebaseBootstrap.InitializeAsync();
                await FirestoreService.InitializeAsync();
                await AuthService.InitializeAsync();
                
                IsInitialized = true;
                MUPLogger.Info("[AccountManager] Initialized successfully.");
                OnInitialized?.Invoke();
            }
            catch (Exception ex)
            {
                MUPLogger.Error($"[AccountManager] Initialization failed: {ex.Message}");
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

        #region Authentication

        private void OnAuthStateChanged(bool isSignedIn)
        {
            MUPLogger.Info($"[AccountManager] Auth state changed: {(isSignedIn ? "Signed In" : "Signed Out")}");
            
            if (isSignedIn)
                OnSignIn?.Invoke();
            else
                OnSignOut?.Invoke();
        }

        /// <summary>
        /// Signs in with email and password.
        /// </summary>
        public Task SignInEmailAsync(string email, string password)
        {
            return AuthService.SignInEmailAsync(email, password);
        }

        /// <summary>
        /// Creates a new account with email and password.
        /// </summary>
        public Task SignUpEmailAsync(string email, string password)
        {
            return AuthService.SignUpEmailAsync(email, password);
        }

        /// <summary>
        /// Sends a verification email to the current user.
        /// </summary>
        public Task SendVerificationEmailAsync()
        {
            return AuthService.SendEmailVerificationAsync();
        }

        /// <summary>
        /// Sends a password reset email.
        /// </summary>
        public Task SendPasswordResetEmailAsync(string email)
        {
            return AuthService.SendPasswordResetEmailAsync(email);
        }

        /// <summary>
        /// Reloads the current user's data (useful for checking email verification status).
        /// </summary>
        public Task ReloadUserAsync()
        {
            return AuthService.ReloadUserAsync();
        }
    
        /// <summary>
        /// Signs out the current user.
        /// </summary>
        public void SignOut()
        {
            AuthService.SignOut();
        }

        /// <summary>
        /// Deletes the current user's account. This action is irreversible.
        /// </summary>
        public Task DeleteAccountAsync()
        {
            return AuthService.DeleteAccountAsync();
        }

        #endregion

        #region Firestore Data

        /// <summary>
        /// Saves user data to Firestore under the current user's document.
        /// </summary>
        /// <typeparam name="T">Type implementing IFirestoreData with [FirestoreData] attribute.</typeparam>
        /// <param name="userData">The data to save.</param>
        public async Task SaveUserDataAsync<T>(T userData) where T : class, IFirestoreData
        {
            var uid = AuthService.UserId;
            if (string.IsNullOrEmpty(uid))
                throw new InvalidOperationException("User is not signed in. Cannot save user data.");
            
            MUPLogger.Info("[AccountManager] Saving user data...");
            await FirestoreService.SaveDataAsync("Users", uid, userData);
            MUPLogger.Info("[AccountManager] User data saved successfully.");
        }
        
        /// <summary>
        /// Loads user data from Firestore for the current user.
        /// </summary>
        /// <typeparam name="T">Type implementing IFirestoreData with [FirestoreData] attribute.</typeparam>
        /// <returns>The loaded data, or null if document doesn't exist.</returns>
        public async Task<T> LoadUserDataAsync<T>() where T : class, IFirestoreData
        {
            var uid = AuthService.UserId;
            if (string.IsNullOrEmpty(uid))
                throw new InvalidOperationException("User is not signed in. Cannot load user data.");
            
            MUPLogger.Info("[AccountManager] Loading user data...");
            T userData = await FirestoreService.LoadDataAsync<T>("Users", uid);
            MUPLogger.Info("[AccountManager] User data loaded successfully.");
            return userData;
        }

        /// <summary>
        /// Saves data to a custom collection/document path.
        /// </summary>
        public Task SaveDataAsync<T>(string collection, string documentId, T data) where T : class, IFirestoreData
        {
            MUPLogger.Info($"[AccountManager] Saving data to collection: {collection}, documentId: {documentId}");
            return FirestoreService.SaveDataAsync(collection, documentId, data);
        }

        /// <summary>
        /// Loads data from a custom collection/document path.
        /// </summary>
        public Task<T> LoadDataAsync<T>(string collection, string documentId) where T : class, IFirestoreData
        {
            MUPLogger.Info($"[AccountManager] Loading data from collection: {collection}, documentId: {documentId}");
            return FirestoreService.LoadDataAsync<T>(collection, documentId);
        }

        #endregion
    }
}
