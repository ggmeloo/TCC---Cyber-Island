// DialogueUI.cs
using UnityEngine;
using UnityEngine.UI; // Necessário para Image
using TMPro;          // Necessário para TextMeshProUGUI
using System.Collections;

public class DialogueUI : MonoBehaviour
{
    [Header("UI Elementos")]
    public GameObject dialoguePanel; // O painel principal do balão de fala
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI sentenceText;
    public Image speakerSpriteImage; // Para mostrar a imagem do falante

    [Header("Configurações de Digitação")]
    public float typingSpeed = 0.03f; // Segundos por caractere

    private Coroutine typingCoroutine;
    private DialogueLine currentLine;
    private bool isCurrentlyTyping = false;

    void Awake()
    {
        if (dialoguePanel == null) Debug.LogError("DialogueUI: dialoguePanel não atribuído!", this);
        if (speakerNameText == null) Debug.LogWarning("DialogueUI: speakerNameText não atribuído.", this);
        if (sentenceText == null) Debug.LogError("DialogueUI: sentenceText não atribuído!", this);
        if (speakerSpriteImage == null) Debug.LogWarning("DialogueUI: speakerSpriteImage não atribuído.", this);

        HideDialogue(); // Começa escondido
    }

    public void ShowDialogue()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
    }

    public void HideDialogue()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        isCurrentlyTyping = false;
    }

    public void DisplayLine(DialogueLine line)
    {
        currentLine = line;
        isCurrentlyTyping = true;

        if (speakerNameText != null)
        {
            speakerNameText.text = string.IsNullOrEmpty(line.speakerName) ? "" : line.speakerName;
            speakerNameText.gameObject.SetActive(!string.IsNullOrEmpty(line.speakerName));
        }

        if (speakerSpriteImage != null)
        {
            if (line.speakerSprite != null)
            {
                speakerSpriteImage.sprite = line.speakerSprite;
                speakerSpriteImage.gameObject.SetActive(true);
            }
            else
            {
                speakerSpriteImage.gameObject.SetActive(false);
            }
        }

        if (sentenceText != null)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeSentence(line.sentence));
        }
    }

    IEnumerator TypeSentence(string sentence)
    {
        sentenceText.text = "";
        foreach (char letter in sentence.ToCharArray())
        {
            sentenceText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        isCurrentlyTyping = false;
        typingCoroutine = null;
    }

    // Chamado pelo DialogueSystem quando o jogador aperta para avançar e o texto está sendo digitado
    public void CompleteSentenceEarly()
    {
        if (isCurrentlyTyping && currentLine != null && sentenceText != null)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }
            sentenceText.text = currentLine.sentence; // Mostra o texto completo imediatamente
            isCurrentlyTyping = false;
        }
    }

    public bool IsTyping()
    {
        return isCurrentlyTyping;
    }
}