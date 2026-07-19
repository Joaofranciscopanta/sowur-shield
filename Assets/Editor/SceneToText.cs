using UnityEngine;
using UnityEditor;
using System.Text;

public class SceneToText
{
    [MenuItem("Tools/Export Scene To Text")]
    static void ExportScene()
    {
        StringBuilder sb = new StringBuilder();

        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.transform.parent == null)
                DumpGameObject(go.transform, sb, 0);
        }

        System.IO.File.WriteAllText("SceneDescription.txt", sb.ToString());
        Debug.Log("Scene exportada para SceneDescription.txt");
    }

    static void DumpGameObject(Transform t, StringBuilder sb, int indent)
    {
        sb.AppendLine($"{new string(' ', indent * 2)}- {t.name}");
        sb.AppendLine($"{new string(' ', indent * 2)}  Position: {t.position}");
        sb.AppendLine($"{new string(' ', indent * 2)}  Rotation: {t.rotation.eulerAngles}");
        sb.AppendLine($"{new string(' ', indent * 2)}  Scale: {t.localScale}");

        foreach (Transform child in t)
            DumpGameObject(child, sb, indent + 1);
    }
}
