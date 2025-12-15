using UnityEngine;
using System.Collections.Generic;

public class TowerManager : MonoBehaviour
{
    public static TowerManager Instance { get; private set; }

    public List<Transform> activeTowers = new List<Transform>();

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

    public void RegisterTower(Transform tower)
    {
        if (!activeTowers.Contains(tower))
        {
            activeTowers.Add(tower);
        }
    }

    public void UnregisterTower(Transform tower)
    {
        if (activeTowers.Contains(tower))
        {
            activeTowers.Remove(tower);
        }
    }

    public Transform GetNearestAvailableTower(Vector3 position, float maxDistance, bool ignorePlayerOccupied = true)
    {
        Transform nearestTower = null;
        float minDistance = Mathf.Infinity;

        foreach (Transform tower in activeTowers)
        {
            if (tower == null) continue;

            TowerCapture towerCapture = tower.GetComponent<TowerCapture>();
            if (towerCapture != null)
            {
                if (towerCapture.currentState == TowerCapture.TowerState.Enemy)
                    continue;

                if (ignorePlayerOccupied && towerCapture.isPlayerInRange)
                    continue;
            }

            float distance = Vector3.Distance(position, tower.position);
            if (distance < maxDistance && distance < minDistance)
            {
                minDistance = distance;
                nearestTower = tower;
            }
        }

        return nearestTower;
    }
}