// ThirdPersonOrbitCamera.cs
using UnityEngine;

public class ThirdPersonOrbitCamera : MonoBehaviour
{
    // <<< NOSSO NOVO INTERRUPTOR P�BLICO >>>
    // Por padr�o, a c�mera pode ser controlada pelo mouse.
    public bool podeOrbitarComMouse = true;

    [Header("Alvo a Seguir")]
    public Transform alvoJogador;

    [Header("Configura��es de �rbita Normal")]
    public float distanciaNormal = 5.0f;
    public float sensibilidadeMouseX = 150.0f;
    public float sensibilidadeMouseY = 100.0f;
    public float anguloYMin = -40.0f;
    public float anguloYMax = 80.0f;
    public float offsetAlturaFocoJogadorNormal = 1.5f;
    public float suavidadeCameraNormal = 12f;

    [Header("Configura��es da C�mera em Trava de Mira")]
    public float distanciaTrava = 4.0f;
    public float alturaTrava = 1.8f;
    public float offsetLateralTrava = 0.7f;
    public float alturaFocoInimigoTrava = 1.0f;
    public float suavidadeRotacaoTrava = 15f;
    public float suavidadePosicaoTrava = 10f;

    [Header("Tratamento de Colis�o")]
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
            Debug.LogError("ThirdPersonOrbitCamera: 'Alvo Jogador' n�o definido!", this);
            enabled = false;
            return;
        }

        distanciaRealCamera = distanciaNormal;
        Vector3 angulosIniciais = transform.eulerAngles;
        anguloXAtual = angulosIniciais.y;
        anguloYAtual = angulosIniciais.x;

        if (anguloYAtual > 180f) anguloYAtual -= 360f;
        anguloYAtual = Mathf.Clamp(anguloYAtual, anguloYMin, anguloYMax);

        // O ControladorInventario agora vai gerenciar o estado do cursor
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
    }

    void LateUpdate()
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
        // <<< CONDI��O ADICIONADA AQUI >>>
        // S� executa a rota��o pelo mouse se o interruptor estiver ligado.
        if (podeOrbitarComMouse)
        {
            // Rota��o baseada no input do mouse
            anguloXAtual += Input.GetAxis("Mouse X") * sensibilidadeMouseX * Time.deltaTime;
            anguloYAtual -= Input.GetAxis("Mouse Y") * sensibilidadeMouseY * Time.deltaTime;
            anguloYAtual = Mathf.Clamp(anguloYAtual, anguloYMin, anguloYMax);
        }

        Quaternion rotacaoDesejada = Quaternion.Euler(anguloYAtual, anguloXAtual, 0);
        Vector3 pontoFocoJogador = alvoJogador.position + alvoJogador.up * offsetAlturaFocoJogadorNormal;

        float distanciaAlvo = distanciaNormal;
        distanciaRealCamera = CalcularDistanciaAjustadaPorColisao(pontoFocoJogador, rotacaoDesejada, distanciaAlvo);

        Vector3 posicaoDesejada = pontoFocoJogador - (rotacaoDesejada * Vector3.forward * distanciaRealCamera);

        transform.rotation = Quaternion.Slerp(transform.rotation, rotacaoDesejada, Time.deltaTime * suavidadeCameraNormal);
        transform.position = Vector3.Lerp(transform.position, posicaoDesejada, Time.deltaTime * suavidadeCameraNormal);
    }

    // O resto do seu c�digo permanece exatamente igual...
    void ControlarCameraEmModoTrava()
    {
        // ... (sem altera��es aqui)
        if (inimigoTravadoAtual == null || alvoJogador == null)
        {
            DesabilitarModoTravaDeMira();
            return;
        }

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
        else
        {
            rotacaoDesejada = transform.rotation;
        }

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
        // ... (sem altera��es aqui)
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
        // ... (sem altera��es aqui)
        if (novoAlvoTravado == null) return;
        estaEmModoTrava = true;
        inimigoTravadoAtual = novoAlvoTravado;
    }

    public void DesabilitarModoTravaDeMira()
    {
        // ... (sem altera��es aqui)
        estaEmModoTrava = false;
        inimigoTravadoAtual = null;
        distanciaRealCamera = Mathf.Lerp(distanciaRealCamera, distanciaNormal, Time.deltaTime * suavidadeRetornoColisao * 2f);
    }

    void OnDrawGizmosSelected()
    {
        // ... (sem altera��es aqui)
        if (alvoJogador == null) return;
        Vector3 pontoFocoJogador = alvoJogador.position + alvoJogador.up * offsetAlturaFocoJogadorNormal;

        if (estaEmModoTrava && inimigoTravadoAtual != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, inimigoTravadoAtual.position + inimigoTravadoAtual.up * alturaFocoInimigoTrava);
        }
    }
}