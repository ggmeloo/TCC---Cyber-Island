// ThirdPersonOrbitCamera.cs
using UnityEngine;

public class ThirdPersonOrbitCamera : MonoBehaviour
{
    [Header("Alvo a Seguir")]
    public Transform alvoJogador;

    [Header("Configura��es de �rbita Normal")]
    public float distanciaNormal = 5.0f;
    public float sensibilidadeMouseX = 150.0f; // Ajustado para usar com Time.deltaTime
    public float sensibilidadeMouseY = 100.0f; // Ajustado para usar com Time.deltaTime
    public float anguloYMin = -40.0f; // Olhar para baixo
    public float anguloYMax = 80.0f;  // Olhar para cima
    [Tooltip("Offset vertical no jogador para a c�mera focar (altura da cabe�a/tronco).")]
    public float offsetAlturaFocoJogadorNormal = 1.5f;
    public float suavidadeCameraNormal = 12f;

    [Header("Configura��es da C�mera em Trava de Mira")]
    [Tooltip("Dist�ncia da c�mera ao jogador durante a trava.")]
    public float distanciaTrava = 4.0f;
    [Tooltip("Altura da c�mera em rela��o ao jogador durante a trava.")]
    public float alturaTrava = 1.8f;
    [Tooltip("Deslocamento lateral da c�mera (0 = diretamente atr�s do jogador, >0 para direita).")]
    public float offsetLateralTrava = 0.7f;
    [Tooltip("Altura do ponto no inimigo que a c�mera foca durante a trava.")]
    public float alturaFocoInimigoTrava = 1.0f;
    public float suavidadeRotacaoTrava = 15f;
    public float suavidadePosicaoTrava = 10f;

    [Header("Tratamento de Colis�o")]
    public LayerMask camadasColisao;
    [Tooltip("Dist�ncia que a c�mera se afasta do ponto de colis�o.")]
    public float preenchimentoColisao = 0.3f;
    [Tooltip("Dist�ncia m�nima da c�mera ao alvo para evitar clipping extremo.")]
    public float distanciaMinimaColisao = 0.5f;
    public float suavidadeRetornoColisao = 5f;

    // Estado Interno
    private float anguloXAtual = 0.0f; // Rota��o horizontal (Yaw)
    private float anguloYAtual = 10.0f; // Rota��o vertical (Pitch)
    private float distanciaRealCamera; // Dist�ncia atual, ajustada por colis�o

    // Estado da Trava de Mira
    private bool estaEmModoTrava = false;
    private Transform inimigoTravadoAtual = null;

    void Start()
    {
        if (alvoJogador == null)
        {
            Debug.LogError("ThirdPersonOrbitCamera: 'Alvo Jogador' n�o definido!", this);
            enabled = false;
            return;
        }

        distanciaRealCamera = distanciaNormal;
        Vector3 angulosIniciais = transform.eulerAngles;
        anguloXAtual = angulosIniciais.y;
        anguloYAtual = angulosIniciais.x;

        // Normaliza o anguloYAtual inicial para o range correto
        if (anguloYAtual > 180f) anguloYAtual -= 360f;
        anguloYAtual = Mathf.Clamp(anguloYAtual, anguloYMin, anguloYMax);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate() // C�meras geralmente s�o atualizadas em LateUpdate
    {
        if (alvoJogador == null) return;

        if (estaEmModoTrava && inimigoTravadoAtual != null)
        {
            ControlarCameraEmModoTrava();
        }
        else
        {
            ControlarCameraNormal();
        }
    }

    void ControlarCameraNormal()
    {
        // Rota��o baseada no input do mouse
        anguloXAtual += Input.GetAxis("Mouse X") * sensibilidadeMouseX * Time.deltaTime;
        anguloYAtual -= Input.GetAxis("Mouse Y") * sensibilidadeMouseY * Time.deltaTime;
        anguloYAtual = Mathf.Clamp(anguloYAtual, anguloYMin, anguloYMax);

        Quaternion rotacaoDesejada = Quaternion.Euler(anguloYAtual, anguloXAtual, 0);
        Vector3 pontoFocoJogador = alvoJogador.position + alvoJogador.up * offsetAlturaFocoJogadorNormal;

        // Calcula a dist�ncia ideal e a ajustada por colis�o
        float distanciaAlvo = distanciaNormal;
        // Recalcula a dist�ncia atual da c�mera, considerando colis�es e suavizando o retorno
        distanciaRealCamera = CalcularDistanciaAjustadaPorColisao(pontoFocoJogador, rotacaoDesejada, distanciaAlvo);


        Vector3 posicaoDesejada = pontoFocoJogador - (rotacaoDesejada * Vector3.forward * distanciaRealCamera);

        // Aplica a rota��o e posi��o suavemente
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacaoDesejada, Time.deltaTime * suavidadeCameraNormal);
        transform.position = Vector3.Lerp(transform.position, posicaoDesejada, Time.deltaTime * suavidadeCameraNormal);
    }

    void ControlarCameraEmModoTrava()
    {
        if (inimigoTravadoAtual == null || alvoJogador == null)
        {
            DesabilitarModoTravaDeMira(); // Se perdeu o alvo ou jogador, volta ao normal
            return;
        }

        Vector3 pontoFocoJogador = alvoJogador.position + alvoJogador.up * offsetAlturaFocoJogadorNormal;
        Vector3 pontoFocoInimigo = inimigoTravadoAtual.position + inimigoTravadoAtual.up * alturaFocoInimigoTrava;

        // --- C�lculo da Posi��o da C�mera ---
        // Dire��o do jogador para o inimigo (ignorando Y para o offset lateral)
        Vector3 direcaoJogadorParaInimigoXZ = (pontoFocoInimigo - pontoFocoJogador);
        direcaoJogadorParaInimigoXZ.y = 0;
        direcaoJogadorParaInimigoXZ.Normalize();

        // Posi��o base: atr�s do jogador, olhando na dire��o oposta ao inimigo
        Vector3 posicaoBaseCam = pontoFocoJogador - direcaoJogadorParaInimigoXZ * distanciaTrava;

        // Adiciona offset lateral (para efeito "sobre o ombro")
        if (direcaoJogadorParaInimigoXZ != Vector3.zero) // Evita Vector3.Cross com vetor zero
        {
            Vector3 direcaoLateral = Vector3.Cross(Vector3.up, direcaoJogadorParaInimigoXZ).normalized;
            posicaoBaseCam += direcaoLateral * offsetLateralTrava;
        }

        // Adiciona altura relativa ao jogador
        posicaoBaseCam += alvoJogador.up * alturaTrava; // Usar alvoJogador.up para altura consistente

        // --- Tratamento de Colis�o para o Modo Trava ---
        // O Raycast de colis�o deve ser da posi��o "segura" do jogador para a 'posicaoBaseCam'
        Quaternion rotacaoParaOlharInimigo = Quaternion.identity;
        if ((pontoFocoInimigo - posicaoBaseCam).sqrMagnitude > 0.001f)
            rotacaoParaOlharInimigo = Quaternion.LookRotation((pontoFocoInimigo - posicaoBaseCam).normalized);

        float distanciaIdealParaPosBase = Vector3.Distance(pontoFocoJogador, posicaoBaseCam);
        distanciaRealCamera = CalcularDistanciaAjustadaPorColisao(pontoFocoJogador, rotacaoParaOlharInimigo, distanciaIdealParaPosBase);

        Vector3 direcaoNormalizadaParaPosBase = (posicaoBaseCam - pontoFocoJogador).normalized;
        if (direcaoNormalizadaParaPosBase == Vector3.zero) direcaoNormalizadaParaPosBase = -transform.forward; // fallback
        Vector3 posicaoFinalCamera = pontoFocoJogador + direcaoNormalizadaParaPosBase * distanciaRealCamera;


        // --- Rota��o da C�mera ---
        // Faz a c�mera olhar para o ponto de foco do inimigo a partir da sua posi��o final calculada
        Quaternion rotacaoDesejada = Quaternion.identity;
        if ((pontoFocoInimigo - posicaoFinalCamera).sqrMagnitude > 0.001f)
        {
            rotacaoDesejada = Quaternion.LookRotation((pontoFocoInimigo - posicaoFinalCamera).normalized);
        }
        else
        { // Fallback se a c�mera estiver muito pr�xima do ponto de foco
            rotacaoDesejada = transform.rotation;
        }


        // --- Aplica��o Suave ---
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacaoDesejada, Time.deltaTime * suavidadeRotacaoTrava);
        transform.position = Vector3.Lerp(transform.position, posicaoFinalCamera, Time.deltaTime * suavidadePosicaoTrava);

        // Sincroniza os �ngulos X e Y para uma transi��o suave ao sair do modo de trava
        Vector3 eulerAtuais = transform.eulerAngles;
        anguloXAtual = eulerAtuais.y;
        anguloYAtual = eulerAtuais.x;
        if (anguloYAtual > 180f) anguloYAtual -= 360f;
        anguloYAtual = Mathf.Clamp(anguloYAtual, anguloYMin, anguloYMax);
    }

    float CalcularDistanciaAjustadaPorColisao(Vector3 origemRaio, Quaternion orientacaoCamera, float distanciaDesejadaMaxima)
    {
        float distanciaAjustada = distanciaDesejadaMaxima;
        RaycastHit hit;
        // A dire��o do raio � "para tr�s" da orienta��o da c�mera, a partir da origem do raio
        Vector3 direcaoRaio = -(orientacaoCamera * Vector3.forward);

        if (Physics.Raycast(origemRaio, direcaoRaio, out hit, distanciaDesejadaMaxima, camadasColisao, QueryTriggerInteraction.Ignore))
        {
            distanciaAjustada = Mathf.Max(distanciaMinimaColisao, hit.distance - preenchimentoColisao);
        }
        // Suaviza o retorno da dist�ncia da c�mera ap�s uma colis�o ter empurrado ela para perto
        // Nota: A suaviza��o principal da dist�ncia (para normalDistance) j� acontece em ControlarCameraNormal
        // Aqui, estamos apenas garantindo que a colis�o seja respeitada.
        // Se quiser um retorno suave da colis�o AQUI, pode usar Lerp:
        // distanciaRealCamera = Mathf.Lerp(distanciaRealCamera, distanciaAjustada, Time.deltaTime * suavidadeRetornoColisao);
        // return distanciaRealCamera;
        return distanciaAjustada; // Retorna a dist�ncia calculada diretamente pela colis�o
    }

    // M�todos p�blicos para serem chamados por PlayerTargetLock
    public void HabilitarModoTravaDeMira(Transform novoAlvoTravado)
    {
        if (novoAlvoTravado == null) return;
        estaEmModoTrava = true;
        inimigoTravadoAtual = novoAlvoTravado;
        //Debug.Log($"ThirdPersonOrbitCamera: Modo Trava ATIVADO para {novoAlvoTravado.name}");
        // Os �ngulos (anguloXAtual, anguloYAtual) ser�o naturalmente ajustados pelo ControlarCameraEmModoTrava()
        // para focar no alvo, ent�o n�o h� necessidade de reset�-los aqui abruptamente.
    }

    public void DesabilitarModoTravaDeMira()
    {
        estaEmModoTrava = false;
        inimigoTravadoAtual = null;
        //Debug.Log("ThirdPersonOrbitCamera: Modo Trava DESATIVADO.");

        // Ao desabilitar, a c�mera j� deve ter seus anguloXAtual e anguloYAtual
        // sincronizados pela �ltima execu��o de ControlarCameraEmModoTrava().
        // A l�gica de ControlarCameraNormal() ent�o assumir� suavemente.
        // Reseta distanciaRealCamera para a dist�ncia normal desejada para que ControlarCameraNormal comece corretamente.
        distanciaRealCamera = Mathf.Lerp(distanciaRealCamera, distanciaNormal, Time.deltaTime * suavidadeRetornoColisao * 2f); // Suaviza um pouco mais r�pido de volta
    }

    void OnDrawGizmosSelected()
    {
        if (alvoJogador == null) return;
        Vector3 pontoFocoJogador = alvoJogador.position + alvoJogador.up * offsetAlturaFocoJogadorNormal;

        if (estaEmModoTrava && inimigoTravadoAtual != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, inimigoTravadoAtual.position + inimigoTravadoAtual.up * alturaFocoInimigoTrava);
        }
        else // Gizmos para modo normal
        {
            // Quaternion rot = Quaternion.Euler(anguloYAtual, anguloXAtual, 0);
            // Vector3 posCam = pontoFocoJogador - (rot * Vector3.forward * distanciaRealCamera);
            // Gizmos.color = Color.green;
            // Gizmos.DrawLine(pontoFocoJogador, posCam);
        }
    }
}