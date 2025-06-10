using UnityEngine;
using UnityEngine.SceneManagement; // Essencial para gerenciar cenas!

public class GerenciadorDeCenas : MonoBehaviour
{
    // Esta fun��o ser� chamada por um bot�o ou outro evento no jogo
    public void CarregarCena(int buildIndex)
    {
        // Carrega a cena baseada no seu n�mero na lista do Build Settings
        SceneManager.LoadScene(buildIndex);
    }

    // Voc� tamb�m pode carregar pelo nome, se preferir
    public void CarregarCenaPeloNome(string nomeDaCena)
    {
        SceneManager.LoadScene(nomeDaCena);
    }
}