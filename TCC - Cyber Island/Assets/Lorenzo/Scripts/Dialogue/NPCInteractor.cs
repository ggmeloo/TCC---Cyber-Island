// NPCInteractor.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(SphereCollider))]
public class NPCInteractor : MonoBehaviour
{
    [Header("Configura��es de Intera��o")]
    public DialogueData dialogueToTrigger;
    public KeyCode interactionKey = KeyCode.E;
    public string promptMessage = "Aperte [E] para Conversar";

    [Header("Refer�ncias do NPC")]
    [Tooltip("O Animator do modelo deste NPC, se diferente do Animator no mesmo GameObject.")]
    public Animator npcAnimator;

    [Header("UI do Prompt")]
    public GameObject promptCanvas;
    public TextMeshProUGUI promptText;
    public Image npcPromptImage;

    private bool playerIsNear = false;
    private Transform playerTransform;
    private bool isThisNpcInDialogue = false;

    void Start()
    {
        SphereCollider proximityCollider = GetComponent<SphereCollider>();
        if (proximityCollider == null) { proximityCollider = gameObject.AddComponent<SphereCollider>(); proximityCollider.radius = 3f; }
        proximityCollider.isTrigger = true;

        if (npcAnimator == null) npcAnimator = GetComponentInChildren<Animator>();
        if (npcAnimator == null) Debug.LogWarning($"[{gameObject.name}] NPCInteractor: npcAnimator n�o encontrado. Anima��o de conversa do NPC n�o funcionar�.", this);

        if (promptCanvas != null)
        {
            if (promptText != null) promptText.text = promptMessage;
            if (npcPromptImage != null) npcPromptImage.gameObject.SetActive(npcPromptImage.sprite != null);
            promptCanvas.SetActive(false);
        }
        else Debug.LogError($"[{gameObject.name}] NPCInteractor: PromptCanvas n�o atribu�do!", this);

        if (dialogueToTrigger == null) Debug.LogError($"[{gameObject.name}] NPCInteractor: DialogueData n�o atribu�do!", this);

        if (DialogueSystem.Instance != null) DialogueSystem.Instance.OnDialogueEnded += HandleDialogueSystemEnd;
        else Debug.LogWarning($"[{gameObject.name}] NPCInteractor: DialogueSystem.Instance � nulo no Start.");
    }

    void OnDestroy()
    {
        if (DialogueSystem.Instance != null) DialogueSystem.Instance.OnDialogueEnded -= HandleDialogueSystemEnd;
    }

    void Update()
    {
        if (playerIsNear && Input.GetKeyDown(interactionKey))
        {
            if (DialogueSystem.Instance != null)
            {
                // --- MODIFICADO: Checar a flag ---
                if (DialogueSystem.Instance.GetDialogueJustEndedThisFrame())
                {
                    // O di�logo acabou de terminar neste frame, ent�o o input 'E' foi para fechar.
                    // N�o fazemos nada para evitar reiniciar o di�logo.
                }
                else if (!DialogueSystem.Instance.IsDialogueActive())
                {
                    isThisNpcInDialogue = true;
                    DialogueSystem.Instance.StartDialogue(dialogueToTrigger, this);
                    if (promptCanvas != null) promptCanvas.SetActive(false);
                }
                // -----------------------------------
            }
        }

        if (promptCanvas != null && promptCanvas.activeSelf && playerTransform != null && Camera.main != null)
        {
            promptCanvas.transform.LookAt(promptCanvas.transform.position + Camera.main.transform.rotation * Vector3.forward,
                                         Camera.main.transform.rotation * Vector3.up);
        }
    }

    void HandleDialogueSystemEnd(DialogueData endedDialogue, NPCInteractor sourceInteractor)
    {
        if (sourceInteractor == this)
        {
            isThisNpcInDialogue = false;
            if (playerIsNear && promptCanvas != null)
            {
                promptCanvas.SetActive(true);
                if (npcPromptImage != null) npcPromptImage.gameObject.SetActive(npcPromptImage.sprite != null);
            }
        }
    }

    public Animator GetNpcAnimator()
    {
        return npcAnimator;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsNear = true;
            playerTransform = other.transform;
            // S� mostra o prompt se o di�logo n�o estiver ativo E n�o acabou de terminar neste frame
            if (promptCanvas != null && DialogueSystem.Instance != null &&
                !DialogueSystem.Instance.IsDialogueActive() &&
                !DialogueSystem.Instance.GetDialogueJustEndedThisFrame()) // Adicionado para consist�ncia
            {
                promptCanvas.SetActive(true);
                if (npcPromptImage != null) npcPromptImage.gameObject.SetActive(npcPromptImage.sprite != null);
            }
            else if (promptCanvas != null && DialogueSystem.Instance == null) // Caso o DialogueSystem ainda n�o esteja pronto
            {
                promptCanvas.SetActive(true);
                if (npcPromptImage != null) npcPromptImage.gameObject.SetActive(npcPromptImage.sprite != null);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsNear = false;
            playerTransform = null;
            if (promptCanvas != null) promptCanvas.SetActive(false);

            if (isThisNpcInDialogue && DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive())
            {
                Debug.Log($"[{gameObject.name}] Player saiu da zona durante o di�logo. Encerrando di�logo.");
                DialogueSystem.Instance.EndDialogue();
            }
        }
    }
}