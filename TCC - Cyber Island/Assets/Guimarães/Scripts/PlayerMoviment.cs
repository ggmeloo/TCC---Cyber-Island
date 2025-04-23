using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Animation")]
    [Tooltip("Assign the Animator component from the player model.")]
    [SerializeField] Animator playerAnimator; // Arraste o componente Animator aqui

    [Header("References")]
    // --- REFERÊNCIA DA CÂMERA PRINCIPAL NECESSÁRIA ---
    [Tooltip("Assign the Transform of the main camera used for orientation.")]
    [SerializeField] Transform mainCameraTransform; // <<< ARRASTE SUA MAIN CAMERA AQUI
    // -------------------------------------------------
    [SerializeField] Transform groundCheckOrigin;
    [SerializeField] LayerMask groundMask;
    [SerializeField] LayerMask interactableMask;

    [Header("Movement Settings")]
    [SerializeField] float walkSpeed = 5f;
    [SerializeField] float sprintSpeed = 8f;
    [SerializeField] float crouchSpeed = 2.5f;
    [SerializeField] float speedChangeRate = 10.0f;
    [SerializeField] float rotationSpeed = 15f; // Velocidade de rotação do corpo do jogador

    [Header("Jump Settings")]
    [SerializeField] float jumpForce = 8f;
    [SerializeField] float gravityMultiplier = 2.5f;
    [SerializeField] float groundCheckDistance = 0.3f;

    [Header("Crouch Settings")]
    [SerializeField] float standingHeight = 2.0f;
    [SerializeField] float crouchingHeight = 1.0f;
    [SerializeField] float crouchTransitionSpeed = 10f;

    // --- Variáveis de Look Removidas ---
    // [Header("Look Settings")]
    // [SerializeField] float lookSensitivityX = 2.0f;
    // [SerializeField] float lookSensitivityY = 2.0f;
    // [SerializeField] float minXRotation = -30f;
    // [SerializeField] float maxXRotation = 70f;

    [Header("Interaction")]
    [SerializeField] float interactionDistance = 3f;

    // --- Private Variables ---
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Vector2 moveInput;
    // private Vector2 lookInput; // Removido
    private float targetSpeed;
    private float currentSpeed;
    // private float cameraPitch = 0f; // Removido
    private float currentCapsuleHeight;
    private Vector3 worldMoveDirection = Vector3.zero;

    // State Booleans
    private bool isGrounded;
    private bool isSprinting;
    private bool isCrouching = false;
    private bool jumpRequested = false;
    private bool crouchHeld = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        // Obtém Animator
        if (playerAnimator == null)
            playerAnimator = GetComponentInChildren<Animator>();
        if (!playerAnimator)
            Debug.LogError("Animator component not found!", this);

        // --- VERIFICA REFERÊNCIA DA CÂMERA PRINCIPAL ---
        if (!mainCameraTransform)
            Debug.LogError("Main Camera Transform not assigned! Movement direction and interaction may fail.", this);
        // -------------------------------------------------

        if (!groundCheckOrigin)
        {
            groundCheckOrigin = transform;
            Debug.LogWarning("Ground Check Origin not assigned, defaulting to player transform base.", this);
        }

        // Cursor Lock/Visibility deve ser gerenciado pelo script da câmera agora
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;

        currentCapsuleHeight = standingHeight;
        capsuleCollider.height = currentCapsuleHeight;
        capsuleCollider.center = new Vector3(0, standingHeight / 2f, 0);
    }

    void Update()
    {
        HandleInput();
        // HandleLook(); // Removido
        HandleInteractionCheck(); // Checa se E foi pressionado
        HandleAttackInput(); // Placeholder
        CalculateMoveDirection(); // Calcula direção baseado na câmera
        UpdateAnimatorParameters(); // Atualiza animações
    }

    void FixedUpdate()
    {
        GroundCheck();
        HandleMovement(); // Aplica movimento físico
        HandleRotation(); // Rotaciona o corpo do jogador
        HandleJump();     // Aplica pulo físico e trigger de animação
        HandleCrouch();   // Ajusta altura do collider
        ApplyGravity();   // Aplica gravidade extra
    }

    void HandleInput()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        // lookInput.x = Input.GetAxis("Mouse X"); // Removido
        // lookInput.y = Input.GetAxis("Mouse Y"); // Removido

        isSprinting = Input.GetKey(KeyCode.LeftShift) && moveInput.magnitude > 0.1f && !isCrouching;
        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching) jumpRequested = true;
        crouchHeld = Input.GetKey(KeyCode.LeftControl);
        if (Input.GetKeyDown(KeyCode.E)) TryInteract(); // Tenta interagir ao apertar E
    }

    // void HandleLook() // Método Removido

    void CalculateMoveDirection()
    {
        // --- USA A REFERÊNCIA DA CÂMERA PRINCIPAL ---
        if (mainCameraTransform == null) return; // Sai se não houver câmera

        Vector3 cameraForward = mainCameraTransform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        Vector3 cameraRight = mainCameraTransform.right;
        cameraRight.y = 0;
        cameraRight.Normalize();

        worldMoveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;
        // --------------------------------------------
    }


    void GroundCheck()
    {
        if (groundCheckOrigin == null) return;
        isGrounded = Physics.CheckSphere(groundCheckOrigin.position, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
    }

    void HandleMovement()
    {
        if (isCrouching) targetSpeed = crouchSpeed;
        else if (isSprinting) targetSpeed = sprintSpeed;
        else targetSpeed = walkSpeed;
        if (moveInput == Vector2.zero) targetSpeed = 0.0f;

        float speedOffset = 0.1f;
        if (Mathf.Abs(currentSpeed - targetSpeed) > speedOffset)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.fixedDeltaTime * speedChangeRate);
            currentSpeed = Mathf.Round(currentSpeed * 100f) / 100f;
        }
        else currentSpeed = targetSpeed;

        Vector3 targetVelocity = worldMoveDirection * currentSpeed;
        targetVelocity.y = rb.linearVelocity.y;
        rb.linearVelocity = targetVelocity;

        if (isGrounded && !jumpRequested) rb.AddForce(Vector3.down * 2f, ForceMode.Force);
    }

    void HandleRotation()
    {
        // Rotaciona o CORPO do jogador para a direção do movimento
        if (worldMoveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(worldMoveDirection, Vector3.up);
            Quaternion newRotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
            rb.MoveRotation(newRotation);
        }
    }

    void ApplyGravity()
    {
        if (!isGrounded && rb.linearVelocity.y < 0)
        {
            rb.AddForce(Physics.gravity * (gravityMultiplier - 1f) * rb.mass);
        }
    }

    void HandleJump()
    {
        if (jumpRequested)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            if (playerAnimator != null)
            {
                playerAnimator.SetTrigger("Jump");
            }
            else { Debug.LogError("Animator is NULL when trying to Jump!"); }

            jumpRequested = false;
        }
    }

    void HandleCrouch()
    {
        float targetHeight = crouchHeld ? crouchingHeight : standingHeight;
        if (Mathf.Abs(currentCapsuleHeight - targetHeight) > 0.01f)
        {
            if (targetHeight > currentCapsuleHeight && CheckObstructionAbove(targetHeight)) targetHeight = currentCapsuleHeight;
            else
            {
                currentCapsuleHeight = Mathf.Lerp(currentCapsuleHeight, targetHeight, Time.fixedDeltaTime * crouchTransitionSpeed);
                capsuleCollider.height = currentCapsuleHeight;
                capsuleCollider.center = new Vector3(0, currentCapsuleHeight / 2f, 0);
            }
        }
        else
        {
            currentCapsuleHeight = targetHeight;
            capsuleCollider.height = currentCapsuleHeight;
            capsuleCollider.center = new Vector3(0, currentCapsuleHeight / 2f, 0);
        }
        isCrouching = currentCapsuleHeight < (standingHeight - 0.1f);
    }

    bool CheckObstructionAbove(float targetHeight)
    {
        if (capsuleCollider == null) return false;
        float radius = capsuleCollider.radius;
        float checkDistance = targetHeight - currentCapsuleHeight;
        Vector3 centerOffset = capsuleCollider.center;
        Vector3 point1 = transform.position + centerOffset + Vector3.up * (currentCapsuleHeight / 2f - radius);
        Vector3 point2 = transform.position + centerOffset - Vector3.up * (currentCapsuleHeight / 2f - radius);
        return Physics.CapsuleCast(point1, point2, radius, Vector3.up, checkDistance + 0.05f, ~groundMask, QueryTriggerInteraction.Ignore);
    }

    void HandleInteractionCheck()
    {
        // Apenas visualização nos Gizmos, a ação ocorre em TryInteract()
    }

    void TryInteract()
    {
        // --- USA A REFERÊNCIA DA CÂMERA PRINCIPAL ---
        if (mainCameraTransform == null)
        {
            Debug.LogWarning("Cannot interact: Main Camera Transform is not assigned.");
            return;
        }
        // --------------------------------------------

        RaycastHit hit;
        // Raycast sai da posição/direção da câmera principal
        if (Physics.Raycast(mainCameraTransform.position, mainCameraTransform.forward, out hit, interactionDistance, interactableMask))
        {
            InteractableItem item = hit.collider.GetComponent<InteractableItem>();
            if (item != null) item.Interact();
            else Debug.LogWarning($"Object {hit.collider.name} is on Interactable layer but has no InteractableItem script.");
        }
    }

    void HandleAttackInput() // Placeholder
    {
        if (Input.GetMouseButtonDown(0)) Debug.Log("Primary Action (Left Mouse Button) Pressed");
        if (Input.GetMouseButtonDown(1)) Debug.Log("Secondary Action (Right Mouse Button) Pressed");
    }

    void UpdateAnimatorParameters()
    {
        if (playerAnimator == null) return;

        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float currentHorizontalSpeed = horizontalVelocity.magnitude;

        playerAnimator.SetFloat("Speed", currentHorizontalSpeed);
        playerAnimator.SetBool("IsGrounded", isGrounded);
        playerAnimator.SetBool("IsCrouching", isCrouching);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheckOrigin != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheckOrigin.position, groundCheckDistance);
        }
        // --- USA A REFERÊNCIA DA CÂMERA PRINCIPAL PARA GIZMO DE INTERAÇÃO ---
        Gizmos.color = Color.yellow;
        if (mainCameraTransform != null)
        {
            Gizmos.DrawRay(mainCameraTransform.position, mainCameraTransform.forward * interactionDistance);
        }
        // -----------------------------------------------------------------
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, worldMoveDirection * 1.5f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, transform.forward * 1.0f);
    }
}

// --- Classe InteractableItem ---
public class InteractableItem : MonoBehaviour
{
    public void Interact()
    {
        Debug.Log($"Interaction Successful with {gameObject.name}");
    }
}