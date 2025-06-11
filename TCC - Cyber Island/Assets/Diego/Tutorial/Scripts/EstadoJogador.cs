using UnityEngine;

public class EstadoJogador : MonoBehaviour
{
    // Padr�o Singleton: garante que s� existe uma inst�ncia deste script no jogo.
    public static EstadoJogador instance;

    // Propriedade para verificar se estamos em modo de UI.
    // Outros scripts podem ler (get), mas s� este script pode alterar (private set).
    public bool EmModoUI { get; private set; }

    void Awake()
    {
        // Configura��o do Singleton
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
        // Come�a o jogo no modo normal (sem UI)
        DesativarModoUI();
    }

    // Chamado quando uma UI � aberta (invent�rio, menu de pausa, etc.)
    public void AtivarModoUI()
    {
        EmModoUI = true;
        Cursor.lockState = CursorLockMode.None; // Libera o cursor do centro da tela.
        Cursor.visible = true; // Torna o cursor vis�vel.
    }

    // Chamado quando a UI � fechada para retornar ao gameplay.
    public void DesativarModoUI()
    {
        EmModoUI = false;
        Cursor.lockState = CursorLockMode.Locked; // Trava o cursor no centro da tela.
        Cursor.visible = false; // Esconde o cursor.
    }
}