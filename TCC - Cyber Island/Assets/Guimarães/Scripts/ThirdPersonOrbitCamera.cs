using UnityEngine;

public class ThirdPersonOrbitCamera : MonoBehaviour // Nome do script igual ao nome do arquivo
{
    [Header("Target")]
    [Tooltip("O objeto que a câmera deve seguir e orbitar (o jogador).")]
    public Transform target;

    [Header("Orbit Settings")]
    [Tooltip("Distância base da câmera ao alvo.")]
    public float distance = 5.0f;
    [Tooltip("Quão rápido a câmera rotaciona horizontalmente com o mouse.")]
    public float sensitivityX = 4.0f;
    [Tooltip("Quão rápido a câmera rotaciona verticalmente com o mouse.")]
    public float sensitivityY = 2.0f;
    [Tooltip("Ângulo vertical mínimo (olhando para baixo).")]
    public float minYAngle = -40.0f;
    [Tooltip("Ângulo vertical máximo (olhando para cima).")]
    public float maxYAngle = 80.0f;

    [Header("Collision & Occlusion")]
    [Tooltip("Layers que a câmera deve considerar como obstáculos.")]
    public LayerMask collisionMask;
    [Tooltip("Quão perto a câmera pode chegar de um obstáculo antes de parar.")]
    public float collisionOffset = 0.3f;
    [Tooltip("Suavidade com que a câmera volta à distância original após colisão.")]
    public float smoothReturnSpeed = 5.0f;

    [Header("Positioning & Smoothing")]
    [Tooltip("Ajuste vertical no ponto de foco do alvo (para mirar na cabeça/tronco, não nos pés).")]
    public float targetHeightOffset = 1.5f;
    [Tooltip("Velocidade de suavização geral da câmera (posição e rotação).")]
    public float cameraSmoothSpeed = 10.0f;


    // Variáveis Privadas
    private float currentYaw = 0.0f;    // Rotação horizontal atual (Y axis)
    private float currentPitch = 10.0f; // Rotação vertical atual (X axis)
    private float currentDistance;      // Distância atual (pode mudar devido a colisões)


    void Start()
    {
        // Verifica se o alvo foi definido
        if (target == null)
        {
            Debug.LogError("Target não definido para a ThirdPersonOrbitCamera! Desativando script.", this);
            enabled = false; // Desativa o script para evitar erros
            return;
        }

        // Inicializa a distância atual
        currentDistance = distance;

        // Pega a rotação inicial (opcional, mas ajuda a evitar um salto inicial)
        Vector3 angles = transform.eulerAngles;
        currentYaw = angles.y;
        currentPitch = angles.x;

        // Trava e esconde o cursor do mouse
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // LateUpdate é executado após todos os Updates, ideal para câmeras
    void LateUpdate()
    {
        if (target == null) return; // Segurança extra

        // --- Rotação da Câmera ---
        // Atualiza os ângulos Yaw (horizontal) e Pitch (vertical) baseado no input do mouse
        currentYaw += Input.GetAxis("Mouse X") * sensitivityX * Time.deltaTime;
        currentPitch -= Input.GetAxis("Mouse Y") * sensitivityY * Time.deltaTime; // Subtrai para movimento natural (cima/baixo)

        // Limita (Clamps) o ângulo vertical para evitar que a câmera vire de cabeça para baixo
        currentPitch = Mathf.Clamp(currentPitch, minYAngle, maxYAngle);

        // Cria a rotação desejada da câmera
        Quaternion desiredRotation = Quaternion.Euler(currentPitch, currentYaw, 0);

        // --- Posição da Câmera ---
        // Calcula o ponto exato no alvo que a câmera deve focar
        Vector3 targetFocusPoint = target.position + (target.up * targetHeightOffset);

        // Calcula a posição desejada da câmera SEM considerar colisões
        // Começa do ponto de foco e vai para trás na direção oposta à rotação desejada, pela distância base.
        Vector3 desiredPositionNoCollision = targetFocusPoint - (desiredRotation * Vector3.forward * distance);

        // --- Verificação de Colisão ---
        float actualDistance = distance; // Assume a distância total inicialmente
        RaycastHit hit;

        // Dispara um raio do ponto de foco EM DIREÇÃO à posição desejada da câmera
        Vector3 rayDirection = (desiredPositionNoCollision - targetFocusPoint).normalized;
        if (Physics.Raycast(targetFocusPoint, rayDirection, out hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            // Se colidiu, a distância real é a distância da colisão, menos um pequeno offset
            actualDistance = hit.distance - collisionOffset;
            // Garante que a distância não seja negativa ou extremamente pequena
            actualDistance = Mathf.Max(actualDistance, 0.1f);
        }

        // Ajusta a distância atual suavemente em direção à distância calculada (com ou sem colisão)
        currentDistance = Mathf.Lerp(currentDistance, actualDistance, Time.deltaTime * smoothReturnSpeed);


        // Calcula a posição FINAL da câmera usando a distância atual (ajustada pela colisão e suavização)
        Vector3 finalPosition = targetFocusPoint - (desiredRotation * Vector3.forward * currentDistance);

        // --- Aplicação Suave ---
        // Move a câmera suavemente para a posição final
        transform.position = Vector3.Lerp(transform.position, finalPosition, Time.deltaTime * cameraSmoothSpeed);
        // Rotaciona a câmera suavemente para a rotação desejada
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * cameraSmoothSpeed);
    }

    // Opcional: Desenha um Gizmo no editor para visualizar a linha de visão/colisão
    private void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Vector3 targetFocusPoint = target.position + (target.up * targetHeightOffset);
        Quaternion desiredRotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        Vector3 finalPosition = targetFocusPoint - (desiredRotation * Vector3.forward * currentDistance); // Usa currentDistance

        Gizmos.color = Color.green;
        Gizmos.DrawLine(targetFocusPoint, finalPosition); // Linha da câmera atual
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(targetFocusPoint, targetFocusPoint - (desiredRotation * Vector3.forward * distance)); // Linha da distância máxima

    }
}