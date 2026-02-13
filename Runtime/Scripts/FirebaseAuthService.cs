using System;
using System.Threading;
using System.Threading.Tasks;
using MyUnityPackage.Toolkit;
using Firebase;
using Firebase.Auth;

namespace MyUnityPackage.Requests
{
    /// <summary>
    /// Firebase Authentication service providing:
    /// - Email/Password SignUp, SignIn, SignOut
    /// - Email verification
    /// - Password reset
    /// - Auth state management
    /// </summary>
    public sealed class FirebaseAuthService : IDisposable
    {
        /// <summary>
        /// Fired when the authentication state changes (sign in/out).
        /// </summary>
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

        /// <summary>
        /// Initializes Firebase Auth (idempotent).
        /// Safe to call multiple times.
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
            await ReloadUserAsync();

            IsInitialized = true;
            MUPLogger.Info("[FirebaseAuthService] Initialized successfully.");
        }

        /// <summary>
        /// Creates a new user with email and password.
        /// Automatically sends a verification email on success.
        /// </summary>
        /// <param name="email">User's email address.</param>
        /// <param name="password">User's password (min 6 characters).</param>
        /// <exception cref="FirebaseAuthException">Thrown when signup fails.</exception>
        public async Task SignUpEmailAsync(string email, string password)
        {
            EnsureReady();
            ValidateCredentials(email, password);

            MUPLogger.Info($"[FirebaseAuthService] Signing up: {email}");

            try
            {
                var result = await _auth.CreateUserWithEmailAndPasswordAsync(email, password);
                _user = result.User;
                MUPLogger.Info($"[FirebaseAuthService] User created: {_user.UserId}");
                
                // Automatically send verification email
                await SendEmailVerificationAsync();
            }
            catch (FirebaseException ex)
            {
                var friendlyMessage = GetFirebaseErrorMessage(ex);
                MUPLogger.Error($"[FirebaseAuthService] SignUp failed: {friendlyMessage}");
                throw new Exception(friendlyMessage, ex);
            }
        }

        /// <summary>
        /// Signs in an existing user with email and password.
        /// </summary>
        /// <param name="email">User's email address.</param>
        /// <param name="password">User's password.</param>
        /// <exception cref="FirebaseAuthException">Thrown when signin fails.</exception>
        public async Task SignInEmailAsync(string email, string password)
        {
            EnsureReady();
            ValidateCredentials(email, password);

            MUPLogger.Info($"[FirebaseAuthService] Signing in: {email}");

            try
            {
                var result = await _auth.SignInWithEmailAndPasswordAsync(email, password);
                _user = result.User;
                MUPLogger.Info($"[FirebaseAuthService] User signed in: {_user.UserId}");
            }
            catch (FirebaseException ex)
            {
                var friendlyMessage = GetFirebaseErrorMessage(ex);
                MUPLogger.Error($"[FirebaseAuthService] SignIn failed: {friendlyMessage}");
                throw new Exception(friendlyMessage, ex);
            }
        }

        /// <summary>
        /// Sends a password reset email to the specified address.
        /// </summary>
        /// <param name="email">The email address to send the reset link to.</param>
        public async Task SendPasswordResetEmailAsync(string email)
        {
            EnsureReady();

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required.", nameof(email));

            MUPLogger.Info($"[FirebaseAuthService] Sending password reset email to: {email}");

            try
            {
                await _auth.SendPasswordResetEmailAsync(email);
                MUPLogger.Info("[FirebaseAuthService] Password reset email sent successfully.");
            }
            catch (FirebaseException ex)
            {
                var friendlyMessage = GetFirebaseErrorMessage(ex);
                MUPLogger.Error($"[FirebaseAuthService] Password reset failed: {friendlyMessage}");
                throw new Exception(friendlyMessage, ex);
            }
        }

        /// <summary>
        /// Sends a verification email to the current user.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when no user is signed in.</exception>
        public async Task SendEmailVerificationAsync()
        {
            if (_user == null)
                throw new InvalidOperationException("No user signed in. Cannot send verification email.");

            MUPLogger.Info("[FirebaseAuthService] Sending email verification...");

            try
            {
                await _user.SendEmailVerificationAsync();
                MUPLogger.Info("[FirebaseAuthService] Verification email sent successfully.");
            }
            catch (FirebaseException ex)
            {
                var friendlyMessage = GetFirebaseErrorMessage(ex);
                MUPLogger.Error($"[FirebaseAuthService] Send verification failed: {friendlyMessage}");
                throw new Exception(friendlyMessage, ex);
            }
        }

        /// <summary>
        /// Reloads the current user's data from Firebase.
        /// Use this to refresh IsEmailVerified status.
        /// </summary>
        public async Task ReloadUserAsync()
        {
            if (_user == null)
            {
                MUPLogger.Warning("[FirebaseAuthService] No user signed in. Cannot reload.");
                return;
            }

            MUPLogger.Info("[FirebaseAuthService] Reloading user data...");

            try
            {
                await _user.ReloadAsync();
                _user = _auth.CurrentUser;
                MUPLogger.Info("[FirebaseAuthService] User data reloaded successfully.");
            }
            catch (FirebaseException ex)
            {
                var friendlyMessage = GetFirebaseErrorMessage(ex);
                MUPLogger.Error($"[FirebaseAuthService] Reload failed: {friendlyMessage}");
                throw new Exception(friendlyMessage, ex);
            }
        }

        /// <summary>
        /// Updates the current user's display name.
        /// </summary>
        /// <param name="displayName">The new display name.</param>
        public async Task UpdateDisplayNameAsync(string displayName)
        {
            if (_user == null)
                throw new InvalidOperationException("No user signed in. Cannot update profile.");

            MUPLogger.Info($"[FirebaseAuthService] Updating display name to: {displayName}");

            try
            {
                var profile = new UserProfile { DisplayName = displayName };
                await _user.UpdateUserProfileAsync(profile);
                MUPLogger.Info("[FirebaseAuthService] Display name updated successfully.");
            }
            catch (FirebaseException ex)
            {
                var friendlyMessage = GetFirebaseErrorMessage(ex);
                MUPLogger.Error($"[FirebaseAuthService] Update profile failed: {friendlyMessage}");
                throw new Exception(friendlyMessage, ex);
            }
        }

        /// <summary>
        /// Signs out the current user.
        /// </summary>
        public void SignOut()
        {
            if (_auth == null) return;

            MUPLogger.Info("[FirebaseAuthService] Signing out...");
            _auth.SignOut();
            _user = null;
        }

        /// <summary>
        /// Deletes the current user's account.
        /// WARNING: This action is irreversible.
        /// </summary>
        public async Task DeleteAccountAsync()
        {
            if (_user == null)
                throw new InvalidOperationException("No user signed in. Cannot delete account.");

            MUPLogger.Info("[FirebaseAuthService] Deleting user account...");

            try
            {
                await _user.DeleteAsync();
                _user = null;
                MUPLogger.Info("[FirebaseAuthService] Account deleted successfully.");
            }
            catch (FirebaseException ex)
            {
                var friendlyMessage = GetFirebaseErrorMessage(ex);
                MUPLogger.Error($"[FirebaseAuthService] Delete account failed: {friendlyMessage}");
                throw new Exception(friendlyMessage, ex);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            MUPLogger.Info("[FirebaseAuthService] Disposing...");
            
            if (_auth != null)
                _auth.StateChanged -= OnAuthStateChanged;
        }

        private void OnAuthStateChanged(object sender, EventArgs e)
        {
            if (_disposed) return;

            var previousUser = _user;
            _user = _auth.CurrentUser;
            
            // Only fire event if state actually changed
            bool wasSignedIn = previousUser != null;
            bool isSignedIn = _user != null;
            
            if (wasSignedIn != isSignedIn)
            {
                MUPLogger.Info($"[FirebaseAuthService] Auth state changed: {(isSignedIn ? "Signed In" : "Signed Out")}");
                AuthStateChanged?.Invoke(isSignedIn);
            }
        }

        private void EnsureReady()
        {
            if (!IsInitialized || _auth == null)
                throw new InvalidOperationException("FirebaseAuthService not initialized. Call InitializeAsync() first.");
        }

        private static void ValidateCredentials(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required.", nameof(email));
            
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password is required.", nameof(password));
            
            if (password.Length < 6)
                throw new ArgumentException("Password must be at least 6 characters.", nameof(password));
        }

        /// <summary>
        /// Extracts a user-friendly error message from a Firebase exception.
        /// </summary>
        private static string GetFirebaseErrorMessage(FirebaseException ex)
        {
            // Firebase error codes: https://firebase.google.com/docs/auth/admin/errors
            return ex.ErrorCode switch
            {
                (int)AuthError.EmailAlreadyInUse => "This email is already registered.",
                (int)AuthError.InvalidEmail => "Invalid email address format.",
                (int)AuthError.WeakPassword => "Password is too weak. Use at least 6 characters.",
                (int)AuthError.WrongPassword => "Incorrect password.",
                (int)AuthError.UserNotFound => "No account found with this email.",
                (int)AuthError.UserDisabled => "This account has been disabled.",
                (int)AuthError.TooManyRequests => "Too many attempts. Please try again later.",
                (int)AuthError.NetworkRequestFailed => "Network error. Check your connection.",
                _ => ex.Message
            };
        }
    }
}
