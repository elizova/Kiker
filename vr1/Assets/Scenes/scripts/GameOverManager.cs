using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameOverManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject allTowersCapturedPanel;
    public GameObject allTowersPlayerPanel;
    public GameObject playerDeadPanel;

    [Header("Player References")]
    public Transform playerObject; // Перетащите сюда XR Origin/Player объект
    public PlayerHealth playerHealth; // Перетащите сюда объект со скриптом PlayerHealth
    public Transform gameEndTeleportPoint; // Перетащите сюда GameObject с точкой телепортации

    [Header("Game Settings")]
    public List<Transform> mainTowers = new List<Transform>();
    public string mainSceneName = "MainScene";

    private bool isGameOver = false;
    private bool isInitialized = false;

    void Start()
    {
        // Проверяем назначены ли обязательные объекты
        if (playerObject == null)
        {
            Debug.LogError("PlayerObject не назначен в инспекторе! Перетащите сюда XR Origin/Player");
        }

        if (playerHealth == null)
        {
            Debug.LogError("PlayerHealth не назначен в инспекторе! Перетащите сюда объект со скриптом PlayerHealth");
        }

        if (gameEndTeleportPoint == null)
        {
            Debug.LogError("GameEndTeleportPoint не назначен в инспекторе!");
        }

        // Если все еще не назначено, пытаемся найти автоматически
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth != null)
            {
                Debug.LogWarning("PlayerHealth найден автоматически, но лучше назначить вручную!");
            }
        }

        FindAllTowers();
        HideAllPanels();

        isInitialized = true;
        Debug.Log($"GameManager инициализирован. Найдено башен: {mainTowers.Count}");
    }

    void HideAllPanels()
    {
        if (allTowersCapturedPanel != null) allTowersCapturedPanel.SetActive(false);
        if (allTowersPlayerPanel != null) allTowersPlayerPanel.SetActive(false);
        if (playerDeadPanel != null) playerDeadPanel.SetActive(false);
    }

    void Update()
    {
        if (!isGameOver && isInitialized)
        {
            CheckGameConditions();
        }
    }

    void FindAllTowers()
    {
        mainTowers.Clear();
        GameObject[] towerObjects = GameObject.FindGameObjectsWithTag("MainTower");
        foreach (GameObject tower in towerObjects)
        {
            mainTowers.Add(tower.transform);
        }
        Debug.Log($"Найдено башен с тегом 'MainTower': {mainTowers.Count}");
    }

    void CheckGameConditions()
    {
        if (CheckWinCondition())
        {
            return;
        }

        CheckLoseCondition();
    }

    bool CheckWinCondition()
    {
        if (mainTowers.Count == 0)
        {
            return false;
        }

        bool allCapturedByPlayer = true;

        foreach (Transform tower in mainTowers)
        {
            if (tower == null) continue;

            TowerCapture towerCapture = tower.GetComponent<TowerCapture>();
            if (towerCapture != null && towerCapture.currentState != TowerCapture.TowerState.Player)
            {
                allCapturedByPlayer = false;
                break;
            }
        }

        if (allCapturedByPlayer)
        {
            WinGame();
            return true;
        }

        return false;
    }

    void CheckLoseCondition()
    {
        if (mainTowers.Count > 0)
        {
            bool allCapturedByEnemy = true;

            foreach (Transform tower in mainTowers)
            {
                if (tower == null) continue;

                TowerCapture towerCapture = tower.GetComponent<TowerCapture>();
                if (towerCapture != null && towerCapture.currentState != TowerCapture.TowerState.Enemy)
                {
                    allCapturedByEnemy = false;
                    break;
                }
            }

            if (allCapturedByEnemy)
            {
                LoseGameAllTowersCaptured();
                return;
            }
        }

        // Проверяем здоровье игрока
        if (playerHealth != null)
        {
            if (playerHealth.currentHealth <= 0)
            {
                Debug.Log($"Игрок мертв! Здоровье: {playerHealth.currentHealth}");
                LoseGamePlayerDead();
            }
        }
        else
        {
            Debug.LogWarning("PlayerHealth не найден!");
        }
    }

    void WinGame()
    {
        if (isGameOver) return;
        isGameOver = true;
        Debug.Log("VICTORY - All towers captured by player!");

        ShowEndScreen(allTowersPlayerPanel);
    }

    void LoseGameAllTowersCaptured()
    {
        if (isGameOver) return;
        isGameOver = true;
        Debug.Log("DEFEAT - All towers captured by enemies!");

        ShowEndScreen(allTowersCapturedPanel);
    }

    void LoseGamePlayerDead()
    {
        if (isGameOver) return;
        isGameOver = true;
        Debug.Log("DEFEAT - Player died!");

        ShowEndScreen(playerDeadPanel);
    }

    void ShowEndScreen(GameObject panelToShow)
    {
        Debug.Log($"Показываем экран: {panelToShow.name}");

        if (panelToShow != null)
        {
            panelToShow.SetActive(true);
        }

        EndGame();
    }

    void EndGame()
    {
        Time.timeScale = 0f;

        // Телепортируем игрока
        TeleportPlayerToEndPoint();

        // Отключаем только нужные скрипты
        DisableSpecificScripts();

        StopEnemySpawning();
    }

    void TeleportPlayerToEndPoint()
    {
        if (gameEndTeleportPoint != null && playerObject != null)
        {
            // Телепортируем игрока в указанную точку
            playerObject.position = gameEndTeleportPoint.position;
            playerObject.rotation = gameEndTeleportPoint.rotation;

            Debug.Log($"Игрок телепортирован в точку: {gameEndTeleportPoint.position}");
        }
        else
        {
            if (playerObject == null)
                Debug.LogError("PlayerObject не назначен!");
            if (gameEndTeleportPoint == null)
                Debug.LogError("GameEndTeleportPoint не назначен!");
        }
    }

    void DisableSpecificScripts()
    {
        // 1. Отключаем движение игрока
        var moveProvider = FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.ActionBasedContinuousMoveProvider>();
        if (moveProvider != null)
        {
            moveProvider.enabled = false;
            Debug.Log("Отключен ActionBasedContinuousMoveProvider");
        }

        var turnProvider = FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.ActionBasedContinuousTurnProvider>();
        if (turnProvider != null)
        {
            turnProvider.enabled = false;
            Debug.Log("Отключен ActionBasedContinuousTurnProvider");
        }

        // 2. Отключаем оружие (твой скрипт WeaponVR)
        var weaponScript = FindFirstObjectByType<WeaponVR>();
        if (weaponScript != null && weaponScript.enabled)
        {
            weaponScript.enabled = false;
            Debug.Log($"Отключен скрипт WeaponVR: {weaponScript.name}");
        }

        // 3. Отключаем спавн турелей
        var spawnerScript = FindFirstObjectByType<TurretSpawner>();
        if (spawnerScript != null && spawnerScript.enabled)
        {
            spawnerScript.enabled = false;
            Debug.Log($"Отключен скрипт SimpleGroundTurretSpawner: {spawnerScript.name}");
        }

        // КОНТРОЛЛЕРЫ НЕ ОТКЛЮЧАЕМ! Они остаются активными
    }

    void StopEnemySpawning()
    {
        var spawners = FindObjectsOfType<EnemySpawner>();
        foreach (var spawner in spawners)
        {
            spawner.StopAllCoroutines();
            spawner.enabled = false;
        }
    }

    // Вызывается кнопками рестарта
    public void RestartGame()
    {
        Debug.Log("Рестарт игры...");

        Time.timeScale = 1f;
        StopAllCoroutines();
        StartCoroutine(RestartGameCoroutine());
    }

    private System.Collections.IEnumerator RestartGameCoroutine()
    {
        Debug.Log("Начинаем рестарт игры...");

        // 1. Останавливаем все аудио
        AudioListener.pause = false;
        AudioListener.volume = 1f;

        // 2. Ждем один кадр
        yield return null;

        // 3. Загружаем сцену
        SceneManager.LoadScene(mainSceneName);

        Debug.Log("Сцена загружена");
    }

    public void CheckGameState()
    {
        if (!isGameOver)
        {
            FindAllTowers();
        }
    }

    // Визуализация в редакторе
    void OnDrawGizmosSelected()
    {
        if (gameEndTeleportPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(gameEndTeleportPoint.position, 0.5f);
            Gizmos.DrawLine(gameEndTeleportPoint.position, gameEndTeleportPoint.position + gameEndTeleportPoint.forward * 1f);

            // Подпись
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.green;
            UnityEditor.Handles.Label(gameEndTeleportPoint.position + Vector3.up * 0.7f, "Game End Point", style);
        }
    }

    public bool IsGameOver
    {
        get { return isGameOver; }
    }
}