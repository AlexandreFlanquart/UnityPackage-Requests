using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Google;
using MyUnityPackage.Toolkit;

namespace MyUnityPackage.Requests
{
    public sealed class FirebaseAuthService : IDisposable
    {
        public event Action<bool> AuthStateChanged;

        public bool IsInitialized { get; private set; }
        public bool IsSignedIn => _user != null;
        public string UserId => _user?.UserId;
        public string Email => _user?.Email;
        public bool IsEmailVerified => _user?.IsEmailVerified ?? false;
        public string DisplayName => _user?.DisplayName;

        private FirebaseAuth _auth;
        private FirebaseUser _user;
        private bool _disposed;
        private Task _initTask;

        private GoogleSignInConfiguration googleConfig;
        private string webClientId;

        private bool _authBusy = false;

        // ---------------------------------------------------------
        // CONFIGURATION
        // ---------------------------------------------------------

        public void SetWebClientId(string id)
        {
            webClientId = id;
            MUPLogger.Info($"[FirebaseAuthService] WebClientId set to: {id}");
        }

        // ---------------------------------------------------------
        // INITIALISATION
        // ---------------------------------------------------------

        public Task InitializeAsync()
        {
            _initTask ??= InitializeInternalAsync();
            return _initTask;
        }

        private async Task InitializeInternalAsync()
        {
            if (IsInitialized) return;

            MUPLogger.Info("[FirebaseAuthService] Initializing FirebaseAuthService...");

            await FirebaseBootstrap.InitializeAsync();

            _auth = FirebaseAuth.DefaultInstance;
            _auth.StateChanged += OnAuthStateChanged;
            _user = _auth.CurrentUser;

            googleConfig = new GoogleSignInConfiguration
            {
                WebClientId = webClientId,
                RequestIdToken = true
            };

            if (_user != null)
            {
                MUPLogger.Info("[FirebaseAuthService] Existing user detected, reloading...");
                await ReloadUserAsync();
            }

            IsInitialized = true;
            MUPLogger.Info("[FirebaseAuthService] Initialization complete.");
        }

        // ---------------------------------------------------------
        // VERROU & TIMEOUT
        // ---------------------------------------------------------

        private async Task RunExclusive(Func<Task> action)
        {
            if (_authBusy)
            {
                MUPLogger.Warning("[FirebaseAuthService] Operation blocked: another auth operation is running.");
                throw new InvalidOperationException("Another authentication operation is already in progress.");
            }

            _authBusy = true;
            MUPLogger.Info("[FirebaseAuthService] Auth lock acquired.");

            try
            {
                await action();
            }
            finally
            {
                _authBusy = false;
                MUPLogger.Info("[FirebaseAuthService] Auth lock released.");
            }
        }

        private async Task<T> WithTimeout<T>(Task<T> task, int timeoutMs = 15000)
        {
            var delay = Task.Delay(timeoutMs);
            var completed = await Task.WhenAny(task, delay);

            if (completed == delay)
            {
                MUPLogger.Error("[FirebaseAuthService] Operation timed out.");
                throw new TimeoutException("The authentication operation timed out.");
            }

            return await task;
        }

        private async Task WithTimeout(Task task, int timeoutMs = 15000)
        {
            var delay = Task.Delay(timeoutMs);
            var completed = await Task.WhenAny(task, delay);

            if (completed == delay)
            {
                MUPLogger.Error("[FirebaseAuthService] Operation timed out.");
                throw new TimeoutException("The authentication operation timed out.");
            }

            await task;
        }

        // ---------------------------------------------------------
        // GOOGLE SIGN-IN
        // ---------------------------------------------------------

        public async Task SignInWithGoogleAsync()
        {
            EnsureReady();

            await RunExclusive(async () =>
            {
                MUPLogger.Info("[FirebaseAuthService] Starting Google Sign-In...");

                try
                {
                    GoogleSignIn.Configuration = googleConfig;

                    GoogleSignInUser googleUser = await WithTimeout(
                        GoogleSignIn.DefaultInstance.SignIn(),
                        15000
                    );

                    MUPLogger.Info("[FirebaseAuthService] Google token received, signing into Firebase...");

                    var credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);

                    var result = await WithTimeout(
                        _auth.SignInWithCredentialAsync(credential),
                        15000
                    );

                    _user = result;

                    MUPLogger.Info($"[FirebaseAuthService] Google Sign-In success. User: {_user.DisplayName}");
                }
                catch (Exception ex)
                {
                    MUPLogger.Error("[FirebaseAuthService] Google Sign-In failed: " + ex.Message);
                    throw;
                }
            });
        }

        public void SignOutGoogle()
        {
            EnsureReady();
            MUPLogger.Info("[FirebaseAuthService] Signing out from Google...");

            _auth.SignOut();
            GoogleSignIn.DefaultInstance.SignOut();

            MUPLogger.Info("[FirebaseAuthService] Google Sign-Out complete.");
        }

        public void DisconnectGoogle()
        {
            EnsureReady();
            MUPLogger.Info("[FirebaseAuthService] Disconnecting Google account...");

            _auth.SignOut();
            GoogleSignIn.DefaultInstance.Disconnect();

            MUPLogger.Info("[FirebaseAuthService] Google account disconnected.");
        }

        // ---------------------------------------------------------
        // EMAIL / PASSWORD
        // ---------------------------------------------------------

        public async Task SignUpEmailAsync(string email, string password)
        {
            EnsureReady();
            ValidateCredentials(email, password);

            await RunExclusive(async () =>
            {
                MUPLogger.Info($"[FirebaseAuthService] Creating account: {email}");

                try
                {
                    var result = await WithTimeout(
                        _auth.CreateUserWithEmailAndPasswordAsync(email, password),
                        15000
                    );

                    _user = result.User;

                    MUPLogger.Info("[FirebaseAuthService] Account created. Sending verification email...");
                    await SendEmailVerificationAsync();
                }
                catch (FirebaseException ex)
                {
                    var msg = GetFirebaseErrorMessage(ex);
                    MUPLogger.Error($"[FirebaseAuthService] SignUp failed: {msg}");
                    throw new Exception(msg, ex);
                }
            });
        }

        public async Task SignInEmailAsync(string email, string password)
        {
            EnsureReady();
            ValidateCredentials(email, password);

            await RunExclusive(async () =>
            {
                MUPLogger.Info($"[FirebaseAuthService] Signing in: {email}");

                try
                {
                    var result = await WithTimeout(
                        _auth.SignInWithEmailAndPasswordAsync(email, password),
                        15000
                    );

                    _user = result.User;

                    MUPLogger.Info($"[FirebaseAuthService] SignIn success. User: {_user.UserId}");
                }
                catch (FirebaseException ex)
                {
                    var msg = GetFirebaseErrorMessage(ex);
                    MUPLogger.Error($"[FirebaseAuthService] SignIn failed: {msg}");
                    throw new Exception(msg, ex);
                }
            });
        }

        public async Task SendPasswordResetEmailAsync(string email)
        {
            EnsureReady();

            MUPLogger.Info($"[FirebaseAuthService] Sending password reset email to: {email}");

            try
            {
                await WithTimeout(_auth.SendPasswordResetEmailAsync(email), 15000);
                MUPLogger.Info("[FirebaseAuthService] Password reset email sent.");
            }
            catch (FirebaseException ex)
            {
                var msg = GetFirebaseErrorMessage(ex);
                MUPLogger.Error($"[FirebaseAuthService] Reset failed: {msg}");
                throw new Exception(msg, ex);
            }
        }

        public async Task SendEmailVerificationAsync()
        {
            if (_user == null)
                throw new InvalidOperationException("No user signed in.");

            MUPLogger.Info("[FirebaseAuthService] Sending verification email...");

            try
            {
                await WithTimeout(_user.SendEmailVerificationAsync(), 15000);
                MUPLogger.Info("[FirebaseAuthService] Verification email sent.");
            }
            catch (FirebaseException ex)
            {
                var msg = GetFirebaseErrorMessage(ex);
                MUPLogger.Error($"[FirebaseAuthService] Verification failed: {msg}");
                throw new Exception(msg, ex);
            }
        }

        public async Task ReloadUserAsync()
        {
            if (_user == null)
            {
                MUPLogger.Warning("[FirebaseAuthService] No user to reload.");
                return;
            }

            MUPLogger.Info("[FirebaseAuthService] Reloading user...");

            try
            {
                await WithTimeout(_user.ReloadAsync(), 15000);
                _user = _auth.CurrentUser;

                MUPLogger.Info("[FirebaseAuthService] User reloaded.");
            }
            catch (FirebaseException ex)
            {
                var msg = GetFirebaseErrorMessage(ex);
                MUPLogger.Error($"[FirebaseAuthService] Reload failed: {msg}");
                throw new Exception(msg, ex);
            }
        }

        public async Task UpdateDisplayNameAsync(string displayName)
        {
            if (_user == null)
                throw new InvalidOperationException("No user signed in.");

            MUPLogger.Info($"[FirebaseAuthService] Updating display name to: {displayName}");

            try
            {
                var profile = new UserProfile { DisplayName = displayName };
                await WithTimeout(_user.UpdateUserProfileAsync(profile), 15000);

                MUPLogger.Info("[FirebaseAuthService] Display name updated.");
            }
            catch (FirebaseException ex)
            {
                var msg = GetFirebaseErrorMessage(ex);
                MUPLogger.Error($"[FirebaseAuthService] Update failed: {msg}");
                throw new Exception(msg, ex);
            }
        }

        public void SignOut()
        {
            if (_auth == null) return;

            MUPLogger.Info("[FirebaseAuthService] Signing out...");
            _auth.SignOut();
            MUPLogger.Info("[FirebaseAuthService] SignOut complete.");
        }

        public async Task DeleteAccountAsync()
        {
            if (_user == null)
                throw new InvalidOperationException("No user signed in.");

            MUPLogger.Info("[FirebaseAuthService] Deleting account...");

            try
            {
                await WithTimeout(_user.DeleteAsync(), 15000);
                _user = null;

                MUPLogger.Info("[FirebaseAuthService] Account deleted.");
            }
            catch (FirebaseException ex)
            {
                var msg = GetFirebaseErrorMessage(ex);
                MUPLogger.Error($"[FirebaseAuthService] Delete failed: {msg}");
                throw new Exception(msg, ex);
            }
        }

        // ---------------------------------------------------------
        // INTERNALS
        // ---------------------------------------------------------

        private void OnAuthStateChanged(object sender, EventArgs e)
        {
            if (_disposed) return;

            var previous = _user;
            _user = _auth.CurrentUser;

            bool changed = previous?.UserId != _user?.UserId;

            if (changed)
            {
                MUPLogger.Info($"[FirebaseAuthService] Auth state changed: {(_user != null ? "Signed In" : "Signed Out")}");
                AuthStateChanged?.Invoke(_user != null);
            }
        }

        private void EnsureReady()
        {
            if (!IsInitialized || _auth == null)
                throw new InvalidOperationException("FirebaseAuthService not initialized.");
        }

        private static void ValidateCredentials(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required.");

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                throw new ArgumentException("Password must be at least 6 characters.");
        }

        private static string GetFirebaseErrorMessage(FirebaseException ex)
        {
            return ex.ErrorCode switch
            {
                (int)AuthError.EmailAlreadyInUse => "Email or password incorrect.",
                (int)AuthError.InvalidEmail => "Invalid email address.",
                (int)AuthError.WeakPassword => "Password is too weak.",
                (int)AuthError.WrongPassword => "Email or password incorrect.",
                (int)AuthError.UserNotFound => "Email or password incorrect.",
                (int)AuthError.UserDisabled => "This account is disabled.",
                (int)AuthError.TooManyRequests => "Too many attempts. Try later.",
                _ => ex.Message
            };
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            MUPLogger.Info("[FirebaseAuthService] Disposing...");

            if (_auth != null)
                _auth.StateChanged -= OnAuthStateChanged;
        }
    }
}
