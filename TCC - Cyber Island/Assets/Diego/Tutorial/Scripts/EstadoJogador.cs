using UnityEngine;

public class EstadoJogador : MonoBehaviour
{
    // Padrão Singleton: garante que só existe uma instância deste script no jogo.
    public static EstadoJogador instance;

    // Propriedade para verificar se estamos em modo de UI.
    // Outros scripts podem ler (get), mas só este script pode alterar (private set).
    public bool EmModoUI { get; private set; }

    void Awake()
    {
        // Configuração do Singleton
        if (instance == null)
        {
            instance = this;
            // DontDestroyOnLoad(gameObject); // Opcional: descomente se precisar que ele persista entre cenas.
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Começa o jogo no modo normal (sem UI)
        DesativarModoUI();
    }

    // Chamado quando uma UI é aberta (inventário, menu de pausa, etc.)
    public void AtivarModoUI()
    {
        EmModoUI = true;
        Cursor.lockState = CursorLockMode.None; // Libera o cursor do centro da tela.
        Cursor.visible = true; // Torna o cursor visível.
    }

    // Chamado quando a UI é fechada para retornar ao gameplay.
    public void DesativarModoUI()
    {
        EmModoUI = false;
        Cursor.lockState = CursorLockMode.Locked; // Trava o cursor no centro da tela.
        Cursor.visible = false; // Esconde o cursor.
    }
}