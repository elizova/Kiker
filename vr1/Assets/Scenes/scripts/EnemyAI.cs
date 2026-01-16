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
    public float explorationRange = 30f;

    [Header("Combat")]
    public float attackRange = 3f;
    public float attackDamage = 10f;
    public float attackCooldown = 2f;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f;
    public float obstacleCheckDistance = 2f;
    public float wallAvoidanceForce = 2f;

    private Transform currentTarget;
    private EnemyState currentState = EnemyState.Exploring;
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
    public LayerMask wallLayer;

    private List<TowerController> targetingTurrets = new List<TowerController>();

    private List<Transform> ignoredTowers = new List<Transform>();
    private float ignoreTowerTime = 10f;

    private Vector3 explorationTarget;
    private float explorationTargetTime = 0f;
    private float explorationTargetDuration = 10f;
    private List<Vector3> visitedPositions = new List<Vector3>();
    private float visitedPositionRadius = 3f;
    private int maxVisitedPositions = 20;

    private Vector3 currentDirection;
    private float stuckCheckTimer = 0f;
    private Vector3 lastPosition;
    private float stuckThreshold = 0.5f;
    private float maxStuckTime = 3f;
    private bool isFollowingWall = false;
    private float wallFollowTimer = 0f;
    private Vector3 wallFollowDirection;

    private enum EnemyState
    {
        Exploring,
        MovingToTarget,
        Attacking,
        CapturingTower,
        Idle
    }

    void Start()
    {
        InitializeTargets();

        currentState = EnemyState.Exploring;
        SetRandomExplorationTarget();

        currentDirection = transform.forward;
        lastPosition = transform.position;

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

    void Update()
    {
        CheckForTargetsInRange();
        UpdateState();
        ExecuteState();
        CheckTurretTargeting();
        CleanLists();

        CheckStuck();

        Debug.Log($"State: {currentState}, Target: {(currentTarget != null ? currentTarget.name : "None")}");
    }

    void CheckForTargetsInRange()
    {
        if (currentTarget != null && currentState != EnemyState.Exploring) return;

        if (player != null && player.gameObject.activeInHierarchy)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= playerDetectionRange)
            {
                SetTarget(player);
                return;
            }
        }

        if (isBeingTargetedByTurret)
        {
            Transform nearestTurret = GetNearestTurret();
            if (nearestTurret != null)
            {
                SetTarget(nearestTurret);
                return;
            }
        }

        Transform nearestTower = GetNearestTowerInRange();
        if (nearestTower != null)
        {
            SetTarget(nearestTower);
            return;
        }
    }

    Transform GetNearestTowerInRange()
    {
        Transform nearestTower = null;
        float closestDistance = Mathf.Infinity;
        Vector3 myPosition = transform.position;

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

            float distance = Vector3.Distance(myPosition, tower.position);
            if (distance <= towerDetectionRange && distance < closestDistance)
            {
                closestDistance = distance;
                nearestTower = tower;
            }
        }

        return nearestTower;
    }

    void SetTarget(Transform newTarget)
    {
        if (newTarget == null) return;

        currentTarget = newTarget;
        currentState = EnemyState.MovingToTarget;

        isFollowingWall = false;

        Debug.Log($"Found target in range: {currentTarget.name}");
    }

    void CleanLists()
    {
        for (int i = turretsInRange.Count - 1; i >= 0; i--)
        {
            if (turretsInRange[i] == null)
                turretsInRange.RemoveAt(i);
        }

        for (int i = targetingTurrets.Count - 1; i >= 0; i--)
        {
            if (targetingTurrets[i] == null)
                targetingTurrets.RemoveAt(i);
        }
    }

    void CheckStuck()
    {
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);

        if (distanceMoved < stuckThreshold)
        {
            stuckCheckTimer += Time.deltaTime;

            if (stuckCheckTimer > maxStuckTime)
            {
                HandleStuckSituation();
                stuckCheckTimer = 0f;
            }
        }
        else
        {
            stuckCheckTimer = 0f;
        }

        lastPosition = transform.position;
    }

    void HandleStuckSituation()
    {
        Debug.Log("Enemy is stuck, changing strategy...");

        if (currentState == EnemyState.Exploring)
        {
            SetRandomExplorationTarget();
        }
        else if (currentState == EnemyState.MovingToTarget && currentTarget != null)
        {
            StartWallFollowing();
        }

        AddVisitedPosition(transform.position);
    }

    void AddVisitedPosition(Vector3 position)
    {
        visitedPositions.Add(position);

        if (visitedPositions.Count > maxVisitedPositions)
        {
            visitedPositions.RemoveAt(0);
        }
    }

    bool IsPositionVisited(Vector3 position)
    {
        foreach (Vector3 visitedPos in visitedPositions)
        {
            if (Vector3.Distance(position, visitedPos) < visitedPositionRadius)
            {
                return true;
            }
        }
        return false;
    }

    void SetRandomExplorationTarget()
    {
        float randomAngle = Random.Range(0f, 360f);
        currentDirection = Quaternion.Euler(0, randomAngle, 0) * Vector3.forward;

        explorationTarget = transform.position + currentDirection * explorationRange;
        explorationTargetTime = Time.time;

        Debug.Log("Setting new exploration target");
    }

    void Explore()
    {
        if (Time.time - explorationTargetTime > explorationTargetDuration)
        {
            SetRandomExplorationTarget();
        }

        RaycastHit hit;
        Vector3 rayStart = transform.position;

        if (Physics.Raycast(rayStart, currentDirection, out hit, obstacleCheckDistance, wallLayer))
        {
            AvoidObstacle(hit.normal);
        }
        else
        {
            MoveInDirection(currentDirection);

            if (Time.frameCount % 30 == 0)
            {
                CheckSideObstacles();
            }
        }

        Debug.DrawRay(rayStart, currentDirection * obstacleCheckDistance, Color.cyan);
        Debug.DrawLine(transform.position, explorationTarget, Color.yellow);
    }

    void AvoidObstacle(Vector3 obstacleNormal)
    {
        if (!isFollowingWall)
        {
            StartWallFollowing();
        }

        wallFollowTimer += Time.deltaTime;

        if (wallFollowTimer > 3f)
        {
            StopWallFollowing();
            SetRandomExplorationTarget();
            return;
        }

        MoveInDirection(wallFollowDirection);

        if (wallFollowTimer % 0.5f < 0.1f)
        {
            RaycastHit hit;
            if (!Physics.Raycast(transform.position, currentDirection, obstacleCheckDistance, wallLayer))
            {
                StopWallFollowing();
            }
        }
    }

    void StartWallFollowing()
    {
        isFollowingWall = true;
        wallFollowTimer = 0f;

        float randomSide = Random.value > 0.5f ? 1f : -1f;
        wallFollowDirection = Vector3.Cross(currentDirection, Vector3.up).normalized * randomSide;

        Debug.Log("Started wall following");
    }

    void StopWallFollowing()
    {
        isFollowingWall = false;
        wallFollowTimer = 0f;
    }

    void CheckSideObstacles()
    {
        Vector3[] sideDirections = new Vector3[]
        {
            Quaternion.Euler(0, 45, 0) * currentDirection,
            Quaternion.Euler(0, -45, 0) * currentDirection,
            Quaternion.Euler(0, 90, 0) * currentDirection,
            Quaternion.Euler(0, -90, 0) * currentDirection
        };

        foreach (Vector3 sideDir in sideDirections)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, sideDir, obstacleCheckDistance * 0.7f, wallLayer))
            {
                Vector3 avoidDir = -sideDir.normalized * 0.3f;
                currentDirection += avoidDir;
                currentDirection.Normalize();
            }
        }
    }

    void MoveInDirection(Vector3 direction)
    {
        if (direction != Vector3.zero)
        {
            direction.y = 0;
            direction.Normalize();

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            Vector3 movement = transform.forward * moveSpeed * Time.deltaTime;
            movement.y = 0;
            transform.position += movement;
        }
    }

    void CheckTurretTargeting()
    {
        isBeingTargetedByTurret = targetingTurrets.Count > 0;
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
            UpdateTargetableTowers();
            yield return new WaitForSeconds(1f);
        }
    }

    void UpdateTargetableTowers()
    {
        for (int i = ignoredTowers.Count - 1; i >= 0; i--)
        {
            if (ignoredTowers[i] == null)
            {
                ignoredTowers.RemoveAt(i);
            }
        }
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
            currentState = EnemyState.Exploring;
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

        TowerCapture towerCapture = currentTarget.GetComponent<TowerCapture>();
        TurretHealth turretHealth = currentTarget.GetComponent<TurretHealth>();
        PlayerHealth playerHealth = currentTarget.GetComponent<PlayerHealth>();

        if (turretHealth != null || playerHealth != null)
        {
            if (distanceToTarget <= attackRange)
            {
                currentState = EnemyState.Attacking;
            }
            else if (distanceToTarget <= (turretHealth != null ? turretDetectionRange : playerDetectionRange))
            {
                currentState = EnemyState.MovingToTarget;
            }
            else
            {
                currentTarget = null;
                currentState = EnemyState.Exploring;
            }
        }
        else if (towerCapture != null)
        {
            if (distanceToTarget <= attackRange)
            {
                if (towerCapture.currentState == TowerCapture.TowerState.Enemy)
                {
                    currentTarget = null;
                    currentState = EnemyState.Exploring;
                }
                else
                {
                    currentState = EnemyState.CapturingTower;
                }
            }
            else if (distanceToTarget <= towerDetectionRange)
            {
                currentState = EnemyState.MovingToTarget;
            }
            else
            {
                currentTarget = null;
                currentState = EnemyState.Exploring;
            }
        }
    }

    void ExecuteState()
    {
        switch (currentState)
        {
            case EnemyState.Exploring:
                Explore();
                break;

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
            currentState = EnemyState.Exploring;
            return;
        }

        Vector3 direction = (currentTarget.position - transform.position).normalized;

        RaycastHit hit;
        Vector3 rayStart = transform.position;

        if (Physics.Raycast(rayStart, direction, out hit, obstacleCheckDistance, wallLayer))
        {
            Vector3 avoidance = AvoidObstacleTowardsTarget(direction, hit.normal);
            MoveInDirection(avoidance);
        }
        else
        {
            MoveInDirection(direction);
        }

        Debug.DrawRay(rayStart, direction * obstacleCheckDistance, Color.green);
        Debug.DrawLine(transform.position, currentTarget.position, Color.blue);
    }

    Vector3 AvoidObstacleTowardsTarget(Vector3 targetDirection, Vector3 obstacleNormal)
    {
        targetDirection.y = 0;
        targetDirection.Normalize();

        Vector3 rightPerp = Vector3.Cross(targetDirection, Vector3.up).normalized;
        Vector3 leftPerp = -rightPerp;

        RaycastHit hitRight, hitLeft;
        bool clearRight = !Physics.Raycast(transform.position, rightPerp, out hitRight, obstacleCheckDistance, wallLayer);
        bool clearLeft = !Physics.Raycast(transform.position, leftPerp, out hitLeft, obstacleCheckDistance, wallLayer);

        if (clearRight && clearLeft)
        {
            float dotRight = Vector3.Dot(rightPerp, targetDirection);
            float dotLeft = Vector3.Dot(leftPerp, targetDirection);
            return (dotRight > dotLeft) ? rightPerp : leftPerp;
        }
        else if (clearRight)
        {
            return rightPerp;
        }
        else if (clearLeft)
        {
            return leftPerp;
        }
        else
        {
            return -targetDirection;
        }
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
            currentState = EnemyState.Exploring;
            return;
        }

        if (towerCapture.IsBeingCapturedByEnemy() && !isCapturingTower)
        {
            currentTarget = null;
            currentState = EnemyState.Exploring;
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
                currentState = EnemyState.Exploring;
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
        currentState = EnemyState.Exploring;
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

    public void StopCapturing()
    {
        if (isCapturingTower)
        {
            isCapturingTower = false;
            captureProgress = 0f;
            StopAllCoroutines();
            currentState = EnemyState.Exploring;
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

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, explorationRange);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);

        if (currentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
}