using UnityEngine;
using UnityEngine.SceneManagement; // Essencial para gerenciar cenas!

public class GerenciadorDeCenas : MonoBehaviour
{
    // Esta função será chamada por um botão ou outro evento no jogo
    public void CarregarCena(int buildIndex)
    {
        // Carrega a cena baseada no seu número na lista do Build Settings
        SceneManager.LoadScene(buildIndex);
    }

    // Você também pode carregar pelo nome, se preferir
    public void CarregarCenaPeloNome(string nomeDaCena)
    {
        SceneManager.LoadScene(nomeDaCena);
    }
}