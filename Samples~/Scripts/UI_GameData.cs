using MyUnityPackage.Toolkit;
using MyUnityPackage.Requests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyUnityPackage.Requests.Samples
{
    public class UI_GameData : UI_Base
    {
        [Header("Actions")]
        [SerializeField] private Button signOutButton;
        [SerializeField] private Button loadProfileButton;
        [SerializeField] private Button saveProfileButton;
        [SerializeField] private Button deleteAccountButton;

        [Header("Profile Display")]
        [SerializeField] private Image avatarImage;
        [SerializeField] private TextMeshProUGUI displayNameText;
        [SerializeField] private TextMeshProUGUI userIdText;
        [SerializeField] private TextMeshProUGUI emailText;

        [Header("Editable Fields")]
        [SerializeField] private TMP_InputField ageInputField;
        [SerializeField] private TMP_Dropdown roleDropdown;
        [SerializeField] private TMP_InputField descriptionInputField;

        private AccountManager _account;
        private FirebaseGameManager _gameManager;
        private User _user;

        protected override void Start()
        {
            base.Start();
            ServiceLocator.AddService<UI_GameData>(gameObject);
            
            _account = ServiceLocator.GetService<AccountManager>();
            _gameManager = ServiceLocator.GetService<FirebaseGameManager>();
            _user = ServiceLocator.GetService<User>();

            if (_account == null)
            {
                MUPLogger.Error("[UI_GameData] AccountManager not found. This component requires AccountManager.");
                return;
            }

            // Button listeners
            if (signOutButton) signOutButton.onClick.AddListener(() => _account.SignOut());
            if (loadProfileButton) loadProfileButton.onClick.AddListener(() => _ = _gameManager.LoadUserDataAsync());
            if (saveProfileButton) saveProfileButton.onClick.AddListener(() => _ = _account.SaveUserDataAsync(_user.userData));
            if(deleteAccountButton) deleteAccountButton.onClick.AddListener(() => _ = OnClickDeleteAccountAsync());

            // Field change listeners
            if (ageInputField) ageInputField.onEndEdit.AddListener(OnAgeChanged);
            if (roleDropdown) roleDropdown.onValueChanged.AddListener(OnRoleChanged);
            if (descriptionInputField) descriptionInputField.onEndEdit.AddListener(OnDescriptionChanged);
        }

        public void LoadData(User user)
        {
            if (user?.userData == null)
            {
                MUPLogger.Warning("[UI_GameData] No user data to display.");
                return;
            }

            MUPLogger.Info("[UI_GameData] Loading profile data into UI...");
            UserData userData = user.userData;

            // Display account info
            if (userIdText) userIdText.text = _account.UserId ?? "N/A";
            if (emailText) emailText.text = _account.Email ?? "N/A";

            // Display editable fields
            if (ageInputField) ageInputField.text = userData.Age.ToString();
            if (roleDropdown) roleDropdown.value = (int)userData.Role;
            if (descriptionInputField) descriptionInputField.text = userData.Description ?? "";
        }

        private void OnAgeChanged(string value)
        {
            if (_user?.userData == null) return;
            
            _user.userData.Age = int.TryParse(value, out int age) ? Mathf.Max(0, age) : 0;
            MUPLogger.Info($"[UI_GameData] Age changed to: {_user.userData.Age}");
        }

        private void OnRoleChanged(int value)
        {
            if (_user?.userData == null) return;
            
            _user.userData.Role = (UserRole)value;
            MUPLogger.Info($"[UI_GameData] Role changed to: {_user.userData.Role}");
        }

        private void OnDescriptionChanged(string value)
        {
            if (_user?.userData == null) return;
            
            _user.userData.Description = value;
            MUPLogger.Info($"[UI_GameData] Description changed to: {_user.userData.Description}");
        }

        private async System.Threading.Tasks.Task OnClickDeleteAccountAsync()
        {
            try
            {
                await _account.DeleteAccountAsync();
            }
            catch (System.Exception ex)
            {
                MUPLogger.Error($"[UI_GameData] Delete account failed: {ex.Message}");
            }
        }

    }
}
