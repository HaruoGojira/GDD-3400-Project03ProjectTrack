using UnityEngine;

public class AISpawner : MonoBehaviour
{
    [SerializeField] float _SpawnRadius = 8;
    [SerializeField] int _InitialSpawnCount = 10;
    [SerializeField] float _SpawnInterval = 1f;
    [SerializeField] GameObject _AIPrefab;


    float _timeSinceLastSpawn = 0f;

    void Start()
    {
        // Populate the scene with the initial spawn count of AI
        for (int i = 0; i < _InitialSpawnCount; i++)
        {
            SpawnAI();
        }
    }

    private void Update()
    {
        // Spawn an AI at a random position within the spawn radius at the spawn interval
        _timeSinceLastSpawn += Time.deltaTime;
        if (_timeSinceLastSpawn >= _SpawnInterval)
        {
            SpawnAI();
            _timeSinceLastSpawn = 0f;
        }
    }

    // Spawn an AI at a random position within the spawn radius
    public void SpawnAI()
    {
        Vector3 spawnPosition = transform.position + new Vector3(Random.Range(-_SpawnRadius, _SpawnRadius), 0, Random.Range(-_SpawnRadius, _SpawnRadius));
        Instantiate(_AIPrefab, spawnPosition, Quaternion.identity);
    }

    //Draw gizmos to visualize the spawn radius
    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, _SpawnRadius);
    }
}
