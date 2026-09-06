using System.Collections;
using TMPro;
using UnityEngine;
using SowurShield.Inventory;

namespace SowurShield.Core
{

/// <summary>
/// Shows a short rising label over the player when something is gained -- an item picked up or
/// harvested, money earned or spent.
///
/// The farm gave no feedback for any of this. Harvesting a crop moved a number in the corner
/// of the screen and nothing else, so the moment of gain was invisible at the place the player
/// was actually looking. Combat already had this (see CombatUnitVFX), but that implementation
/// parents the label to a combat unit and reads its state, so it cannot serve the farm.
///
/// <para>This listens to <see cref="Inventory.OnItemAdded"/> and
/// <see cref="PlayerStats.OnMoneyChanged"/> rather than being called from gameplay code, so
/// harvesting, picking up, buying and selling are all covered without touching any of them.</para>
///
/// <para>Attach to the player. It finds the Inventory and PlayerStats on the same object or in
/// the scene, and needs nothing wired in the inspector.</para>
/// </summary>
public class FarmFloatingText : MonoBehaviour
{
    [Header("Placement")]
    [Tooltip("Where the label starts, relative to this object.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.1f, 0f);

    [Tooltip("How far it drifts up over its lifetime, in world units.")]
    [SerializeField] private float riseDistance = 0.7f;

    [Tooltip("Seconds from spawn to fully faded.")]
    [SerializeField] private float duration = 1.1f;

    [Header("Appearance")]
    [SerializeField] private float fontSize = 2.6f;
    [SerializeField] private Color itemColor = new Color(0.98f, 0.95f, 0.85f);
    [SerializeField] private Color moneyGainColor = new Color(0.45f, 0.9f, 0.35f);
    [SerializeField] private Color moneySpendColor = new Color(0.95f, 0.45f, 0.35f);

    [Tooltip("Minimum seconds between labels, so a stack of pickups does not stutter one on " +
             "top of another.")]
    [SerializeField] private float minInterval = 0.12f;

    private SowurShield.Inventory.Inventory inventory;
    private PlayerStats stats;
    private Camera mainCamera;
    private TMP_FontAsset defaultFont;
    private int lastMoney;
    private float lastSpawnTime = -999f;
    private int stackDepth;

    private void Awake()
    {
        mainCamera = Camera.main;

        // Borrow the font an existing label in the scene already uses, so this matches the
        // rest of the UI without needing a Resources copy or an inspector reference.
        TMP_Text sample = FindFirstObjectByType<TMP_Text>();
        defaultFont = sample != null ? sample.font : TMP_Settings.defaultFontAsset;

        inventory = GetComponent<SowurShield.Inventory.Inventory>()
                    ?? FindFirstObjectByType<SowurShield.Inventory.Inventory>();
        stats = GetComponent<PlayerStats>() ?? FindFirstObjectByType<PlayerStats>();
    }

    private void OnEnable()
    {
        if (inventory != null) inventory.OnItemAdded += HandleItemAdded;

        if (stats != null)
        {
            lastMoney = stats.Money;
            stats.OnMoneyChanged += HandleMoneyChanged;
        }
    }

    private void OnDisable()
    {
        if (inventory != null) inventory.OnItemAdded -= HandleItemAdded;
        if (stats != null) stats.OnMoneyChanged -= HandleMoneyChanged;
    }

    private void HandleItemAdded(ItemStack stack)
    {
        if (stack == null || stack.item == null || stack.quantity <= 0) return;

        // GetDisplayName(), not itemName: itemName is the internal database key and is
        // documented as never being shown to the player. GetDisplayName resolves the
        // LocalizedString and falls back to the key only if the table has no entry.
        Spawn($"+{stack.quantity} {stack.item.GetDisplayName()}", itemColor);
    }

    private void HandleMoneyChanged(int current)
    {
        int delta = current - lastMoney;
        lastMoney = current;

        // A load or a SetMoney call reports a jump that is not something the player just did.
        if (delta == 0) return;

        Spawn(delta > 0 ? $"+${delta}" : $"-${-delta}",
              delta > 0 ? moneyGainColor : moneySpendColor);
    }

    private void Spawn(string text, Color color)
    {
        // Selling a full box fires one event per stack in the same frame. Without this the
        // labels land exactly on top of each other and read as one smeared blur.
        if (Time.unscaledTime - lastSpawnTime < minInterval)
            stackDepth++;
        else
            stackDepth = 0;

        lastSpawnTime = Time.unscaledTime;

        var go = new GameObject("FarmFloatingText");
        go.transform.position = transform.position + offset + new Vector3(0f, stackDepth * 0.28f, 0f);

        var tmp = go.AddComponent<TextMeshPro>();
        // Nunito is the only atlas in the project carrying accents, which localized item
        // names need. It does not live under a Resources folder, so it is taken from whatever
        // TMP is already configured to use by default rather than loaded by path; leaving the
        // font unset would fall back to LiberationSans, which has no accents.
        if (defaultFont != null) tmp.font = defaultFont;

        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = 9999;   // above every Y-sorted sprite
        tmp.rectTransform.sizeDelta = new Vector2(6f, 1f);

        StartCoroutine(RiseAndFade(go, tmp));
    }

    private IEnumerator RiseAndFade(GameObject go, TextMeshPro tmp)
    {
        Vector3 start = go.transform.position;
        Vector3 end = start + Vector3.up * riseDistance;
        Color baseColor = tmp.color;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            // O rotulo pode ser destruido a meio da animacao — uma mudanca de cena leva
            // consigo tudo o que nao seja DontDestroyOnLoad, e esta corrotina corre no
            // jogador, que ATRAVESSA as cenas. Sem esta guarda ficava a mexer num
            // GameObject destruido e a consola enchia-se de MissingReferenceException
            // a cada porta.
            if (go == null || tmp == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            go.transform.position = Vector3.Lerp(start, end, t);
            // Hold full opacity for the first third, then fade -- fading from frame one makes
            // a short label hard to read before it is gone.
            tmp.color = new Color(baseColor.r, baseColor.g, baseColor.b,
                                  1f - Mathf.InverseLerp(0.35f, 1f, t));

            if (mainCamera != null)
                go.transform.rotation = mainCamera.transform.rotation;

            yield return null;
        }

        Destroy(go);
    }
}

}
