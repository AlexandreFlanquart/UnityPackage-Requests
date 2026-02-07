using System;
using System.Threading.Tasks;
using MyUnityPackage.Toolkit;
using Firebase.Auth;

namespace MyUnityPackage.Requests
{
    /// <summary>
    /// Minimal Firebase Auth service:
    /// - Email/Password SignUp & SignIn
    /// - SignOut
    /// - Auth state + basic user info
    /// </summary>
    public sealed class FirebaseAuthService : IDisposable
    {
        public event Action<bool> AuthStateChanged;

        public bool IsInitialized { get; private set; }

        public bool IsSignedIn
        {
            get
            {
#if FIREBASE_AUTH
                return _user != null;
#else
                return false;
#endif
            }
        }

        public string UserId
        {
            get
            {
#if FIREBASE_AUTH
                return _user?.UserId;
#else
                return null;
#endif
            }
        }

        public string Email
        {
            get
            {
#if FIREBASE_AUTH
                return _user?.Email;
#else
                return null;
#endif
            }
        }

        private FirebaseAuth _auth;
        public FirebaseUser _user;
        private bool _disposed;

        private Task _initTask;

        /// <summary>
        /// Initializes Firebase Auth (idempotent).
        /// Call once at startup (or let your manager call it).
        /// </summary>
        public Task InitializeAsync()
        {
            _initTask ??= InitializeInternalAsync();
            return _initTask;
        }

        private async Task InitializeInternalAsync()
        {
            if (IsInitialized) return;
            MUPLogger.Info("[FirebaseAuthService] Initializing...");
            await FirebaseBootstrap.InitializeAsync();

            _auth = FirebaseAuth.DefaultInstance;
            _auth.StateChanged += OnAuthStateChanged;
            _user = _auth.CurrentUser;

            IsInitialized = true;
            //AuthStateChanged?.Invoke(IsSignedIn);
        }

        /// <summary>
        /// Creates a new user with email & password.
        /// </summary>
        public async Task SignUpEmailAsync(string email, string password)
        {
            MUPLogger.Info($"FirebaseAuthService.SignUpEmailAsync: {email}");

            EnsureReady();
            try
            {
                var result = await _auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(task => {
                    if (task.IsCanceled) {
                        MUPLogger.Error("CreateUserWithEmailAndPasswordAsync was canceled.");
                        return null;
                    }
                    if (task.IsFaulted) {
                        MUPLogger.Error("CreateUserWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                        return null;
                    }
                    return task.Result;
                });

                _user = result.User;

                SendEmailVerification();
            }
            catch (Exception ex)
            {
                MUPLogger.Error("Error during SignUpEmailAsync: " + ex);
                throw; 
            }
        }

        public async void SendEmailVerification()
        {
            MUPLogger.Info("FirebaseAuthService.SendEmailVerification");
            if (_user == null)
            {
                MUPLogger.Error("No user signed in. Cannot send verification email.");
                return;
            }

            try
            {
                await _user.SendEmailVerificationAsync().ContinueWith(task => {
                    if (task.IsCanceled) {
                        MUPLogger.Error("SendEmailVerificationAsync was canceled.");
                        return;
                    }
                    if (task.IsFaulted) {
                        MUPLogger.Error("SendEmailVerificationAsync encountered an error: " + task.Exception);
                        return;
                    }
                });
            }
            catch (Exception ex)
            {
                MUPLogger.Error("Error during SendEmailVerification: " + ex);
            }
        }

        /// <summary>
        /// Signs in an existing user with email & password.
        /// </summary>
        public async Task SignInEmailAsync(string email, string password)
        {
            MUPLogger.Info($"FirebaseAuthService.SignInEmailAsync: {email}");
            EnsureReady();
            try
            {                
                var result = await _auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task => {
                if (task.IsCanceled) {
                    MUPLogger.Error("SignInWithEmailAndPasswordAsync was canceled.");
                    return null;
                }
                if (task.IsFaulted) {
                    MUPLogger.Error("SignInWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                    return null;
                }
                return task.Result;
                });
                _user = result.User;
            }
            catch (Exception ex)
            {
                MUPLogger.Error("Error during SignInEmailAsync: " + ex);
                throw; 
            }           
        }

        public async Task ReloadUser()
        {
            MUPLogger.Info("FirebaseAuthService.ReloadUser");
            if (_user == null)
            {
                MUPLogger.Error("No user signed in. Cannot reload.");
                return;
            }

            try
            {
                await _user.ReloadAsync().ContinueWith(task => {
                    if (task.IsCanceled) {
                        MUPLogger.Error("ReloadAsync was canceled.");
                        return;
                    }
                    if (task.IsFaulted) {
                        MUPLogger.Error("ReloadAsync encountered an error: " + task.Exception);
                        return;
                    }
                });
                _user = _auth.CurrentUser; // Refresh local user reference
            }
            catch (Exception ex)
            {
                MUPLogger.Error("Error during ReloadUser: " + ex);
            }
        }

        /// <summary>
        /// Signs out the current user.
        /// </summary>
        public void SignOut()
        {
            MUPLogger.Info("FirebaseAuthService.SignOut");
            if (_auth == null) return;

            _auth.SignOut();
            _user = null;

        }

        public void Dispose()
        {
            MUPLogger.Info("FirebaseAuthService.Dispose");
            if (_disposed) return;
            _disposed = true;

            if (_auth != null)
                _auth.StateChanged -= OnAuthStateChanged;
        }

        private void OnAuthStateChanged(object sender, EventArgs e)
        {
            MUPLogger.Info("FirebaseAuthService.OnAuthStateChanged");
            if (_disposed) return;

            _user = _auth.CurrentUser;
            AuthStateChanged?.Invoke(_user != null);
        }

        private void EnsureReady()
        {
            if (!IsInitialized || _auth == null)
                throw new InvalidOperationException("FirebaseAuthService not initialized. Call InitializeAsync() first.");
        }

    }
}
