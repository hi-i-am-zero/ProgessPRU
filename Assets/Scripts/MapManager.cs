using System.Collections.Generic;
using UnityEngine;

// Map vo han: moi lan can mo rong se random 1 trong cac chunk prefab
public class MapManager : MonoBehaviour
{
    public GameObject[] chunkPrefabs;
    public Transform player;
    public int chunkWidth = 32;
    public int chunksAhead = 2;
    public int chunksBehind = 1;

    readonly Dictionary<int, GameObject> chunks = new Dictionary<int, GameObject>();

    void Start()
    {
        UpdateChunks();
    }

    void Update()
    {
        UpdateChunks();
    }

    void UpdateChunks()
    {
        if (player == null || chunkPrefabs == null || chunkPrefabs.Length == 0) return;

        int current = Mathf.FloorToInt(player.position.x / chunkWidth);
        int min = current - chunksBehind;
        int max = current + chunksAhead;

        // Xoa chunk qua xa
        var toRemove = new List<int>();
        foreach (var kv in chunks)
            if (kv.Key < min || kv.Key > max) toRemove.Add(kv.Key);
        foreach (var k in toRemove)
        {
            Destroy(chunks[k]);
            chunks.Remove(k);
        }

        // Sinh chunk moi: random 1 prefab trong danh sach
        for (int i = min; i <= max; i++)
        {
            if (chunks.ContainsKey(i)) continue;
            var prefab = chunkPrefabs[Random.Range(0, chunkPrefabs.Length)];
            var go = Instantiate(prefab, new Vector3(i * chunkWidth, 0f, 0f), Quaternion.identity, transform);
            go.name = prefab.name + "_" + i;
            chunks[i] = go;
        }
    }
}
