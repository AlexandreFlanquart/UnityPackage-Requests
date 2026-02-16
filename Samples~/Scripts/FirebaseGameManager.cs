using MyUnityPackage.Toolkit;
using MyUnityPackage.Requests;
using UnityEngine;
using System.Threading.Tasks;

namespace MyUnityPackage.Requests.Samples
{
    public sealed class FirebaseGameManager : MonoBehaviour
    {
        private AccountManager _account;
        private UI_Connexion _uiConnexion;
        private UI_GameData _uiGameData;
        private User _user;

        private void Awake()
        {
            _uiConnexion = ServiceLocator.GetService<UI_Connexion>();
            _uiGameData = ServiceLocator.GetService<UI_GameData>();
            _user = ServiceLocator.GetService<User>();
           
            _account = ServiceLocator.GetService<AccountManager>();
            _account.OnSignOut += OnUserSignedOut;
            _account.OnInitialized += OnAccountInitialized;
        }

        private void OnDestroy()
        {
            if (_account != null)
            {
                _account.OnSignOut -= OnUserSignedOut;
                _account.OnInitialized -= OnAccountInitialized;
            }
        }

        private async void OnAccountInitialized()
        {
            MUPLogger.Info("[FirebaseGameManager] Account system initialized. Checking user status.");
            _uiConnexion.HideLoadingScreen();
            // Si l'utilisateur est déjà connecté (et email vérifié) au démarrage, on affiche directement les données de jeu
            if (_account.IsSignedIn && _account.IsEmailVerified)
            {
                MUPLogger.Info("[FirebaseGameManager] User already signed in on startup. Showing game data.");
                await ShowGameDataAsync();
            }
        }

        private void OnUserSignedOut()
        {
            MUPLogger.Info("[FirebaseGameManager] User signed out. Showing login panel.");
            _uiConnexion.Show();
            _uiGameData.Hide();
        }

        public async Task ShowGameDataAsync()
        {
            MUPLogger.Info("[FirebaseGameManager] Showing game data panel.");
            _uiConnexion.Hide();
            _uiGameData.Show();
            await LoadUserDataAsync();      
        }

        public async Task LoadUserDataAsync()
        {
            MUPLogger.Info("[FirebaseGameManager] Loading user data.");
            try
            {
                UserData userData = await _account.LoadUserDataAsync<UserData>();
                
                if (userData != null)
                {
                    _user.LoadData(userData);
                    MUPLogger.Info($"[FirebaseGameManager] User data loaded: {userData.Description}, Age: {userData.Age}, Role: {userData.Role}");
                    _uiGameData.LoadData(_user);
                }
                else
                {
                    MUPLogger.Info("[FirebaseGameManager] No existing user data found. Using defaults.");
                }
            }
            catch (System.Exception ex)
            {
                MUPLogger.Error($"[FirebaseGameManager] Failed to load user data: {ex.Message}");
            }
        }
    }
}
