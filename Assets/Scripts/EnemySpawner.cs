using UnityEngine;

// Spawn enemy tu ngoai mep phai camera di vao
public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float spawnInterval = 3f;
    public float spawnMargin = 2f;
    public int maxEnemies = 6;
    public float spawnY = 1.5f;

    float timer;

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.player == null || gm.player.IsDead)
        {
            timer = 0f;
            return;
        }

        timer += Time.deltaTime;
        if (timer < spawnInterval) return;
        timer = 0f;

        if (FindObjectsByType<EnemyController>(FindObjectsSortMode.None).Length >= maxEnemies) return;

        var cam = Camera.main;
        if (cam == null || enemyPrefab == null) return;

        float halfWidth = cam.orthographicSize * cam.aspect;
        float x = cam.transform.position.x + halfWidth + spawnMargin; // ngoai camera ben phai
        Instantiate(enemyPrefab, new Vector3(x, spawnY, 0f), Quaternion.identity);
    }
}
