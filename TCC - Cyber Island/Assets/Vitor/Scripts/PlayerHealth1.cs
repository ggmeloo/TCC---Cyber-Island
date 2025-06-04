using UnityEngine;
using UnityEngine.UI;

public class HealthSystem : MonoBehaviour
{
    [Header("Configura��es de Vida")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Refer�ncias UI")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image healthFill;
    [SerializeField] private Text healthText;

    [Header("Cores")]
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color lowHealthColor = Color.red;

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;

        // Garante que a vida n�o fique negativa
        currentHealth = Mathf.Max(0, currentHealth);

        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int healAmount)
    {
        currentHealth += healAmount;

        // Garante que a vida n�o ultrapasse o m�ximo
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (healthFill != null)
        {
            // Interpola a cor entre verde e vermelho baseado na porcentagem de vida
            healthFill.color = Color.Lerp(lowHealthColor, fullHealthColor, (float)currentHealth / maxHealth);
        }

        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
        }
    }

    private void Die()
    {
        Debug.Log("O jogador morreu!");
        // Aqui voc� pode adicionar l�gica de game over, anima��o de morte, etc.
    }
}