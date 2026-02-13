using MyUnityPackage.Toolkit;
using MyUnityPackage.Requests;
using UnityEngine;

namespace MyUnityPackage.Requests.Samples
{
    public class User : MonoBehaviour
    {
        public UserData userData { get; private set; }

        private void Awake()
        {
            ServiceLocator.AddService<User>(gameObject);
            userData = new UserData();
        }

        public void LoadData(UserData data)
        {
            if (data == null)
            {
                MUPLogger.Warning("[User] Attempted to load null data. Using defaults.");
                userData = new UserData();
                return;
            }
            
            userData = data;
            MUPLogger.Info($"[User] Data loaded: {userData.Description}, Age: {userData.Age}, Role: {userData.Role}");
        }
    }
}
