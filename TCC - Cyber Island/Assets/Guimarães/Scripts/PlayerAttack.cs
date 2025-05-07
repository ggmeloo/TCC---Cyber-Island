using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class PlayerAttack : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] Animator playerAnimator;
    [SerializeField] PlayerMovement playerMovement;

    [Header("Attack Settings")]
    [SerializeField] int maxComboSteps = 3;
    [Tooltip("Nome EXATO do estado de Ataque 1.")][SerializeField] string attack1StateName = "Attack1";
    [Tooltip("Nome EXATO do estado de Ataque 2.")][SerializeField] string attack2StateName = "Attack2";
    [Tooltip("Nome EXATO do estado de Ataque 3.")][SerializeField] string attack3StateName = "Attack3";
    [Tooltip("Nome EXATO do estado de Transição Pós-Ataque.")][SerializeField] string transitionStateName = "Transition";
    [Tooltip("Nome EXATO do estado Idle (para forçar volta após transição).")][SerializeField] string idleStateForReset = "Idle"; // O mesmo que idleStateName em PlayerMovement

    [Header("State (Read Only)")]
    [SerializeField] private bool isAttacking = false;
    [SerializeField] private int currentComboStep = 0;
    [SerializeField] private bool nextAttackBuffered = false;
    [SerializeField] private Coroutine attackCoroutine = null;

    private int attack1Hash, attack2Hash, attack3Hash, transitionHash, idleResetHash;

    public bool IsAttacking => isAttacking;

    void Awake()
    {
        // ... (Awake do PlayerAttack como antes, pegando componentes e hashes) ...
        if (playerAnimator == null) playerAnimator = GetComponent<Animator>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (!playerAnimator) Debug.LogError("PlayerAttack: Animator missing!", this);
        if (!playerMovement) Debug.LogError("PlayerAttack: PlayerMovement missing!", this);

        attack1Hash = Animator.StringToHash(attack1StateName);
        attack2Hash = Animator.StringToHash(attack2StateName);
        attack3Hash = Animator.StringToHash(attack3StateName);
        transitionHash = Animator.StringToHash(transitionStateName);
        idleResetHash = Animator.StringToHash(idleStateForReset);

        if (attack1StateName == "" || attack2StateName == "" || attack3StateName == "" || transitionStateName == "" || idleStateForReset == "")
            Debug.LogError("Nomes dos estados de Ataque/Transição/Idle não definidos no Inspector!", this);
    }

    void Start() { ResetAttackStateInternal(); } // Garante reset

    void Update()
    {
        if (Time.timeScale <= 0f || playerAnimator == null || playerMovement == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (isAttacking) { if (currentComboStep < maxComboSteps) { nextAttackBuffered = true; } }
            // --- USA CanCurrentlyStartAction do PlayerMovement ---
            else if (playerMovement.CanCurrentlyStartAction()) // <<< MUDANÇA AQUI
            {
                StartAttackCombo();
            }
        }
    }

    void StartAttackCombo()
    {
        if (attackCoroutine != null) { StopCoroutine(attackCoroutine); }
        attackCoroutine = StartCoroutine(AttackSequence());
    }

    IEnumerator AttackSequence()
    {
        isAttacking = true;
        // --- USA A PROPRIEDADE CORRETA ---
        playerMovement.CanControlLocomotion = false; // <<< MUDANÇA AQUI
        // ---------------------------------
        currentComboStep = 1;
        nextAttackBuffered = false;
        RotateTowardsCamera();

        while (currentComboStep <= maxComboSteps)
        {
            int currentHash = GetAttackHash(currentComboStep);
            string currentStateName = GetAttackStateName(currentComboStep);
            if (currentHash == 0) { Debug.LogError("AttackSequence: Invalid Hash, breaking."); break; }

            playerAnimator.Play(currentHash, 0, 0f);
            yield return null;

            yield return StartCoroutine(WaitForAnimationFinish(currentHash, currentStateName));

            if (nextAttackBuffered && currentComboStep < maxComboSteps)
            { currentComboStep++; nextAttackBuffered = false; }
            else { break; }
        }
        yield return StartCoroutine(PlayTransitionAndReset());
    }

    IEnumerator WaitForAnimationFinish(int stateHash, string stateNameForDebug)
    {
        yield return null;
        float timer = 0f;
        AnimatorClipInfo[] clipInfo = playerAnimator.GetCurrentAnimatorClipInfo(0);
        float animationLength = 0.5f;
        if (clipInfo.Length > 0 && clipInfo[0].clip != null) animationLength = clipInfo[0].clip.length;
        else Debug.LogWarning($"Could not get clip length for {stateNameForDebug}, using fallback {animationLength}s");

        while (playerAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash == stateHash && playerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
        {
            timer += Time.deltaTime;
            if (timer > animationLength * 1.5f) { Debug.LogWarning($"WaitForAnimationFinish: Safety break for {stateNameForDebug}"); yield break; }
            yield return null;
        }
    }

    IEnumerator PlayTransitionAndReset()
    {
        if (playerAnimator != null && transitionHash != 0 && transitionStateName != "")
        {
            playerAnimator.Play(transitionHash, 0, 0f);
            yield return null;
            yield return StartCoroutine(WaitForAnimationFinish(transitionHash, transitionStateName));
        }
        else { Debug.LogWarning("Transition state invalid or Animator missing, skipping transition."); }
        ResetAttackStateInternal();
    }

    int GetAttackHash(int step) { /* ... */ return (step == 1) ? attack1Hash : (step == 2) ? attack2Hash : (step == 3) ? attack3Hash : 0; }
    string GetAttackStateName(int step) { /* ... */ return (step == 1) ? attack1StateName : (step == 2) ? attack2StateName : (step == 3) ? attack3StateName : "Invalid"; }
    void RotateTowardsCamera() { /* ... */ if (playerMovement != null && playerMovement.MainCameraTransformProperty != null) { Vector3 ld = playerMovement.MainCameraTransformProperty.forward; ld.y = 0; if (ld != Vector3.zero) { transform.rotation = Quaternion.LookRotation(ld); } } }

    void ResetAttackStateInternal()
    {
        if (!isAttacking && (playerMovement != null && playerMovement.CanControlLocomotion)) return;
        isAttacking = false;
        currentComboStep = 0;
        nextAttackBuffered = false;
        // --- USA A PROPRIEDADE CORRETA ---
        if (playerMovement != null) playerMovement.CanControlLocomotion = true; // <<< MUDANÇA AQUI
        // ---------------------------------
        attackCoroutine = null;
        Debug.Log("[AttackLogic] Reset. Locomotion control enabled.");

        // Força o Animator a voltar para Idle explicitamente.
        // PlayerMovement.UpdateLocomotionAnimationState deve pegar daqui.
        if (playerAnimator != null && idleResetHash != 0)
        {
            AnimatorStateInfo currentStateInfo = playerAnimator.GetCurrentAnimatorStateInfo(0);
            if (currentStateInfo.shortNameHash != idleResetHash) // Só toca se não já estiver no Idle
            {
                // Debug.Log($"[AttackLogic] Forcing Animator back to '{idleStateForReset}' state.");
                // playerAnimator.Play(idleResetHash, 0, 0f); // PlayerMovement deve cuidar disso agora
            }
        }
    }
}