using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnTowerCaptured(bool capturedByPlayer)
    {
        Debug.Log($"Tower captured by: {(capturedByPlayer ? "Player" : "Enemy")}");
        // Здесь можно добавить логику начисления очков, спавн юнитов и т.д.
    }
}