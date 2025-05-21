using UnityEngine;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    [Header("Refer�ncias")]
    public Animator characterAnimator;
    public Transform meleeAttackPoint;

    [Header("Configura��es Gerais de Ataque")]
    public int maxComboSteps = 3;
    private int currentComboStep = 0;
    private float lastSuccessfulAttackInputTime = 0f;
    public float comboResetTime = 2.0f;
    private bool attackBuffered = false;
    private bool isAnimatorInAttackState = false; // Ainda �til para a l�gica de buffer

    public enum WeaponAnimType { Unarmed = 0, Melee = 1, Ranged = 2 }
    public WeaponAnimType equippedWeaponAnimType = WeaponAnimType.Unarmed;
    private const string ATTACK_STATE_TAG = "Attack";

    [Header("Configura��es de Dano do Player")]
    public int baseDamage = 20;
    public float criticalHitChance = 0.2f;
    public int criticalDamageMultiplier = 2;
    public float meleeAttackRadius = 0.5f;
    public LayerMask enemyLayer;

    // --- NOVO PARA DETEC��O DE DANO SEM ANIMATION EVENTS ---
    [Tooltip("Percentual da dura��o da anima��o de ataque em que o dano deve ser aplicado (0.0 a 1.0). Ex: 0.5 para meio da anima��o.")]
    public float damageApplicationPointNormalized = 0.5f; // Ex: aplica dano na metade da anima��o
    private Coroutine activeHitDetectionCoroutine;
    // ---------------------------------------------------------

    void Start()
    {
        if (characterAnimator == null)
        {
            characterAnimator = GetComponentInChildren<Animator>();
            if (characterAnimator == null) { Debug.LogError("PLAYER ATTACK: ANIMATOR N�O ENCONTRADO! Desabilitando.", this.gameObject); this.enabled = false; return; }
        }
        if (meleeAttackPoint == null)
        {
            meleeAttackPoint = transform;
        }
    }

    void Update()
    {
        if (characterAnimator == null) return;

        // Atualiza se estamos em um estado de ataque do Animator
        bool previousAttackState = isAnimatorInAttackState;
        isAnimatorInAttackState = characterAnimator.GetCurrentAnimatorStateInfo(0).IsTag(ATTACK_STATE_TAG);

        // Se acabamos de SAIR de um estado de ataque, paramos qualquer coroutine de hit pendente
        if (previousAttackState && !isAnimatorInAttackState)
        {
            if (activeHitDetectionCoroutine != null)
            {
                StopCoroutine(activeHitDetectionCoroutine);
                activeHitDetectionCoroutine = null;
                //Debug.Log("Saindo do estado de ataque, parando coroutine de hit detection.");
            }
        }


        if (currentComboStep > 0 && !isAnimatorInAttackState && Time.time > lastSuccessfulAttackInputTime + comboResetTime)
        {
            ResetCombo();
        }

        if (Input.GetMouseButtonDown(0))
        {
            attackBuffered = true;
        }

        if (attackBuffered)
        {
            bool attackProcessedThisFrame = false; // Para garantir que o buffer s� seja consumido uma vez por l�gica de ataque

            if (equippedWeaponAnimType == WeaponAnimType.Unarmed || equippedWeaponAnimType == WeaponAnimType.Melee)
            {
                if (!isAnimatorInAttackState || currentComboStep == 0)
                {
                    currentComboStep = 1;
                    ProcessAttackParametersForAnimator(true); // true para iniciar detec��o de hit
                    attackProcessedThisFrame = true;
                }
                else if (isAnimatorInAttackState && currentComboStep > 0 && currentComboStep < maxComboSteps)
                {
                    currentComboStep++;
                    ProcessAttackParametersForAnimator(true);
                    attackProcessedThisFrame = true;
                }
                else if (isAnimatorInAttackState && currentComboStep == maxComboSteps)
                {
                    currentComboStep = 1;
                    ProcessAttackParametersForAnimator(true);
                    attackProcessedThisFrame = true;
                }
            }
            else if (equippedWeaponAnimType == WeaponAnimType.Ranged)
            {
                if (!isAnimatorInAttackState)
                {
                    currentComboStep = 1;
                    ProcessAttackParametersForAnimator(false); // Ranged pode ter sua pr�pria l�gica de hit
                    // PerformRangedHitLogic(); // Fun��o espec�fica para tiro
                    attackProcessedThisFrame = true;
                }
            }

            if (attackProcessedThisFrame)
            {
                lastSuccessfulAttackInputTime = Time.time;
                attackBuffered = false; // Consome o buffer apenas se um ataque foi processado
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) { EquipWeapon(WeaponAnimType.Unarmed); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { EquipWeapon(WeaponAnimType.Melee); }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { EquipWeapon(WeaponAnimType.Ranged); }
    }

    void ProcessAttackParametersForAnimator(bool shouldAttemptHitDetection)
    {
        if (characterAnimator == null) return;
        characterAnimator.SetInteger("WeaponType", (int)equippedWeaponAnimType);
        characterAnimator.SetInteger("ComboStep", currentComboStep);
        characterAnimator.SetTrigger("AttackInput"); // Dispara o Animator

        // Se este ataque deve tentar uma detec��o de hit baseada em tempo
        if (shouldAttemptHitDetection && (equippedWeaponAnimType == WeaponAnimType.Unarmed || equippedWeaponAnimType == WeaponAnimType.Melee))
        {
            // Para qualquer coroutine de hit anterior que possa estar rodando
            if (activeHitDetectionCoroutine != null)
            {
                StopCoroutine(activeHitDetectionCoroutine);
            }
            // Inicia uma nova coroutine para este ataque
            activeHitDetectionCoroutine = StartCoroutine(HitDetectionCoroutine());
        }
    }

    IEnumerator HitDetectionCoroutine()
    {
        // Espera um pequeno delay para o Animator realmente transitar para o novo estado de ataque
        yield return null; // Espera o pr�ximo frame
        yield return null; // Espera mais um para garantir a transi��o

        if (!characterAnimator.GetCurrentAnimatorStateInfo(0).IsTag(ATTACK_STATE_TAG))
        {
            //Debug.LogWarning("HitDetectionCoroutine: Animator n�o est� mais em estado de ataque. Abortando hit.");
            activeHitDetectionCoroutine = null;
            yield break; // Sai se n�o estamos mais em um estado de ataque
        }

        AnimatorStateInfo stateInfo = characterAnimator.GetCurrentAnimatorStateInfo(0);
        // Animator.GetCurrentAnimatorClipInfo N�O funciona bem para pegar o clipe de um estado espec�fico
        // se o estado � parte de uma sub-state machine ou se h� blends.
        // A forma mais robusta seria ter uma refer�ncia direta aos AnimationClips se poss�vel,
        // ou usar nomes de estado para buscar a dura��o.

        // SOLU��O MAIS SIMPLES (mas menos flex�vel): Hardcodear dura��es ou ter um array de dura��es
        // Por simplicidade, vamos assumir que podemos pegar a dura��o do estado atual (pode n�o ser 100% preciso para todos os setups)
        float animationLength = stateInfo.length; // Dura��o do estado atual (inclui blends de transi��o)
        float waitTime = animationLength * damageApplicationPointNormalized;

        //Debug.Log($"HitDetectionCoroutine: Estado '{stateInfo.shortNameHash}', Dura��o: {animationLength:F2}s, Esperando: {waitTime:F2}s para hit.");

        if (waitTime > 0.01f) // S� espera se o tempo for significativo
        {
            yield return new WaitForSeconds(waitTime);
        }
        else
        {
            //Debug.LogWarning("HitDetectionCoroutine: Tempo de espera calculado � muito baixo ou zero. Chamando PerformHitDetection imediatamente.");
        }


        // Verifica novamente se ainda estamos no mesmo tipo de ataque (ou em um estado de ataque)
        // antes de aplicar o dano, para o caso de o jogador ter trocado de arma ou sido interrompido.
        if (characterAnimator.GetCurrentAnimatorStateInfo(0).IsTag(ATTACK_STATE_TAG) &&
            (equippedWeaponAnimType == WeaponAnimType.Unarmed || equippedWeaponAnimType == WeaponAnimType.Melee))
        {
            PerformHitDetection();
        }
        else
        {
            //Debug.Log("HitDetectionCoroutine: Estado mudou antes do hit ou tipo de arma mudou. Dano n�o aplicado por esta coroutine.");
        }
        activeHitDetectionCoroutine = null; // Marca que a coroutine terminou
    }


    // ESTA FUN��O AGORA � CHAMADA PELA COROUTINE
    public void PerformHitDetection()
    {
        if (equippedWeaponAnimType == WeaponAnimType.Ranged) return; // J� tratado

        //Debug.Log("PerformHitDetection (Coroutine) chamada para Melee/Unarmed.");
        if (meleeAttackPoint == null) return;

        Collider[] hitColliders = Physics.OverlapSphere(meleeAttackPoint.position, meleeAttackRadius, enemyLayer);
        foreach (Collider hitEnemyCollider in hitColliders)
        {
            ApplyDamageToEnemy(hitEnemyCollider.gameObject);
        }
    }

    void ApplyDamageToEnemy(GameObject hitEnemyObject)
    {
        if (hitEnemyObject == null) return;
        EnemyHealth enemyAI = hitEnemyObject.GetComponent<EnemyHealth>();
        if (enemyAI != null)
        {
            bool isCritical = Random.value < criticalHitChance;
            int damageToDeal = baseDamage;
            if (isCritical) damageToDeal *= criticalDamageMultiplier;
            //Debug.Log($"PLAYER: Dano de {damageToDeal} (Cr�tico: {isCritical}) em {hitEnemyObject.name}.");
            enemyAI.TakeDamage(damageToDeal, isCritical);
        }
    }

    public void EquipWeapon(WeaponAnimType type)
    {
        equippedWeaponAnimType = type;
        ResetCombo();
    }

    public void ResetCombo()
    {
        currentComboStep = 0;
        attackBuffered = false;
        if (characterAnimator != null)
        {
            characterAnimator.SetInteger("ComboStep", 0);
        }
        // Para qualquer coroutine de hit que possa estar rodando quando o combo � resetado
        if (activeHitDetectionCoroutine != null)
        {
            StopCoroutine(activeHitDetectionCoroutine);
            activeHitDetectionCoroutine = null;
            //Debug.Log("ResetCombo: Coroutine de hit detection parada.");
        }
        //Debug.Log($"Combo resetado. CurrentStep: {currentComboStep}");
    }
}