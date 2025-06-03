using UnityEngine;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    [Header("Refer�ncias")]
    public Animator characterAnimator;
    public PlayerMovement playerMovementScript;

    [Header("Configura��es Gerais de Ataque")]
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

    [Header("Configura��es de Ataque Desarmado (Punch)")]
    public Transform unarmedAttackPoint; // Ponto para socos
    public float unarmedAttackRadius = 0.3f;
    public int unarmedBaseDamage = 15;

    [Header("Configura��es de Ataque com Arma Melee")]
    public Transform meleeWeaponAttackPoint; // Ser� definido pelo PlayerPickup ou manualmente
    public float meleeWeaponAttackRadius = 0.6f;
    public int meleeWeaponBaseDamage = 30;
    // Nota: meleeWeaponAttackPoint pode ser null se nenhuma arma melee estiver equipada ou se ela n�o tiver um "WeaponDamagePoint"

    [Header("Configura��es Cr�ticas (Compartilhadas)")]
    public float criticalHitChance = 0.2f;
    public int criticalDamageMultiplier = 2;
    public LayerMask enemyLayer;

    [Tooltip("Percentual da dura��o da anima��o para aplicar dano (0.0 a 1.0).")]
    public float damageApplicationPointNormalized = 0.5f;
    private Coroutine activeHitDetectionCoroutine;

    void Start()
    {
        if (playerMovementScript == null)
        {
            playerMovementScript = GetComponent<PlayerMovement>() ?? GetComponentInParent<PlayerMovement>();
            if (playerMovementScript == null) Debug.LogError("PLAYER ATTACK: PlayerMovement n�o encontrado!");
        }
        if (characterAnimator == null)
        {
            if (playerMovementScript != null && playerMovementScript.characterAnimator != null)
                characterAnimator = playerMovementScript.characterAnimator;
            else
                characterAnimator = GetComponentInChildren<Animator>();
            if (characterAnimator == null) { Debug.LogError("PLAYER ATTACK: Animator n�o encontrado!", this.gameObject); this.enabled = false; return; }
        }

        // Garante que o unarmedAttackPoint tenha um valor padr�o se n�o for definido no inspector
        if (unarmedAttackPoint == null)
        {
            // Cria um ponto padr�o filho do jogador para unarmed
            GameObject defaultUnarmedPoint = new GameObject("DefaultUnarmedAttackPoint");
            defaultUnarmedPoint.transform.SetParent(transform);
            defaultUnarmedPoint.transform.localPosition = new Vector3(0, capsuleCollider != null ? capsuleCollider.height / 2 : 1f, capsuleCollider != null ? capsuleCollider.radius + 0.3f : 0.5f); // Ajuste conforme necess�rio
            unarmedAttackPoint = defaultUnarmedPoint.transform;
            Debug.LogWarning("PLAYER ATTACK: UnarmedAttackPoint n�o atribu�do. Criando um padr�o. Por favor, ajuste sua posi��o.");
        }
        // meleeWeaponAttackPoint � intencionalmente deixado para ser definido por PlayerPickup ou manualmente,
        // pois depende da arma.

        if (targetLockScript == null) targetLockScript = GetComponent<PlayerTargetLock>();
    }
    private CapsuleCollider capsuleCollider; // Adicionado para o ponto padr�o
    void Awake() // Usar Awake para pegar o CapsuleCollider antes do Start
    {
        capsuleCollider = GetComponent<CapsuleCollider>();
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

        if (Input.GetMouseButtonDown(0))
        {
            bool canCurrentlyMove = (playerMovementScript != null) ? playerMovementScript.CanMove : true;
            if (canCurrentlyMove || isAnimatorInAttackState) // Permite buffer se puder mover OU j� estiver atacando
            {
                attackBuffered = true;
            }
        }

        if (attackBuffered)
        {
            bool attackProcessedThisFrame = false;
            if (equippedWeaponAnimType == WeaponAnimType.Unarmed || equippedWeaponAnimType == WeaponAnimType.Melee)
            {
                // Condi��o para iniciar ou continuar combo
                if (!isAnimatorInAttackState || currentComboStep == 0) // Iniciar novo combo
                {
                    currentComboStep = 1;
                    ProcessAttackParametersForAnimator(true);
                    attackProcessedThisFrame = true;
                }
                else if (isAnimatorInAttackState && currentComboStep < maxComboSteps) // Continuar combo
                {
                    currentComboStep++;
                    ProcessAttackParametersForAnimator(true);
                    attackProcessedThisFrame = true;
                }
                else if (isAnimatorInAttackState && currentComboStep == maxComboSteps) // Reiniciar combo se clicou no �ltimo passo
                {
                    currentComboStep = 1;
                    ProcessAttackParametersForAnimator(true);
                    attackProcessedThisFrame = true;
                }
            }
            // (L�gica para Ranged omitida por foco, mas seria aqui)

            if (attackProcessedThisFrame)
            {
                lastSuccessfulAttackInputTime = Time.time;
                attackBuffered = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) EquipWeapon(WeaponAnimType.Unarmed, null);
        // Para Alpha2 (Melee), o PlayerPickup.cs cuidaria de chamar EquipWeapon com o ponto da arma.
        // Se voc� quiser um bot�o de debug para equipar uma "arma melee padr�o" sem pegar:
        // if (Input.GetKeyDown(KeyCode.Alpha2)) EquipWeapon(WeaponAnimType.Melee, algumaReferenciaTransformParaUmaArmaMeleeDebug);
    }

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
        yield return null; yield return null; // Espera Animator atualizar

        if (!characterAnimator.GetCurrentAnimatorStateInfo(0).IsTag(ATTACK_STATE_TAG))
        {
            activeHitDetectionCoroutine = null;
            yield break;
        }

        AnimatorStateInfo stateInfo = characterAnimator.GetCurrentAnimatorStateInfo(0);
        float animationLength = stateInfo.length;
        float waitTime = animationLength * damageApplicationPointNormalized;

        //Debug.Log($"HitDetectionCoroutine para {equippedWeaponAnimType}: Esperando {waitTime:F2}s para hit.");

        if (waitTime > 0.01f) yield return new WaitForSeconds(waitTime);

        if (characterAnimator.GetCurrentAnimatorStateInfo(0).IsTag(ATTACK_STATE_TAG) &&
            (equippedWeaponAnimType == WeaponAnimType.Unarmed || equippedWeaponAnimType == WeaponAnimType.Melee))
        {
            //Debug.Log("HitDetectionCoroutine: Chamando PerformHitDetection.");
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
            // Se meleeWeaponAttackPoint (da arma) n�o foi definido, tenta usar o unarmedAttackPoint como fallback
            // ou um ponto filho espec�fico do jogador para melee, se voc� criar um.
            // Idealmente, meleeWeaponAttackPoint � definido pela arma.
            currentAttackPoint = meleeWeaponAttackPoint != null ? meleeWeaponAttackPoint : unarmedAttackPoint; // Fallback para unarmed se weapon point for null
            currentAttackRadius = meleeWeaponAttackRadius;
            currentBaseDamage = meleeWeaponBaseDamage;

            if (meleeWeaponAttackPoint == null)
            {
                Debug.LogWarning($"PLAYER ATTACK (Melee): meleeWeaponAttackPoint � null. Usando unarmedAttackPoint ({unarmedAttackPoint.name}) como fallback. Certifique-se de que a arma melee tem 'WeaponDamagePoint' ou defina meleeWeaponAttackPoint manualmente.");
            }
        }
        else // Ranged ou outros tipos
        {
            return;
        }

        if (currentAttackPoint == null)
        {
            Debug.LogError($"PLAYER ATTACK: currentAttackPoint � NULL para {equippedWeaponAnimType}. Dano n�o ser� aplicado.");
            return;
        }

        //Debug.Log($"PerformHitDetection para {equippedWeaponAnimType}. Ponto: {currentAttackPoint.name} ({currentAttackPoint.position}), Raio: {currentAttackRadius}");

        Collider[] hitColliders = Physics.OverlapSphere(currentAttackPoint.position, currentAttackRadius, enemyLayer);
        if (hitColliders.Length == 0 && (equippedWeaponAnimType == WeaponAnimType.Melee || equippedWeaponAnimType == WeaponAnimType.Unarmed))
        {
            //Debug.Log($"Nenhum inimigo detectado no OverlapSphere para {equippedWeaponAnimType}.");
        }

        foreach (Collider hitEnemyCollider in hitColliders)
        {
            //Debug.Log($"Inimigo detectado: {hitEnemyCollider.name} com {equippedWeaponAnimType}");
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
            if (isCritical) damageToDeal = Mathf.RoundToInt(damageToDeal * criticalDamageMultiplier); // Arredonda para inteiro
            //Debug.Log($"PLAYER: Dano de {damageToDeal} (Base: {baseDamageToApply}, Cr�tico: {isCritical}) em {hitEnemyObject.name}.");
            enemyAI.TakeDamage(damageToDeal, isCritical);
        }
    }

    // M�todo p�blico para PlayerPickup definir o ponto de ataque da arma melee
    public void SetMeleeWeaponAttackPoint(Transform weaponPoint)
    {
        meleeWeaponAttackPoint = weaponPoint;
        if (weaponPoint != null)
        {
            //Debug.Log($"PlayerAttack: meleeWeaponAttackPoint definido para {weaponPoint.name}");
        }
        else
        {
            //Debug.Log("PlayerAttack: meleeWeaponAttackPoint limpo (null).");
        }
    }


    public void EquipWeapon(WeaponAnimType type, Transform specificWeaponDamagePoint)
    {
        equippedWeaponAnimType = type;
        ResetCombo(); // Sempre reseta combo ao trocar de equipamento/tipo
        //Debug.Log($"Arma equipada: {type}");

        if (type == WeaponAnimType.Melee)
        {
            SetMeleeWeaponAttackPoint(specificWeaponDamagePoint);
            // Se specificWeaponDamagePoint for null, PlayerAttack usar� seu unarmedAttackPoint como fallback
            // ou voc� pode definir um meleeWeaponAttackPoint padr�o no Inspector do PlayerAttack.
            if (specificWeaponDamagePoint == null && meleeWeaponAttackPoint == null)
            {
                //Debug.LogWarning("EquipWeapon (Melee): Nenhum specificWeaponDamagePoint fornecido e o meleeWeaponAttackPoint padr�o tamb�m � nulo. O ataque melee pode n�o funcionar corretamente.");
            }
        }
        else
        {
            // Para Unarmed ou Ranged, n�o precisamos do specificWeaponDamagePoint dessa forma,
            // ent�o podemos limpar o meleeWeaponAttackPoint para evitar confus�o.
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
        // Gizmo para Ataque Desarmado
        if (unarmedAttackPoint != null && equippedWeaponAnimType == WeaponAnimType.Unarmed) // Mostra apenas se unarmed estiver ativo
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(unarmedAttackPoint.position, unarmedAttackRadius);
        }

        // Gizmo para Ataque com Arma Melee
        // Mostra o gizmo do meleeWeaponAttackPoint se ele estiver definido E o tipo de arma for Melee.
        // Se meleeWeaponAttackPoint for null, mas estivermos em Melee, mostra o gizmo do unarmedAttackPoint como fallback (para visualiza��o).
        if (equippedWeaponAnimType == WeaponAnimType.Melee)
        {
            Gizmos.color = Color.red;
            if (meleeWeaponAttackPoint != null)
            {
                Gizmos.DrawWireSphere(meleeWeaponAttackPoint.position, meleeWeaponAttackRadius);
            }
            else if (unarmedAttackPoint != null) // Visualiza o fallback se o ponto da arma n�o estiver definido
            {
                Gizmos.color = new Color(1f, 0.5f, 0.5f, 0.7f); // Um vermelho mais claro para indicar fallback
                Gizmos.DrawWireSphere(unarmedAttackPoint.position, meleeWeaponAttackRadius); // Usa o raio do melee, mas ponto do unarmed
                // Isso ajuda a ver onde o ataque "aconteceria" se o ponto da arma n�o estiver configurado.
            }
        }
    }
}