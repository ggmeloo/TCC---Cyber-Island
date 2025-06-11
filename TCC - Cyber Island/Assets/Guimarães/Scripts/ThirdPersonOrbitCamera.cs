// ThirdPersonOrbitCamera.cs
using UnityEngine;

public class ThirdPersonOrbitCamera : MonoBehaviour
{
    [Header("Alvo a Seguir")]
    public Transform alvoJogador;

    [Header("Configurações de Órbita Normal")]
    public float distanciaNormal = 5.0f;
    public float sensibilidadeMouseX = 150.0f;
    public float sensibilidadeMouseY = 100.0f;
    public float anguloYMin = -40.0f;
    public float anguloYMax = 80.0f;
    public float offsetAlturaFocoJogadorNormal = 1.5f;
    public float suavidadeCameraNormal = 12f;

    [Header("Configurações da Câmera em Trava de Mira")]
    public float distanciaTrava = 4.0f;
    public float alturaTrava = 1.8f;
    public float offsetLateralTrava = 0.7f;
    public float alturaFocoInimigoTrava = 1.0f;
    public float suavidadeRotacaoTrava = 15f;
    public float suavidadePosicaoTrava = 10f;

    [Header("Tratamento de Colisão")]
    public LayerMask camadasColisao;
    public float preenchimentoColisao = 0.3f;
    public float distanciaMinimaColisao = 0.5f;
    public float suavidadeRetornoColisao = 5f;

    private float anguloXAtual = 0.0f;
    private float anguloYAtual = 10.0f;
    private float distanciaRealCamera;
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

        if (anguloYAtual > 180f) anguloYAtual -= 360f;
        anguloYAtual = Mathf.Clamp(anguloYAtual, anguloYMin, anguloYMax);
    }

    void LateUpdate()
    {
        if (alvoJogador == null) return;

        // Se estiver em modo de trava de mira, a lógica dele tem prioridade.
        if (estaEmModoTrava && inimigoTravadoAtual != null)
        {
            ControlarCameraEmModoTrava();
        }
        else
        {
            // <<< MUDANÇA NA LÓGICA PRINCIPAL >>>
            // Separamos a atualização da rotação do posicionamento.

            // 1. ATUALIZAR ROTAÇÃO: Isso só acontece se NÃO estivermos no modo UI.
            if (EstadoJogador.instance == null || !EstadoJogador.instance.EmModoUI)
            {
                // Rotação baseada no input do mouse
                anguloXAtual += Input.GetAxis("Mouse X") * sensibilidadeMouseX * Time.deltaTime;
                anguloYAtual -= Input.GetAxis("Mouse Y") * sensibilidadeMouseY * Time.deltaTime;
                anguloYAtual = Mathf.Clamp(anguloYAtual, anguloYMin, anguloYMax);
            }

            // 2. POSICIONAR CÂMERA: Isso acontece SEMPRE, garantindo que a câmera siga o jogador.
            PosicionarCameraNormal();
        }
    }

    // A antiga função "ControlarCameraNormal" foi renomeada e simplificada.
    // Ela agora só cuida de posicionar a câmera usando os ângulos atuais.
    void PosicionarCameraNormal()
    {
        Quaternion rotacaoDesejada = Quaternion.Euler(anguloYAtual, anguloXAtual, 0);
        Vector3 pontoFocoJogador = alvoJogador.position + alvoJogador.up * offsetAlturaFocoJogadorNormal;

        float distanciaAlvo = distanciaNormal;
        distanciaRealCamera = CalcularDistanciaAjustadaPorColisao(pontoFocoJogador, rotacaoDesejada, distanciaAlvo);

        Vector3 posicaoDesejada = pontoFocoJogador - (rotacaoDesejada * Vector3.forward * distanciaRealCamera);

        transform.rotation = Quaternion.Slerp(transform.rotation, rotacaoDesejada, Time.deltaTime * suavidadeCameraNormal);
        transform.position = Vector3.Lerp(transform.position, posicaoDesejada, Time.deltaTime * suavidadeCameraNormal);
    }

    // A função "ControlarCameraNormal" foi substituída pela lógica acima.
    // Você pode remover a função antiga se ela ainda existir.

    // O resto do seu código (ControlarCameraEmModoTrava, etc.) permanece igual.
    void ControlarCameraEmModoTrava()
    {
        if (inimigoTravadoAtual == null || alvoJogador == null) { DesabilitarModoTravaDeMira(); return; }
        Vector3 pontoFocoJogador = alvoJogador.position + alvoJogador.up * offsetAlturaFocoJogadorNormal;
        Vector3 pontoFocoInimigo = inimigoTravadoAtual.position + inimigoTravadoAtual.up * alturaFocoInimigoTrava;
        Vector3 direcaoJogadorParaInimigoXZ = (pontoFocoInimigo - pontoFocoJogador);
        direcaoJogadorParaInimigoXZ.y = 0;
        direcaoJogadorParaInimigoXZ.Normalize();
        Vector3 posicaoBaseCam = pontoFocoJogador - direcaoJogadorParaInimigoXZ * distanciaTrava;
        if (direcaoJogadorParaInimigoXZ != Vector3.zero)
        {
            Vector3 direcaoLateral = Vector3.Cross(Vector3.up, direcaoJogadorParaInimigoXZ).normalized;
            posicaoBaseCam += direcaoLateral * offsetLateralTrava;
        }
        posicaoBaseCam += alvoJogador.up * alturaTrava;
        Quaternion rotacaoParaOlharInimigo = Quaternion.identity;
        if ((pontoFocoInimigo - posicaoBaseCam).sqrMagnitude > 0.001f)
            rotacaoParaOlharInimigo = Quaternion.LookRotation((pontoFocoInimigo - posicaoBaseCam).normalized);
        float distanciaIdealParaPosBase = Vector3.Distance(pontoFocoJogador, posicaoBaseCam);
        distanciaRealCamera = CalcularDistanciaAjustadaPorColisao(pontoFocoJogador, rotacaoParaOlharInimigo, distanciaIdealParaPosBase);
        Vector3 direcaoNormalizadaParaPosBase = (posicaoBaseCam - pontoFocoJogador).normalized;
        if (direcaoNormalizadaParaPosBase == Vector3.zero) direcaoNormalizadaParaPosBase = -transform.forward;
        Vector3 posicaoFinalCamera = pontoFocoJogador + direcaoNormalizadaParaPosBase * distanciaRealCamera;
        Quaternion rotacaoDesejada = Quaternion.identity;
        if ((pontoFocoInimigo - posicaoFinalCamera).sqrMagnitude > 0.001f)
        {
            rotacaoDesejada = Quaternion.LookRotation((pontoFocoInimigo - posicaoFinalCamera).normalized);
        }
        else { rotacaoDesejada = transform.rotation; }
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacaoDesejada, Time.deltaTime * suavidadeRotacaoTrava);
        transform.position = Vector3.Lerp(transform.position, posicaoFinalCamera, Time.deltaTime * suavidadePosicaoTrava);
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
        Vector3 direcaoRaio = -(orientacaoCamera * Vector3.forward);
        if (Physics.Raycast(origemRaio, direcaoRaio, out hit, distanciaDesejadaMaxima, camadasColisao, QueryTriggerInteraction.Ignore))
        {
            distanciaAjustada = Mathf.Max(distanciaMinimaColisao, hit.distance - preenchimentoColisao);
        }
        return distanciaAjustada;
    }
    public void HabilitarModoTravaDeMira(Transform novoAlvoTravado)
    {
        if (novoAlvoTravado == null) return;
        estaEmModoTrava = true;
        inimigoTravadoAtual = novoAlvoTravado;
    }
    public void DesabilitarModoTravaDeMira()
    {
        estaEmModoTrava = false;
        inimigoTravadoAtual = null;
        distanciaRealCamera = Mathf.Lerp(distanciaRealCamera, distanciaNormal, Time.deltaTime * suavidadeRetornoColisao * 2f);
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
    }
}