using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue/New Dialogue Asset")]
public class DialogueData : ScriptableObject
{
    public string dialogueName; // Um nome para identificar este diálogo no editor
    public DialogueLine[] lines;
}