// DialogueSystem.cs
using UnityEngine;
using System.Collections.Generic;
using System.Collections; // Necessário para IEnumerator se usar a opção de corrotina

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    [Header("Referências")]
    public DialogueUI dialogueUI;
    public PlayerMovement playerMovement;
    private Animator playerAnimator;

    [Header("Controles")]
    public KeyCode advanceDialogueKey = KeyCode.E;
    public KeyCode exitDialogueKey = KeyCode.Escape;

    private Queue<DialogueLine> linesQueue;
    private DialogueData currentDialogueData;
    private bool isDialogueActive = false;

    private Animator currentNpcAnimator;
    private const string IS_TALKING_PARAM = "IsTalking";

    public delegate void DialogueEndedAction(DialogueData dialogueData, NPCInteractor interactor);
    public event DialogueEndedAction OnDialogueEnded;

    // --- NOVO: Flag para controlar input no frame de término ---
    private bool dialogueJustEndedThisFrame = false;
    // -------------------------------------------------------------

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        linesQueue = new Queue<DialogueLine>();

        if (dialogueUI == null) Debug.LogError("DialogueSystem: DialogueUI não atribuído!", this);
        if (playerMovement == null) playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement == null) Debug.LogError("DialogueSystem: PlayerMovement não encontrado/atribuído!", this);
        else playerAnimator = playerMovement.GetComponentInChildren<Animator>();

        if (playerAnimator == null && playerMovement != null) Debug.LogWarning("DialogueSystem: Animator do Player não encontrado (filho do PlayerMovement).");
    }

    void Update()
    {
        if (isDialogueActive)
        {
            if (Input.GetKeyDown(advanceDialogueKey))
            {
                if (dialogueUI != null && dialogueUI.IsTyping()) dialogueUI.CompleteSentenceEarly();
                else DisplayNextLine();
            }
            else if (Input.GetKeyDown(exitDialogueKey)) EndDialogue();
        }
    }

    // --- NOVO: Resetar a flag no LateUpdate ---
    void LateUpdate()
    {
        if (dialogueJustEndedThisFrame)
        {
            dialogueJustEndedThisFrame = false;
        }
    }
    // ----------------------------------------

    public void StartDialogue(DialogueData dialogueToStart, NPCInteractor interactor)
    {
        if (dialogueToStart == null || dialogueToStart.lines.Length == 0 || dialogueUI == null || playerMovement == null || interactor == null)
        {
            Debug.LogWarning("DialogueSystem: Não foi possível iniciar o diálogo. Referências faltando ou diálogo vazio.", this);
            return;
        }
        if (isDialogueActive)
        {
            Debug.LogWarning("DialogueSystem: Tentativa de iniciar novo diálogo enquanto outro está ativo.", this);
            return;
        }

        isDialogueActive = true;
        currentDialogueData = dialogueToStart;
        currentNpcAnimator = interactor.GetNpcAnimator();

        playerMovement.SetMovementEnabled(false);
        if (playerAnimator != null && HasParameter(playerAnimator, IS_TALKING_PARAM)) playerAnimator.SetBool(IS_TALKING_PARAM, true);
        if (currentNpcAnimator != null && HasParameter(currentNpcAnimator, IS_TALKING_PARAM)) currentNpcAnimator.SetBool(IS_TALKING_PARAM, true);

        dialogueUI.ShowDialogue();

        linesQueue.Clear();
        foreach (DialogueLine line in currentDialogueData.lines) linesQueue.Enqueue(line);
        DisplayNextLine();
    }

    void DisplayNextLine()
    {
        if (linesQueue.Count == 0)
        {
            EndDialogue();
            return;
        }
        DialogueLine line = linesQueue.Dequeue();
        dialogueUI.DisplayLine(line);
    }

    public void EndDialogue()
    {
        if (!isDialogueActive) return;

        isDialogueActive = false;
        // --- NOVO: Setar a flag ---
        dialogueJustEndedThisFrame = true;
        // -------------------------

        if (playerMovement != null) playerMovement.SetMovementEnabled(true);
        if (playerAnimator != null && HasParameter(playerAnimator, IS_TALKING_PARAM)) playerAnimator.SetBool(IS_TALKING_PARAM, false);
        if (currentNpcAnimator != null && HasParameter(currentNpcAnimator, IS_TALKING_PARAM)) currentNpcAnimator.SetBool(IS_TALKING_PARAM, false);

        if (dialogueUI != null) dialogueUI.HideDialogue();

        NPCInteractor interactorAssociatedWithDialogue = FindNPCInteractorByDialogue(currentDialogueData);

        OnDialogueEnded?.Invoke(currentDialogueData, interactorAssociatedWithDialogue);

        currentDialogueData = null;
        currentNpcAnimator = null;
    }

    private NPCInteractor FindNPCInteractorByDialogue(DialogueData data)
    {
        if (data == null) return null;
        NPCInteractor[] interactors = FindObjectsByType<NPCInteractor>(FindObjectsSortMode.InstanceID);
        foreach (NPCInteractor interactor in interactors)
        {
            if (interactor.dialogueToTrigger == data)
            {
                return interactor;
            }
        }
        return null;
    }

    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }

    // --- NOVO: Getter para a flag ---
    public bool GetDialogueJustEndedThisFrame()
    {
        return dialogueJustEndedThisFrame;
    }
    // --------------------------------

    private bool HasParameter(Animator animator, string paramName)
    {
        if (animator == null || string.IsNullOrEmpty(paramName)) return false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }
}