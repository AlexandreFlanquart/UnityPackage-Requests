using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using MyUnityPackage.Toolkit;

namespace MyUnityPackage.Requests.Samples
{
    /// <summary>
    /// Minimal Firestore service to load/save the current user's document.
    /// </summary>
    public sealed class FirestoreUserDataService
    {
        public bool IsInitialized { get; private set; }

        private Task _initTask;

        private FirebaseFirestore _db;

        /// <summary>
        /// Initializes Firestore (idempotent).
        /// Safe to call multiple times.
        /// </summary>
        public Task InitializeAsync()
        {
            _initTask ??= InitializeInternalAsync();
            return _initTask;
        }

        private async Task InitializeInternalAsync()
        {
            if (IsInitialized) return;

            // Ensure Firebase Core is initialized first
            await FirebaseBootstrap.InitializeAsync();

            _db = FirebaseFirestore.DefaultInstance;
            IsInitialized = true;
        }

        /// <summary>
        /// Loads the user document from: users/{uid}
        /// Returns an empty dictionary if the document does not exist (optional behavior).
        /// </summary>
        public async Task<UserData> LoadUserAsync(string uid)
        {
            EnsureReady();
            if (string.IsNullOrWhiteSpace(uid))
                throw new ArgumentException("uid is null/empty.", nameof(uid));
            
            await EnsureUserDocExistsAsync(uid);

            DocumentReference docRef = _db.Collection("users").Document(uid);
            DocumentSnapshot snap = await docRef.GetSnapshotAsync();
            return snap.ConvertTo<UserData>(); 
        }

        /// <summary>
        /// Saves user data to: users/{uid}
        /// Uses MergeAll to avoid overwriting existing fields.
        /// </summary>
        public async Task SaveUserAsync(string uid, UserData data)
        {
            EnsureReady();
            if (string.IsNullOrWhiteSpace(uid))
                throw new ArgumentException("uid is null/empty.", nameof(uid));

            if (data == null)
                throw new ArgumentNullException(nameof(data));
        MUPLogger.Info("Saving user data to Firestore for uid: " + uid + ", data: " + data.Description + ", " + data.Age + ", " + data.Role);
            DocumentReference docRef = _db.Collection("users").Document(uid);
            MUPLogger.Info("Firestore document reference created: " + docRef.Path);
            // MergeAll prevents deleting existing fields not provided in 'data'
            await docRef.SetAsync(data, SetOptions.MergeAll);
            MUPLogger.Info("User data saved successfully.");
        }

        /// <summary>
        /// Creates the user doc if missing (useful right after signup/login).
        /// Adds basic metadata fields.
        /// </summary>
        public async Task EnsureUserDocExistsAsync(string uid, string emailOrNull = null)
        {
            MUPLogger.Info("Ensuring user document exists for uid: " + uid);
            EnsureReady();
            if (string.IsNullOrWhiteSpace(uid))
                throw new ArgumentException("uid is null/empty.", nameof(uid));

            var docRef = _db.Collection("users").Document(uid);
            var snap = await docRef.GetSnapshotAsync();

            if (snap.Exists)
                return;

            var initial = new Dictionary<string, object>
            {
                { "email", emailOrNull ?? "" },
                { "createdAt", FieldValue.ServerTimestamp },
                { "updatedAt", FieldValue.ServerTimestamp }
            };

            await docRef.SetAsync(initial);
        }

        private void EnsureReady()
        {
            if (!IsInitialized || _db == null)
                throw new InvalidOperationException("FirestoreUserDataService not initialized. Call InitializeAsync() first.");
        }
    }
}
