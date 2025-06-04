using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    // Configurações de vida
    [Header("Vida")]
    public float maxHealth = 100f;
    public float currentHealth;
    public Slider healthSlider;
    public Image healthFill;
    public Color fullHealthColor = Color.green;
    public Color lowHealthColor = Color.red;
    public float healthRegenRate = 0f; // Regeneração por segundo

    // Configurações de estamina
    [Header("Estamina")]
    public float maxStamina = 100f;
    public float currentStamina;
    public Slider staminaSlider;
    public Image staminaFill;
    public Color fullStaminaColor = Color.blue;
    public Color lowStaminaColor = Color.yellow;
    public float staminaRegenRate = 10f; // Regeneração por segundo
    public float staminaDrainRate = 20f; // Consumo por segundo ao correr
    public float staminaCooldown = 1f; // Tempo antes de começar a regenerar após uso
    private float staminaCooldownTimer;

    void Start()
    {
        // Inicializa valores
        currentHealth = maxHealth;
        currentStamina = maxStamina;

        // Configura os sliders
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = currentStamina;
        }
    }

    void Update()
    {
        // Atualiza a regeneração de vida
        if (currentHealth < maxHealth && healthRegenRate > 0)
        {
            currentHealth += healthRegenRate * Time.deltaTime;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            UpdateHealthUI();
        }

        // Lógica de regeneração de estamina
        if (staminaCooldownTimer > 0)
        {
            staminaCooldownTimer -= Time.deltaTime;
        }
        else if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
            UpdateStaminaUI();
        }

        // Exemplo de uso de estamina (corrida)
        if (Input.GetKey(KeyCode.LeftShift)) // Quando segurar Shift
        {
            UseStamina(staminaDrainRate * Time.deltaTime);
        }

        // Exemplo de dano (apenas para teste)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TakeDamage(10);
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthUI();
    }

    public bool UseStamina(float amount)
    {
        if (currentStamina >= amount)
        {
            currentStamina -= amount;
            staminaCooldownTimer = staminaCooldown;
            UpdateStaminaUI();
            return true;
        }
        return false;
    }

    private void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        if (healthFill != null)
        {
            // Muda a cor gradualmente conforme a vida diminui
            healthFill.color = Color.Lerp(lowHealthColor, fullHealthColor, currentHealth / maxHealth);
        }
    }

    private void UpdateStaminaUI()
    {
        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina;
        }

        if (staminaFill != null)
        {
            // Muda a cor gradualmente conforme a estamina diminui
            staminaFill.color = Color.Lerp(lowStaminaColor, fullStaminaColor, currentStamina / maxStamina);
        }
    }

    private void Die()
    {
        Debug.Log("Player morreu!");
        // Aqui você pode adicionar lógica de game over, reiniciar cena, etc.
    }
}