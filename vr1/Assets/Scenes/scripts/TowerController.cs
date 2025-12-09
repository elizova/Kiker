using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TowerController : MonoBehaviour
{
    [Header("Tower Settings")]
    public float attackRange = 10f;
    public float rotationSpeed = 5f;
    public float fireRate = 1f;

    [Header("Combat")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletForce = 15f;
    public float bulletDamage = 10f;

    [Header("Visuals")]
    public Transform turretHead;
    public Transform barrel;

    [Header("Detection")]
    public LayerMask enemyLayerMask = 1;
    public bool showGizmos = true;

    private Transform currentTarget;
    private float nextFireTime;
    private List<Transform> enemiesInRange = new List<Transform>();

    void Update()
    {
        FindTarget();

        if (currentTarget != null)
        {
            RotateTowardsTarget();

            if (Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + 1f / fireRate;
            }

            EnemyAI enemyAI = currentTarget.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.AddTargetingTurret(this);
            }
        }
    }

    void FindTarget()
    {
        if (currentTarget != null &&
            (Vector3.Distance(transform.position, currentTarget.position) > attackRange ||
             !currentTarget.gameObject.activeInHierarchy))
        {
            EnemyAI enemyAI = currentTarget.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.RemoveTargetingTurret(this);
            }
            currentTarget = null;
        }

        if (currentTarget == null)
        {
            float closestDistance = Mathf.Infinity;
            Transform closestEnemy = null;

            foreach (Transform enemy in enemiesInRange.ToArray())
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy)
                {
                    enemiesInRange.Remove(enemy);
                    continue;
                }

                float distance = Vector3.Distance(transform.position, enemy.position);
                if (distance <= attackRange && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemy;
                }
            }

            currentTarget = closestEnemy;
        }
    }

    void RotateTowardsTarget()
    {
        if (turretHead == null || currentTarget == null) return;

        Vector3 direction = currentTarget.position - turretHead.position;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            turretHead.rotation = Quaternion.Slerp(turretHead.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void Shoot()
    {
        if (bulletPrefab == null || firePoint == null || currentTarget == null) return;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        TowerBullet bulletController = bullet.GetComponent<TowerBullet>();
        if (bulletController != null)
        {
            bulletController.damage = bulletDamage;
            bulletController.target = currentTarget;
        }
        else
        {
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 direction = (currentTarget.position - firePoint.position).normalized;
                rb.linearVelocity = direction * bulletForce;
            }
        }

        Destroy(bullet, 5f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & enemyLayerMask) != 0)
        {
            if (!enemiesInRange.Contains(other.transform))
            {
                enemiesInRange.Add(other.transform);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & enemyLayerMask) != 0)
        {
            enemiesInRange.Remove(other.transform);

            if (currentTarget == other.transform)
            {
                EnemyAI enemyAI = other.GetComponent<EnemyAI>();
                if (enemyAI != null)
                {
                    enemyAI.RemoveTargetingTurret(this);
                }
                currentTarget = null;
            }
        }
    }

    void OnDestroy()
    {
        foreach (Transform enemy in enemiesInRange)
        {
            if (enemy != null)
            {
                EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
                if (enemyAI != null)
                {
                    enemyAI.RemoveTargetingTurret(this);
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (showGizmos)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            if (currentTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(firePoint != null ? firePoint.position : transform.position, currentTarget.position);
            }
        }
    }
}