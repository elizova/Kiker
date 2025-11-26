using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    [Header("Target Priorities")]
    public Transform player;
    public List<Transform> mainTowers = new List<Transform>();

    [Header("Detection Ranges")]
    public float turretDetectionRange = 15f;
    public float towerDetectionRange = 20f;
    public float playerDetectionRange = 25f;

    [Header("Combat")]
    public float attackRange = 3f;
    public float attackDamage = 10f;
    public float attackCooldown = 2f;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f;

    private Transform currentTarget;
    private EnemyState currentState = EnemyState.MovingToTower;
    private float lastAttackTime;
    private List<Transform> turretsInRange = new List<Transform>();
    private bool isCapturingTower = false;
    private float captureProgress = 0f;
    private Transform targetTower;
    private Vector3 moveDirection = Vector3.zero;

    private const int TURRET_PRIORITY = 3;
    private const int TOWER_PRIORITY = 2;
    private const int PLAYER_PRIORITY = 1;

    private Vector3 lastTargetPosition;
    private bool isCheckingTargetMovement = false;

    private enum EnemyState
    {
        MovingToTower,
        MovingToTarget,
        Attacking,
        CapturingTower
    }

    void Start()
    {
        InitializeTargets();
        FindClosestTower();

        if (currentTarget == null && targetTower != null)
        {
            currentTarget = targetTower;
            currentState = EnemyState.MovingToTower;
        }

        if (currentTarget != null)
        {
            Vector3 direction = (currentTarget.position - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }

    void InitializeTargets()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (mainTowers.Count == 0)
        {
            GameObject[] towerObjects = GameObject.FindGameObjectsWithTag("MainTower");
            foreach (GameObject tower in towerObjects)
            {
                if (tower != null && tower.activeInHierarchy)
                    mainTowers.Add(tower.transform);
            }
        }
    }

    void FindClosestTower()
    {
        targetTower = null;

        if (mainTowers.Count == 0)
        {
            targetTower = player;
            return;
        }

        float closestDistance = Mathf.Infinity;

        foreach (Transform tower in mainTowers)
        {
            if (tower == null || !tower.gameObject.activeInHierarchy) continue;

            TowerCapture towerCapture = tower.GetComponent<TowerCapture>();
            if (towerCapture != null && towerCapture.IsCapturedByEnemy) continue;

            float distance = Vector3.Distance(transform.position, tower.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                targetTower = tower;
            }
        }
    }

    void Update()
    {
        AvoidOtherEnemies();
        UpdateTarget();
        UpdateState();
        ExecuteState();

        Debug.Log($"State: {currentState}, Target: {(currentTarget != null ? currentTarget.name : "None")}");


    }

    void UpdateTarget()
    {
        Transform bestTarget = null;
        int highestPriority = 0;

        foreach (Transform turret in turretsInRange.ToArray())
        {
            if (turret == null || !turret.gameObject.activeInHierarchy)
            {
                turretsInRange.Remove(turret);
                continue;
            }

            float distance = Vector3.Distance(transform.position, turret.position);
            if (distance <= turretDetectionRange && TURRET_PRIORITY > highestPriority)
            {
                bestTarget = turret;
                highestPriority = TURRET_PRIORITY;
            }
        }

        if (targetTower != null && targetTower.gameObject.activeInHierarchy)
        {
            float distance = Vector3.Distance(transform.position, targetTower.position);
            if (distance <= towerDetectionRange && TOWER_PRIORITY > highestPriority)
            {
                bestTarget = targetTower;
                highestPriority = TOWER_PRIORITY;
            }
        }

        if (player != null && player.gameObject.activeInHierarchy)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= playerDetectionRange && PLAYER_PRIORITY > highestPriority)
            {
                bestTarget = player;
                highestPriority = PLAYER_PRIORITY;
            }
        }

        currentTarget = bestTarget;

        if (currentTarget == null && targetTower != null && targetTower.gameObject.activeInHierarchy)
        {
            currentTarget = targetTower;
            currentState = EnemyState.MovingToTower;
        }

        if (currentTarget == null)
        {
            FindClosestTower();
            if (targetTower != null)
            {
                currentTarget = targetTower;
                currentState = EnemyState.MovingToTower;
            }
        }
    }

    void UpdateState()
    {
        if (currentTarget == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

        bool isTower = mainTowers.Contains(currentTarget);
        bool isTurret = currentTarget.CompareTag("Turret");
        bool isPlayer = currentTarget == player;

        if (isTower)
        {
            if (distanceToTarget <= attackRange)
            {
                currentState = EnemyState.CapturingTower;
            }
            else
            {
                currentState = EnemyState.MovingToTarget;
            }
        }
        else if (isTurret || isPlayer)
        {
            if (distanceToTarget <= attackRange)
            {
                currentState = EnemyState.Attacking;
            }
            else
            {
                currentState = EnemyState.MovingToTarget;
            }
        }
    }

    void ExecuteState()
    {
        switch (currentState)
        {
            case EnemyState.MovingToTower:
            case EnemyState.MovingToTarget:
                MoveToTarget();
                break;

            case EnemyState.Attacking:
                AttackTarget();
                break;

            case EnemyState.CapturingTower:
                CaptureTower();
                break;
        }
    }

    void MoveToTarget()
    {
        if (currentTarget == null)
        {
            FindClosestTower();
            return;
        }

        Vector3 direction = (currentTarget.position - transform.position).normalized;

        Vector3 horizontalDirection = new Vector3(direction.x, 0, direction.z).normalized;
        if (horizontalDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(horizontalDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime, Space.Self);

        Debug.DrawRay(transform.position, transform.forward * 3f, Color.red);
        Debug.DrawLine(transform.position, currentTarget.position, Color.green);
    }

    void AvoidOtherEnemies()
    {
        Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, 1f);
        foreach (Collider enemy in nearbyEnemies)
        {
            if (enemy.gameObject != this.gameObject && enemy.CompareTag("Enemy"))
            {
                Vector3 avoidDirection = (transform.position - enemy.transform.position).normalized;
                transform.position += avoidDirection * Time.deltaTime;
            }
        }
    }

    void AttackTarget()
    {
        if (Time.time - lastAttackTime >= attackCooldown && currentTarget != null)
        {
            TurretHealth turretHealth = currentTarget.GetComponent<TurretHealth>();
            PlayerHealth playerHealth = currentTarget.GetComponent<PlayerHealth>();

            if (turretHealth != null)
            {
                turretHealth.TakeDamage(attackDamage);
                Debug.Log($"Enemy attacked turret for {attackDamage} damage!");
            }
            else if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
                Debug.Log($"Enemy attacked player for {attackDamage} damage!");
            }

            lastAttackTime = Time.time;
        }
    }

    void CaptureTower()
    {
        if (!isCapturingTower && currentTarget != null)
        {
            isCapturingTower = true;
            captureProgress = 0f;
            StartCoroutine(CaptureTowerRoutine());
        }
    }

    IEnumerator CaptureTowerRoutine()
    {
        float captureTime = 5f;

        while (captureProgress < 1f && currentTarget != null &&
               Vector3.Distance(transform.position, currentTarget.position) <= attackRange)
        {
            captureProgress += Time.deltaTime / captureTime;
            yield return null;
        }

        if (captureProgress >= 1f && currentTarget != null)
        {
            CompleteTowerCapture();
        }
        else
        {
            isCapturingTower = false;
            captureProgress = 0f;
        }
    }

    void CompleteTowerCapture()
    {
        Debug.Log("Tower captured by enemy!");

        TowerCapture towerCapture = currentTarget.GetComponent<TowerCapture>();
        if (towerCapture != null)
        {
            towerCapture.OnCapturedByEnemy();
            towerCapture.IsCapturedByEnemy = true;
            mainTowers.Remove(currentTarget);
        }

        isCapturingTower = false;
        captureProgress = 0f;
        currentTarget = null;
        FindClosestTower();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Turret"))
        {
            if (!turretsInRange.Contains(other.transform))
            {
                turretsInRange.Add(other.transform);
            }
        }

        if (other.CompareTag("MainTower") && !mainTowers.Contains(other.transform))
        {
            mainTowers.Add(other.transform);
            FindClosestTower();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Turret"))
        {
            turretsInRange.Remove(other.transform);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, turretDetectionRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, towerDetectionRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, playerDetectionRange);

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);

        if (currentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
}