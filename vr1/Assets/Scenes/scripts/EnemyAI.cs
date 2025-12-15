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

    private Coroutine targetUpdateCoroutine;
    private bool isBeingTargetedByTurret = false;

    [Header("Layers")]
    public LayerMask turretLayer;
    public LayerMask towerLayer;
    public LayerMask playerLayer;

    private List<TowerController> targetingTurrets = new List<TowerController>();

    private List<Transform> ignoredTowers = new List<Transform>();
    private float ignoreTowerTime = 10f;

    private enum EnemyState
    {
        MovingToTower,
        MovingToTarget,
        Attacking,
        CapturingTower,
        Idle
    }

    void Start()
    {
        InitializeTargets();
        FindInitialTarget();

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

        if (targetUpdateCoroutine != null)
            StopCoroutine(targetUpdateCoroutine);
        targetUpdateCoroutine = StartCoroutine(TargetUpdateRoutine());

        StartCoroutine(CleanIgnoredTowersRoutine());
    }

    void InitializeTargets()
    {
        TowerManager towerManager = TowerManager.Instance;
        if (towerManager != null && towerManager.activeTowers.Count > 0)
        {
            mainTowers = new List<Transform>(towerManager.activeTowers);
        }
        else
        {
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

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void FindInitialTarget()
    {
        targetTower = null;

        if (mainTowers.Count == 0)
        {
            if (player != null)
                targetTower = player;
            return;
        }

        float closestDistance = Mathf.Infinity;
        Transform closestTower = null;
        Vector3 myPosition = transform.position;

        foreach (Transform tower in mainTowers)
        {
            if (tower == null || !tower.gameObject.activeInHierarchy) continue;

            TowerCapture towerCapture = tower.GetComponent<TowerCapture>();
            if (towerCapture != null)
            {
                if (towerCapture.currentState == TowerCapture.TowerState.Enemy)
                    continue;

                if (towerCapture.isPlayerInRange)
                    continue;
            }

            float distance = Vector3.Distance(myPosition, tower.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTower = tower;
            }
        }

        targetTower = closestTower;
    }

    void Update()
    {
        AvoidOtherEnemies();
        UpdateState();
        ExecuteState();
        CheckTurretTargeting();
        CleanTurretList();

        Debug.Log($"State: {currentState}, Target: {(currentTarget != null ? currentTarget.name : "None")}");
    }

    void AvoidOtherEnemies()
    {
        Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, 1.5f);
        foreach (Collider enemy in nearbyEnemies)
        {
            if (enemy.gameObject != this.gameObject && enemy.CompareTag("Enemy"))
            {
                Vector3 avoidDirection = (transform.position - enemy.transform.position).normalized;
                transform.position += avoidDirection * moveSpeed * 0.5f * Time.deltaTime;
            }
        }
    }

    void CheckTurretTargeting()
    {
        isBeingTargetedByTurret = targetingTurrets.Count > 0;

        if (isBeingTargetedByTurret && currentTarget != null)
        {
            TowerController currentTurret = currentTarget.GetComponent<TowerController>();
            bool isCurrentTargetTurret = currentTurret != null;

            if (!isCurrentTargetTurret)
            {
                Transform nearestTurret = GetNearestTurret();
                if (nearestTurret != null)
                {
                    currentTarget = nearestTurret;
                    currentState = EnemyState.MovingToTarget;
                }
            }
        }
    }

    Transform GetNearestTurret()
    {
        Transform nearestTurret = null;
        float closestDistance = Mathf.Infinity;
        Vector3 myPosition = transform.position;

        foreach (Transform turret in turretsInRange)
        {
            if (turret == null || !turret.gameObject.activeInHierarchy) continue;

            float distance = Vector3.Distance(myPosition, turret.position);
            if (distance < closestDistance && distance <= turretDetectionRange)
            {
                closestDistance = distance;
                nearestTurret = turret;
            }
        }

        if (nearestTurret == null)
        {
            Collider[] nearbyTurrets = Physics.OverlapSphere(myPosition, turretDetectionRange, turretLayer);
            foreach (Collider turretCollider in nearbyTurrets)
            {
                Transform turret = turretCollider.transform;
                float distance = Vector3.Distance(myPosition, turret.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    nearestTurret = turret;

                    if (!turretsInRange.Contains(turret))
                    {
                        turretsInRange.Add(turret);
                    }
                }
            }
        }

        return nearestTurret;
    }

    IEnumerator TargetUpdateRoutine()
    {
        while (true)
        {
            UpdateTarget();
            yield return new WaitForSeconds(0.5f);
        }
    }

    void UpdateTarget()
    {
        if (currentTarget != null && (!currentTarget.gameObject.activeInHierarchy || IsTowerIgnored(currentTarget)))
        {
            currentTarget = null;
        }

        if (isBeingTargetedByTurret)
        {
            Transform nearestTurret = GetNearestTurret();
            if (nearestTurret != null)
            {
                currentTarget = nearestTurret;
                return;
            }
        }

        if (player != null && player.gameObject.activeInHierarchy)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= playerDetectionRange)
            {
                currentTarget = player;
                return;
            }
        }

        Transform availableTower = FindAvailableTower();
        if (availableTower != null)
        {
            currentTarget = availableTower;
            targetTower = availableTower;
            return;
        }

        // if (turretsInRange.Count > 0)
        // {
        //     Transform nearestTurret = GetNearestTurret();
        //     if (nearestTurret != null)
        //     {
        //         currentTarget = nearestTurret;
        //         return;
        //     }
        // }

        if (currentTarget == null)
        {
            FindAnyTower();
            if (targetTower != null)
            {
                currentTarget = targetTower;
                currentState = EnemyState.MovingToTower;
            }
            else if (player != null)
            {
                currentTarget = player;
                currentState = EnemyState.MovingToTarget;
            }
            else
            {
                currentState = EnemyState.Idle;
            }
        }
    }

    Transform FindAvailableTower()
    {
        if (mainTowers.Count == 0) return null;

        Transform bestTower = null;
        float closestDistance = Mathf.Infinity;
        Vector3 myPosition = transform.position;
        float detectionSqr = towerDetectionRange * towerDetectionRange;

        foreach (Transform tower in mainTowers)
        {
            if (tower == null || !tower.gameObject.activeInHierarchy) continue;

            if (IsTowerIgnored(tower)) continue;

            TowerCapture towerCapture = tower.GetComponent<TowerCapture>();
            if (towerCapture != null)
            {
                if (towerCapture.currentState == TowerCapture.TowerState.Enemy)
                    continue;

                if (towerCapture.isPlayerInRange)
                {
                    AddTowerToIgnoreList(tower);
                    continue;
                }
            }

            float sqrDistance = (tower.position - myPosition).sqrMagnitude;
            if (sqrDistance <= detectionSqr && sqrDistance < closestDistance)
            {
                closestDistance = sqrDistance;
                bestTower = tower;
            }
        }

        return bestTower;
    }

    void FindAnyTower()
    {
        targetTower = null;

        if (mainTowers.Count == 0)
        {
            if (player != null)
                targetTower = player;
            return;
        }

        float closestDistance = Mathf.Infinity;
        Transform closestTower = null;
        Vector3 myPosition = transform.position;

        foreach (Transform tower in mainTowers)
        {
            if (tower == null || !tower.gameObject.activeInHierarchy) continue;

            TowerCapture towerCapture = tower.GetComponent<TowerCapture>();
            if (towerCapture != null && towerCapture.currentState == TowerCapture.TowerState.Enemy)
                continue;

            float sqrDistance = (tower.position - myPosition).sqrMagnitude;
            float detectionSqr = towerDetectionRange * towerDetectionRange;

            if (sqrDistance <= detectionSqr && sqrDistance < closestDistance)
            {
                closestDistance = sqrDistance;
                closestTower = tower;
            }
        }

        targetTower = closestTower;
    }

    void AddTowerToIgnoreList(Transform tower)
    {
        if (!ignoredTowers.Contains(tower))
        {
            ignoredTowers.Add(tower);
            Debug.Log($"Added tower {tower.name} to ignore list (player nearby)");

            StartCoroutine(RemoveTowerFromIgnoreListAfterTime(tower, ignoreTowerTime));
        }
    }

    IEnumerator RemoveTowerFromIgnoreListAfterTime(Transform tower, float time)
    {
        yield return new WaitForSeconds(time);

        if (ignoredTowers.Contains(tower))
        {
            ignoredTowers.Remove(tower);
            Debug.Log($"Removed tower {tower.name} from ignore list");
        }
    }

    IEnumerator CleanIgnoredTowersRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);

            for (int i = ignoredTowers.Count - 1; i >= 0; i--)
            {
                if (ignoredTowers[i] == null)
                {
                    ignoredTowers.RemoveAt(i);
                }
            }
        }
    }

    bool IsTowerIgnored(Transform tower)
    {
        return ignoredTowers.Contains(tower);
    }

    void UpdateState()
    {
        if (currentTarget == null)
        {
            currentState = EnemyState.Idle;
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

        bool isTower = mainTowers.Contains(currentTarget);
        bool isTurret = turretLayer == (turretLayer | (1 << currentTarget.gameObject.layer));
        bool isPlayer = currentTarget == player;

        if (isTurret)
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
        else if (isTower)
        {
            TowerCapture towerCapture = currentTarget.GetComponent<TowerCapture>();
            if (towerCapture != null)
            {
                if (towerCapture.isPlayerInRange)
                {
                    AddTowerToIgnoreList(currentTarget);
                    currentTarget = null;
                    currentState = EnemyState.Idle;
                    return;
                }

                if (distanceToTarget <= attackRange)
                {
                    if (towerCapture.currentState == TowerCapture.TowerState.Enemy)
                    {
                        currentState = EnemyState.Attacking;
                    }
                    else
                    {
                        currentState = EnemyState.CapturingTower;
                    }
                }
                else
                {
                    currentState = EnemyState.MovingToTarget;
                }
            }
        }
        else if (isPlayer)
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

            case EnemyState.Idle:
                transform.Rotate(0, rotationSpeed * 0.5f * Time.deltaTime, 0);
                break;
        }
    }

    void MoveToTarget()
    {
        if (currentTarget == null)
        {
            UpdateTarget();
            if (currentTarget == null)
            {
                currentState = EnemyState.Idle;
            }
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

    void AttackTarget()
    {
        if (Time.time - lastAttackTime >= attackCooldown && currentTarget != null)
        {
            TurretHealth turretHealth = currentTarget.GetComponent<TurretHealth>();
            PlayerHealth playerHealth = currentTarget.GetComponent<PlayerHealth>();
            TowerCapture towerCapture = currentTarget.GetComponent<TowerCapture>();

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
            else if (towerCapture != null)
            {
                towerCapture.TakeDamage(attackDamage);
                Debug.Log($"Enemy attacked tower defense for {attackDamage} damage!");
            }

            lastAttackTime = Time.time;
        }
    }

    void CaptureTower()
    {
        if (currentTarget == null) return;

        TowerCapture towerCapture = currentTarget.GetComponent<TowerCapture>();
        if (towerCapture == null) return;

        if (towerCapture.isPlayerInRange)
        {
            AddTowerToIgnoreList(currentTarget);
            currentTarget = null;
            currentState = EnemyState.Idle;
            return;
        }

        if (towerCapture.IsBeingCapturedByEnemy() && !isCapturingTower)
        {
            currentTarget = null;
            currentState = EnemyState.MovingToTarget;
            return;
        }

        if (!isCapturingTower)
        {
            isCapturingTower = true;
            captureProgress = 0f;
            towerCapture.SetEnemyCapturing(true);
            StartCoroutine(CaptureTowerRoutine(currentTarget));
        }
    }

    IEnumerator CaptureTowerRoutine(Transform tower)
    {
        float captureTime = 5f;
        TowerCapture towerCapture = tower.GetComponent<TowerCapture>();

        if (towerCapture == null)
        {
            isCapturingTower = false;
            yield break;
        }

        while (captureProgress < 1f && tower != null &&
               Vector3.Distance(transform.position, tower.position) <= attackRange)
        {
            if (towerCapture.isPlayerInRange)
            {
                isCapturingTower = false;
                captureProgress = 0f;
                towerCapture.SetEnemyCapturing(false);
                AddTowerToIgnoreList(tower);
                currentTarget = null;
                currentState = EnemyState.Idle;
                yield break;
            }

            captureProgress += Time.deltaTime / captureTime;
            yield return null;
        }

        if (captureProgress >= 1f && tower != null)
        {
            CompleteTowerCapture(tower);
        }
        else
        {
            isCapturingTower = false;
            captureProgress = 0f;
            if (towerCapture != null)
            {
                towerCapture.SetEnemyCapturing(false);
            }
        }
    }

    void CompleteTowerCapture(Transform tower)
    {
        Debug.Log("Tower captured by enemy!");

        TowerCapture towerCapture = tower.GetComponent<TowerCapture>();
        if (towerCapture != null)
        {
            towerCapture.OnCapturedByEnemy();
        }

        isCapturingTower = false;
        captureProgress = 0f;
        currentTarget = null;
        currentState = EnemyState.MovingToTarget;

        CleanTowerList();
        UpdateTarget();
    }

    public void AddTargetingTurret(TowerController turret)
    {
        if (!targetingTurrets.Contains(turret))
        {
            targetingTurrets.Add(turret);
        }
    }

    public void RemoveTargetingTurret(TowerController turret)
    {
        if (targetingTurrets.Contains(turret))
        {
            targetingTurrets.Remove(turret);
        }
    }

    void CleanTurretList()
    {
        for (int i = turretsInRange.Count - 1; i >= 0; i--)
        {
            if (turretsInRange[i] == null || !turretsInRange[i].gameObject.activeInHierarchy)
            {
                turretsInRange.RemoveAt(i);
            }
        }

        for (int i = targetingTurrets.Count - 1; i >= 0; i--)
        {
            if (targetingTurrets[i] == null || !targetingTurrets[i].gameObject.activeInHierarchy)
            {
                targetingTurrets.RemoveAt(i);
            }
        }
    }

    void CleanTowerList()
    {
        for (int i = mainTowers.Count - 1; i >= 0; i--)
        {
            if (mainTowers[i] == null)
            {
                mainTowers.RemoveAt(i);
            }
        }
    }

    public void StopCapturing()
    {
        if (isCapturingTower)
        {
            isCapturingTower = false;
            captureProgress = 0f;
            StopAllCoroutines();
            currentState = EnemyState.MovingToTarget;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        int layer = other.gameObject.layer;

        if (turretLayer == (turretLayer | (1 << layer)))
        {
            if (!turretsInRange.Contains(other.transform))
            {
                turretsInRange.Add(other.transform);
            }
        }

        if (towerLayer == (towerLayer | (1 << layer)) && !mainTowers.Contains(other.transform))
        {
            mainTowers.Add(other.transform);
        }

        TowerController turret = other.GetComponent<TowerController>();
        if (turret != null)
        {
            AddTargetingTurret(turret);
        }
    }

    void OnTriggerExit(Collider other)
    {
        int layer = other.gameObject.layer;

        if (turretLayer == (turretLayer | (1 << layer)))
        {
            turretsInRange.Remove(other.transform);
        }

        TowerController turret = other.GetComponent<TowerController>();
        if (turret != null)
        {
            RemoveTargetingTurret(turret);
        }
    }

    void OnDestroy()
    {
        if (targetUpdateCoroutine != null)
            StopCoroutine(targetUpdateCoroutine);
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