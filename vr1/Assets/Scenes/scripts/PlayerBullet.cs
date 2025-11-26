using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float damage = 10f;
    public float speed = 20f;
    public GameObject shooter;

    void Start()
    {
        Destroy(gameObject, 5f);

        if (shooter != null)
        {
            Collider shooterCollider = shooter.GetComponent<Collider>();
            Collider bulletCollider = GetComponent<Collider>();
            if (shooterCollider != null && bulletCollider != null)
            {
                Physics.IgnoreCollision(shooterCollider, bulletCollider);
            }
        }
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime, Space.Self);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bullet") || other.gameObject == shooter) return;

        EnemyHealth enemy = other.GetComponent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Destroy(gameObject);
    }
}