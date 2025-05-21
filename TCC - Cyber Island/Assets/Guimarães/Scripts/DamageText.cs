// DamageText.cs
using UnityEngine;
using TMPro; // Se estiver usando TextMeshPro
// using UnityEngine.UI; // Se estiver usando UI Text normal (menos recomendado para texto no mundo)

public class DamageText : MonoBehaviour
{
    // --- ESCOLHA UM: TextMeshPro ou TextMesh normal ---
    public TMP_Text textMeshProComponent; // Para UI Canvas com TextMeshPro
    // OU
    // public TextMeshPro worldSpaceTextMeshPro; // Para TextMeshPro no espaço do mundo (sem Canvas)
    // OU
    // public Text textComponent; // Para UI Canvas com UI Text normal
    // OU
    public TextMesh regularTextMesh; // Para TextMesh normal no espaço do mundo (componente TextMesh)


    public float lifetime = 1.5f;
    public float moveSpeed = 1f;
    public Color normalDamageColor = Color.yellow;
    public Color criticalDamageColor = Color.red;
    public float criticalFontSizeMultiplier = 1.2f; // Críticos são um pouco maiores

    private float initialFontSize;


    void Awake()
    {
        // Tenta pegar o componente de texto automaticamente se não foi atribuído
        if (textMeshProComponent == null && regularTextMesh == null /* && worldSpaceTextMeshPro == null && textComponent == null */)
        {
            textMeshProComponent = GetComponentInChildren<TextMeshProUGUI>();
            if (textMeshProComponent == null)
            {
                // worldSpaceTextMeshPro = GetComponentInChildren<TextMeshPro>();
                // if (worldSpaceTextMeshPro == null) {
                regularTextMesh = GetComponentInChildren<TextMesh>();
                if (regularTextMesh == null)
                {
                    // textComponent = GetComponentInChildren<Text>();
                    // if (textComponent == null) {
                    Debug.LogError("Nenhum componente de Texto (TextMeshProUGUI, TextMeshPro, TextMesh, UI.Text) encontrado no DamageText Prefab!", this.gameObject);
                    Destroy(gameObject); // Destroi se não tem como mostrar texto
                    return;
                    // }
                }
                // }
            }
        }

        // Guarda o tamanho inicial da fonte
        if (textMeshProComponent != null) initialFontSize = textMeshProComponent.fontSize;
        // else if (worldSpaceTextMeshPro != null) initialFontSize = worldSpaceTextMeshPro.fontSize;
        // else if (textComponent != null) initialFontSize = textComponent.fontSize;
        else if (regularTextMesh != null) initialFontSize = (float)regularTextMesh.fontSize; // TextMesh.fontSize é int

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Faz o texto subir
        transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);
    }

    public void SetText(string text, bool isCritical)
    {
        if (textMeshProComponent != null)
        {
            textMeshProComponent.text = text;
            textMeshProComponent.color = isCritical ? criticalDamageColor : normalDamageColor;
            textMeshProComponent.fontSize = isCritical ? initialFontSize * criticalFontSizeMultiplier : initialFontSize;
        }
        // else if (worldSpaceTextMeshPro != null)
        // {
        //     worldSpaceTextMeshPro.text = text;
        //     worldSpaceTextMeshPro.color = isCritical ? criticalDamageColor : normalDamageColor;
        //     worldSpaceTextMeshPro.fontSize = isCritical ? initialFontSize * criticalFontSizeMultiplier : initialFontSize;
        // }
        // else if (textComponent != null)
        // {
        //     textComponent.text = text;
        //     textComponent.color = isCritical ? criticalDamageColor : normalDamageColor;
        //     textComponent.fontSize = isCritical ? (int)(initialFontSize * criticalFontSizeMultiplier) : (int)initialFontSize;
        // }
        else if (regularTextMesh != null)
        {
            regularTextMesh.text = text;
            regularTextMesh.color = isCritical ? criticalDamageColor : normalDamageColor;
            // Font size para TextMesh é um pouco diferente, você pode precisar de um shader que suporte escala melhor
            // ou ajustar o character size. Para simplicidade, vamos manter assim ou você pode ajustar 'characterSize'.
            // regularTextMesh.fontSize = isCritical ? (int)(initialFontSize * criticalFontSizeMultiplier) : (int)initialFontSize;
        }
    }
}