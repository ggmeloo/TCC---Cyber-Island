using UnityEngine;

public class ControladorInventario : MonoBehaviour
{
    public GameObject painelInventario;

    // <<< MUDANÇA 1: Referência à câmera foi REMOVIDA >>>
    // Não precisamos mais falar diretamente com a câmera.
    // public ThirdPersonOrbitCamera scriptDaCamera; 

    private bool inventarioAberto = false;

    void Start()
    {
        // Garante que o inventário comece fechado
        painelInventario.SetActive(false);

        // O EstadoJogador já cuida do estado inicial do cursor e da câmera,
        // então não precisamos configurar nada aqui.
    }

    void Update()
    {
        // Permite abrir/fechar o inventário com TAB, mas apenas se não estivermos já no modo de UI
        // por outro motivo (como o baú aberto). Isso evita conflitos.
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // Se o inventário está aberto E o modo UI está ativo, então podemos fechá-lo.
            if (inventarioAberto && EstadoJogador.instance.EmModoUI)
            {
                FecharInventario();
            }
            // Só podemos abrir se o modo UI ainda não estiver ativo.
            else if (!EstadoJogador.instance.EmModoUI)
            {
                AbrirInventario();
            }
        }
    }

    void AbrirInventario()
    {
        inventarioAberto = true;
        painelInventario.SetActive(true);

        // <<< MUDANÇA 2: Usamos o EstadoJogador >>>
        // Avisa ao sistema central para entrar no modo de UI (travar câmera, mostrar cursor, etc.)
        EstadoJogador.instance.AtivarModoUI();
    }

    void FecharInventario()
    {
        inventarioAberto = false;
        painelInventario.SetActive(false);

        // <<< MUDANÇA 3: Usamos o EstadoJogador >>>
        // Avisa ao sistema central para sair do modo de UI e voltar ao jogo.
        EstadoJogador.instance.DesativarModoUI();
    }
}