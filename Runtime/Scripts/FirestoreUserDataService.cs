using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Extensions;
using Firebase.Firestore;
using MyUnityPackage.Toolkit;

namespace MyUnityPackage.Requests
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
        /// Loads a document from Firestore and converts it to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the document into. Must have [FirestoreData] attribute.</typeparam>
        /// <param name="collection">The collection name.</param>
        /// <param name="documentId">The document ID.</param>
        /// <returns>The deserialized data, or default(T) if document doesn't exist.</returns>
        public async Task<T> LoadDataAsync<T>(string collection, string documentId) where T : class, IFirestoreData
        {
            EnsureReady();
            if (string.IsNullOrWhiteSpace(collection))
                throw new ArgumentException("collection is null/empty.", nameof(collection));
            if (string.IsNullOrWhiteSpace(documentId))
                throw new ArgumentException("documentId is null/empty.", nameof(documentId));
            
            DocumentReference docRef = _db.Collection(collection).Document(documentId);
            DocumentSnapshot snap = await docRef.GetSnapshotAsync();
            
            if (!snap.Exists)
            {
                MUPLogger.Warning($"Document {collection}/{documentId} does not exist.");
                return default;
            }
            
            return snap.ConvertTo<T>(); 
        }

        /// <summary>
        /// Saves user data to: users/{uid}
        /// Uses MergeAll to avoid overwriting existing fields.
        /// </summary>
        public async Task SaveDataAsync<T>(string collection, string documentId, T data) where T : class, IFirestoreData
        {
            MUPLogger.Info("Saving data for collection: " + collection + ", documentId: " + documentId + ", data: " + data);
            EnsureReady();
            
            if (string.IsNullOrWhiteSpace(collection))
                throw new ArgumentException("collection is null/empty.", nameof(collection));
            
            if (string.IsNullOrWhiteSpace(documentId))
                throw new ArgumentException("documentId is null/empty.", nameof(documentId));

            if (data == null)
                throw new ArgumentNullException(nameof(data));

            DocumentReference docRef = _db.Collection(collection).Document(documentId);
            await docRef.SetAsync(data, SetOptions.MergeAll);
        }


        /// <summary>
        /// Fetches all documents from a collection and converts them to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize each document into. Must have [FirestoreData] attribute.</typeparam>
        /// <param name="collection">The collection name.</param>
        /// <returns>A list of all documents in the collection.</returns>
        public async Task<List<T>> GetCollectionAsync<T>(string collection) where T : class, IFirestoreData
        {
            EnsureReady();
            if (string.IsNullOrWhiteSpace(collection))
                throw new ArgumentException("collection is null/empty.", nameof(collection));

            QuerySnapshot snap = await _db.Collection(collection).GetSnapshotAsync();
            var results = new List<T>(snap.Count);

            foreach (DocumentSnapshot doc in snap.Documents)
            {
                if (doc.Exists)
                    results.Add(doc.ConvertTo<T>());
            }

            return results;
        }

        private void EnsureReady()
        {
            if (!IsInitialized || _db == null)
                throw new InvalidOperationException("FirestoreUserDataService not initialized. Call InitializeAsync() first.");
        }

        public string GetIDByCollection(string collection)
        {
            if (string.IsNullOrWhiteSpace(collection))
                throw new ArgumentException("collection is null/empty.", nameof(collection));

            return _db.Collection(collection).Document().Id;
        }
    }
}
