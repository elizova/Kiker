using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class KeyboardController : MonoBehaviour
{
    public float moveSpeed = 2.0f;
    public Transform leftController;
    public Transform rightController;

    void Update()
    {
        // Левая рука - движение WASD
        if (leftController != null)
        {
            Vector3 move = new Vector3(
                Input.GetKey(KeyCode.A) ? -1 : Input.GetKey(KeyCode.D) ? 1 : 0,
                Input.GetKey(KeyCode.Q) ? 1 : Input.GetKey(KeyCode.E) ? -1 : 0,
                Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0
            );

            leftController.Translate(move * moveSpeed * Time.deltaTime);
        }

        // Правая рука - движение IJKL
        if (rightController != null)
        {
            Vector3 move = new Vector3(
                Input.GetKey(KeyCode.J) ? -1 : Input.GetKey(KeyCode.L) ? 1 : 0,
                Input.GetKey(KeyCode.U) ? 1 : Input.GetKey(KeyCode.O) ? -1 : 0,
                Input.GetKey(KeyCode.I) ? 1 : Input.GetKey(KeyCode.K) ? -1 : 0
            );

            rightController.Translate(move * moveSpeed * Time.deltaTime);
        }
    }
}