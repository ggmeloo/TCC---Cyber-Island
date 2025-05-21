// PlayerHealth.cs
using UnityEngine;
using System.Collections;
// using UnityEngine.UI; // Para futuras barras de vida na UI

public class PlayerHealth : MonoBehaviour
{
    [Header("Configura��es de Vida")]
    public int maxHealth = 100;
    [SerializeField] private int currentHealth; // Para ver no Inspector
    public bool isInvincible = false; // Para adicionar iframes se desejar

    // --- Para UI de Barra de Vida (Exemplo Futuro) ---
    // public Image healthBarFill;
    // -------------------------------------------------

    // --- Para Efeitos de Morte/Game Over (Exemplo Futuro) ---
    // public GameObject deathEffectPrefab;
    // public float delayBeforeRestart = 3f;
    // -------------------------------------------------------


    void Awake()
    {
        currentHealth = maxHealth;
        Debug.Log($"[{gameObject.name}] PlayerHealth: Vida inicializada em {currentHealth}");
        UpdateHealthUI(); // Chame se voc� j� tiver uma UI
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public void TakeDamage(int damageAmount)
    {
        if (isInvincible)
        {
            Debug.Log($"[{gameObject.name}] PlayerHealth: Dano de {damageAmount} ignorado (invenc�vel).");
            return;
        }
        if (currentHealth <= 0) // J� morto
        {
            return;
        }

        currentHealth -= damageAmount;
        Debug.Log($"[{gameObject.name}] PlayerHealth: Tomou {damageAmount} de dano. Vida restante: {currentHealth}");

        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        UpdateHealthUI(); // Atualiza a barra de vida

        // Voc� pode adicionar um feedback de dano aqui (som, piscar o sprite/modelo)
        // GetComponent<Animator>().SetTrigger("Hit"); // Se o player tiver anima��o de tomar dano

        if (currentHealth == 0)
        {
            Die();
        }
    }

    public void Heal(int healAmount)
    {
        if (currentHealth <= 0) return; // N�o pode curar se morto

        currentHealth += healAmount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        Debug.Log($"[{gameObject.name}] PlayerHealth: Curou {healAmount}. Vida atual: {currentHealth}");
        UpdateHealthUI();
    }


    void Die()
    {
        Debug.Log($"[{gameObject.name}] PlayerHealth: MORREU!");
        // L�gica de Game Over:
        // - Tocar anima��o de morte do player
        // - Mostrar tela de Game Over
        // - Parar o jogo / Recarregar a cena
        // GetComponent<Animator>().SetBool("IsDead", true);
        // Time.timeScale = 0f; // Pausa o jogo (simples)
        // Invoke("RestartLevel", delayBeforeRestart); // Exemplo
        gameObject.SetActive(false); // Exemplo simples de "morte"
    }

    void RestartLevel()
    {
        Time.timeScale = 1f;
        // UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        Debug.Log("REINICIANDO N�VEL (Implementar)");
    }

    void UpdateHealthUI()
    {
        // Se voc� tiver uma barra de vida na UI:
        // if (healthBarFill != null)
        // {
        //     healthBarFill.fillAmount = (float)currentHealth / maxHealth;
        // }
        // Debug.Log($"[{gameObject.name}] PlayerHealth: UI de vida atualizada (Vida: {currentHealth})");
    }

    // Exemplo de como o player poderia se tornar invenc�vel por um tempo ap�s tomar dano
    public void StartInvincibility(float duration)
    {
        StartCoroutine(InvincibilityCoroutine(duration));
    }

    IEnumerator InvincibilityCoroutine(float duration)
    {
        isInvincible = true;
        // Debug.Log("Player Invenc�vel por " + duration + "s");
        // Adicionar feedback visual de invencibilidade (piscar sprite, etc.)
        yield return new WaitForSeconds(duration);
        isInvincible = false;
        // Debug.Log("Player n�o est� mais invenc�vel");
        // Remover feedback visual
    }
}