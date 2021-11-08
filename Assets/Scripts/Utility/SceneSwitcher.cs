using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Utility
{
    /// <summary>
    /// Dynamically switches to the scene in the environment variable "TRAIN_SCENE".
    /// </summary>
    public sealed class SceneSwitcher : MonoBehaviour
    {
        private void Awake()
        {
            var trainScene = Environment.GetEnvironmentVariable("TRAIN_SCENE");
            Debug.Log($"Starting scene {trainScene}");
            SceneManager.LoadScene(trainScene, LoadSceneMode.Single);
        }
    }
}