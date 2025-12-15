using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using System;
using System.Collections.Generic;
using TMPro;

namespace Unity.VRTemplate
{
    public class SimpleRestartButton : MonoBehaviour
    {
        [Header("Настройки")]
        public string sceneToLoad = "MainScene";
        public float cooldownTime = 1f;

        [Serializable]
        class Step
        {
            [SerializeField]
            public GameObject stepObject;

            [SerializeField]
            public string buttonText;
        }

        private float lastPressTime;
        private bool canPress = true;

        public void OnButtonPressed()
        {
            if (!canPress) return;

            Debug.Log($"Кнопка '{gameObject.name}' нажата, запускаем рестарт");
            RestartGame();

            canPress = false;
            lastPressTime = Time.unscaledTime;
            Invoke(nameof(ResetCooldown), cooldownTime);
        }

        void ResetCooldown()
        {
            canPress = true;
        }

        public void RestartGame()
        {
            if (Time.timeScale < 1f)
            {
                Time.timeScale = 1f;
            }

            SceneManager.LoadScene(sceneToLoad);
        }
    }
}