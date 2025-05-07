using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Animator))]

public class PlayerMovement : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] Animator playerAnimator;

    [Header("References")]
    [SerializeField] Transform mainCameraTransform;
    [SerializeField] Transform groundCheckOrigin;
    [SerializeField] LayerMask groundMask = 1;

    [Header("Movement Settings")]
    [SerializeField] float walkSpeed = 5f;
    [SerializeField] float sprintSpeed = 8f;
    [SerializeField] float crouchSpeed = 2.5f;
    [SerializeField] float speedChangeRate = 10.0f;
    [SerializeField] float rotationSpeed = 15f;

    [Header("Jump Settings")]
    [SerializeField] float jumpForce = 8f;
    [SerializeField] float gravityMultiplier = 2.5f;
    [SerializeField] float groundCheckDistance = 0.3f;

    [Header("Crouch Settings")]
    [SerializeField] float standingHeight = 2.0f;
    [SerializeField] float crouchingHeight = 1.0f;
    [SerializeField] float crouchTransitionSpeed = 10f;

    [Header("Animation State Names (Locomotion Only)")] // <<< VERIFIQUE ESSES NOMES NO INSPECTOR
    [SerializeField] string idleStateName = "Idle";
    [SerializeField] string walkStateName = "Walk";
    [SerializeField] string runStateName = "Run";
    [SerializeField] string jumpStartStateName = "JumpStart";
    [SerializeField] string fallingStateName = "Falling";
    [SerializeField] string crouchIdleStateName = "CrouchIdle";
    [SerializeField] string crouchWalkStateName = "CrouchWalk";

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Vector2 moveInput;
    private float targetSpeed;
    private float currentSpeed;
    private float currentCapsuleHeight;
    private Vector3 worldMoveDirection = Vector3.zero;
    private bool isGrounded;
    private bool isSprinting;
    private bool isCrouching = false;
    private bool jumpRequested = false; // Flag para a física do pulo
    private bool animationJumpRequested = false; // Flag separada para a ANIMAÇÃO de pulo
    private bool crouchHeld = false;
    private bool canControlLocomotion = true;
    private string currentLocomotionAnimationState = "";

    public bool IsGrounded => isGrounded;
    public bool IsCrouching => isCrouching;
    public bool CanCurrentlyStartAction() => isGrounded && !isCrouching && canControlLocomotion;
    public bool CanControlLocomotion { get => canControlLocomotion; set => canControlLocomotion = value; }
    public Transform MainCameraTransformProperty => mainCameraTransform;

    // Hashes (para performance com Animator.Play)
    private int idleStateHash;
    private int walkStateHash;
    private int runStateHash;
    private int jumpStartStateHash;
    private int fallingStateHash;
    private int crouchIdleStateHash;
    private int crouchWalkStateHash;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        playerAnimator = GetComponent<Animator>();

        if (!playerAnimator) Debug.LogError("FATAL: Animator missing!", this);
        else { playerAnimator.applyRootMotion = false; }
        if (!mainCameraTransform) Debug.LogError("FATAL: MainCameraTransform missing!", this);
        if (rb == null) Debug.LogError("FATAL: Rigidbody missing!", this);
        if (capsuleCollider == null) Debug.LogError("FATAL: CapsuleCollider missing!", this);
        if (!groundCheckOrigin) { groundCheckOrigin = transform; Debug.LogWarning("GroundCheckOrigin not set."); }

        currentCapsuleHeight = standingHeight;
        if (capsuleCollider != null) { capsuleCollider.height = currentCapsuleHeight; capsuleCollider.center = new Vector3(0, standingHeight / 2f, 0); }

        // Calcula Hashes dos nomes dos estados de locomoção
        idleStateHash = Animator.StringToHash(idleStateName);
        walkStateHash = Animator.StringToHash(walkStateName);
        runStateHash = Animator.StringToHash(runStateName);
        jumpStartStateHash = Animator.StringToHash(jumpStartStateName);
        fallingStateHash = Animator.StringToHash(fallingStateName);
        crouchIdleStateHash = Animator.StringToHash(crouchIdleStateName);
        crouchWalkStateHash = Animator.StringToHash(crouchWalkStateName);

        currentLocomotionAnimationState = idleStateName; // Começa assumindo Idle
    }

    void Start() { CanControlLocomotion = true; }

    void Update()
    {
        if (Time.timeScale <= 0f) return;
        HandleInput();
        CalculateMoveDirection();
        UpdateLocomotionAnimationState(); // << Chama o método de animação
    }

    void FixedUpdate()
    {
        if (Time.timeScale <= 0f) return;
        GroundCheck();
        if (CanControlLocomotion) { HandleMovement(); HandleRotation(); }
        else { if (rb != null && !rb.isKinematic) rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, new Vector3(0, rb.linearVelocity.y, 0), Time.fixedDeltaTime * 10f); }
        HandleJump(); // Processa a física do pulo
        HandleCrouch();
        ApplyGravity();
    }

    void HandleInput()
    {
        if (CanControlLocomotion)
        {
            moveInput.x = Input.GetAxisRaw("Horizontal");
            moveInput.y = Input.GetAxisRaw("Vertical");
            isSprinting = Input.GetKey(KeyCode.LeftShift) && moveInput.magnitude > 0.1f && !isCrouching;

            // Requisita pulo para física E para animação
            if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
            {
                jumpRequested = true; // Para HandleJump em FixedUpdate
                animationJumpRequested = true; // Para UpdateLocomotionAnimationState
                Debug.Log("Jump Input Registered (animationJumpRequested = true)");
            }
            crouchHeld = Input.GetKey(KeyCode.LeftControl);
        }
        else { moveInput = Vector2.zero; isSprinting = false; jumpRequested = false; animationJumpRequested = false; crouchHeld = false; }
    }

    void CalculateMoveDirection() { /* ...código anterior... */ if (mainCameraTransform == null) return; Vector3 camF = mainCameraTransform.forward; camF.y = 0; camF.Normalize(); Vector3 camR = mainCameraTransform.right; camR.y = 0; camR.Normalize(); worldMoveDirection = (camF * moveInput.y + camR * moveInput.x).normalized; }
    void GroundCheck() { /* ...código anterior... */ if (groundCheckOrigin == null) { isGrounded = false; return; } bool prevGrounded = isGrounded; isGrounded = Physics.CheckSphere(groundCheckOrigin.position, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore); if (prevGrounded && !isGrounded) Debug.Log("Left Ground"); else if (!prevGrounded && isGrounded) Debug.Log("Landed on Ground"); }
    void HandleMovement() { /* ...código anterior... */ if (rb == null || rb.isKinematic) return; if (isCrouching) targetSpeed = crouchSpeed; else if (isSprinting) targetSpeed = sprintSpeed; else targetSpeed = walkSpeed; if (moveInput == Vector2.zero) targetSpeed = 0.0f; currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.fixedDeltaTime * speedChangeRate); currentSpeed = Mathf.Round(currentSpeed * 100f) / 100f; Vector3 targetVelocity = worldMoveDirection * currentSpeed; targetVelocity.y = rb.linearVelocity.y; rb.linearVelocity = targetVelocity; if (isGrounded && !jumpRequested) rb.AddForce(Vector3.down * 2f, ForceMode.Force); }
    void HandleRotation() { /* ...código anterior... */ if (rb == null || rb.isKinematic) return; if (worldMoveDirection != Vector3.zero && moveInput.magnitude > 0.1f && CanControlLocomotion) { Quaternion targetRotation = Quaternion.LookRotation(worldMoveDirection, Vector3.up); Quaternion newRotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed); rb.MoveRotation(newRotation); } }
    void ApplyGravity() { /* ...código anterior... */ if (rb == null || rb.isKinematic) return; if (!isGrounded && rb.linearVelocity.y < 0) { rb.AddForce(Physics.gravity * (gravityMultiplier - 1f) * rb.mass); } }

    void HandleJump() // Só física
    {
        if (rb == null || rb.isKinematic) return;
        if (jumpRequested) // jumpRequested é setado em HandleInput se CanControlLocomotion e condições são verdadeiras
        {
            Debug.Log("HandleJump: Applying Jump Force.");
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpRequested = false; // Consome pedido da física
        }
    }
    void HandleCrouch() { /* ...código anterior... */ if (capsuleCollider == null) return; float targetHeight = crouchHeld ? crouchingHeight : standingHeight; if (!CanControlLocomotion && isCrouching) targetHeight = standingHeight; if (Mathf.Abs(currentCapsuleHeight - targetHeight) > 0.01f) { if (targetHeight > currentCapsuleHeight && CheckObstructionAbove(targetHeight)) targetHeight = currentCapsuleHeight; else { currentCapsuleHeight = Mathf.Lerp(currentCapsuleHeight, targetHeight, Time.fixedDeltaTime * crouchTransitionSpeed); } } else { currentCapsuleHeight = targetHeight; } capsuleCollider.height = currentCapsuleHeight; capsuleCollider.center = new Vector3(0, currentCapsuleHeight / 2f, 0); isCrouching = currentCapsuleHeight < (standingHeight - 0.1f); if (Mathf.Approximately(currentCapsuleHeight, targetHeight)) { capsuleCollider.height = targetHeight; capsuleCollider.center = new Vector3(0, targetHeight / 2f, 0); } }
    bool CheckObstructionAbove(float targetHeight) { /* ...código anterior... */ if (capsuleCollider == null) return false; float radius = capsuleCollider.radius; float checkDistance = targetHeight - currentCapsuleHeight; Vector3 centerOffset = capsuleCollider.center; Vector3 p1 = transform.position + centerOffset + Vector3.up * (currentCapsuleHeight / 2f - radius); Vector3 p2 = transform.position + centerOffset - Vector3.up * (currentCapsuleHeight / 2f - radius); return Physics.CapsuleCast(p1, p2, radius, Vector3.up, checkDistance + 0.05f, ~groundMask, QueryTriggerInteraction.Ignore); }


    void UpdateLocomotionAnimationState()
    {
        if (playerAnimator == null) { Debug.LogError("UpdateLocomotionAnimationState: Animator is NULL."); return; }

        // Se este script não deve controlar a animação (ex: PlayerAttack está no controle), não faz nada.
        if (!CanControlLocomotion)
        {
            // Debug.Log("UpdateLocomotionAnimationState: SKIPPED (CanControlLocomotion is false)");
            currentLocomotionAnimationState = ""; // Reseta para que na próxima vez que puder, ele toque o estado correto
            return;
        }

        string targetStateName = idleStateName; // Assume Idle por padrão

        if (!isGrounded)
        {
            // Estamos no ar
            if (animationJumpRequested) // Se acabamos de pedir um pulo para a animação
            {
                targetStateName = jumpStartStateName;
                Debug.Log($"Animation Target: JumpStart (animationJumpRequested=true)");
            }
            else // Se não pedimos pulo AGORA, mas estamos no ar, estamos caindo
            {
                // Evita spammar Play(Falling) se já está em Falling ou JumpStart
                AnimatorStateInfo stateInfo = playerAnimator.GetCurrentAnimatorStateInfo(0);
                if (!stateInfo.IsName(fallingStateName) && !stateInfo.IsName(jumpStartStateName))
                {
                    targetStateName = fallingStateName;
                    Debug.Log($"Animation Target: Falling (not grounded, no jump request)");
                }
                else
                {
                    targetStateName = currentLocomotionAnimationState; // Mantém estado aéreo atual
                }
            }
        }
        else if (isCrouching) // No chão e agachado
        {
            targetStateName = (moveInput.magnitude > 0.1f) ? crouchWalkStateName : crouchIdleStateName;
            // Debug.Log($"Animation Target: {targetStateName} (Crouching, MoveMag: {moveInput.magnitude})");
        }
        else if (isSprinting) // No chão, em pé, correndo
        {
            targetStateName = runStateName;
            // Debug.Log($"Animation Target: Run");
        }
        else if (moveInput.magnitude > 0.1f) // No chão, em pé, andando
        {
            targetStateName = walkStateName;
            // Debug.Log($"Animation Target: Walk (MoveMag: {moveInput.magnitude})");
        }
        else // No chão, em pé, parado
        {
            targetStateName = idleStateName;
            // Debug.Log($"Animation Target: Idle");
        }

        // Toca a animação apenas se o estado alvo mudou
        if (currentLocomotionAnimationState != targetStateName && !string.IsNullOrEmpty(targetStateName))
        {
            Debug.Log($"PlayerMovement Playing Locomotion: FROM '{currentLocomotionAnimationState}' TO '{targetStateName}'");
            playerAnimator.Play(targetStateName, 0, 0f); // Layer 0, começa do início
            currentLocomotionAnimationState = targetStateName;
        }

        // Consome a flag de animação de pulo DEPOIS de usá-la
        if (animationJumpRequested)
        {
            animationJumpRequested = false;
        }
    }

    // --- Gizmos ---
    private void OnDrawGizmosSelected() { /* ...código anterior... */ }
}