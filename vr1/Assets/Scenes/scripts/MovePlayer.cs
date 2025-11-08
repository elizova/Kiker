using UnityEngine;
using UnityEngine.InputSystem;

public class MovePlayer : MonoBehaviour
{
    public float moveSpeed = 3.0f;
    public Transform playerCamera;

    private Vector2 moveInput;

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void Update()
    {
        Vector3 moveDirection = (playerCamera.forward * moveInput.y + playerCamera.right * moveInput.x).normalized;
        moveDirection.y = 0;

        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
    }
}