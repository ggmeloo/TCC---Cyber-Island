using UnityEngine;

public class ThirdPersonOrbitCamera : MonoBehaviour // Nome do script igual ao nome do arquivo
{
    [Header("Target")]
    [Tooltip("O objeto que a c�mera deve seguir e orbitar (o jogador).")]
    public Transform target;

    [Header("Orbit Settings")]
    [Tooltip("Dist�ncia base da c�mera ao alvo.")]
    public float distance = 5.0f;
    [Tooltip("Qu�o r�pido a c�mera rotaciona horizontalmente com o mouse.")]
    public float sensitivityX = 4.0f;
    [Tooltip("Qu�o r�pido a c�mera rotaciona verticalmente com o mouse.")]
    public float sensitivityY = 2.0f;
    [Tooltip("�ngulo vertical m�nimo (olhando para baixo).")]
    public float minYAngle = -40.0f;
    [Tooltip("�ngulo vertical m�ximo (olhando para cima).")]
    public float maxYAngle = 80.0f;

    [Header("Collision & Occlusion")]
    [Tooltip("Layers que a c�mera deve considerar como obst�culos.")]
    public LayerMask collisionMask;
    [Tooltip("Qu�o perto a c�mera pode chegar de um obst�culo antes de parar.")]
    public float collisionOffset = 0.3f;
    [Tooltip("Suavidade com que a c�mera volta � dist�ncia original ap�s colis�o.")]
    public float smoothReturnSpeed = 5.0f;

    [Header("Positioning & Smoothing")]
    [Tooltip("Ajuste vertical no ponto de foco do alvo (para mirar na cabe�a/tronco, n�o nos p�s).")]
    public float targetHeightOffset = 1.5f;
    [Tooltip("Velocidade de suaviza��o geral da c�mera (posi��o e rota��o).")]
    public float cameraSmoothSpeed = 10.0f;


    // Vari�veis Privadas
    private float currentYaw = 0.0f;    // Rota��o horizontal atual (Y axis)
    private float currentPitch = 10.0f; // Rota��o vertical atual (X axis)
    private float currentDistance;      // Dist�ncia atual (pode mudar devido a colis�es)


    void Start()
    {
        // Verifica se o alvo foi definido
        if (target == null)
        {
            Debug.LogError("Target n�o definido para a ThirdPersonOrbitCamera! Desativando script.", this);
            enabled = false; // Desativa o script para evitar erros
            return;
        }

        // Inicializa a dist�ncia atual
        currentDistance = distance;

        // Pega a rota��o inicial (opcional, mas ajuda a evitar um salto inicial)
        Vector3 angles = transform.eulerAngles;
        currentYaw = angles.y;
        currentPitch = angles.x;

        // Trava e esconde o cursor do mouse
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // LateUpdate � executado ap�s todos os Updates, ideal para c�meras
    void LateUpdate()
    {
        if (target == null) return; // Seguran�a extra

        // --- Rota��o da C�mera ---
        // Atualiza os �ngulos Yaw (horizontal) e Pitch (vertical) baseado no input do mouse
        currentYaw += Input.GetAxis("Mouse X") * sensitivityX * Time.deltaTime;
        currentPitch -= Input.GetAxis("Mouse Y") * sensitivityY * Time.deltaTime; // Subtrai para movimento natural (cima/baixo)

        // Limita (Clamps) o �ngulo vertical para evitar que a c�mera vire de cabe�a para baixo
        currentPitch = Mathf.Clamp(currentPitch, minYAngle, maxYAngle);

        // Cria a rota��o desejada da c�mera
        Quaternion desiredRotation = Quaternion.Euler(currentPitch, currentYaw, 0);

        // --- Posi��o da C�mera ---
        // Calcula o ponto exato no alvo que a c�mera deve focar
        Vector3 targetFocusPoint = target.position + (target.up * targetHeightOffset);

        // Calcula a posi��o desejada da c�mera SEM considerar colis�es
        // Come�a do ponto de foco e vai para tr�s na dire��o oposta � rota��o desejada, pela dist�ncia base.
        Vector3 desiredPositionNoCollision = targetFocusPoint - (desiredRotation * Vector3.forward * distance);

        // --- Verifica��o de Colis�o ---
        float actualDistance = distance; // Assume a dist�ncia total inicialmente
        RaycastHit hit;

        // Dispara um raio do ponto de foco EM DIRE��O � posi��o desejada da c�mera
        Vector3 rayDirection = (desiredPositionNoCollision - targetFocusPoint).normalized;
        if (Physics.Raycast(targetFocusPoint, rayDirection, out hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            // Se colidiu, a dist�ncia real � a dist�ncia da colis�o, menos um pequeno offset
            actualDistance = hit.distance - collisionOffset;
            // Garante que a dist�ncia n�o seja negativa ou extremamente pequena
            actualDistance = Mathf.Max(actualDistance, 0.1f);
        }

        // Ajusta a dist�ncia atual suavemente em dire��o � dist�ncia calculada (com ou sem colis�o)
        currentDistance = Mathf.Lerp(currentDistance, actualDistance, Time.deltaTime * smoothReturnSpeed);


        // Calcula a posi��o FINAL da c�mera usando a dist�ncia atual (ajustada pela colis�o e suaviza��o)
        Vector3 finalPosition = targetFocusPoint - (desiredRotation * Vector3.forward * currentDistance);

        // --- Aplica��o Suave ---
        // Move a c�mera suavemente para a posi��o final
        transform.position = Vector3.Lerp(transform.position, finalPosition, Time.deltaTime * cameraSmoothSpeed);
        // Rotaciona a c�mera suavemente para a rota��o desejada
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * cameraSmoothSpeed);
    }

    // Opcional: Desenha um Gizmo no editor para visualizar a linha de vis�o/colis�o
    private void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Vector3 targetFocusPoint = target.position + (target.up * targetHeightOffset);
        Quaternion desiredRotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        Vector3 finalPosition = targetFocusPoint - (desiredRotation * Vector3.forward * currentDistance); // Usa currentDistance

        Gizmos.color = Color.green;
        Gizmos.DrawLine(targetFocusPoint, finalPosition); // Linha da c�mera atual
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(targetFocusPoint, targetFocusPoint - (desiredRotation * Vector3.forward * distance)); // Linha da dist�ncia m�xima

    }
}