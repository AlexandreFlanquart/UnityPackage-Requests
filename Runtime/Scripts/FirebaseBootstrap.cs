using System;
using System.Threading.Tasks;
using UnityEngine;


using Firebase;
using MyUnityPackage.Toolkit;


namespace MyUnityPackage.Requests
{
    public static class FirebaseBootstrap
    {
        private static bool _initialized;

        public static bool IsInitialized => _initialized;

        public static async Task InitializeAsync()
        {
            if (_initialized) return;


            var status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (status != DependencyStatus.Available)
                throw new Exception($"Firebase dependencies not available: {status}");

            _initialized = true;
            MUPLogger.Info("[Firebase] Initialized.");

        }
    }
}
