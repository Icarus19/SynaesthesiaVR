using UnityEngine;

public class ScenerySpawner : MonoBehaviour
{
    public GameObject spherePrefab;  // Reference to the sphere prefab
    public int numberOfSpheres = 10;  // Number of spheres to spawn
    public float spawnRange = 10f;    // Range for random positions (sphere spawn area)

    [ContextMenu("SpawnScenery")]
    void SpawnScenery()
    {
        // Instantiate random spheres
        for (int i = 0; i < numberOfSpheres; i++)
        {
            // Random position within the defined range
            Vector3 randomPosition = new Vector3(
                Random.Range(-spawnRange, spawnRange),
                Random.Range(-spawnRange, spawnRange),
                Random.Range(-spawnRange, spawnRange)
            );

            // Instantiate the sphere at the random position
            Instantiate(spherePrefab, randomPosition, Quaternion.identity);
        }
    }
}
