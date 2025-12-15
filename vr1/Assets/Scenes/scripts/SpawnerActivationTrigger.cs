using UnityEngine;

public class SpawnerActivationTrigger : MonoBehaviour
{
    [Header("Activation Settings")]
    public EnemySpawner[] spawnersToActivate;
    public bool hasBeenTriggered = false;
    public bool oneTimeActivation = true;

    void Start()
    {
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!oneTimeActivation || (oneTimeActivation && !hasBeenTriggered))
            {
                ActivateSpawners();
                hasBeenTriggered = true;

                if (oneTimeActivation)
                {
                    GetComponent<Collider>().enabled = false;
                }
            }
        }
    }

    void ActivateSpawners()
    {
        foreach (EnemySpawner spawner in spawnersToActivate)
        {
            if (spawner != null)
            {
                spawner.ActivateThisSpawner();
            }
        }

        Debug.Log($"Trigger {gameObject.name} activated {spawnersToActivate.Length} spawner(s)");
    }

    // void OnDrawGizmos()
    // {
    //     if (GetComponent<Collider>() != null)
    //     {
    //         Gizmos.color = hasBeenTriggered ? Color.green : Color.cyan;
    //         Gizmos.matrix = transform.localToWorldMatrix;
    //         if (GetComponent<BoxCollider>() != null)
    //         {
    //             Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    //         }
    //         else if (GetComponent<SphereCollider>() != null)
    //         {
    //             Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
    //         }

    //         Gizmos.color = Color.magenta;
    //         foreach (EnemySpawner spawner in spawnersToActivate)
    //         {
    //             if (spawner != null)
    //             {
    //                 Gizmos.DrawLine(transform.position, spawner.transform.position);
    //             }
    //         }
    //     }
    // }
}