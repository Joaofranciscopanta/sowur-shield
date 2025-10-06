using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue/Dialogue Object")]
public class DialogueObject : ScriptableObject
{
    [SerializeField] [TextArea(3, 10)] private string[] dialogue;
    
    public string[] Dialogue => dialogue;
}