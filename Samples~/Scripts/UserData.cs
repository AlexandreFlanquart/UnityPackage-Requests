using System;
using Firebase.Firestore;
using MyUnityPackage.Requests;

namespace MyUnityPackage.Requests.Samples
{
    public enum UserRole
    {
        Unknown = 0,
        Warrior = 1,
        Mage = 2,
        Healer = 3
    }

    [Serializable]
    [FirestoreData]
    public sealed class UserData : IFirestoreData
    {
        [FirestoreProperty("description")]
        public string Description { get; set; }
        
        [FirestoreProperty("age")]
        public int Age { get; set; }
        
        [FirestoreProperty("role")]
        public UserRole Role { get; set; }

        public UserData()
        {
            Description = "No description";
            Age = 0;
            Role = UserRole.Unknown;
        }
    }
}
