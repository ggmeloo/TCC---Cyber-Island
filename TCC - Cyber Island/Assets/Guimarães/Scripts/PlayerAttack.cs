using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems; // <<< MUDANÇA 1: Adicionado o namespace necessário

public class PlayerAttack : MonoBehaviour
{
    [Header("Referências")]
    public Animator characterAnimator;
    public PlayerMovement playerMovementScript;

    [Header("Configurações Gerais de Ataque")]
    public int maxComboSteps = 3;
    private int currentComboStep = 0;
    private float lastSuccessfulAttackInputTime = 0f;
    public float comboResetTime = 2.0f;
    private bool attackBuffered = false;
    private bool isAnimatorInAttackState = false;
    private bool wasPreviouslyInAttackState = false;

    public PlayerTargetLock targetLockScript;

    public enum WeaponAnimType { Unarmed = 0, Melee = 1, Ranged = 2 }
    public WeaponAnimType equippedWeaponAnimType = WeaponAnimType.Unarmed;
    private const string ATTACK_STATE_TAG = "Attack";

    [Header("Configurações de Ataque Desarmado (Punch)")]
    public Transform unarmedAttackPoint;
    public float unarmedAttackRadius = 0.3f;
    public int unarmedBaseDamage = 15;

    [Header("Configurações de Ataque com Arma Melee")]
    public Transform meleeWeaponAttackPoint;
    public float meleeWeaponAttackRadius = 0.6f;
    public int meleeWeaponBaseDamage = 30;

    [Header("Configurações Críticas (Compartilhadas)")]
    public float criticalHitChance = 0.2f;
    public int criticalDamageMultiplier = 2;
    public LayerMask enemyLayer;

    [Tooltip("Percentual da duração da animação para aplicar dano (0.0 a 1.0).")]
    public float damageApplicationPointNormalized = 0.5f;
    private Coroutine activeHitDetectionCoroutine;
    private CapsuleCollider capsuleCollider;

    void Awake()
    {
        capsuleCollider = GetComponent<CapsuleCollider>();
    }

    void Start()
    {
        if (playerMovementScript == null)
        {
            playerMovementScript = GetComponent<PlayerMovement>() ?? GetComponentInParent<PlayerMovement>();
            if (playerMovementScript == null) Debug.LogError("PLAYER ATTACK: PlayerMovement não encontrado!");
        }
        if (characterAnimator == null)
        {
            if (playerMovementScript != null && playerMovementScript.characterAnimator != null)
                characterAnimator = playerMovementScript.characterAnimator;
            else
                characterAnimator = GetComponentInChildren<Animator>();
            if (characterAnimator == null) { Debug.LogError("PLAYER ATTACK: Animator não encontrado!", this.gameObject); this.enabled = false; return; }
        }

        if (unarmedAttackPoint == null)
        {
            GameObject defaultUnarmedPoint = new GameObject("DefaultUnarmedAttackPoint");
            defaultUnarmedPoint.transform.SetParent(transform);
            defaultUnarmedPoint.transform.localPosition = new Vector3(0, capsuleCollider != null ? capsuleCollider.height / 2 : 1f, capsuleCollider != null ? capsuleCollider.radius + 0.3f : 0.5f);
            unarmedAttackPoint = defaultUnarmedPoint.transform;
            Debug.LogWarning("PLAYER ATTACK: UnarmedAttackPoint não atribuído. Criando um padrão. Por favor, ajuste sua posição.");
        }

        if (targetLockScript == null) targetLockScript = GetComponent<PlayerTargetLock>();
    }

    void Update()
    {
        if (characterAnimator == null) return;

        wasPreviouslyInAttackState = isAnimatorInAttackState;
        isAnimatorInAttackState = characterAnimator.GetCurrentAnimatorStateInfo(0).IsTag(ATTACK_STATE_TAG);

        if (playerMovementScript != null)
        {
            if (isAnimatorInAttackState && !wasPreviouslyInAttackState)
                playerMovementScript.SetCanMove(false);
            else if (!isAnimatorInAttackState && wasPreviouslyInAttackState)
                playerMovementScript.SetCanMove(true);
        }

        if (wasPreviouslyInAttackState && !isAnimatorInAttackState && activeHitDetectionCoroutine != null)
        {
            StopCoroutine(activeHitDetectionCoroutine);
            activeHitDetectionCoroutine = null;
        }

        if (currentComboStep > 0 && !isAnimatorInAttackState && Time.time > lastSuccessfulAttackInputTime + comboResetTime)
            ResetCombo();

        // <<< MUDANÇA 2: Adicionada a verificação da UI
        // Se o cursor do mouse estiver sobre um elemento de UI (como o inventário),
        // não processe o input de ataque.
        if (EventSystem.current.IsPointerOverGameObject())
        {
            // Opcional: Se quiser que um clique na UI cancele o buffer de ataque, descomente a linha abaixo
            // attackBuffered = false; 
            return; // Impede que o código de ataque abaixo seja executado.
        }

        if (Input.GetMouseButtonDown(0))
        {
            bool canCurrentlyMove = (playerMovementScript != null) ? playerMovementScript.CanMove : true;
            if (canCurrentlyMove || isAnimatorInAttackState)
            {
                attackBuffered = true;
            }
        }

        if (attackBuffered)
        {
            bool attackProcessedThisFrame = false;
            if (equippedWeaponAnimType == WeaponAnimType.Unarmed || equippedWeaponAnimType == WeaponAnimType.Melee)
            {
                if (!isAnimatorInAttackState || currentComboStep == 0)
                {
                    currentComboStep = 1;
                    ProcessAttackParametersForAnimator(true);
                    attackProcessedThisFrame = true;
                }
                else if (isAnimatorInAttackState && currentComboStep < maxComboSteps)
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

            if (attackProcessedThisFrame)
            {
                lastSuccessfulAttackInputTime = Time.time;
                attackBuffered = false;
            }
        }

        // Seus botões de debug
        if (Input.GetKeyDown(KeyCode.Alpha1)) EquipWeapon(WeaponAnimType.Unarmed, null);
    }

    // O resto do seu código permanece exatamente o mesmo, não precisa mudar mais nada.
    // ... (ProcessAttackParametersForAnimator, HitDetectionCoroutine, etc.)
    void ProcessAttackParametersForAnimator(bool shouldAttemptHitDetection)
    {
        if (characterAnimator == null) return;
        characterAnimator.SetInteger("WeaponType", (int)equippedWeaponAnimType);
        characterAnimator.SetInteger("ComboStep", currentComboStep);
        characterAnimator.SetTrigger("AttackInput");

        if (shouldAttemptHitDetection &&
            (equippedWeaponAnimType == WeaponAnimType.Unarmed || equippedWeaponAnimType == WeaponAnimType.Melee))
        {
            if (activeHitDetectionCoroutine != null) StopCoroutine(activeHitDetectionCoroutine);
            activeHitDetectionCoroutine = StartCoroutine(HitDetectionCoroutine());
        }
    }

    IEnumerator HitDetectionCoroutine()
    {
        yield return null; yield return null;

        if (!characterAnimator.GetCurrentAnimatorStateInfo(0).IsTag(ATTACK_STATE_TAG))
        {
            activeHitDetectionCoroutine = null;
            yield break;
        }

        AnimatorStateInfo stateInfo = characterAnimator.GetCurrentAnimatorStateInfo(0);
        float animationLength = stateInfo.length;
        float waitTime = animationLength * damageApplicationPointNormalized;

        if (waitTime > 0.01f) yield return new WaitForSeconds(waitTime);

        if (characterAnimator.GetCurrentAnimatorStateInfo(0).IsTag(ATTACK_STATE_TAG) &&
            (equippedWeaponAnimType == WeaponAnimType.Unarmed || equippedWeaponAnimType == WeaponAnimType.Melee))
        {
            PerformHitDetection();
        }
        activeHitDetectionCoroutine = null;
    }

    public void PerformHitDetection()
    {
        Transform currentAttackPoint = null;
        float currentAttackRadius = 0f;
        int currentBaseDamage = 0;

        if (equippedWeaponAnimType == WeaponAnimType.Unarmed)
        {
            currentAttackPoint = unarmedAttackPoint;
            currentAttackRadius = unarmedAttackRadius;
            currentBaseDamage = unarmedBaseDamage;
        }
        else if (equippedWeaponAnimType == WeaponAnimType.Melee)
        {
            currentAttackPoint = meleeWeaponAttackPoint != null ? meleeWeaponAttackPoint : unarmedAttackPoint;
            currentAttackRadius = meleeWeaponAttackRadius;
            currentBaseDamage = meleeWeaponBaseDamage;

            if (meleeWeaponAttackPoint == null)
            {
                Debug.LogWarning($"PLAYER ATTACK (Melee): meleeWeaponAttackPoint é null. Usando unarmedAttackPoint ({unarmedAttackPoint.name}) como fallback.");
            }
        }
        else
        {
            return;
        }

        if (currentAttackPoint == null)
        {
            Debug.LogError($"PLAYER ATTACK: currentAttackPoint é NULL para {equippedWeaponAnimType}.");
            return;
        }

        Collider[] hitColliders = Physics.OverlapSphere(currentAttackPoint.position, currentAttackRadius, enemyLayer);
        foreach (Collider hitEnemyCollider in hitColliders)
        {
            ApplyDamageToEnemy(hitEnemyCollider.gameObject, currentBaseDamage);
        }
    }

    void ApplyDamageToEnemy(GameObject hitEnemyObject, int baseDamageToApply)
    {
        if (hitEnemyObject == null) return;
        EnemyHealth enemyAI = hitEnemyObject.GetComponent<EnemyHealth>();
        if (enemyAI != null)
        {
            bool isCritical = Random.value < criticalHitChance;
            int damageToDeal = baseDamageToApply;
            if (isCritical) damageToDeal = Mathf.RoundToInt(damageToDeal * criticalDamageMultiplier);
            enemyAI.TakeDamage(damageToDeal, isCritical);
        }
    }

    public void SetMeleeWeaponAttackPoint(Transform weaponPoint)
    {
        meleeWeaponAttackPoint = weaponPoint;
    }

    public void EquipWeapon(WeaponAnimType type, Transform specificWeaponDamagePoint)
    {
        equippedWeaponAnimType = type;
        ResetCombo();

        if (type == WeaponAnimType.Melee)
        {
            SetMeleeWeaponAttackPoint(specificWeaponDamagePoint);
        }
        else
        {
            SetMeleeWeaponAttackPoint(null);
        }
    }

    public void ResetCombo()
    {
        currentComboStep = 0;
        attackBuffered = false;
        if (characterAnimator != null)
            characterAnimator.SetInteger("ComboStep", 0);
        if (activeHitDetectionCoroutine != null)
        {
            StopCoroutine(activeHitDetectionCoroutine);
            activeHitDetectionCoroutine = null;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (unarmedAttackPoint != null && equippedWeaponAnimType == WeaponAnimType.Unarmed)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(unarmedAttackPoint.position, unarmedAttackRadius);
        }

        if (equippedWeaponAnimType == WeaponAnimType.Melee)
        {
            Gizmos.color = Color.red;
            if (meleeWeaponAttackPoint != null)
            {
                Gizmos.DrawWireSphere(meleeWeaponAttackPoint.position, meleeWeaponAttackRadius);
            }
            else if (unarmedAttackPoint != null)
            {
                Gizmos.color = new Color(1f, 0.5f, 0.5f, 0.7f);
                Gizmos.DrawWireSphere(unarmedAttackPoint.position, meleeWeaponAttackRadius);
            }
        }
    }
}