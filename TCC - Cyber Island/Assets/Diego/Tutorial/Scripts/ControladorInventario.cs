using UnityEngine;

public class ControladorInventario : MonoBehaviour
{
    public GameObject painelInventario;
    public ThirdPersonOrbitCamera scriptDaCamera;

    // (Lembre-se: se você também quiser parar o movimento do personagem, precisará da referência dele aqui)
    // public MovimentoJogador scriptDoJogador;

    private bool inventarioAberto = false;

    void Start()
    {
        // Garante que o inventário comece fechado
        painelInventario.SetActive(false);

        // Garante que a câmera comece podendo orbitar
        if (scriptDaCamera != null) scriptDaCamera.podeOrbitarComMouse = true;

        // Garante que o cursor comece travado e invisível
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (inventarioAberto)
            {
                FecharInventario();
            }
            else
            {
                AbrirInventario();
            }
        }
    }

    void AbrirInventario()
    {
        inventarioAberto = true;
        painelInventario.SetActive(true);
        // Time.timeScale = 0f; // <<< LINHA REMOVIDA

        // Libera e mostra o cursor para usar no inventário
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Trava a rotação da câmera pelo mouse
        if (scriptDaCamera != null)
        {
            scriptDaCamera.podeOrbitarComMouse = false;
        }

        // Se você quisesse parar o movimento do jogador, faria aqui:
        // if(scriptDoJogador != null) scriptDoJogador.podeMover = false;
    }

    void FecharInventario()
    {
        inventarioAberto = false;
        painelInventario.SetActive(false);
        // Time.timeScale = 1f; // <<< LINHA REMOVIDA

        // Trava e esconde o cursor para voltar ao gameplay normal
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Libera a rotação da câmera pelo mouse novamente
        if (scriptDaCamera != null)
        {
            scriptDaCamera.podeOrbitarComMouse = true;
        }

        // Se você tivesse parado o movimento do jogador, liberaria aqui:
        // if(scriptDoJogador != null) scriptDoJogador.podeMover = true;
    }
}