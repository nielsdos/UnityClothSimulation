using UnityEngine;
using UnityEngine.SceneManagement;

namespace Utility
{
    /// <summary>
    /// Utility component which allows the current scene to be restarted by pressing the R key on the keyboard.
    /// This can be useful for demo purposes.
    /// </summary>
    public sealed class RestartScene : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}