using Firebase.Auth;
using MyUnityPackage.Toolkit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyUnityPackage.Requests.Samples
{
    public class UI_GameData : UI_Base
    {
        [Header("Signed-in actions")]
        [SerializeField] private Button signOutButton;
        [SerializeField] private Button loadProfileButton;
        [SerializeField] private Button saveProfileButton;

        [Header("Profile fields (Samples)")]
        [SerializeField] private Image avatarImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI userIDText;
        [SerializeField] private TextMeshProUGUI mailText;
        
        [SerializeField] private TextMeshProUGUI oldText;
        [SerializeField] private TMP_InputField oldInputField;
        [SerializeField] private TMP_Dropdown roleDropdown; 
        
        private FirebaseGameManager firebaseGameManager;
        private User user;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        protected override void Start()
        {
            base.Start();      
            var account = ServiceLocator.GetService<AccountManager>();
            firebaseGameManager = ServiceLocator.GetService<FirebaseGameManager>();
            user = ServiceLocator.GetService<User>();

            if (account == null)
            {
                MUPLogger.Error("AccountManager not found (ServiceLocator). UI_GameData requires AccountManager to function.");
                return;
            }

            if (signOutButton) signOutButton.onClick.AddListener(() => account.SignOut());
            if (loadProfileButton) loadProfileButton.onClick.AddListener(() => firebaseGameManager.LoadUserData()); 
            if (saveProfileButton) saveProfileButton.onClick.AddListener(() => firebaseGameManager.SaveUserData());

            oldInputField.onValueChanged.AddListener(_ => OnOldChanged());
            roleDropdown.onValueChanged.AddListener(_ => OnRoleChanged());
        }
        
        public void LoadData(User user)
        {
            MUPLogger.Info("Loading profile data into UI fields");
            UserData userData = user.userData;
            if (oldText) oldText.text = userData.Age.ToString();
            if (roleDropdown) roleDropdown.value = (int)userData.Role; 
        }

        private void OnOldChanged()
        {
            MUPLogger.Info("Old changed to: " + oldInputField.text);
            user.userData.Age = int.TryParse(oldInputField.text, out int age) ? age : 0;
        }

        private void OnRoleChanged()
        {
            MUPLogger.Info("Role changed to: " + roleDropdown.value);
            user.userData.Role = (UserRole)roleDropdown.value;
        }
        
    }
}
