using UnityEngine;

public class InteracaoBau : MonoBehaviour
{
    [Header("Configurações da Interface")]
    public GameObject painelBau;
    public KeyCode teclaInteracao = KeyCode.E;

    // <<< MUDANÇA: Renomeamos a variável para ser mais genérica. >>>
    [Header("Prompt de Interação")]
    public GameObject promptVisual;

    [Header("Controle do Jogador")]
    public ThirdPersonOrbitCamera scriptDaCamera;

    private Animator animator;
    private bool jogadorPerto = false;
    private bool bauAberto = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("O objeto do Baú não tem um componente Animator!", this);
        }

        if (painelBau != null) painelBau.SetActive(false);

        // Garante que o prompt visual comece desligado
        if (promptVisual != null) promptVisual.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jogadorPerto = true;
            if (!bauAberto && promptVisual != null)
            {
                promptVisual.SetActive(true);
            }

            if (scriptDaCamera == null)
                scriptDaCamera = FindObjectOfType<ThirdPersonOrbitCamera>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jogadorPerto = false;
            if (promptVisual != null)
            {
                promptVisual.SetActive(false);
            }

            if (bauAberto)
            {
                FecharBau();
            }
        }
    }

    void Update()
    {
        if (jogadorPerto && Input.GetKeyDown(teclaInteracao))
        {
            if (bauAberto)
            {
                FecharBau();
            }
            else
            {
                AbrirBau();
            }
        }
    }

    private void AbrirBau()
    {
        bauAberto = true;
        painelBau.SetActive(true);
        if (promptVisual != null) promptVisual.SetActive(false);

        if (animator != null) animator.SetTrigger("AbrirTrigger");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (scriptDaCamera != null) scriptDaCamera.podeOrbitarComMouse = false;
    }

    private void FecharBau()
    {
        bauAberto = false;
        painelBau.SetActive(false);
        if (jogadorPerto && promptVisual != null)
        {
            promptVisual.SetActive(true);
        }

        if (animator != null) animator.SetTrigger("FecharTrigger");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (scriptDaCamera != null) scriptDaCamera.podeOrbitarComMouse = true;
    }
}