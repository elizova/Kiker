using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float damage = 1f;
    public float speed = 20f;
    public GameObject shooter;

    void Start()
    {
        Destroy(gameObject, 5f);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime, Space.Self);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == shooter || other.CompareTag("Bullet")) return;

        // TowerCapture tower = other.GetComponent<TowerCapture>();
        // if (tower != null && tower.currentState == TowerCapture.TowerState.Enemy)
        // {
        //     tower.TakeDamage(damage);
        //     Destroy(gameObject);
        //     return;
        // }

        EnemyHealth enemy = other.GetComponent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage * 5);
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}