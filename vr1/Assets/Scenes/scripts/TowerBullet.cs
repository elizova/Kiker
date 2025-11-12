using UnityEngine;

public class TowerBullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float damage = 10f;
    public float speed = 20f;
    public float explosionRadius = 0f;

    [Header("Homming")]
    public bool isHoming = true;
    public float homingStrength = 5f;

    public Transform target;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Destroy(gameObject, 5f);
    }

    void FixedUpdate()
    {
        if (target != null && isHoming)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, direction * speed, homingStrength * Time.deltaTime);

            if (rb.linearVelocity != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
            }
        }
        else if (!isHoming && rb.linearVelocity == Vector3.zero)
        {
            rb.linearVelocity = transform.forward * speed;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        EnemyHealth enemy = other.GetComponent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}