// EnemyHealth.cs
using UnityEngine;
using System.Collections; // Necess�rio para IEnumerator

public class EnemyHealth : MonoBehaviour
{
    [Header("Refer�ncias")]
    [Tooltip("Animator do modelo do inimigo.")]
    public Animator characterAnimator;
    [Tooltip("Script de movimento do inimigo para par�-lo ao tomar hit ou morrer.")]
    public EnemyMovement enemyMovement; // Refer�ncia ao script de movimento

    [Header("Configura��es de Vida e Dano")]
    public int maxHealth = 100;
    [SerializeField] private int currentHealth; // Use [SerializeField] para ver no Inspector, mas manter privado
    public GameObject damageTextPrefab; // Prefab para o texto de dano flutuante
    public Transform damageTextSpawnPoint; // Ponto onde o texto de dano aparece
    public float hitStunDuration = 0.5f; // Dura��o que o inimigo fica "atordoado" ao levar hit

    private Coroutine hitStunCoroutine;
    private bool isDead = false;

    void Awake()
    {
        currentHealth = maxHealth;

        if (characterAnimator == null)
        {
            characterAnimator = GetComponentInChildren<Animator>();
            if (characterAnimator == null) Debug.LogWarning($"[{gameObject.name}] EnemyHealth: Animator n�o encontrado.", this);
        }
        if (enemyMovement == null)
        {
            enemyMovement = GetComponent<EnemyMovement>();
            if (enemyMovement == null) Debug.LogError($"[{gameObject.name}] EnemyHealth: Script EnemyMovement n�o encontrado! Funcionalidades de hit/morte podem falhar.", this);
        }
        if (damageTextPrefab == null)
            Debug.LogWarning($"[{gameObject.name}] EnemyHealth: DamageTextPrefab n�o atribu�do.", this);
        if (damageTextSpawnPoint == null)
        {
            damageTextSpawnPoint = transform; // Fallback
        }
    }

    public bool IsDead()
    {
        return isDead;
    }

    public void TakeDamage(int damageAmount, bool isCritical)
    {
        if (isDead)
        {
            // Debug.Log($"[{gameObject.name}] EnemyHealth: J� est� morto, ignorando dano.");
            return;
        }

        currentHealth -= damageAmount;
        Debug.Log($"[{gameObject.name}] EnemyHealth: Tomou {damageAmount} de dano. Cr�tico: {isCritical}. Vida restante: {currentHealth}");

        if (damageTextPrefab != null)
        {
            ShowDamageText(damageAmount, isCritical);
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        else
        {
            // Entra no estado de HIT se n�o morreu
            if (hitStunCoroutine != null) StopCoroutine(hitStunCoroutine);
            hitStunCoroutine = StartCoroutine(HitStunSequence());
        }
    }

    IEnumerator HitStunSequence()
    {
        Debug.Log($"[{gameObject.name}] EnemyHealth: Iniciando HitStunSequence.");
        if (enemyMovement != null) enemyMovement.EnterHitState(); // Avisa o script de movimento

        if (characterAnimator != null)
        {
            // Debug.Log($"[{gameObject.name}] EnemyHealth: Disparando Trigger 'Hit' no Animator.");
            characterAnimator.SetTrigger("Hit");
        }

        yield return new WaitForSeconds(hitStunDuration);
        Debug.Log($"[{gameObject.name}] EnemyHealth: Stun de {hitStunDuration}s terminado.");

        if (!isDead) // S� sai do hit stun se ainda estiver vivo
        {
            if (enemyMovement != null) enemyMovement.ExitHitState(); // Avisa o script de movimento para retomar
        }
        hitStunCoroutine = null;
    }

    void ShowDamageText(int damageAmount, bool isCritical)
    {
        if (damageTextPrefab == null || damageTextSpawnPoint == null) return;

        Vector3 spawnPosition = damageTextSpawnPoint.position + Vector3.up * 1.5f;
        GameObject damageTextInstance = Instantiate(damageTextPrefab, spawnPosition, Quaternion.LookRotation(Camera.main.transform.forward));
        // Debug.Log($"[{gameObject.name}] EnemyHealth: Instanciado prefab de dano.");

        DamageText dt = damageTextInstance.GetComponent<DamageText>();
        if (dt != null)
        {
            dt.SetText(damageAmount.ToString(), isCritical);
            // Debug.Log($"[{gameObject.name}] EnemyHealth: Texto '{damageAmount}' (Cr�tico: {isCritical}) definido.");
        }
        else Debug.LogWarning($"[{gameObject.name}] EnemyHealth: Script DamageText n�o encontrado na inst�ncia do prefab.");
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log($"[{gameObject.name}] EnemyHealth: Morrendo.");

        if (hitStunCoroutine != null) StopCoroutine(hitStunCoroutine);

        if (enemyMovement != null) enemyMovement.EnterDeadState(); // Avisa o script de movimento

        if (characterAnimator != null)
        {
            // Debug.Log($"[{gameObject.name}] EnemyHealth: Definindo Bool 'IsDead' para true e Speed para 0 no Animator.");
            characterAnimator.SetBool("IsDead", true);
            characterAnimator.SetFloat("Speed", 0f); // Garante que para de tentar animar movimento
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // O NavMeshAgent � parado pelo EnemyMovement ao entrar no estado DEAD.
        // A destrui��o do objeto pode ser mantida aqui, ou movida para o final de uma anima��o de morte via evento.
        Destroy(gameObject, 5f); // Ajuste este tempo para a dura��o da sua anima��o de morte
        Debug.Log($"[{gameObject.name}] EnemyHealth: Objeto ser� destru�do em 5 segundos.");
    }
}