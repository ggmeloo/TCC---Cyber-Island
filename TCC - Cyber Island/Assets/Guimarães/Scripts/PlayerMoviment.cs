// PlayerMovement.cs
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Referências")]
    public Transform cameraTransform;
    public Animator characterAnimator;
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;

    [Tooltip("Referência ao script de trava de mira do jogador.")]
    public PlayerTargetLock targetLockScript; // Atribua no Inspector ou deixe o Start tentar pegar

    [Header("Configurações de Movimento")]
    public float walkSpeed = 3.0f;
    public float sprintSpeed = 6.0f;
    public float crouchSpeed = 1.5f;
    public float rotationSpeed = 720f; // Velocidade de rotação normal
    [Tooltip("Multiplicador para a velocidade de rotação ao travar em um alvo. Mais alto = vira mais rápido para o alvo.")]
    public float lockOnRotationSpeedMultiplier = 2.5f; // Jogador vira mais rápido para o alvo travado
    public float jumpForce = 7f;
    private bool _canMove = true; // Flag interna para controlar se o movimento está habilitado

    [Header("Configurações de Agachar")]
    public float standingHeight = 1.8f;
    public float crouchingHeight = 0.9f;
    private float standingCenterY;
    private float crouchingCenterY;
    public LayerMask obstructionLayers;

    [Header("Verificação de Chão")]
    public Transform groundCheckPoint;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Estado Atual (Debug)")]
    [SerializeField] private Vector3 moveInputDirection = Vector3.zero;
    [SerializeField] private Vector3 finalMoveVelocity = Vector3.zero;
    [SerializeField] private float currentSpeed;
    [SerializeField] private bool isCrouching = false;
    [SerializeField] private bool wantsToCrouch = false;
    [SerializeField] private bool isSprinting = false;
    [SerializeField] private bool isGrounded = true;
    [SerializeField] private bool isJumpingOrFalling = false;
    private bool jumpRequestedThisFrame = false;

    // Propriedade pública para scripts externos controlarem/verificarem se o jogador pode se mover
    public bool CanMove
    {
        get { return _canMove; }
        set
        {
            _canMove = value;
            if (!_canMove) // Se o movimento for desabilitado externamente
            {
                moveInputDirection = Vector3.zero; // Para a intenção de movimento
                isSprinting = false;
                // O Rigidbody será parado em FixedUpdate se !CanMove
            }
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        if (characterAnimator == null) characterAnimator = GetComponentInChildren<Animator>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        else if (cameraTransform == null) Debug.LogError("PlayerMovement: Câmera principal não encontrada e cameraTransform não atribuída!");

        if (rb != null)
        {
            rb.freezeRotation = true; // Congela rotação por física, controlaremos via script
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        capsuleCollider.height = standingHeight;
        standingCenterY = capsuleCollider.center.y;
        crouchingCenterY = standingCenterY - (standingHeight - crouchingHeight) / 2.0f;
        capsuleCollider.center = new Vector3(capsuleCollider.center.x, standingCenterY, capsuleCollider.center.z);

        if (groundCheckPoint == null)
        {
            GameObject gcp = new GameObject(gameObject.name + "_GroundCheckPoint_Auto");
            gcp.transform.SetParent(transform);
            gcp.transform.localPosition = new Vector3(0, (-standingHeight / 2f) + capsuleCollider.radius * 0.5f, 0);
            groundCheckPoint = gcp.transform;
        }

        // Tenta pegar o PlayerTargetLock se não estiver atribuído
        if (targetLockScript == null)
        {
            targetLockScript = GetComponent<PlayerTargetLock>();
            if (targetLockScript == null)
                Debug.LogWarning("PlayerMovement: PlayerTargetLock não encontrado neste GameObject. Funcionalidade de rotação em lock-on não funcionará.");
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Processa input apenas se puder se mover
        if (CanMove)
        {
            HandleInput();
            ProcessCrouch(); // Pode ser chamado mesmo se !CanMove se você quiser a lógica de agachar visualmente
        }
        else // Se não pode se mover (ex: durante uma animação, hitstun)
        {
            moveInputDirection = Vector3.zero;
            isSprinting = false;
            jumpRequestedThisFrame = false;
        }

        if (characterAnimator != null) UpdateAnimatorParameters();
    }

    void FixedUpdate()
    {
        CheckGrounded(); // Verifica o chão independentemente de CanMove

        if (rb == null) return;

        if (CanMove)
        {
            CalculateFinalMoveVelocity();
            // Aplica movimento (posição)
            rb.linearVelocity = new Vector3(finalMoveVelocity.x, rb.linearVelocity.y, finalMoveVelocity.z); // Mantém Y para gravidade/pulo

            ApplyRotation(); // Aplica rotação
            ApplyJump();     // Aplica pulo
        }
        else // Se não pode se mover
        {
            // Para o movimento horizontal do Rigidbody, mas permite que a gravidade atue
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            rb.angularVelocity = Vector3.zero; // Para qualquer rotação residual
            finalMoveVelocity = new Vector3(0, rb.linearVelocity.y, 0); // Atualiza para refletir parada
        }
    }

    void HandleInput() // Chamado de Update, apenas se CanMove for true
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Se estiver travado, o input de movimento pode ser interpretado de forma diferente (strafe)
        // Por enquanto, vamos manter a lógica de movimento relativa à câmera.
        // Uma melhoria seria fazer o movimento ser relativo ao jogador quando em lock-on.
        bool isLocked = (targetLockScript != null && targetLockScript.EstaTravado);

        if (cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            if (isLocked)
            {
                // Em lock-on, o forward/right do input se torna relativo ao JOGADOR
                // para permitir strafe e movimento para trás enquanto encara o alvo.
                // A rotação do jogador já está sendo cuidada por ApplyRotation para encarar o alvo.
                moveInputDirection = (transform.forward * vertical + transform.right * horizontal).normalized;
            }
            else
            {
                moveInputDirection = (camForward * vertical + camRight * horizontal).normalized;
            }
        }
        else
        {
            moveInputDirection = new Vector3(horizontal, 0, vertical).normalized;
        }

        isSprinting = Input.GetKey(KeyCode.LeftShift) && !isCrouching && moveInputDirection.magnitude > 0.1f && !isLocked; // Não corre em lock-on? (Opcional)
        wantsToCrouch = Input.GetKey(KeyCode.LeftControl);

        jumpRequestedThisFrame = false;
        if (isGrounded && Input.GetKeyDown(KeyCode.Space) && !isCrouching)
        {
            jumpRequestedThisFrame = true;
        }
    }

    // Método público para outros scripts (como PlayerPickup, PlayerAttack) desabilitarem o movimento
    public void SetCanMove(bool state)
    {
        CanMove = state;
    }

    private bool HasParameter(Animator animator, string paramName)
    {
        if (animator == null || string.IsNullOrEmpty(paramName)) return false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    void CheckGrounded()
    {
        bool previouslyGrounded = isGrounded;
        if (groundCheckPoint == null) { isGrounded = false; return; }
        isGrounded = Physics.CheckSphere(groundCheckPoint.position, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);

        if (!previouslyGrounded && isGrounded && isJumpingOrFalling)
        {
            isJumpingOrFalling = false;
            if (characterAnimator != null && HasParameter(characterAnimator, "LandTrigger")) characterAnimator.SetTrigger("LandTrigger");
        }

        float yVelocity = (rb != null) ? rb.linearVelocity.y : -1f;
        if (!isGrounded && !isJumpingOrFalling && yVelocity < -0.1f)
        {
            isJumpingOrFalling = true;
        }
    }

    void ProcessCrouch()
    {
        if (wantsToCrouch)
        {
            if (!isCrouching)
            {
                isCrouching = true;
                capsuleCollider.height = crouchingHeight;
                capsuleCollider.center = new Vector3(capsuleCollider.center.x, crouchingCenterY, capsuleCollider.center.z);
                if (groundCheckPoint != null)
                    groundCheckPoint.localPosition = new Vector3(groundCheckPoint.localPosition.x, (-crouchingHeight / 2f) + capsuleCollider.radius * 0.5f, groundCheckPoint.localPosition.z);
            }
        }
        else
        {
            if (isCrouching)
            {
                if (CanStandUp())
                {
                    isCrouching = false;
                    capsuleCollider.height = standingHeight;
                    capsuleCollider.center = new Vector3(capsuleCollider.center.x, standingCenterY, capsuleCollider.center.z);
                    if (groundCheckPoint != null)
                        groundCheckPoint.localPosition = new Vector3(groundCheckPoint.localPosition.x, (-standingHeight / 2f) + capsuleCollider.radius * 0.5f, groundCheckPoint.localPosition.z);
                }
            }
        }
    }

    bool CanStandUp()
    {
        Vector3 currentCapsuleCenter = transform.TransformPoint(capsuleCollider.center);
        Vector3 castOrigin = currentCapsuleCenter - transform.up * (crouchingHeight / 2f - capsuleCollider.radius * 1.01f);
        float castDistance = standingHeight - crouchingHeight + capsuleCollider.radius * 0.02f;
        return !Physics.SphereCast(castOrigin, capsuleCollider.radius * 0.9f, transform.up, out RaycastHit _, castDistance, obstructionLayers, QueryTriggerInteraction.Ignore);
    }

    void CalculateFinalMoveVelocity() // Chamado apenas se CanMove é true
    {
        if (isCrouching) currentSpeed = crouchSpeed;
        else if (isSprinting) currentSpeed = sprintSpeed;
        else currentSpeed = walkSpeed;

        // moveInputDirection já está normalizado e relativo à câmera ou jogador (se lock-on)
        Vector3 targetVelocity = moveInputDirection * currentSpeed;
        finalMoveVelocity = new Vector3(targetVelocity.x, 0, targetVelocity.z); // Não mexe no Y aqui, rb.velocity.y cuida disso
    }

    void ApplyRotation() // Chamado de FixedUpdate, apenas se CanMove é true
    {
        // Não precisa verificar rb == null ou !CanMove aqui, pois FixedUpdate já faz isso.

        if (targetLockScript != null && targetLockScript.EstaTravado && targetLockScript.AlvoTravadoAtual != null)
        {
            // MODO LOCK-ON: Encarar o alvo travado
            Vector3 direcaoParaAlvo = (targetLockScript.AlvoTravadoAtual.position - transform.position);
            direcaoParaAlvo.y = 0; // Ignora a diferença de altura para a rotação no plano XZ

            if (direcaoParaAlvo.sqrMagnitude > 0.001f) // Evita LookRotation com vetor zero
            {
                Quaternion rotacaoAlvoLockOn = Quaternion.LookRotation(direcaoParaAlvo);
                // Usa rotationSpeed multiplicada para uma virada mais rápida e "firme" no alvo
                rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, rotacaoAlvoLockOn, rotationSpeed * lockOnRotationSpeedMultiplier * Time.fixedDeltaTime));
            }
        }
        else
        {
            // MODO NORMAL: Rotação baseada na direção do input de movimento (moveInputDirection)
            if (moveInputDirection.sqrMagnitude > 0.01f) // Se houver input de movimento
            {
                Quaternion rotacaoAlvoNormal = Quaternion.LookRotation(moveInputDirection);
                rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, rotacaoAlvoNormal, rotationSpeed * Time.fixedDeltaTime));
            }
        }
    }

    void ApplyJump() // Chamado de FixedUpdate, apenas se CanMove é true
    {
        if (jumpRequestedThisFrame && isGrounded) // isGrounded é atualizado em CheckGrounded()
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            if (characterAnimator != null && HasParameter(characterAnimator, "JumpTrigger")) characterAnimator.SetTrigger("JumpTrigger");
            isJumpingOrFalling = true; // Define o estado de pulo
            jumpRequestedThisFrame = false; // Consome a solicitação
        }
    }

    void UpdateAnimatorParameters() // Chamado de Update
    {
        if (characterAnimator == null) return;

        float targetSpeedPercent = 0f;
        bool isCurrentlyLockedOn = (targetLockScript != null && targetLockScript.EstaTravado);

        // A velocidade para o Animator deve ser baseada no moveInputDirection,
        // que já considera se está em lock-on (strafe) ou não.
        if (moveInputDirection.magnitude > 0.1f && CanMove) // Se há intenção de movimento e pode se mover
        {
            // Se estiver em lock-on, a velocidade de "sprint" pode não ser aplicável,
            // ou você pode querer uma velocidade de "combate" diferente.
            // Por ora, se estiver correndo (isSprinting) E não em lock-on, usa 1.0. Senão 0.5 para andar.
            if (isSprinting && !isCurrentlyLockedOn) targetSpeedPercent = 1.0f;
            else targetSpeedPercent = 0.5f;
        }

        if (HasParameter(characterAnimator, "SpeedPercent"))
            characterAnimator.SetFloat("SpeedPercent", targetSpeedPercent, 0.1f, Time.deltaTime);
        else if (HasParameter(characterAnimator, "Speed")) // Fallback
            characterAnimator.SetFloat("Speed", targetSpeedPercent * (isSprinting && !isCurrentlyLockedOn ? sprintSpeed : walkSpeed), 0.1f, Time.deltaTime);


        if (HasParameter(characterAnimator, "IsCrouching")) characterAnimator.SetBool("IsCrouching", isCrouching);
        if (HasParameter(characterAnimator, "IsGrounded")) characterAnimator.SetBool("IsGrounded", isGrounded);
        if (HasParameter(characterAnimator, "IsJumpingOrFalling")) characterAnimator.SetBool("IsJumpingOrFalling", isJumpingOrFalling);
        if (HasParameter(characterAnimator, "IsLockedOn")) characterAnimator.SetBool("IsLockedOn", isCurrentlyLockedOn); // NOVO: Parâmetro para o Animator
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        }
        if (capsuleCollider != null && isCrouching && Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Vector3 currentCapsuleCenter = transform.TransformPoint(capsuleCollider.center);
            Vector3 castOrigin = currentCapsuleCenter - transform.up * (crouchingHeight / 2f - capsuleCollider.radius * 1.01f);
            float castDistance = standingHeight - crouchingHeight + capsuleCollider.radius * 0.02f;
            Gizmos.DrawWireSphere(castOrigin + transform.up * castDistance, capsuleCollider.radius * 0.9f);
        }
    }
}