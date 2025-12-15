using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TowerCapture : MonoBehaviour
{
    [Header("Tower States")]
    public TowerState currentState = TowerState.Neutral;
    public float captureProgress = 0f;
    public float defenseHealth = 5f;

    [Header("Capture Settings")]
    public float captureRadius = 5f;
    public float captureSpeed = 0.5f;
    public float enemyCaptureSpeed = 0.3f;

    [Header("Visual Feedback")]
    public Renderer towerRenderer;
    public Color neutralColor = Color.gray;
    public Color playerColor = Color.blue;
    public Color enemyColor = Color.red;
    public GameObject defenseShield;

    [Header("UI Elements")]
    public Slider captureSlider;
    public GameObject captureUI;

    private GameManager gameManager;
    public bool isPlayerInRange = false;
    private float currentDefenseHealth;
    private bool defenseDestroyed = false; // Новый флаг

    private List<EnemyAI> capturingEnemies = new List<EnemyAI>();
    private bool isBeingCapturedByEnemy = false;

    public enum TowerState
    {
        Neutral,
        Player,
        Enemy
    }

    void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        currentDefenseHealth = defenseHealth;
        defenseDestroyed = false;

        TowerManager towerManager = TowerManager.Instance;
        if (towerManager != null)
        {
            towerManager.RegisterTower(transform);
        }

        SetupVisuals();
        UpdateUI();
    }

    void OnDestroy()
    {
        TowerManager towerManager = TowerManager.Instance;
        if (towerManager != null)
        {
            towerManager.UnregisterTower(transform);
        }
    }

    void SetupVisuals()
    {
        if (towerRenderer != null)
        {
            switch (currentState)
            {
                case TowerState.Neutral:
                    towerRenderer.material.color = neutralColor;
                    break;
                case TowerState.Player:
                    towerRenderer.material.color = playerColor;
                    break;
                case TowerState.Enemy:
                    towerRenderer.material.color = enemyColor;
                    break;
            }
        }

        if (defenseShield != null)
        {
            defenseShield.SetActive(currentState == TowerState.Enemy && !defenseDestroyed);
        }
    }

    void Update()
    {
        // ИЗМЕНЕНО: Теперь игрок может захватывать вражескую башню после уничтожения защиты
        if (isPlayerInRange)
        {
            if (currentState == TowerState.Neutral)
            {
                CaptureByPlayer();
                Debug.Log("Player capturing neutral tower");
            }
            else if (currentState == TowerState.Enemy && defenseDestroyed)
            {
                CaptureByPlayer();
                Debug.Log("Player capturing enemy tower (defense destroyed)");
            }
        }

        if (!isPlayerInRange && captureProgress > 0)
        {
            captureProgress = 0f;
        }

        UpdateUI();
    }

    void CaptureByPlayer()
    {
        if (currentState == TowerState.Neutral)
        {
            captureProgress += captureSpeed * Time.deltaTime;

            if (captureProgress >= 1f)
            {
                CaptureByPlayerComplete();
            }
        }
        else if (currentState == TowerState.Enemy && defenseDestroyed)
        {
            captureProgress += captureSpeed * Time.deltaTime;

            if (captureProgress >= 1f)
            {
                RecaptureFromEnemy();
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (currentState == TowerState.Enemy && !defenseDestroyed)
        {
            currentDefenseHealth -= damage;
            Debug.Log($"Tower defense hit! Health: {currentDefenseHealth}/{defenseHealth}");

            if (currentDefenseHealth <= 0)
            {
                currentDefenseHealth = 0;
                defenseDestroyed = true;

                if (defenseShield != null)
                {
                    defenseShield.SetActive(false);
                }

                Debug.Log("Tower defense destroyed! Player can now capture.");

                // Сразу начинаем захват если игрок в зоне
                if (isPlayerInRange)
                {
                    Debug.Log("Player is in range, starting capture...");
                    captureProgress = 0f; // Сбрасываем прогресс захвата
                }
            }
        }
    }

    void CaptureByPlayerComplete()
    {
        currentState = TowerState.Player;
        captureProgress = 0f;
        defenseDestroyed = false; // Сбрасываем флаг

        Debug.Log($"Tower captured by player!");
        SetupVisuals();

        InterruptEnemyCapture();

        if (gameManager != null)
        {
            gameManager.CheckGameState();
        }
    }

    void InterruptEnemyCapture()
    {
        foreach (EnemyAI enemy in capturingEnemies)
        {
            if (enemy != null)
            {
                enemy.StopCapturing();
            }
        }
        capturingEnemies.Clear();
        isBeingCapturedByEnemy = false;
    }

    public void SetEnemyCapturing(bool isCapturing)
    {
        isBeingCapturedByEnemy = isCapturing;
    }

    public bool IsBeingCapturedByEnemy()
    {
        return isBeingCapturedByEnemy;
    }

    public void OnCapturedByEnemy()
    {
        currentState = TowerState.Enemy;
        currentDefenseHealth = defenseHealth;
        defenseDestroyed = false; // Сбрасываем флаг
        captureProgress = 0f;

        Debug.Log($"Tower captured by enemy!");
        SetupVisuals();

        capturingEnemies.Clear();
        isBeingCapturedByEnemy = false;

        if (gameManager != null)
        {
            gameManager.CheckGameState();
        }
    }

    void RecaptureFromEnemy()
    {
        currentState = TowerState.Player;
        captureProgress = 0f;
        currentDefenseHealth = 0f;
        defenseDestroyed = false; // Сбрасываем флаг

        Debug.Log($"Tower recaptured from enemy!");
        SetupVisuals();

        if (gameManager != null)
        {
            gameManager.CheckGameState();
        }
    }

    void UpdateUI()
    {
        if (captureUI != null)
        {
            // ИЗМЕНЕНО: Показываем UI для вражеских башен только когда защита уничтожена
            bool showUI = isPlayerInRange &&
                         (currentState == TowerState.Neutral ||
                          (currentState == TowerState.Enemy && defenseDestroyed));

            captureUI.SetActive(showUI);

            if (showUI && captureSlider != null)
            {
                captureSlider.value = captureProgress;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            InterruptEnemyCapture();

            // Если защита уже уничтожена, начинаем захват сразу
            if (currentState == TowerState.Enemy && defenseDestroyed)
            {
                captureProgress = 0f; // Начинаем с нуля
                Debug.Log("Player entered zone, starting capture of enemy tower");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            captureProgress = 0f;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, captureRadius);
    }
}