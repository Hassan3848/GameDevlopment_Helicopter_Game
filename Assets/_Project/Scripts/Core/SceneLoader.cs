using UnityEngine;
using UnityEngine.SceneManagement;

namespace HelicopterCombat.Core
{
    [DisallowMultipleComponent]
    public sealed class SceneLoader : MonoBehaviour
    {
        public void ReloadCurrentScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void QuitApplication()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            Debug.Log("Quit requested. Application.Quit() will run in a player build.", this);
#else
            Application.Quit();
#endif
        }
    }
}
