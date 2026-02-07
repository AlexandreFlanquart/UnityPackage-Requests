using System;
using Firebase.Firestore;

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
        [FirestoreProperty]
        public string Description{ get; set; }
        [FirestoreProperty]
        public int Age { get; set; }
        [FirestoreProperty]
        public UserRole Role { get; set; }

        public UserData()
        {
            Description = "No description";
            Age = 0;
            Role = UserRole.Unknown;
        }
    }
}
