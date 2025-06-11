using UnityEngine;

public class InteracaoBau : MonoBehaviour
{
    [Header("Referências da UI")]
    public GameObject painelDeInteracaoUI;
    public GameObject painelInventarioBau;

    [Header("Referências de Animação")]
    public Animator animatorBau;

    private bool jogadorEstaPerto = false;
    private bool inventarioEstaAberto = false;
    private const string PARAMETRO_ANIMACAO_ABERTO = "EstaAberto";

    void Start()
    {
        if (painelDeInteracaoUI != null) painelDeInteracaoUI.SetActive(false);
        if (painelInventarioBau != null) painelInventarioBau.SetActive(false);
        AtualizarAnimacao();
    }

    void Update()
    {
        if (jogadorEstaPerto && Input.GetKeyDown(KeyCode.E))
        {
            inventarioEstaAberto = !inventarioEstaAberto;

            // Ativa/desativa a UI do inventário
            if (painelInventarioBau != null)
            {
                painelInventarioBau.SetActive(inventarioEstaAberto);
            }

            // Mostra/esconde o prompt de interação
            if (painelDeInteracaoUI != null)
            {
                painelDeInteracaoUI.SetActive(!inventarioEstaAberto);
            }

            // <<< MUDANÇA PRINCIPAL AQUI >>>
            if (inventarioEstaAberto)
            {
                // Avisa ao gerenciador para entrar no modo de UI
                EstadoJogador.instance.AtivarModoUI();
            }
            else
            {
                // Avisa ao gerenciador para sair do modo de UI e voltar ao jogo
                EstadoJogador.instance.DesativarModoUI();
            }
            // ---------------------------------

            AtualizarAnimacao();
        }
    }

    // ... (OnTriggerEnter não precisa de mudança) ...
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jogadorEstaPerto = true;
            if (!inventarioEstaAberto && painelDeInteracaoUI != null)
            {
                painelDeInteracaoUI.SetActive(true);
            }
        }
    }


    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jogadorEstaPerto = false;

            if (painelDeInteracaoUI != null)
            {
                painelDeInteracaoUI.SetActive(false);
            }

            if (inventarioEstaAberto)
            {
                inventarioEstaAberto = false;
                if (painelInventarioBau != null)
                {
                    painelInventarioBau.SetActive(false);
                }

                // <<< MUDANÇA AQUI TAMBÉM >>>
                // Garante que, ao sair, o modo de UI seja desativado
                EstadoJogador.instance.DesativarModoUI();
                // ---------------------------

                AtualizarAnimacao();
            }
        }
    }

    private void AtualizarAnimacao()
    {
        if (animatorBau != null)
        {
            animatorBau.SetBool(PARAMETRO_ANIMACAO_ABERTO, inventarioEstaAberto);
        }
    }
}