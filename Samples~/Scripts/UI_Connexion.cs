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
        [SerializeField] private TextMeshProUGUI feedbackSignInText;

        [Header("Sign-up panel")]
        [SerializeField] private TMP_InputField signUpEmailInputField;
        [SerializeField] private TMP_InputField signUpPasswordInputField;
        [SerializeField] private TMP_InputField signUpConfirmPasswordInputField;
        [SerializeField] private Button signUpButton;
        [SerializeField] private Button backToLoginFromSignUpButton;
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

        private void Awake()
        {
            ServiceLocator.AddService<UI_Connexion>(gameObject);
        }

        protected override void Start()
        {
            base.Start();
            _account = ServiceLocator.GetService<AccountManager>();
            _gameManager = ServiceLocator.GetService<FirebaseGameManager>();
            _user = ServiceLocator.GetService<User>();

            if (_account == null)
            {
                MUPLogger.Error("[UI_Connexion] AccountManager not found. This component requires AccountManager.");
                return;
            }

            // Sign in panel
            if (signInButton) signInButton.onClick.AddListener(() => _ = OnClickSignInButtonAsync());
            if (forgotPasswordButton) forgotPasswordButton.onClick.AddListener(OnClickForgotPasswordButton);
            if (goToSignUpButton) goToSignUpButton.onClick.AddListener(ShowSignUpPanel);

            // Sign-up panel
            if (signUpButton) signUpButton.onClick.AddListener(() => _ = OnClickSignUpButtonAsync());
            if (backToLoginFromSignUpButton) backToLoginFromSignUpButton.onClick.AddListener(ShowSignInPanel);

            // Verify email panel
            if (resendVerificationEmailButton) resendVerificationEmailButton.onClick.AddListener(() => _ = OnClickResendVerificationAsync());
            if (verifyNextButton && _gameManager != null) verifyNextButton.onClick.AddListener(() => _ = CheckEmailVerifiedAsync());

            // Forgot password panel
            if (sendResetEmailButton) sendResetEmailButton.onClick.AddListener(() => _ = OnClickSendResetEmailAsync());
            if (backToLoginButton) backToLoginButton.onClick.AddListener(OnClickBackToLoginFromForgot);
        }


        public override void Show()
        {
            base.Show();
            if (signInPanel) signInPanel.SetActive(true);
            if (signUpPanel) signUpPanel.SetActive(false);
            if (verifyEmailPanel) verifyEmailPanel.SetActive(false);
            if (forgotPasswordPanel) forgotPasswordPanel.SetActive(false);
            HideAllFeedbacks();
        }

        #region UI Helpers
        private void ShowFeedbackLogin(string message, bool isError = true)
        {
            MUPLogger.Info($"[UI_Connexion] Showing feedback: {message}");
            if (!feedbackSignInText) return;
            
            feedbackSignInText.gameObject.SetActive(true);
            feedbackSignInText.text = message;
            feedbackSignInText.color = isError ? Color.red : Color.green;
        }

        private void HideFeedbackLogin()
        {
            MUPLogger.Info("[UI_Connexion] Hiding feedback login.");
            if (feedbackSignInText) feedbackSignInText.gameObject.SetActive(false);
        }

        private void ShowFeedbackSignUp(string message, bool isError = true)
        {
            MUPLogger.Info($"[UI_Connexion] Showing feedback: {message}");
            if (!feedbackSignUpText) return;
            
            feedbackSignUpText.gameObject.SetActive(true);
            feedbackSignUpText.text = message;
            feedbackSignUpText.color = isError ? Color.red : Color.green;
        }

        private void HideFeedbackSignUp()
        {
            MUPLogger.Info("[UI_Connexion] Hiding feedback sign up.");
            if (feedbackSignUpText) feedbackSignUpText.gameObject.SetActive(false);
        }

        private void ShowFeedbackVerify(string message, bool isError = true)
        {
            MUPLogger.Info($"[UI_Connexion] Showing feedback: {message}");
            if (!feedbackVerifyText) return;
            feedbackVerifyText.gameObject.SetActive(true);
            feedbackVerifyText.text = message;
            feedbackVerifyText.color = isError ? Color.red : Color.green;
        }

        private void HideFeedbackVerify()
        {
            MUPLogger.Info("[UI_Connexion] Hiding feedback verify.");
            if (feedbackVerifyText) feedbackVerifyText.gameObject.SetActive(false);
        }

        private void HideFeedbackForgotPassword()
        {
            MUPLogger.Info("[UI_Connexion] Hiding feedback forgot password.");
            if (feedbackForgotPasswordText) feedbackForgotPasswordText.gameObject.SetActive(false);
        }

        /// <summary>Cache tous les messages de feedback (nouvelle session ou changement de contexte).</summary>
        private void HideAllFeedbacks()
        {
            HideFeedbackLogin();
            HideFeedbackSignUp();
            HideFeedbackVerify();
            HideFeedbackForgotPassword();
        }

        #endregion

        #region Auth Actions

        private async Task OnClickSignInButtonAsync()
        {
            MUPLogger.Info("[UI_Connexion] Sign in button clicked.");
            HideFeedbackLogin();
            signInButton.interactable = false;

            var email = signInEmailInputField ? signInEmailInputField.text.Trim() : "";
            var password = signInPasswordInputField ? signInPasswordInputField.text : "";

            if (!ValidateCredentials(email, password, null, forLogin: true))
            {
                signInButton.interactable = true;
                return;
            }

            try
            {
                await _account.SignInEmailAsync(email, password);
                _ = CheckEmailVerifiedAsync();
            }
            catch (Exception ex)
            {
                ShowFeedbackLogin(ex.Message);
                MUPLogger.Error($"[UI_Connexion] Sign in failed: {ex.Message}");
                signInButton.interactable = true;
            }
        }

        private async Task OnClickSignUpButtonAsync()
        {
            MUPLogger.Info("[UI_Connexion] Sign-up button clicked.");
            HideFeedbackSignUp();
            signUpButton.interactable = false;

            var email = signUpEmailInputField ? signUpEmailInputField.text.Trim() : "";
            var password = signUpPasswordInputField ? signUpPasswordInputField.text : "";
            var confirmPassword = signUpConfirmPasswordInputField ? signUpConfirmPasswordInputField.text : "";

            if (!ValidateCredentials(email, password, confirmPassword, forLogin: false))
            {
                signUpButton.interactable = true;
                return;
            }

            try
            {
                await _account.SignUpEmailAsync(email, password);

                // Save initial user data after signup
                await _account.SaveUserDataAsync(_user.userData);

                ShowVerifyEmailPanel(true);
                ShowFeedbackVerify("Compte créé! Veuillez vérifier votre email.", false);
            }
            catch (Exception ex)
            {
                ShowFeedbackSignUp(ex.Message);
                MUPLogger.Error($"[UI_Connexion] Sign up failed: {ex.Message}");
                signUpButton.interactable = true;
            }
        }

        private async Task OnClickResendVerificationAsync()
        {
            MUPLogger.Info("[UI_Connexion] Resend verification email button clicked.");
            HideFeedbackVerify();
            resendVerificationEmailButton.interactable = false;
            try
            {
                await _account.SendVerificationEmailAsync();
                ShowFeedbackVerify("Email de vérification envoyé! Veuillez vérifier votre email.", false);
            }
            catch (Exception ex)
            {
                ShowFeedbackVerify(ex.Message);
                MUPLogger.Error($"[UI_Connexion] Resend verification email failed: {ex.Message}");
            }
            finally
            {
                resendVerificationEmailButton.interactable = true;
            }
        }

        private void OnClickForgotPasswordButton()
        {
            MUPLogger.Info("[UI_Connexion] Forgot password button clicked.");
            HideFeedbackForgotPassword();
            if (forgotPasswordPanel) forgotPasswordPanel.SetActive(true);
        }

        private void OnClickBackToLoginFromForgot()
        {
            MUPLogger.Info("[UI_Connexion] Back to login from forgot password button clicked.");
            if (forgotPasswordPanel) forgotPasswordPanel.SetActive(false);
            HideFeedbackForgotPassword();
            if (signInPanel) signInPanel.SetActive(true);
        }

        private bool ValidateCredentials(string email, string password, string confirmPassword = null, bool forLogin = false)
        {
            MUPLogger.Info("[UI_Connexion] Validating credentials.");
            void Feedback(string message, bool isError = true)
            {
                if (forLogin)
                    ShowFeedbackLogin(message, isError);
                else
                    ShowFeedbackSignUp(message, isError);
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                Feedback("Veuillez entrer votre email.");
                return false;
            }

            if (!email.Contains("@") || !email.Contains("."))
            {
                Feedback("Veuillez entrer un email valide.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                Feedback("Veuillez entrer votre mot de passe.");
                return false;
            }

            if (password.Length < 6)
            {
                Feedback("Le mot de passe doit contenir au moins 6 caractères.");
                return false;
            }

            if (confirmPassword != null && password != confirmPassword)
            {
                Feedback("Les mots de passe ne correspondent pas.");
                return false;
            }

            return true;
        }

        private async Task OnClickSendResetEmailAsync()
        {
            MUPLogger.Info("[UI_Connexion] Send reset email button clicked.");
            HideFeedbackForgotPassword();
            sendResetEmailButton.interactable = false;

            var email = forgotPasswordEmailInputField ? forgotPasswordEmailInputField.text.Trim() : "";

            if (string.IsNullOrWhiteSpace(email))
            {
                ShowFeedbackForgotPassword("Veuillez entrer votre email.", true);
                sendResetEmailButton.interactable = true;
                return;
            }

            try
            {
                await _account.SendPasswordResetEmailAsync(email);
                ShowFeedbackForgotPassword("Email de réinitialisation envoyé! Veuillez vérifier votre boîte de réception.", false);
            }
            catch (Exception ex)
            {
                ShowFeedbackForgotPassword(ex.Message);
                MUPLogger.Error($"[UI_Connexion] Send reset email failed: {ex.Message}");
            }
            finally
            {
                sendResetEmailButton.interactable = true;
            }
        }

        private void ShowFeedbackForgotPassword(string message, bool isError = true)
        {
            MUPLogger.Info($"[UI_Connexion] Showing feedback: {message}");
            if (!feedbackForgotPasswordText) return;
            
            feedbackForgotPasswordText.gameObject.SetActive(true);
            feedbackForgotPasswordText.text = message;
            feedbackForgotPasswordText.color = isError ? Color.red : Color.green;
        }

        public void ShowVerifyEmailPanel(bool show)
        {
            MUPLogger.Info($"[UI_Connexion] Showing verify email panel: {show}");
            if (show)
            {
                if (signInPanel) signInPanel.SetActive(false);
                if (signUpPanel) signUpPanel.SetActive(false);
                if (forgotPasswordPanel) forgotPasswordPanel.SetActive(false);
                if (verifyEmailPanel) verifyEmailPanel.SetActive(true);
                // Ne pas cacher le feedback verify : il affiche souvent "Account created!..." ou message de resend
            }
            else
            {
                if (verifyEmailPanel) verifyEmailPanel.SetActive(false);
                HideFeedbackVerify();
            }
        }

        /// <summary>Affiche le panel de connexion (login).</summary>
        public void ShowSignInPanel()
        {
            MUPLogger.Info("[UI_Connexion] Showing signIn panel.");
            if (signInPanel) signInPanel.SetActive(true);
            if (signUpPanel) signUpPanel.SetActive(false);
            HideFeedbackLogin();
            HideFeedbackSignUp();
        }

        /// <summary>Affiche le panel de création de compte (sign-up).</summary>
        public void ShowSignUpPanel()
        {
            MUPLogger.Info("[UI_Connexion] Showing sign up panel.");
            if (signInPanel) signInPanel.SetActive(false);
            if (signUpPanel) signUpPanel.SetActive(true);
            HideFeedbackLogin();
            HideFeedbackSignUp();
        }

        private async Task CheckEmailVerifiedAsync()
        {
            await _account.ReloadUserAsync();
            
            MUPLogger.Info($"[UI_Connexion] Email verified: {_account.IsEmailVerified}");
            
            if (!_account.IsEmailVerified)
            {
                ShowVerifyEmailPanel(true);
            }
            else
            {
                await _gameManager.ShowGameDataAsync();
            }
        }

        #endregion
    }
}
