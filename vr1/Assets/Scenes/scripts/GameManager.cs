using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject allTowersCapturedPanel;
    public GameObject allTowersPlayerPanel;
    public GameObject playerDeadPanel;

    [Header("Restart Buttons")]
    public Button restartButtonTowersCaptured;
    public Button restartButtonPlayerWin;
    public Button restartButtonPlayerDead;

    [Header("Game Settings")]
    public List<Transform> mainTowers = new List<Transform>();
    public string mainSceneName = "MainScene";

    private bool isGameOver = false;
    private PlayerHealth playerHealth;
    private bool isInitialized = false;

    void Start()
    {
        if (playerHealth == null)
        {
            Debug.LogError("PlayerHealth не найден на сцене!");
        }
        else
        {
            Debug.Log($"PlayerHealth найден. Текущее здоровье: {playerHealth.currentHealth}");
        }

        FindAllTowers();

        SetupRestartButtons();

        if (allTowersCapturedPanel != null) allTowersCapturedPanel.SetActive(false);
        if (allTowersPlayerPanel != null) allTowersPlayerPanel.SetActive(false);
        if (playerDeadPanel != null) playerDeadPanel.SetActive(false);

        isInitialized = true;

        Debug.Log($"GameManager инициализирован. Найдено башен: {mainTowers.Count}, Игрок жив: {playerHealth != null && playerHealth.currentHealth > 0}");
    }

    void Update()
    {
        if (!isGameOver && isInitialized)
        {
            CheckGameConditions();
        }
    }

    void SetupRestartButtons()
    {
        if (restartButtonTowersCaptured != null)
            restartButtonTowersCaptured.onClick.AddListener(RestartGame);

        if (restartButtonPlayerWin != null)
            restartButtonPlayerWin.onClick.AddListener(RestartGame);

        if (restartButtonPlayerDead != null)
            restartButtonPlayerDead.onClick.AddListener(RestartGame);
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

        if (playerHealth != null && playerHealth.currentHealth <= 0)
        {
            Debug.Log($"Игрок мертв! Здоровье: {playerHealth.currentHealth}");
            LoseGamePlayerDead();
        }
        else if (playerHealth == null)
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
        DisablePlayerControls();
        StopEnemySpawning();
    }

    void DisablePlayerControls()
    {
        var moveProvider = FindAnyObjectByType<UnityEngine.XR.Interaction.Toolkit.ActionBasedContinuousMoveProvider>();
        if (moveProvider != null) moveProvider.enabled = false;

        var turnProvider = FindAnyObjectByType<UnityEngine.XR.Interaction.Toolkit.ActionBasedContinuousTurnProvider>();
        if (turnProvider != null) turnProvider.enabled = false;

        var weapons = FindObjectsOfType<WeaponController>();
        foreach (var weapon in weapons)
        {
            weapon.enabled = false;
        }
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

    public void RestartGame()
    {
        Debug.Log("Рестарт игры...");
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainSceneName);
    }

    public void CheckGameState()
    {
        if (!isGameOver)
        {
            FindAllTowers();
        }
    }
}