using UnityEngine;

public class TowerCapture : MonoBehaviour
{
    public float captureProgress = 0f;
    public bool isCaptured = false;

    public bool IsCapturedByEnemy { get; set; } = false;

    public void OnCapturedByEnemy()
    {
        isCaptured = true;
        Debug.Log("Main tower has been captured");

        GetComponent<Renderer>().material.color = Color.red;
    }

    public void OnRecapturedByPlayer()
    {
        isCaptured = false;
        captureProgress = 0f;
        GetComponent<Renderer>().material.color = Color.blue;
    }
}