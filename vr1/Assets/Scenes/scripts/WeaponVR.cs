using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class WeaponVR : MonoBehaviour
{
    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform bulletSpawnPoint;
    public float bulletForce = 15f;
    public float fireRate = 0.3f;

    [Header("Recoil Animation")]
    public float recoilDistance = 0.05f;
    public float recoilSpeed = 8f;

    [Header("XR Controller")]
    public ActionBasedController xrController;

    public InputActionReference triggerAction;

    private float nextFireTime;
    private Vector3 originalPosition;

    void Start()
    {
        triggerAction.action.Enable();
        triggerAction.action.performed += onTriggerClick;
        originalPosition = transform.localPosition;

        if (bulletSpawnPoint == null)
        {
            GameObject spawnPoint = new GameObject("BulletSpawn");
            spawnPoint.transform.SetParent(transform);
            spawnPoint.transform.localPosition = new Vector3(0, 0, 0.3f);
            bulletSpawnPoint = spawnPoint.transform;
        }
    }

    void Update()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition,
                                             originalPosition,
                                             recoilSpeed * Time.deltaTime);

        if (IsTriggerPressed() && Time.time >= nextFireTime)
        {
            Shoot();
        }
    }

    void onTriggerClick(InputAction.CallbackContext context)
    {

    }

    bool IsTriggerPressed()
    {
        if (triggerAction == null) return false;

        float triggerValue = triggerAction.action.ReadValue<float>();
        return triggerValue > 0.5f;
    }

    void Shoot()
    {
        if (Time.time < nextFireTime) return;

        if (bulletPrefab != null)
        {
            GameObject bullet = Instantiate(bulletPrefab,
                                          bulletSpawnPoint.position,
                                          bulletSpawnPoint.rotation);

            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = bulletSpawnPoint.forward * bulletForce;
            }

            Destroy(bullet, 3f);
        }

        transform.localPosition = originalPosition - transform.forward * recoilDistance;

        SendHapticImpulse();

        nextFireTime = Time.time + fireRate;
    }

    void SendHapticImpulse()
    {
        XRBaseController controller = GetComponentInParent<XRBaseController>();
        if (controller != null)
        {
            controller.SendHapticImpulse(0.3f, 0.1f);
        }
    }
}