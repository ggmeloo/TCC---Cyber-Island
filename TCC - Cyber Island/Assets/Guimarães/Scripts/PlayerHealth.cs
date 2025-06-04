using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Configurações de Vida")]
    public int maxHealth = 100;
    [SerializeField] private int currentHealth;
    public bool isInvincible = false;
    public float invincibilityDuration = 1f;

    [Header("Configurações de UI")]
    public Slider healthSlider;
    public Image healthFill;
    public Gradient healthGradient;
    public Text healthText;
    public Image damageEffect;

    [Header("Configurações de Morte")]
    public GameObject deathEffectPrefab;
    public float delayBeforeRestart = 3f;
    public string gameOverSceneName = "GameOver";

    void Awake()
    {
        currentHealth = maxHealth;
        InitializeHealthUI();
        Debug.Log($"[{gameObject.name}] PlayerHealth: Vida inicializada em {currentHealth}");
    }

    void InitializeHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (healthFill != null && healthGradient != null)
        {
            healthFill.color = healthGradient.Evaluate(1f);
        }

        UpdateHealthText();
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public void TakeDamage(int damageAmount)
    {
        if (isInvincible || currentHealth <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - damageAmount);
        Debug.Log($"[{gameObject.name}] PlayerHealth: Tomou {damageAmount} de dano. Vida restante: {currentHealth}");

        UpdateHealthUI();

        if (damageEffect != null)
        {
            StartCoroutine(FlashDamageEffect());
        }

        if (currentHealth == 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibilityCoroutine(invincibilityDuration));
        }
    }

    public void Heal(int healAmount)
    {
        if (currentHealth <= 0) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        Debug.Log($"[{gameObject.name}] PlayerHealth: Curou {healAmount}. Vida atual: {currentHealth}");

        UpdateHealthUI();
    }

    void Die()
    {
        Debug.Log($"[{gameObject.name}] PlayerHealth: MORREU!");

        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        }

        if (TryGetComponent(out Animator animator))
        {
            animator.SetBool("IsDead", true);
        }

        if (TryGetComponent(out PlayerMovement movement))
        {
            movement.enabled = false;
        }

        if (TryGetComponent(out Collider2D collider))
        {
            collider.enabled = false;
        }

        if (!string.IsNullOrEmpty(gameOverSceneName))
        {
            Invoke("LoadGameOverScene", delayBeforeRestart);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        if (healthFill != null && healthGradient != null)
        {
            float healthPercentage = (float)currentHealth / maxHealth;
            healthFill.color = healthGradient.Evaluate(healthPercentage);
        }

        UpdateHealthText();
    }

    void UpdateHealthText()
    {
        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
        }
    }

    IEnumerator FlashDamageEffect()
    {
        if (damageEffect != null)
        {
            damageEffect.enabled = true;
            yield return new WaitForSeconds(0.1f);
            damageEffect.enabled = false;
        }
    }

    IEnumerator InvincibilityCoroutine(float duration)
    {
        isInvincible = true;

        // Efeito de piscar durante a invencibilidade
        if (TryGetComponent(out SpriteRenderer spriteRenderer))
        {
            float elapsedTime = 0f;
            float blinkSpeed = 0.1f;

            while (elapsedTime < duration)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
                yield return new WaitForSeconds(blinkSpeed);
                elapsedTime += blinkSpeed;
            }

            spriteRenderer.enabled = true;
        }
        else
        {
            yield return new WaitForSeconds(duration);
        }

        isInvincible = false;
    }

    void LoadGameOverScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameOverSceneName);
    }

    // Método para resetar a vida (útil quando reinicia o nível)
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();

        if (TryGetComponent(out Animator animator))
        {
            animator.SetBool("IsDead", false);
        }

        if (TryGetComponent(out PlayerMovement movement))
        {
            movement.enabled = true;
        }

        if (TryGetComponent(out Collider2D collider))
        {
            collider.enabled = true;
        }
    }
}