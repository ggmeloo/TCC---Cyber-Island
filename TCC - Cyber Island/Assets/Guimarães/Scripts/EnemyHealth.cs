using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("Referências")]
    public Animator characterAnimator;
    public EnemyMovement enemyMovement;

    [Header("Configurações de Vida e Dano")]
    public int maxHealth = 100;
    [SerializeField] private int currentHealth;
    public GameObject damageTextPrefab;
    public Transform damageTextSpawnPoint;
    public float hitStunDuration = 0.5f;

    private Coroutine hitStunCoroutine;
    private bool isDead = false;

    void Awake() // Awake é bom para inicializar referências e estado antes do Start de outros scripts
    {
        currentHealth = maxHealth;

        if (characterAnimator == null) characterAnimator = GetComponentInChildren<Animator>();
        if (characterAnimator == null) Debug.LogWarning($"[{gameObject.name}] EnemyHealth: Animator não encontrado.", this);

        if (enemyMovement == null) enemyMovement = GetComponent<EnemyMovement>();
        if (enemyMovement == null) Debug.LogError($"[{gameObject.name}] EnemyHealth: EnemyMovement não encontrado!", this);

        if (damageTextPrefab == null) Debug.LogWarning($"[{gameObject.name}] EnemyHealth: DamageTextPrefab não atribuído.", this);
        if (damageTextSpawnPoint == null) damageTextSpawnPoint = transform;
    }

    public bool IsDead() => isDead;

    // Modificado para aceitar o parâmetro isCritical, embora o inimigo não o use aqui, mantém consistência com o PlayerAttack.
    public void TakeDamage(int damageAmount, bool isCritical) // Adicionado isCritical para consistência
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        //Debug.Log($"[{gameObject.name}] Tomou {damageAmount} de dano (Crítico: {isCritical}). Vida restante: {currentHealth}");

        if (damageTextPrefab != null) ShowDamageText(damageAmount, isCritical);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        else
        {
            if (hitStunCoroutine != null) StopCoroutine(hitStunCoroutine);
            hitStunCoroutine = StartCoroutine(HitStunSequence());
        }
    }

    IEnumerator HitStunSequence()
    {
        if (isDead) yield break; // Não fazer hit stun se já estiver morrendo/morto

        //Debug.Log($"[{gameObject.name}] Iniciando HitStun.");
        if (enemyMovement != null) enemyMovement.EnterHitState();

        if (characterAnimator != null && characterAnimator.runtimeAnimatorController != null) // Verifica se há um controller
        {
            // Verifica se o parâmetro "Hit" existe antes de tentar dispará-lo
            bool hasHitTrigger = false;
            foreach (AnimatorControllerParameter param in characterAnimator.parameters)
            {
                if (param.name == "Hit" && param.type == AnimatorControllerParameterType.Trigger)
                {
                    hasHitTrigger = true;
                    break;
                }
            }
            if (hasHitTrigger)
            {
                characterAnimator.SetTrigger("Hit");
            }
            else Debug.LogWarning($"[{gameObject.name}] EnemyHealth: Animator não possui o Trigger 'Hit'.");
        }

        yield return new WaitForSeconds(hitStunDuration);

        if (!isDead) // Só sai do hit stun se ainda estiver vivo
        {
            if (enemyMovement != null) enemyMovement.ExitHitState();
        }
        hitStunCoroutine = null;
    }

    void ShowDamageText(int damageAmount, bool isCritical)
    {
        if (damageTextPrefab == null || damageTextSpawnPoint == null || Camera.main == null) return;

        Vector3 spawnPosition = damageTextSpawnPoint.position + Random.insideUnitSphere * 0.5f; // Pequeno offset aleatório
        GameObject damageTextInstance = Instantiate(damageTextPrefab, spawnPosition, Quaternion.LookRotation(Camera.main.transform.forward));

        DamageText dt = damageTextInstance.GetComponent<DamageText>();
        if (dt != null) dt.SetText(damageAmount.ToString(), isCritical);
        else Debug.LogWarning($"[{gameObject.name}] Script DamageText não encontrado no prefab de texto de dano.");
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        //Debug.Log($"[{gameObject.name}] Morreu.");

        if (hitStunCoroutine != null) StopCoroutine(hitStunCoroutine);
        if (enemyMovement != null) enemyMovement.EnterDeadState();

        if (characterAnimator != null && characterAnimator.runtimeAnimatorController != null)
        {
            bool hasIsDeadBool = false;
            foreach (AnimatorControllerParameter param in characterAnimator.parameters)
            {
                if (param.name == "IsDead" && param.type == AnimatorControllerParameterType.Bool)
                {
                    hasIsDeadBool = true;
                    break;
                }
            }
            if (hasIsDeadBool) characterAnimator.SetBool("IsDead", true);
            else Debug.LogWarning($"[{gameObject.name}] EnemyHealth: Animator não possui o Bool 'IsDead'.");

            characterAnimator.SetFloat("Speed", 0f); // Garante que animação de movimento pare
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false; // Desabilita colisor para não interagir mais

        // Considerar desabilitar outros scripts aqui também se necessário
        if (GetComponent<EnemyAttack>() != null) GetComponent<EnemyAttack>().enabled = false;


        Destroy(gameObject, 5f); // Ajuste o tempo para a duração da animação de morte
    }
}