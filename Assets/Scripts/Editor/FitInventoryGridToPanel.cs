using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SowurShield.Editor
{

/// <summary>
/// Puts the storage grid back inside the inventory panel it is supposed to sit in.
///
/// StorageContainer carried a localScale of 1.74 while the panel behind it stayed at 1. Its
/// 604x255 rect therefore drew at 1052x444 and burst out of the frame on every side -- 46px
/// past the painted top, 112px below the bottom, 199px past the right. The slots spilled onto
/// the woodwork and over the panel's edge onto the world behind it.
///
/// <para>The scale also made the two halves of one inventory disagree: a storage slot rendered
/// at about 104px against the hotbar's 48px, so the same 45 slots were drawn at two very
/// different sizes depending on which half you looked at.</para>
///
/// <para>At scale 1 the grid measures 9x64 + 8x5 = 580 wide and 4x64 + 3x5 = 271 tall, which
/// fits the panel's 678x285 painted interior with room to spare. The container is also
/// re-centred: the panel art is asymmetric (it paints roughly 82/113/86/150px of its 512px
/// source), so the middle of the *painted* area sits about 33px left of and 33px above the
/// middle of the rect, and a grid centred on the rect looks off-centre in the frame.</para>
///
/// Menu: Sowur Shield > UI > Fit Inventory Grid To Panel
/// </summary>
public static class FitInventoryGridToPanel
{
    // panel_wood_generic paints this much of its 512px source as frame, per side. The art is
    // asymmetric, which is why the interior centre is not the rect centre.
    private const float FrameLeft   = 82f / 512f;
    private const float FrameRight  = 113f / 512f;
    private const float FrameTop    = 86f / 512f;
    private const float FrameBottom = 150f / 512f;

    [MenuItem("Sowur Shield/UI/Fit Inventory Grid To Panel")]
    public static void Fit()
    {
        RectTransform storage = Find("StorageContainer");
        RectTransform panel   = Find("InventoryPanelBG");

        if (storage == null || panel == null)
        {
            Debug.LogError("[FitInventoryGridToPanel] Needs both StorageContainer and " +
                           "InventoryPanelBG in the open scene.");
            return;
        }

        Undo.RecordObject(storage, "Fit inventory grid");

        Vector3 oldScale = storage.localScale;
        Vector2 oldPos = storage.anchoredPosition;

        storage.localScale = Vector3.one;

        float pw = panel.sizeDelta.x;
        float ph = panel.sizeDelta.y;

        // Centre of the painted interior, measured from the panel's own centre.
        float interiorCentreX = ((-pw / 2f + FrameLeft * pw) + (pw / 2f - FrameRight * pw)) / 2f;
        float interiorCentreY = ((-ph / 2f + FrameBottom * ph) + (ph / 2f - FrameTop * ph)) / 2f;

        storage.anchoredPosition = new Vector2(panel.anchoredPosition.x + interiorCentreX,
                                               panel.anchoredPosition.y + interiorCentreY);

        EditorUtility.SetDirty(storage);
        if (PrefabUtility.IsPartOfPrefabInstance(storage))
            PrefabUtility.RecordPrefabInstancePropertyModifications(storage);

        EditorSceneManager.MarkSceneDirty(storage.gameObject.scene);

        Debug.Log($"[FitInventoryGridToPanel] scale {oldScale.x:F2} -> 1, position {oldPos} -> " +
                  $"{storage.anchoredPosition}. Save the scene.");
    }

    private static RectTransform Find(string name)
    {
        return Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include,
                                                       FindObjectsSortMode.None)
            .FirstOrDefault(r => r.name == name);
    }
}

}
