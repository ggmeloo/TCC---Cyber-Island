// PlayerTargetLock.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerTargetLock : MonoBehaviour
{
    [Header("Configurações da Trava de Mira")]
    public KeyCode teclaTravaDeMira = KeyCode.Mouse2;
    public KeyCode teclaMudarAlvoEsquerda = KeyCode.LeftArrow;
    public KeyCode teclaMudarAlvoDireita = KeyCode.RightArrow;
    public float distanciaMaxTrava = 20f;
    [Tooltip("Ângulo (em graus) à frente do jogador/câmera para considerar alvos.")]
    public float anguloTrava = 120f;
    public LayerMask camadaInimigos;
    [Tooltip("Ponto de referência no jogador (olhos/peito) para cálculos de ângulo e distância.")]
    public Transform pontoReferenciaJogador;

    [Header("Feedback Visual (Opcional)")]
    public GameObject prefabIndicadorAlvo;
    private GameObject indicadorAlvoAtual;
    public Vector3 offsetIndicador = new Vector3(0, 1.9f, 0);

    [Header("Referências Externas")]
    public ThirdPersonOrbitCamera scriptCameraPrincipal;

    private Transform alvoTravadoAtual = null;
    private List<Transform> alvosPotenciais = new List<Transform>();
    private int indiceAlvoAtualNaLista = -1;

    public bool EstaTravado => alvoTravadoAtual != null;
    public Transform AlvoTravadoAtual => alvoTravadoAtual;


    void Start()
    {
        if (pontoReferenciaJogador == null)
        {
            pontoReferenciaJogador = transform; // Fallback para o transform do jogador
        }

        if (scriptCameraPrincipal == null)
        {
            if (Camera.main != null)
                scriptCameraPrincipal = Camera.main.GetComponent<ThirdPersonOrbitCamera>();
            if (scriptCameraPrincipal == null)
                Debug.LogError("PlayerTargetLock: Script 'ThirdPersonOrbitCamera' não encontrado!", this);
        }
    }

    void Update()
    {
        ProcessarInputTrava();

        if (EstaTravado)
        {
            ManterTravaDeMira();
            if (EstaTravado) // Verifica novamente
            {
                ProcessarInputMudarAlvo();
            }
        }
    }

    void ProcessarInputTrava()
    {
        if (Input.GetKeyDown(teclaTravaDeMira))
        {
            if (EstaTravado) DesabilitarTravaDeMira();
            else TentarHabilitarTravaDeMira();
        }
    }

    void EncontrarAlvosPotenciais(bool paraTrocaDeAlvo = false)
    {
        alvosPotenciais.Clear();
        if (pontoReferenciaJogador == null) return;

        Collider[] colisoresProximos = Physics.OverlapSphere(pontoReferenciaJogador.position, distanciaMaxTrava, camadaInimigos);
        Transform transformReferenciaAngulo = (scriptCameraPrincipal != null && scriptCameraPrincipal.transform != null) ? scriptCameraPrincipal.transform : pontoReferenciaJogador;

        foreach (Collider col in colisoresProximos)
        {
            if (col.transform == transform) continue; // Ignora o próprio jogador
            Vector3 direcaoParaInimigo = (col.transform.position - transformReferenciaAngulo.position);
            float anguloParaInimigo = Vector3.Angle(transformReferenciaAngulo.forward, direcaoParaInimigo.normalized);

            if (anguloParaInimigo <= anguloTrava / 2f)
            {
                if (TemLinhaDeVisao(col.transform, transformReferenciaAngulo))
                {
                    alvosPotenciais.Add(col.transform);
                }
            }
        }

        if (alvosPotenciais.Count == 0) return;

        Camera cam = (scriptCameraPrincipal != null) ? scriptCameraPrincipal.GetComponent<Camera>() : null;
        if (cam != null)
        {
            if (paraTrocaDeAlvo)
            {
                alvosPotenciais = alvosPotenciais.OrderBy(t => {
                    Vector3 posTela = cam.WorldToScreenPoint(t.position);
                    return posTela.z < 0 ? float.MaxValue : posTela.x;
                }).ToList();
            }
            else
            {
                alvosPotenciais = alvosPotenciais.OrderBy(t => {
                    Vector3 posTela = cam.WorldToScreenPoint(t.position);
                    if (posTela.z < 0) return float.MaxValue;
                    return Vector2.Distance(new Vector2(posTela.x, posTela.y), new Vector2(Screen.width / 2f, Screen.height / 2f));
                }).ToList();
            }
        }
        else
        {
            alvosPotenciais = alvosPotenciais.OrderBy(t => Vector3.Distance(pontoReferenciaJogador.position, t.position)).ToList();
        }
    }

    bool TemLinhaDeVisao(Transform alvo, Transform origemRaio)
    {
        if (alvo == null || origemRaio == null) return false;
        RaycastHit hit;
        Vector3 origem = origemRaio.position;
        Collider colisorAlvo = alvo.GetComponent<Collider>();
        Vector3 pontoAlvo = colisorAlvo != null ? colisorAlvo.bounds.center : alvo.position + Vector3.up * 0.5f;
        Vector3 direcao = (pontoAlvo - origem).normalized;
        float distancia = Vector3.Distance(origem, pontoAlvo);
        int layerJogador = gameObject.layer;
        LayerMask mascaraIgnorar = (1 << layerJogador);
        if (indicadorAlvoAtual != null) mascaraIgnorar |= (1 << indicadorAlvoAtual.layer);

        if (Physics.Raycast(origem, direcao, out hit, distancia * 0.99f, ~mascaraIgnorar))
        {
            if (hit.transform != alvo && !hit.transform.IsChildOf(alvo)) return false;
        }
        return true;
    }

    void TentarHabilitarTravaDeMira()
    {
        EncontrarAlvosPotenciais(false);
        if (alvosPotenciais.Count > 0)
        {
            indiceAlvoAtualNaLista = 0;
            DefinirAlvoTravado(alvosPotenciais[0]);
        }
    }

    void DefinirAlvoTravado(Transform novoAlvo)
    {
        if (novoAlvo == null) { DesabilitarTravaDeMira(); return; }
        alvoTravadoAtual = novoAlvo;
        // Garante que o índice na lista 'alvosPotenciais' (que pode ter sido reordenada) seja o do novo alvo.
        indiceAlvoAtualNaLista = alvosPotenciais.IndexOf(alvoTravadoAtual);
        if (indiceAlvoAtualNaLista == -1 && alvosPotenciais.Count > 0)
        { // Se não encontrou, pega o primeiro (fallback)
            alvosPotenciais.Insert(0, alvoTravadoAtual); // Adiciona se não estiver, para garantir que o índice seja válido
            indiceAlvoAtualNaLista = 0;
        }


        if (prefabIndicadorAlvo != null)
        {
            if (indicadorAlvoAtual != null) Destroy(indicadorAlvoAtual);
            Collider colisorAlvo = novoAlvo.GetComponent<Collider>();
            Vector3 posicaoIndicador = colisorAlvo != null ? colisorAlvo.bounds.center + offsetIndicador : novoAlvo.position + offsetIndicador;
            indicadorAlvoAtual = Instantiate(prefabIndicadorAlvo, posicaoIndicador, Quaternion.identity);
            indicadorAlvoAtual.transform.SetParent(novoAlvo);
        }
        if (scriptCameraPrincipal != null) scriptCameraPrincipal.HabilitarModoTravaDeMira(alvoTravadoAtual);
    }

    void DesabilitarTravaDeMira()
    {
        alvoTravadoAtual = null;
        indiceAlvoAtualNaLista = -1;
        if (indicadorAlvoAtual != null) { Destroy(indicadorAlvoAtual); indicadorAlvoAtual = null; }
        if (scriptCameraPrincipal != null) scriptCameraPrincipal.DesabilitarModoTravaDeMira();
    }

    void ManterTravaDeMira()
    {
        if (alvoTravadoAtual == null) { DesabilitarTravaDeMira(); return; }

        EnemyHealth saudeAlvo = alvoTravadoAtual.GetComponent<EnemyHealth>();
        if (saudeAlvo != null && saudeAlvo.IsDead())
        {
            DesabilitarTravaDeMira();
            TentarHabilitarTravaDeMira(); // Tenta encontrar novo alvo
            return;
        }

        Transform transformReferenciaAngulo = (scriptCameraPrincipal != null && scriptCameraPrincipal.transform != null) ? scriptCameraPrincipal.transform : pontoReferenciaJogador;
        float distancia = Vector3.Distance(pontoReferenciaJogador.position, alvoTravadoAtual.position);
        Vector3 direcaoParaInimigo = (alvoTravadoAtual.position - transformReferenciaAngulo.position).normalized;
        if (direcaoParaInimigo == Vector3.zero) direcaoParaInimigo = transformReferenciaAngulo.forward;
        float angulo = Vector3.Angle(transformReferenciaAngulo.forward, direcaoParaInimigo);

        if (distancia > distanciaMaxTrava * 1.2f || angulo > (anguloTrava / 2f) * 1.5f || !TemLinhaDeVisao(alvoTravadoAtual, transformReferenciaAngulo))
        {
            DesabilitarTravaDeMira();
            return;
        }
        // ROTAÇÃO DO JOGADOR FOI REMOVIDA DAQUI - AGORA É FEITA PELO PlayerMovement.cs
    }

    void ProcessarInputMudarAlvo()
    {
        int direcaoMudar = 0;
        if (Input.GetKeyDown(teclaMudarAlvoDireita)) direcaoMudar = 1;
        else if (Input.GetKeyDown(teclaMudarAlvoEsquerda)) direcaoMudar = -1;
        if (direcaoMudar != 0) MudarAlvo(direcaoMudar);
    }

    void MudarAlvo(int direcao)
    {
        // 1. Obter todos os alvos visíveis e válidos no momento da troca
        List<Transform> candidatosAtuais = new List<Transform>();
        Transform transformReferenciaAngulo = (scriptCameraPrincipal != null && scriptCameraPrincipal.transform != null) ? scriptCameraPrincipal.transform : pontoReferenciaJogador;
        Collider[] colisores = Physics.OverlapSphere(pontoReferenciaJogador.position, distanciaMaxTrava, camadaInimigos);
        foreach (Collider col in colisores)
        {
            if (col.transform == transform) continue;
            Vector3 dir = (col.transform.position - transformReferenciaAngulo.position);
            if (Vector3.Angle(transformReferenciaAngulo.forward, dir.normalized) <= anguloTrava / 2f && TemLinhaDeVisao(col.transform, transformReferenciaAngulo))
            {
                candidatosAtuais.Add(col.transform);
            }
        }

        if (candidatosAtuais.Count == 0) { DesabilitarTravaDeMira(); return; } // Nenhum alvo para onde mudar
        if (candidatosAtuais.Count == 1) { DefinirAlvoTravado(candidatosAtuais[0]); return; } // Só um alvo, trava nele

        // 2. Ordenar os candidatos (ex: pela posição X na tela)
        Camera cam = (scriptCameraPrincipal != null) ? scriptCameraPrincipal.GetComponent<Camera>() : null;
        if (cam != null)
        {
            candidatosAtuais = candidatosAtuais.OrderBy(t => {
                Vector3 posTela = cam.WorldToScreenPoint(t.position);
                return posTela.z < 0 ? float.MaxValue : posTela.x; // Prioriza quem está na frente
            }).ToList();
        }
        else
        { // Fallback se não houver câmera para ordenação visual
            candidatosAtuais = candidatosAtuais.OrderBy(t => Vector3.SignedAngle(transformReferenciaAngulo.forward, (t.position - transformReferenciaAngulo.position), Vector3.up)).ToList();
        }


        // 3. Encontrar o índice do alvo ATUALMENTE travado (se houver) nesta lista recém ordenada
        int indiceDoAlvoTravadoNaLista = alvoTravadoAtual != null ? candidatosAtuais.IndexOf(alvoTravadoAtual) : -1;

        int proximoIndice;
        if (indiceDoAlvoTravadoNaLista == -1) // Se o alvo anterior não está na lista ou não havia alvo
        {
            // Pega o primeiro ou o último da nova lista ordenada, dependendo da direção
            proximoIndice = (direcao > 0) ? 0 : candidatosAtuais.Count - 1;
        }
        else
        {
            proximoIndice = indiceDoAlvoTravadoNaLista + direcao;
        }

        // 4. Fazer o loop na lista
        if (proximoIndice >= candidatosAtuais.Count) proximoIndice = 0;
        else if (proximoIndice < 0) proximoIndice = candidatosAtuais.Count - 1;

        // Atualiza a lista 'alvosPotenciais' para que 'indiceAlvoAtualNaLista' seja consistente
        alvosPotenciais = new List<Transform>(candidatosAtuais); // Copia a lista ordenada para alvosPotenciais
        indiceAlvoAtualNaLista = proximoIndice; // Define o índice na lista atualizada
        DefinirAlvoTravado(alvosPotenciais[indiceAlvoAtualNaLista]);
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || pontoReferenciaJogador == null) return;
        Transform transformReferenciaAngulo = (scriptCameraPrincipal != null && scriptCameraPrincipal.transform != null) ? scriptCameraPrincipal.transform : pontoReferenciaJogador;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pontoReferenciaJogador.position, distanciaMaxTrava);
        Vector3 forward = transformReferenciaAngulo.forward;
        Vector3 limiteDireito = Quaternion.AngleAxis(anguloTrava / 2f, transformReferenciaAngulo.up) * forward;
        Vector3 limiteEsquerdo = Quaternion.AngleAxis(-anguloTrava / 2f, transformReferenciaAngulo.up) * forward;
        Gizmos.color = new Color(1, 1, 0, 0.15f);
        Gizmos.DrawRay(transformReferenciaAngulo.position, limiteDireito * distanciaMaxTrava);
        Gizmos.DrawRay(transformReferenciaAngulo.position, limiteEsquerdo * distanciaMaxTrava);
#if UNITY_EDITOR
        UnityEditor.Handles.color = new Color(1, 1, 0, 0.05f);
        UnityEditor.Handles.DrawSolidArc(transformReferenciaAngulo.position, transformReferenciaAngulo.up, limiteEsquerdo, anguloTrava, distanciaMaxTrava);
#endif
        if (EstaTravado && alvoTravadoAtual != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(pontoReferenciaJogador.position, alvoTravadoAtual.position);
            if (indicadorAlvoAtual != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(indicadorAlvoAtual.transform.position, 0.3f);
            }
        }
    }
}