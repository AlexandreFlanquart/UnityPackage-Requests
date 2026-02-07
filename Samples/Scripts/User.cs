using Firebase.Auth;
using MyUnityPackage.Toolkit;
using Unity.VisualScripting;
using UnityEngine;

namespace MyUnityPackage.Requests.Samples
{
    public class User : MonoBehaviour
    {
        public UserData userData;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            userData = new UserData();
        }

        public void LoadData(UserData data)
        {
            MUPLogger.Info("User.LoadData: " + data.Description + ", " + data.Age + ", " + data.Role);
            userData = data;

        }

    }
}
