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

    [Header("Configurações de Movimento")]
    public float walkSpeed = 3.0f;
    public float sprintSpeed = 6.0f;
    public float crouchSpeed = 1.5f;
    public float rotationSpeed = 720f;
    public float jumpForce = 7f;
    private bool canMove = true; // Flag para controlar se o movimento está habilitado

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
    [SerializeField] private Vector3 moveInputDirection = Vector3.zero; // Input do jogador
    [SerializeField] private Vector3 finalMoveVelocity = Vector3.zero; // Velocidade a ser aplicada ao Rigidbody
    [SerializeField] private float currentSpeed;
    [SerializeField] private bool isCrouching = false;
    [SerializeField] private bool wantsToCrouch = false;
    [SerializeField] private bool isSprinting = false;
    [SerializeField] private bool isGrounded = true;
    [SerializeField] private bool isJumpingOrFalling = false;
    private bool jumpRequestedThisFrame = false; // Para registrar a intenção de pulo

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        if (characterAnimator == null) characterAnimator = GetComponentInChildren<Animator>();
        if (cameraTransform == null) cameraTransform = Camera.main.transform;

        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        // Configurações do CapsuleCollider
        capsuleCollider.height = standingHeight;
        standingCenterY = capsuleCollider.center.y;
        crouchingCenterY = standingCenterY - (standingHeight - crouchingHeight) / 2.0f;
        capsuleCollider.center = new Vector3(capsuleCollider.center.x, standingCenterY, capsuleCollider.center.z);

        if (groundCheckPoint == null)
        {
            // Debug.LogError("GroundCheckPoint não atribuído no PlayerMovement! Criando um.", this.gameObject);
            GameObject gcp = new GameObject("GroundCheckPoint_Auto");
            gcp.transform.SetParent(transform);
            gcp.transform.localPosition = new Vector3(0, (-standingHeight / 2f) + capsuleCollider.radius * 0.5f, 0);
            groundCheckPoint = gcp.transform;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // A verificação de canMove deve ser a PRIMEIRA COISA no Update
        if (!canMove)
        {
            // Se não pode mover, zera a intenção de movimento
            moveInputDirection = Vector3.zero;
            isSprinting = false;
            // wantsToCrouch também pode ser resetado se não quiser permitir agachar durante o diálogo
            // wantsToCrouch = false;
            jumpRequestedThisFrame = false;

            // Garante que o Animator reflita a parada
            if (characterAnimator != null)
            {
                // Verifica se o parâmetro existe antes de tentar defini-lo
                if (characterAnimator.parameters.Any(p => p.name == "SpeedPercent"))
                    characterAnimator.SetFloat("SpeedPercent", 0f, 0.1f, Time.deltaTime);
                else if (characterAnimator.parameters.Any(p => p.name == "Speed")) // Fallback para nome "Speed"
                    characterAnimator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
            }
            return; // Sai do Update para não processar mais nada relacionado a input/movimento
        }

        // Se canMove é true, processa os inputs
        HandleInput();
        ProcessCrouch(); // ProcessCrouch pode continuar para permitir agachar/levantar visualmente se desejado
                         // ou pode ser movido para dentro do if(canMove) de HandleInput.
                         // Por ora, vamos deixar aqui, mas o movimento físico será impedido em FixedUpdate.

        if (characterAnimator != null) UpdateAnimatorParameters();
    }

    void FixedUpdate()
    {
        CheckGrounded(); // Verifica o chão independentemente de canMove, pois afeta o estado do animator

        if (!canMove) // Se não pode mover, também não aplica física de movimento
        {
            // Garante que o Rigidbody pare se não for cinemático
            if (rb != null && !rb.isKinematic)
            {
                // Para o movimento horizontal, mas permite que a gravidade atue se estiver no ar
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                rb.angularVelocity = Vector3.zero;
            }
            finalMoveVelocity = new Vector3(0, rb.linearVelocity.y, 0); // Atualiza finalMoveVelocity para refletir parada
            return;
        }

        // Se canMove é true, processa a física do movimento
        CalculateFinalMoveVelocity();
        ApplyMovementAndRotation();
        ApplyJump();
    }

    void HandleInput() // Esta função agora só coleta inputs se canMove for true (devido ao return em Update)
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if (cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();
            moveInputDirection = (camForward * vertical + camRight * horizontal).normalized;
        }
        else
        {
            moveInputDirection = new Vector3(horizontal, 0, vertical).normalized;
        }


        isSprinting = Input.GetKey(KeyCode.LeftShift) && !isCrouching && moveInputDirection.magnitude > 0.1f;
        wantsToCrouch = Input.GetKey(KeyCode.LeftControl);

        jumpRequestedThisFrame = false; // Reseta a flag de pulo
        if (isGrounded && Input.GetKeyDown(KeyCode.Space) && !isCrouching)
        {
            jumpRequestedThisFrame = true; // Registra a intenção de pular
            // O trigger do animator é enviado em ApplyJump para melhor sincronia com a física
        }
    }

    public void SetMovementEnabled(bool enabledStatus)
    {
        canMove = enabledStatus;
        // Debug.Log("PlayerMovement: Movimento " + (canMove ? "HABILITADO" : "DESABILITADO"));

        if (!canMove)
        {
            // Zera a intenção de movimento imediatamente
            moveInputDirection = Vector3.zero;
            isSprinting = false;
            jumpRequestedThisFrame = false;
            finalMoveVelocity = new Vector3(0, rb != null ? rb.linearVelocity.y : 0, 0);

            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                rb.angularVelocity = Vector3.zero;
            }

            // O DialogueSystem vai tentar controlar o IsTalking do player.
            // Mas se quiser garantir que a animação de movimento pare:
            if (characterAnimator != null && HasParameter(characterAnimator, "SpeedPercent")) // Ou "Speed"
            {
                characterAnimator.SetFloat("SpeedPercent", 0f);
            }
        }
        // Se canMove se torna true, o Update normal do PlayerMovement reassumirá o controle da animação de Speed.
    }

    // Função auxiliar, se ainda não tiver uma similar
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
        // ... (código como antes, mas usando rb.linearVelocity.y) ...
        bool previouslyGrounded = isGrounded;
        if (groundCheckPoint == null) { isGrounded = false; return; }
        isGrounded = Physics.CheckSphere(groundCheckPoint.position, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);

        if (!previouslyGrounded && isGrounded && isJumpingOrFalling)
        {
            isJumpingOrFalling = false;
            if (characterAnimator != null) characterAnimator.SetTrigger("LandTrigger");
        }
        // Usar rb.linearVelocity.y se rb existir
        float yVelocity = (rb != null) ? rb.linearVelocity.y : -1f; // Fallback se rb for nulo
        if (!isGrounded && !isJumpingOrFalling && yVelocity < -0.1f)
        {
            isJumpingOrFalling = true;
        }
    }

    void ProcessCrouch()
    {
        // ... (código como antes) ...
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
        // ... (código como antes) ...
        Vector3 currentCapsuleCenter = transform.TransformPoint(capsuleCollider.center);
        Vector3 castOrigin = currentCapsuleCenter + transform.up * (crouchingHeight / 2f - capsuleCollider.radius);
        float castDistance = standingHeight - crouchingHeight;
        return !Physics.SphereCast(castOrigin, capsuleCollider.radius * 0.9f, transform.up, out RaycastHit _, castDistance, obstructionLayers, QueryTriggerInteraction.Ignore);
    }

    void CalculateFinalMoveVelocity() // Chamado em FixedUpdate apenas se canMove == true
    {
        if (isCrouching) currentSpeed = crouchSpeed;
        else if (isSprinting) currentSpeed = sprintSpeed;
        else currentSpeed = walkSpeed;

        Vector3 targetVelocity = moveInputDirection * currentSpeed;
        // Mantém a velocidade Y atual do Rigidbody (para gravidade, pulo)
        finalMoveVelocity = new Vector3(targetVelocity.x, rb != null ? rb.linearVelocity.y : 0, targetVelocity.z);
    }

    void ApplyMovementAndRotation() // Chamado em FixedUpdate apenas se canMove == true
    {
        if (rb == null) return;
        rb.linearVelocity = finalMoveVelocity;

        if (moveInputDirection.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveInputDirection);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }
    }

    void ApplyJump() // Chamado em FixedUpdate apenas se canMove == true
    {
        if (rb == null) return;
        if (jumpRequestedThisFrame && isGrounded) // Verifica se o pulo foi solicitado e ainda estamos no chão
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            if (characterAnimator != null) characterAnimator.SetTrigger("JumpTrigger"); // Dispara animação aqui
            isJumpingOrFalling = true; // Define o estado de pulo
            jumpRequestedThisFrame = false; // Consome a solicitação de pulo
        }
    }

    void UpdateAnimatorParameters() // Chamado em Update, mas os inputs de movimento (moveInputDirection) serão zero se !canMove
    {
        if (characterAnimator == null) return;

        float targetSpeedPercent = 0f;
        // Usa moveInputDirection, que será zerado em Update se !canMove
        if (moveInputDirection.magnitude > 0.1f && canMove) // Adicionada checagem de canMove aqui também para clareza
        {
            if (isSprinting) targetSpeedPercent = 1.0f;
            else targetSpeedPercent = 0.5f;
        }
        // Se !canMove, moveInputDirection será zero, então targetSpeedPercent será zero.

        characterAnimator.SetFloat("SpeedPercent", targetSpeedPercent, 0.1f, Time.deltaTime);
        characterAnimator.SetBool("IsCrouching", isCrouching);
        characterAnimator.SetBool("IsGrounded", isGrounded);
        characterAnimator.SetBool("IsJumpingOrFalling", isJumpingOrFalling);
    }
}