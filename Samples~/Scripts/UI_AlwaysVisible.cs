using UnityEngine;

namespace MyUnityPackage.Requests.Samples
{
    public class UI_AlwaysVisible : MonoBehaviour
    {
        [SerializeField] private GameObject loadingScreen;

        public void ShowLoading()
        {
            loadingScreen?.SetActive(true);
        }

        public void HideLoading()
        {
            loadingScreen?.SetActive(false);
        }
    }
}