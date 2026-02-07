using MyUnityPackage.Toolkit;
using UnityEngine;
using Firebase.Auth;
using System.Threading.Tasks;
using System.Collections.Generic;
using Firebase.Firestore;
using System;

namespace MyUnityPackage.Requests.Samples
{
    public sealed class FirebaseGameManager : MonoBehaviour
    {
        private AccountManager _account;
        private UI_Connexion _uiConnexion;
        private UI_GameData _uiGameData;
        private User user;

        private  void Awake()
        {
            _uiConnexion = ServiceLocator.GetService<UI_Connexion>();
            _uiGameData = ServiceLocator.GetService<UI_GameData>();
            user = ServiceLocator.GetService<User>();
           
            _account = ServiceLocator.GetService<AccountManager>();
            _account.OnSignIn += () => {_ = CheckEmailVerified();};
            _account.OnSignOut += () => { _uiConnexion.Show(); _uiGameData.Hide(); };
            _account.gameObject.SetActive(true);
        }

        public async Task CheckEmailVerified()
        {
            await _account.ReloadUser();
            MUPLogger.Info("CheckEmailVerified : " + _account.authService._user.IsEmailVerified);
            if(_account.authService._user != null && !_account.authService._user.IsEmailVerified)
            {
                _uiConnexion.ShowVerifyEmailPanel(true);
            }
            else
            {
                _uiGameData.Show();
                UserData userData = _account.LoadUserData();
                MUPLogger.Info("User data loaded (typed): " + userData.Description + ", " + userData.Age + ", " + userData.Role);
                user.LoadData(userData);
                MUPLogger.Info("User data loaded into User component: " + user.userData.Description + ", " + user.userData.Age + ", " + user.userData.Role);
                _uiGameData.LoadData(user);
            }
        }


        public void LoadUserData()
        {
            UserData userData = _account.LoadUserData();
            MUPLogger.Info("User data loaded (typed): " + userData.Description + ", " + userData.Age + ", " + userData.Role);
            user.LoadData(userData);
            MUPLogger.Info("User data loaded into User component: " + user.userData.Description + ", " + user.userData.Age + ", " + user.userData.Role);
        }


        public async Task SaveUserData()
        {
            MUPLogger.Info("Saving user data from User component: " + user.userData.Description + ", " + user.userData.Age + ", " + user.userData.Role);
            await _account.SaveUserData(user.userData);
        }

    }
}

