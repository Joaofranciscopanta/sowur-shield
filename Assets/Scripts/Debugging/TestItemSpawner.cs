using UnityEngine;
using SowurShield.Core;
using SowurShield.Inventory;

namespace SowurShield.Debugging
{

/// <summary>
/// Debug-only helper: press <see cref="spawnKey"/> to drop a random giftable item
/// on the ground near the player, for quickly testing the NPC gift flow without
/// needing to obtain items normally.
/// </summary>
public class TestItemSpawner : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private KeyCode spawnKey = KeyCode.F9;
    [SerializeField] private string[] testItemNames = { "Apple", "Bread" };

    private Transform player;
    private int spawnCounter = 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<TestItemSpawner>() != null)
            return;

        var go = new GameObject("TestItemSpawner");
        go.AddComponent<TestItemSpawner>();
        DontDestroyOnLoad(go);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(spawnKey))
            return;

        SpawnRandomItem();
    }

    private void SpawnRandomItem()
    {
        if (player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (player == null || testItemNames.Length == 0)
            return;

        string itemName = testItemNames[Random.Range(0, testItemNames.Length)];
        Item item = ItemDatabase.GetItem(itemName);
        if (item == null)
            return;

        spawnCounter++;

        GameObject groundItemObj = new GameObject($"TestGroundItem_{item.itemName}_{spawnCounter}");
        Vector3 offset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0);
        groundItemObj.transform.position = player.position + offset;

        SpriteRenderer sr = groundItemObj.AddComponent<SpriteRenderer>();
        sr.sprite = item.icon;
        sr.sortingOrder = 10;

        CircleCollider2D collider = groundItemObj.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.3f;

        GroundItem groundItem = groundItemObj.AddComponent<GroundItem>();
        groundItem.SetItem(item, 1);
    }
}

} // namespace SowurShield.Debugging
