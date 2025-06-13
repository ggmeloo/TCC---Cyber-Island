using UnityEngine;

public class ControladorInventario : MonoBehaviour
{
    public GameObject painelInventario;

    // <<< MUDAN�A 1: Refer�ncia � c�mera foi REMOVIDA >>>
    // N�o precisamos mais falar diretamente com a c�mera.
    // public ThirdPersonOrbitCamera scriptDaCamera; 

    private bool inventarioAberto = false;

    void Start()
    {
        // Garante que o invent�rio comece fechado
        painelInventario.SetActive(false);

        // O EstadoJogador j� cuida do estado inicial do cursor e da c�mera,
        // ent�o n�o precisamos configurar nada aqui.
    }

    void Update()
    {
        // Permite abrir/fechar o invent�rio com TAB, mas apenas se n�o estivermos j� no modo de UI
        // por outro motivo (como o ba� aberto). Isso evita conflitos.
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // Se o invent�rio est� aberto E o modo UI est� ativo, ent�o podemos fech�-lo.
            if (inventarioAberto && EstadoJogador.instance.EmModoUI)
            {
                FecharInventario();
            }
            // S� podemos abrir se o modo UI ainda n�o estiver ativo.
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

        // <<< MUDAN�A 2: Usamos o EstadoJogador >>>
        // Avisa ao sistema central para entrar no modo de UI (travar c�mera, mostrar cursor, etc.)
        EstadoJogador.instance.AtivarModoUI();
    }

    void FecharInventario()
    {
        inventarioAberto = false;
        painelInventario.SetActive(false);

        // <<< MUDAN�A 3: Usamos o EstadoJogador >>>
        // Avisa ao sistema central para sair do modo de UI e voltar ao jogo.
        EstadoJogador.instance.DesativarModoUI();
    }
}