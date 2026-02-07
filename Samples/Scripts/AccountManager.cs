using UnityEngine;
using System;
using System.Threading.Tasks;
using MyUnityPackage.Toolkit;
using System.Collections.Generic;
using UnityEngine.Events;
using Firebase.Firestore;
using Unity.Android.Types;

namespace MyUnityPackage.Requests.Samples
{
    /// <summary>
    /// Manages user authentication and profile data.
    /// </summary>
    public sealed class AccountManager : MonoBehaviour
    {
        [SerializeField] private UnityEvent OnAccountReady;

        public FirebaseAuthService authService { get; private set; }
        public FirestoreUserDataService firestoreService { get; private set; }

        public event Action OnSignIn;
        public event Action OnSignOut;

        private async void Awake()
        {
            ServiceLocator.AddService<AccountManager>(gameObject);

            authService = new FirebaseAuthService();
            firestoreService = new FirestoreUserDataService();
            authService.AuthStateChanged += OnAuthStateChanged;
            OnAccountReady.Invoke();
           
            await FirebaseBootstrap.InitializeAsync();
            await firestoreService.InitializeAsync();
            await authService.InitializeAsync();
        }

        private void OnDestroy()
        {
            authService.AuthStateChanged -= OnAuthStateChanged;
        }


    ///  Authentication ----------------------------

        private void OnAuthStateChanged(bool isSignedIn)
        {
            MUPLogger.Info("AccountManager.OnAuthStateChanged: " + isSignedIn);
            if (isSignedIn)
                OnSignIn?.Invoke();
            else
                OnSignOut?.Invoke();
        }

        public async void SignInEmail(string email, string password)
        {
            await authService.SignInEmailAsync(email, password);
        }

        public async Task SignUpEmail(string email, string password)
        {
            await authService.SignUpEmailAsync(email, password);
            
        }

        public void SendVerificationEmail()
        {
            authService.SendEmailVerification();
        }

        public async Task ReloadUser()
        {
            await authService.ReloadUser();
        }
    
        public void SignOut()
        {
            authService.SignOut();
        }


        /// Firestore user data ----------------------------
        
        public async Task SaveUserData(UserData userData)
        {
            MUPLogger.Info("Saving user data to Firestore...");
            var uid = authService._user?.UserId;
            await firestoreService.SaveUserAsync(uid, userData);
            MUPLogger.Info("Saving user data to Firestore completed.");
        }
        
        public UserData LoadUserData()
        {
            MUPLogger.Info("Loading user data from Firestore...");
            var uid = authService._user?.UserId;
            UserData userData = null;
            firestoreService.LoadUserAsync(uid).ContinueWith(task => {
                if (task.IsFaulted)
                {
                    MUPLogger.Error("Failed to load user data: " + task.Exception);
                    return;
                }
                userData = task.Result;
            });

            return userData;
        }
    }
}