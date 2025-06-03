// ThirdPersonOrbitCamera.cs
using UnityEngine;

public class ThirdPersonOrbitCamera : MonoBehaviour
{
    [Header("Alvo a Seguir")]
    public Transform alvoJogador;

    [Header("Configurações de Órbita Normal")]
    public float distanciaNormal = 5.0f;
    public float sensibilidadeMouseX = 150.0f; // Ajustado para usar com Time.deltaTime
    public float sensibilidadeMouseY = 100.0f; // Ajustado para usar com Time.deltaTime
    public float anguloYMin = -40.0f; // Olhar para baixo
    public float anguloYMax = 80.0f;  // Olhar para cima
    [Tooltip("Offset vertical no jogador para a câmera focar (altura da cabeça/tronco).")]
    public float offsetAlturaFocoJogadorNormal = 1.5f;
    public float suavidadeCameraNormal = 12f;

    [Header("Configurações da Câmera em Trava de Mira")]
    [Tooltip("Distância da câmera ao jogador durante a trava.")]
    public float distanciaTrava = 4.0f;
    [Tooltip("Altura da câmera em relação ao jogador durante a trava.")]
    public float alturaTrava = 1.8f;
    [Tooltip("Deslocamento lateral da câmera (0 = diretamente atrás do jogador, >0 para direita).")]
    public float offsetLateralTrava = 0.7f;
    [Tooltip("Altura do ponto no inimigo que a câmera foca durante a trava.")]
    public float alturaFocoInimigoTrava = 1.0f;
    public float suavidadeRotacaoTrava = 15f;
    public float suavidadePosicaoTrava = 10f;

    [Header("Tratamento de Colisão")]
    public LayerMask camadasColisao;
    [Tooltip("Distância que a câmera se afasta do ponto de colisão.")]
    public float preenchimentoColisao = 0.3f;
    [Tooltip("Distância mínima da câmera ao alvo para evitar clipping extremo.")]
    public float distanciaMinimaColisao = 0.5f;
    public float suavidadeRetornoColisao = 5f;

    // Estado Interno
    private float anguloXAtual = 0.0f; // Rotação horizontal (Yaw)
    private float anguloYAtual = 10.0f; // Rotação vertical (Pitch)
    private float distanciaRealCamera; // Distância atual, ajustada por colisão

    // Estado da Trava de Mira
    private bool estaEmModoTrava = false;
    private Transform inimigoTravadoAtual = null;

    void Start()
    {
        if (alvoJogador == null)
        {
            Debug.LogError("ThirdPersonOrbitCamera: 'Alvo Jogador' não definido!", this);
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

    void LateUpdate() // Câmeras geralmente são atualizadas em LateUpdate
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
        // Rotação baseada no input do mouse
        anguloXAtual += Input.GetAxis("Mouse X") * sensibilidadeMouseX * Time.deltaTime;
        anguloYAtual -= Input.GetAxis("Mouse Y") * sensibilidadeMouseY * Time.deltaTime;
        anguloYAtual = Mathf.Clamp(anguloYAtual, anguloYMin, anguloYMax);

        Quaternion rotacaoDesejada = Quaternion.Euler(anguloYAtual, anguloXAtual, 0);
        Vector3 pontoFocoJogador = alvoJogador.position + alvoJogador.up * offsetAlturaFocoJogadorNormal;

        // Calcula a distância ideal e a ajustada por colisão
        float distanciaAlvo = distanciaNormal;
        // Recalcula a distância atual da câmera, considerando colisões e suavizando o retorno
        distanciaRealCamera = CalcularDistanciaAjustadaPorColisao(pontoFocoJogador, rotacaoDesejada, distanciaAlvo);


        Vector3 posicaoDesejada = pontoFocoJogador - (rotacaoDesejada * Vector3.forward * distanciaRealCamera);

        // Aplica a rotação e posição suavemente
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

        // --- Cálculo da Posição da Câmera ---
        // Direção do jogador para o inimigo (ignorando Y para o offset lateral)
        Vector3 direcaoJogadorParaInimigoXZ = (pontoFocoInimigo - pontoFocoJogador);
        direcaoJogadorParaInimigoXZ.y = 0;
        direcaoJogadorParaInimigoXZ.Normalize();

        // Posição base: atrás do jogador, olhando na direção oposta ao inimigo
        Vector3 posicaoBaseCam = pontoFocoJogador - direcaoJogadorParaInimigoXZ * distanciaTrava;

        // Adiciona offset lateral (para efeito "sobre o ombro")
        if (direcaoJogadorParaInimigoXZ != Vector3.zero) // Evita Vector3.Cross com vetor zero
        {
            Vector3 direcaoLateral = Vector3.Cross(Vector3.up, direcaoJogadorParaInimigoXZ).normalized;
            posicaoBaseCam += direcaoLateral * offsetLateralTrava;
        }

        // Adiciona altura relativa ao jogador
        posicaoBaseCam += alvoJogador.up * alturaTrava; // Usar alvoJogador.up para altura consistente

        // --- Tratamento de Colisão para o Modo Trava ---
        // O Raycast de colisão deve ser da posição "segura" do jogador para a 'posicaoBaseCam'
        Quaternion rotacaoParaOlharInimigo = Quaternion.identity;
        if ((pontoFocoInimigo - posicaoBaseCam).sqrMagnitude > 0.001f)
            rotacaoParaOlharInimigo = Quaternion.LookRotation((pontoFocoInimigo - posicaoBaseCam).normalized);

        float distanciaIdealParaPosBase = Vector3.Distance(pontoFocoJogador, posicaoBaseCam);
        distanciaRealCamera = CalcularDistanciaAjustadaPorColisao(pontoFocoJogador, rotacaoParaOlharInimigo, distanciaIdealParaPosBase);

        Vector3 direcaoNormalizadaParaPosBase = (posicaoBaseCam - pontoFocoJogador).normalized;
        if (direcaoNormalizadaParaPosBase == Vector3.zero) direcaoNormalizadaParaPosBase = -transform.forward; // fallback
        Vector3 posicaoFinalCamera = pontoFocoJogador + direcaoNormalizadaParaPosBase * distanciaRealCamera;


        // --- Rotação da Câmera ---
        // Faz a câmera olhar para o ponto de foco do inimigo a partir da sua posição final calculada
        Quaternion rotacaoDesejada = Quaternion.identity;
        if ((pontoFocoInimigo - posicaoFinalCamera).sqrMagnitude > 0.001f)
        {
            rotacaoDesejada = Quaternion.LookRotation((pontoFocoInimigo - posicaoFinalCamera).normalized);
        }
        else
        { // Fallback se a câmera estiver muito próxima do ponto de foco
            rotacaoDesejada = transform.rotation;
        }


        // --- Aplicação Suave ---
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacaoDesejada, Time.deltaTime * suavidadeRotacaoTrava);
        transform.position = Vector3.Lerp(transform.position, posicaoFinalCamera, Time.deltaTime * suavidadePosicaoTrava);

        // Sincroniza os ângulos X e Y para uma transição suave ao sair do modo de trava
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
        // A direção do raio é "para trás" da orientação da câmera, a partir da origem do raio
        Vector3 direcaoRaio = -(orientacaoCamera * Vector3.forward);

        if (Physics.Raycast(origemRaio, direcaoRaio, out hit, distanciaDesejadaMaxima, camadasColisao, QueryTriggerInteraction.Ignore))
        {
            distanciaAjustada = Mathf.Max(distanciaMinimaColisao, hit.distance - preenchimentoColisao);
        }
        // Suaviza o retorno da distância da câmera após uma colisão ter empurrado ela para perto
        // Nota: A suavização principal da distância (para normalDistance) já acontece em ControlarCameraNormal
        // Aqui, estamos apenas garantindo que a colisão seja respeitada.
        // Se quiser um retorno suave da colisão AQUI, pode usar Lerp:
        // distanciaRealCamera = Mathf.Lerp(distanciaRealCamera, distanciaAjustada, Time.deltaTime * suavidadeRetornoColisao);
        // return distanciaRealCamera;
        return distanciaAjustada; // Retorna a distância calculada diretamente pela colisão
    }

    // Métodos públicos para serem chamados por PlayerTargetLock
    public void HabilitarModoTravaDeMira(Transform novoAlvoTravado)
    {
        if (novoAlvoTravado == null) return;
        estaEmModoTrava = true;
        inimigoTravadoAtual = novoAlvoTravado;
        //Debug.Log($"ThirdPersonOrbitCamera: Modo Trava ATIVADO para {novoAlvoTravado.name}");
        // Os ângulos (anguloXAtual, anguloYAtual) serão naturalmente ajustados pelo ControlarCameraEmModoTrava()
        // para focar no alvo, então não há necessidade de resetá-los aqui abruptamente.
    }

    public void DesabilitarModoTravaDeMira()
    {
        estaEmModoTrava = false;
        inimigoTravadoAtual = null;
        //Debug.Log("ThirdPersonOrbitCamera: Modo Trava DESATIVADO.");

        // Ao desabilitar, a câmera já deve ter seus anguloXAtual e anguloYAtual
        // sincronizados pela última execução de ControlarCameraEmModoTrava().
        // A lógica de ControlarCameraNormal() então assumirá suavemente.
        // Reseta distanciaRealCamera para a distância normal desejada para que ControlarCameraNormal comece corretamente.
        distanciaRealCamera = Mathf.Lerp(distanciaRealCamera, distanciaNormal, Time.deltaTime * suavidadeRetornoColisao * 2f); // Suaviza um pouco mais rápido de volta
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