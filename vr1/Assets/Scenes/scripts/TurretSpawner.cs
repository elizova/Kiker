using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections.Generic;

public class TurretSpawner : MonoBehaviour
{
    public GameObject turretPrefab;
    public InputActionReference gripAction;
    public float spawnDistance = 1.5f;
    public float spawnHeight = 3f;
    public float groundLevel = 0.1f;

    [Header("Разрешенные слои для спавна")]
    [Tooltip("Если луч попадает на объект с этим слоем - спавнить можно. Если список пустой - действует старая логика.")]
    public List<string> allowedLayers = new List<string>();

    private XRRayInteractor rayInteractor;
    private List<int> allowedLayerMasks = new List<int>();
    private GameOverManager gameOverManager;

    void Start()
    {
        gameOverManager = FindFirstObjectByType<GameOverManager>();
        rayInteractor = GetComponent<XRRayInteractor>();
        gripAction.action.Enable();
        gripAction.action.started += OnGripPressed;

        UpdateLayerMasks();
    }

    void UpdateLayerMasks()
    {
        allowedLayerMasks.Clear();
        foreach (string layerName in allowedLayers)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer != -1) // Если слой существует
            {
                allowedLayerMasks.Add(layer);
            }
            else
            {
                Debug.LogWarning($"Слой '{layerName}' не найден!");
            }
        }
    }

    void OnGripPressed(InputAction.CallbackContext context)
    {
        if (IsGameOver())
        {
            Debug.Log("Игра окончена, спавн турелей отключен");
            return;
        }

        if (IsPointingAtForbiddenCollider()) return;

        SpawnTurret();
    }

    bool IsGameOver()
    {
        if (gameOverManager != null)
        {
            return gameOverManager.IsGameOver;
        }
        return false;
    }

    bool IsPointingAtForbiddenCollider()
    {
        if (rayInteractor == null) return false;

        if (rayInteractor.interactablesSelected.Count > 0) return true;

        RaycastHit hit;
        if (rayInteractor.TryGetCurrent3DRaycastHit(out hit))
        {
            if (allowedLayerMasks.Count == 0)
            {
                return true;
            }

            GameObject hitObject = hit.collider.gameObject;
            return !IsLayerAllowed(hitObject.layer);
        }

        return false;
    }

    bool IsLayerAllowed(int layer)
    {
        foreach (int allowedLayer in allowedLayerMasks)
        {
            if (layer == allowedLayer)
            {
                return true;
            }
        }
        return false;
    }

    void SpawnTurret()
    {
        if (turretPrefab == null) return;

        Vector3 direction = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
        Vector3 spawnPos = transform.position + direction * spawnDistance;
        spawnPos.y = groundLevel;

        GameObject turret = Instantiate(
            turretPrefab,
            spawnPos,
            Quaternion.Euler(0, transform.eulerAngles.y, 0)
        );

        SetupTurret(turret);
    }

    void SetupTurret(GameObject turret)
    {
        Rigidbody rb = turret.GetComponent<Rigidbody>();
        if (rb == null) rb = turret.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ |
                         RigidbodyConstraints.FreezePositionY;

        if (turret.GetComponent<Collider>() == null)
        {
            turret.AddComponent<BoxCollider>();
        }

        FixedHeightGrabInteractable grab = turret.GetComponent<FixedHeightGrabInteractable>();
        if (grab == null) grab = turret.AddComponent<FixedHeightGrabInteractable>();

        grab.groundLevel = groundLevel;
    }

    void OnDestroy()
    {
        if (gripAction != null)
        {
            gripAction.action.started -= OnGripPressed;
        }
    }
}

public class FixedHeightGrabInteractable : XRGrabInteractable
{
    public float groundLevel = 0f;
    private Vector3 grabOffset;
    private XRBaseInteractor currentInteractor;
    private Rigidbody rb;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody>();

        trackPosition = true;
        trackRotation = false;
        throwOnDetach = false;
        movementType = MovementType.VelocityTracking;
        attachEaseInTime = 0.05f;
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        currentInteractor = args.interactorObject as XRBaseInteractor;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotationX |
                           RigidbodyConstraints.FreezeRotationZ |
                           RigidbodyConstraints.FreezePositionY;
        }

        if (currentInteractor != null)
        {
            Vector3 interactorPos = currentInteractor.transform.position;
            Vector3 myPos = transform.position;
            grabOffset = new Vector3(myPos.x - interactorPos.x, 0, myPos.z - interactorPos.z);
        }
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        currentInteractor = null;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Vector3 pos = transform.position;
            pos.y = groundLevel;
            transform.position = pos;
        }
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);

        if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic && isSelected)
        {
            if (currentInteractor != null && rb != null)
            {
                Vector3 targetPosition = currentInteractor.transform.position + grabOffset;

                targetPosition.y = groundLevel;

                rb.MovePosition(targetPosition);
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                Vector3 currentPos = rb.position;
                if (Mathf.Abs(currentPos.y - groundLevel) > 0.001f)
                {
                    currentPos.y = groundLevel;
                    rb.MovePosition(currentPos);
                }
            }
        }
    }

    void Update()
    {
        Vector3 pos = transform.position;
        if (Mathf.Abs(pos.y - groundLevel) > 0.001f)
        {
            pos.y = groundLevel;
            transform.position = pos;
        }
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            Vector3 pos = rb.position;
            if (Mathf.Abs(pos.y - groundLevel) > 0.001f)
            {
                pos.y = groundLevel;
                rb.MovePosition(pos);
            }
        }
    }
}