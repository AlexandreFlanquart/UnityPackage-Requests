using System;
using System.Threading.Tasks;
using MyUnityPackage.Toolkit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyUnityPackage.Requests.Samples
{
    public sealed class UI_Connexion : UI_Base
    {
        [Header("Panels")]
        [SerializeField] private GameObject signInPanel;
        [SerializeField] private GameObject signUpPanel;
        [SerializeField] private GameObject verifyEmailPanel;
        [SerializeField] private GameObject forgotPasswordPanel;

        [Header("Sign in panel")]
        [SerializeField] private TMP_InputField signInEmailInputField;
        [SerializeField] private TMP_InputField signInPasswordInputField;
        [SerializeField] private Button signInButton;
        [SerializeField] private Button forgotPasswordButton;
        [SerializeField] private Button goToSignUpButton;
        [SerializeField] private Button googleSignInButton;
        [SerializeField] private TextMeshProUGUI feedbackSignInText;

        [Header("Sign-up panel")]
        [SerializeField] private TMP_InputField signUpEmailInputField;
        [SerializeField] private TMP_InputField signUpPasswordInputField;
        [SerializeField] private TMP_InputField signUpConfirmPasswordInputField;
        [SerializeField] private Button signUpButton;
        [SerializeField] private Button backToLoginFromSignUpButton;
        [SerializeField] private Button googleSignUpButton;
        [SerializeField] private TextMeshProUGUI feedbackSignUpText;

        [Header("Verify email panel")]
        [SerializeField] private Button resendVerificationEmailButton;
        [SerializeField] private Button verifyNextButton;
        [SerializeField] private TextMeshProUGUI feedbackVerifyText;

        [Header("Forgot password panel")]
        [SerializeField] private TMP_InputField forgotPasswordEmailInputField;
        [SerializeField] private Button sendResetEmailButton;
        [SerializeField] private Button backToLoginButton;
        [SerializeField] private TextMeshProUGUI feedbackForgotPasswordText;

        private AccountManager _account;
        private FirebaseGameManager _gameManager;
        private User _user;
        private bool _isUiLocked;
        private UI_AlwaysVisible uiAlwaysVisible;

        private enum UIPanel { SignIn, SignUp, ForgotPassword, VerifyEmail }

        protected override void Start()
        {
            base.Start();

            _account = ServiceLocator.GetService<AccountManager>();
            _gameManager = ServiceLocator.GetService<FirebaseGameManager>();
            _user = ServiceLocator.GetService<User>();
            uiAlwaysVisible = ServiceLocator.GetService<UI_AlwaysVisible>();

            if (uiAlwaysVisible == null)
                MUPLogger.Warning("[UI_Connexion] UI_AlwaysVsible not found. Loading screen will not be shown.");

            signInButton.onClick.AddListener(() => _ = OnClickSignInAsync());
            goToSignUpButton.onClick.AddListener(() => ShowPanel(UIPanel.SignUp));
            forgotPasswordButton.onClick.AddListener(() => ShowPanel(UIPanel.ForgotPassword));
            googleSignInButton.onClick.AddListener(() => _ = OnClickGoogleSignInAsync());
            googleSignUpButton.onClick.AddListener(() => _ = OnClickGoogleSignInAsync());
            signUpButton.onClick.AddListener(() => _ = OnClickSignUpAsync());
            backToLoginFromSignUpButton.onClick.AddListener(() => ShowPanel(UIPanel.SignIn));

            resendVerificationEmailButton.onClick.AddListener(() => _ = OnClickResendVerificationAsync());
            verifyNextButton.onClick.AddListener(() => _ = OnClickVerifyNextAsync());

            sendResetEmailButton.onClick.AddListener(() => _ = OnClickSendResetEmailAsync());
            backToLoginButton.onClick.AddListener(() => ShowPanel(UIPanel.SignIn));
        }

        public override void Show()
        {
            base.Show();
            ShowPanel(UIPanel.SignIn);
            HideAllFeedbacks();
        }

        // ---------------------------------------------------------
        // PANEL MANAGEMENT
        // ---------------------------------------------------------

        private void ShowPanel(UIPanel panel)
        {
            MUPLogger.Info($"[UI_Connexion] Showing panel: {panel}");
            signInPanel.SetActive(panel == UIPanel.SignIn);
            signUpPanel.SetActive(panel == UIPanel.SignUp);
            forgotPasswordPanel.SetActive(panel == UIPanel.ForgotPassword);
            verifyEmailPanel.SetActive(panel == UIPanel.VerifyEmail);
        }

        private async Task RunWithUiLockAsync(Func<Task> action)
        {
            if (_isUiLocked)
            {
                MUPLogger.Warning("[UI_Connexion] Click ignored: UI is locked by an ongoing operation.");
                return;
            }

            _isUiLocked = true;
            uiAlwaysVisible?.ShowLoading();

            try
            {
                await action();
            }
            finally
            {
                uiAlwaysVisible?.HideLoading();
                _isUiLocked = false;
            }
        }

        // ---------------------------------------------------------
        // FEEDBACK
        // ---------------------------------------------------------

        private void HideAllFeedbacks()
        {
            MUPLogger.Info("[UI_Connexion] Hiding all feedback messages.");
            feedbackSignInText?.gameObject.SetActive(false);
            feedbackSignUpText?.gameObject.SetActive(false);
            feedbackForgotPasswordText?.gameObject.SetActive(false);
            feedbackVerifyText?.gameObject.SetActive(false);
        }

        private void ShowFeedback(TextMeshProUGUI target, string message, bool isError = true)
        {
            MUPLogger.Info($"[UI_Connexion] Showing feedback: {(isError ? "Error" : "Success")} - {message}");
            if (target == null) return;

            target.gameObject.SetActive(true);
            target.text = message;
            target.color = isError ? Color.red : Color.green;
        }

        // ---------------------------------------------------------
        // VALIDATION
        // ---------------------------------------------------------

        private bool ValidateEmail(string email, out string error)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                error = "Veuillez entrer votre email.";
                return false;
            }

            if (!email.Contains("@") || !email.Contains("."))
            {
                error = "Veuillez entrer un email valide.";
                return false;
            }

            error = null;
            return true;
        }

        private bool ValidatePassword(string password, out string error)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                error = "Veuillez entrer votre mot de passe.";
                return false;
            }

            if (password.Length < 6)
            {
                error = "Le mot de passe doit contenir au moins 6 caractères.";
                return false;
            }

            error = null;
            return true;
        }

        private bool ValidatePasswordMatch(string p1, string p2, out string error)
        {
            if (p1 != p2)
            {
                error = "Les mots de passe ne correspondent pas.";
                return false;
            }

            error = null;
            return true;
        }

        /// ---------------------------------------------------------
        /// GOOGLE SIGN-IN
        /// ----------------------------------------------------------
        
        private async Task OnClickGoogleSignInAsync()
        {
            await RunWithUiLockAsync(async () =>
            {
                HideAllFeedbacks();

                try
                {
                    await _account.SignInGoogleAsync();
                    await CheckEmailVerifiedAsync();
                }
                catch (Exception ex)
                {
                    ShowFeedback(feedbackSignInText, ex.Message);
                }
            });
        }

        // ---------------------------------------------------------
        // SIGN IN
        // ---------------------------------------------------------

        private async Task OnClickSignInAsync()
        {
            await RunWithUiLockAsync(async () =>
            {
                HideAllFeedbacks();

                var email = signInEmailInputField.text.Trim();
                var password = signInPasswordInputField.text;

                if (!ValidateEmail(email, out var error) ||
                    !ValidatePassword(password, out error))
                {
                    ShowFeedback(feedbackSignInText, error);
                    return;
                }

                try
                {
                    await _account.SignInEmailAsync(email, password);
                    await CheckEmailVerifiedAsync();
                }
                catch (Exception ex)
                {
                    ShowFeedback(feedbackSignInText, ex.Message);
                }
            });
        }

        // ---------------------------------------------------------
        // SIGN UP
        // ---------------------------------------------------------

        private async Task OnClickSignUpAsync()
        {
            await RunWithUiLockAsync(async () =>
            {
                HideAllFeedbacks();

                var email = signUpEmailInputField.text.Trim();
                var password = signUpPasswordInputField.text;
                var confirm = signUpConfirmPasswordInputField.text;

                if (!ValidateEmail(email, out var error) ||
                    !ValidatePassword(password, out error) ||
                    !ValidatePasswordMatch(password, confirm, out error))
                {
                    ShowFeedback(feedbackSignUpText, error);
                    return;
                }

                try
                {
                    await _account.SignUpEmailAsync(email, password);
                    await _account.SaveUserDataAsync(_user.userData);

                    ShowPanel(UIPanel.VerifyEmail);
                    ShowFeedback(feedbackVerifyText, "Compte créé ! Vérifiez votre email.", false);
                }
                catch (Exception ex)
                {
                    ShowFeedback(feedbackSignUpText, ex.Message);
                }
            });
        }

        // ---------------------------------------------------------
        // FORGOT PASSWORD
        // ---------------------------------------------------------

        private async Task OnClickSendResetEmailAsync()
        {
            await RunWithUiLockAsync(async () =>
            {
                HideAllFeedbacks();

                var email = forgotPasswordEmailInputField.text.Trim();

                if (!ValidateEmail(email, out var error))
                {
                    ShowFeedback(feedbackForgotPasswordText, error);
                    return;
                }

                try
                {
                    await _account.SendPasswordResetEmailAsync(email);
                    ShowFeedback(feedbackForgotPasswordText, "Email envoyé ! Vérifiez votre boîte de réception.", false);
                }
                catch (Exception ex)
                {
                    ShowFeedback(feedbackForgotPasswordText, ex.Message);
                }
            });
        }

        // ---------------------------------------------------------
        // VERIFY EMAIL
        // ---------------------------------------------------------

        private async Task OnClickResendVerificationAsync()
        {
            await RunWithUiLockAsync(async () =>
            {
                HideAllFeedbacks();

                try
                {
                    await _account.SendVerificationEmailAsync();
                    ShowFeedback(feedbackVerifyText, "Email envoyé !", false);
                }
                catch (Exception ex)
                {
                    ShowFeedback(feedbackVerifyText, ex.Message);
                }
            });
        }

        private async Task OnClickVerifyNextAsync()
        {
            await RunWithUiLockAsync(CheckEmailVerifiedAsync);
        }

        private async Task CheckEmailVerifiedAsync()
        {
            if (_isUiLocked == false)
            {
                MUPLogger.Warning("[UI_Connexion] CheckEmailVerifiedAsync called outside UI lock.");
            }

            await _account.ReloadUserAsync();

            if (_account.IsEmailVerified)
            {
                await _gameManager.ShowGameDataAsync();
            }
            else
            {
                ShowPanel(UIPanel.VerifyEmail);
            }
        }
    }
}
