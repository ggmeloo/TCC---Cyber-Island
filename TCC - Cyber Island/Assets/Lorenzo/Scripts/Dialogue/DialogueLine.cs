// DialogueData.cs
using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public string speakerName;
    [TextArea(3, 10)]
    public string sentence;
    public Sprite speakerSprite; // Sprite do personagem que est� falando
    // Voc� pode adicionar mais campos aqui, como som da fala, etc.
}
