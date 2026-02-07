using System;
using System.Threading.Tasks;
using MyUnityPackage.Toolkit;
using MyUnityPackage.Requests;
using MyUnityPackage.Requests.Samples;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace MyUnityPackage.Requests.Samples
{
    public sealed class UI_Connexion : UI_Base
    {
        [Header("Status")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI feedbackText;

        [Header("Menu Buttons")]
        [SerializeField] private Button loginChooseButton;
        [SerializeField] private Button signInChooseButton;

        [Header("Panels")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject signUpPanel;
        [SerializeField] private GameObject verifyEmailPanel;

        [Header("Login panel")]
        [SerializeField] private TMP_InputField loginEmailInputField;
        [SerializeField] private TMP_InputField loginPasswordInputField;
        [SerializeField] private Button loginButton;

        [Header("Sign-up panel")]
        [SerializeField] private TMP_InputField signUpEmailInputField;
        [SerializeField] private TMP_InputField signUpPasswordInputField;
        [SerializeField] private TMP_InputField signUpConfirmPasswordInputField;
        [SerializeField] private Button signUpButton;

        [Header("comfirm email panel")]
        [SerializeField] private Button resendVerificationEmailButton;
        [SerializeField] private Button nextButton;

        private AccountManager _account;
        private FirebaseGameManager gameManager;
        private User user;

        private void Awake()
        {
            ServiceLocator.AddService<UI_Connexion>(gameObject);
        }

        protected override void Start()
        {
            base.Start();
            _account = ServiceLocator.GetService<AccountManager>();
            gameManager = ServiceLocator.GetService<FirebaseGameManager>();
            user = ServiceLocator.GetService<User>();

            if (_account == null)
            {
                MUPLogger.Error("AccountManager not found (ServiceLocator). UI_Connexion requires AccountManager to function.");
                return;
            }

            if (loginChooseButton) loginChooseButton.onClick.AddListener(() => ShowLogInPanel(true));
            if (signInChooseButton) signInChooseButton.onClick.AddListener(() => ShowSignUpPanel(true));

            if (loginButton) loginButton.onClick.AddListener(() => OnClickLogInButton());
            if (signUpButton) signUpButton.onClick.AddListener(() => _ = OnClickSignUpButton());

            if (resendVerificationEmailButton) resendVerificationEmailButton.onClick.AddListener(() => _account.SendVerificationEmail());
            if (nextButton) nextButton.onClick.AddListener(() => _ = gameManager.CheckEmailVerified()); // In a real app, you would check if the email is verified before proceeding
        }

        void OnEnable()
        {
            feedbackText.gameObject.SetActive(false);
            loginButton.interactable = true;
            signUpButton.interactable = true;
        }

        // ---------------- UI helpers ----------------

        private void ShowLogInPanel(bool login)
        {
            if (loginPanel) loginPanel.SetActive(login);
        }

        private void ShowSignUpPanel(bool login)
        {
            if (signUpPanel) signUpPanel.SetActive(login);
        }

        public void ShowVerifyEmailPanel(bool show)
        {
            if (verifyEmailPanel) verifyEmailPanel.SetActive(show);
        }

        private void SetStatus(string message)
        {
            if (statusText) statusText.text = message;
            MUPLogger.Info(message);
        }

        private void SetFeedbackText(string message)
        {
            if (!feedbackText) return;
            
            feedbackText.gameObject.SetActive(true);
            feedbackText.text = message;
        }

        // ---------------- Auth actions ----------------

        private void OnClickLogInButton()
        {
            MUPLogger.Info("Login button clicked.");
            loginButton.interactable = false;

            var email = loginEmailInputField ? loginEmailInputField.text.Trim() : "";
            var pwd = loginPasswordInputField ? loginPasswordInputField.text : "";

            if (!ValidateCredentials(email, pwd)) {
                MUPLogger.Warning("Login validation failed.");
                loginButton.interactable = true;
                return;
            }

            _account.SignInEmail(email, pwd);
        }

        private async Task OnClickSignUpButton()
        {
            MUPLogger.Info("Sign-up button clicked.");
            signUpButton.interactable = false;

            // Get credentials from input fields
            var email = signUpEmailInputField ? signUpEmailInputField.text.Trim() : "";
            var pwd = signUpPasswordInputField ? signUpPasswordInputField.text : "";
            var confirmPwd = signUpConfirmPasswordInputField ? signUpConfirmPasswordInputField.text : "";

            if (!ValidateCredentials(email, pwd, confirmPwd)) {
                signUpButton.interactable = true;
                MUPLogger.Warning("Sign-up validation failed.");
                return;
            }

            await _account.SignUpEmail(email, pwd);
            await _account.SaveUserData(user.userData);
        }

        // Check validation email & password 
        private bool ValidateCredentials(string email, string pwd, string confirmPwd = null)
        {
            MUPLogger.Info("Validating credentials...");
            // Email rules : not empty, contains @
            if (string.IsNullOrWhiteSpace(email))
            {
                SetFeedbackText("Email is required.");
                return false;
            }
            if (!email.Contains("@"))
            {
                SetFeedbackText("Email seems invalid.");
                return false;
            }

            // PWD rules : not empty, at least 6 chars, match confirm pwd
            if (string.IsNullOrWhiteSpace(pwd))
            {
                SetFeedbackText("Password is required.");
                return false;
            }
            if (confirmPwd != null && pwd != confirmPwd)
            {
                SetFeedbackText("Passwords do not match.");
                return false;
            }
            if (pwd.Length < 6)
            {
                SetFeedbackText("Password must be at least 6 characters.");
                return false;
            }

            return true;
        }

    }
}
