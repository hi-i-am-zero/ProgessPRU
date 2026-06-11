using System.Collections;
using UnityEngine;

// Quan ly score va respawn player
public class GameManager : MonoBehaviour
{
    static GameManager instance;

    // Tu tim lai instance neu static bi mat (vi du sau domain reload giua play mode)
    public static GameManager Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<GameManager>();
            return instance;
        }
    }

    public PlayerController player;
    public Vector3 spawnPoint = new Vector3(0f, 2f, 0f);
    public float respawnDelay = 2f;

    int score;
    bool respawning;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        Debug.Log("Score: " + score);
    }

    public void AddScore(int amount)
    {
        score += amount;
        Debug.Log("Score: " + score);
    }

    public void PlayerDied()
    {
        if (respawning || player == null) return;
        respawning = true;
        player.Die();
        Debug.Log("Player chet! Respawn sau " + respawnDelay + " giay...");
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        // Don sach enemy roi cho player choi lai tu dau
        foreach (var e in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            Destroy(e.gameObject);

        score = 0;
        player.Respawn(spawnPoint);

        var cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cam != null) cam.Snap();

        Debug.Log("Respawn! Score: " + score);
        respawning = false;
    }
}
