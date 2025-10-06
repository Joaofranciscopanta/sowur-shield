using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Tool Type", menuName = "Farming/Tool Type")]
public class ToolType : ScriptableObject
{
    public string typeName;
    public List<string> itemNames;

    public bool MatchesItem(Item item)
    {
        if (item == null || item.itemType != ItemType.Tool)
            return false;

        foreach (string name in itemNames)
        {
            if (item.itemName.Contains(name))
                return true;
        }

        return false;
    }
}
