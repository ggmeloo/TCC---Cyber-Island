using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Animation")]
    [Tooltip("Assign the Animator component from the player model.")]
    [SerializeField] Animator playerAnimator;

    [Header("References")]
    [Tooltip("Assign the Transform of the main camera used for orientation.")]
    [SerializeField] Transform mainCameraTransform;
    [SerializeField] Transform groundCheckOrigin;
    [SerializeField] LayerMask groundMask;
    [SerializeField] LayerMask interactableMask;

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

    [Header("Interaction")]
    [SerializeField] float interactionDistance = 3f;

    // --- Variáveis de Ataque ---
    [Header("Attack Settings")]
    [Tooltip("Número máximo de ataques no combo.")]
    [SerializeField] int maxComboSteps = 3;
    private bool isAttacking = false;       // O jogador está atualmente executando uma animação de ataque?
    private int currentComboStep = 0;      // Qual ataque no combo está ativo (0=nenhum, 1, 2, 3)
    private bool nextAttackRequested = false; // O jogador clicou durante a janela de combo?

    // --- Private Variables ---
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Vector2 moveInput;
    private float targetSpeed;
    private float currentSpeed;
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

        if (playerAnimator == null) playerAnimator = GetComponentInChildren<Animator>();
        if (!playerAnimator) Debug.LogError("Animator component not found!", this);
        if (!mainCameraTransform) Debug.LogError("Main Camera Transform not assigned!", this);
        if (!groundCheckOrigin) groundCheckOrigin = transform;

        currentCapsuleHeight = standingHeight;
        capsuleCollider.height = currentCapsuleHeight;
        capsuleCollider.center = new Vector3(0, standingHeight / 2f, 0);
    }

    void Update()
    {
        HandleInput(); // Lê todos os inputs, incluindo ataque
        HandleInteractionCheck();
        CalculateMoveDirection();
        UpdateAnimatorParameters(); // Atualiza Speed, IsGrounded, IsCrouching
    }

    void FixedUpdate()
    {
        GroundCheck();
        // Só permite movimento/rotação se NÃO estiver atacando (ou permite movimento limitado)
        if (!isAttacking)
        {
            HandleMovement();
            HandleRotation();
        }
        else
        {
            // Opcional: Reduzir a velocidade do Rigidbody durante o ataque para evitar deslizar
            // rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0); // Para completamente
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, new Vector3(0, rb.linearVelocity.y, 0), Time.fixedDeltaTime * 5f); // Desacelera
        }

        HandleJump();     // Só pula se não estiver atacando
        HandleCrouch();   // Só agacha se não estiver atacando
        ApplyGravity();
    }

    void HandleInput()
    {
        // Inputs de Movimento
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        isSprinting = Input.GetKey(KeyCode.LeftShift) && moveInput.magnitude > 0.1f && !isCrouching;
        if (Input.GetButtonDown("Jump") && CanPerformAction()) jumpRequested = true; // Só pula se puder
        crouchHeld = Input.GetKey(KeyCode.LeftControl);
        if (Input.GetKeyDown(KeyCode.E)) TryInteract();

        // --- Input de Ataque (Botão Esquerdo do Mouse) ---
        if (Input.GetMouseButtonDown(0))
        {
            HandleAttackInput();
        }
        // ------------------------------------------------
    }

    // Verifica se o jogador pode realizar ações como pular, agachar, começar ataque
    bool CanPerformAction()
    {
        return isGrounded && !isCrouching && !isAttacking;
    }

    void HandleAttackInput()
    {
        // Se já estamos atacando, registra que o próximo ataque foi solicitado (se estiver na janela válida)
        if (isAttacking)
        {
            // A lógica para verificar a "janela válida" será feita pelo Animation Event "EnableCombo"
            // Aqui, apenas dizemos que o jogador clicou de novo.
            if (currentComboStep < maxComboSteps) // Só requisita se não for o último ataque
            {
                nextAttackRequested = true;
                // Debug.Log("Next Attack Requested");
            }
        }
        // Se NÃO estamos atacando e podemos realizar ações, começa o combo
        else if (CanPerformAction())
        {
            // Debug.Log("Starting Combo - Attack 1");
            isAttacking = true;         // Entra no estado de ataque
            currentComboStep = 1;       // Começa no primeiro ataque
            nextAttackRequested = false;// Limpa a flag de próximo ataque
            playerAnimator.SetInteger("AttackIndex", currentComboStep); // Diz ao Animator para tocar o Ataque 1
            // Opcional: Rotacionar o jogador para a direção da câmera instantaneamente ao atacar
            if (mainCameraTransform != null)
            {
                Vector3 lookDirection = mainCameraTransform.forward;
                lookDirection.y = 0;
                if (lookDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(lookDirection);
                }
            }
        }
    }


    void CalculateMoveDirection()
    {
        if (mainCameraTransform == null) return;
        Vector3 cameraForward = mainCameraTransform.forward;
        cameraForward.y = 0; cameraForward.Normalize();
        Vector3 cameraRight = mainCameraTransform.right;
        cameraRight.y = 0; cameraRight.Normalize();
        worldMoveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;
    }

    void GroundCheck()
    {
        if (groundCheckOrigin == null) return;
        isGrounded = Physics.CheckSphere(groundCheckOrigin.position, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
    }

    void HandleMovement()
    {
        // Mesma lógica de antes
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
        // Mesma lógica de antes
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
        // Só executa o pulo se não estiver atacando
        if (jumpRequested && !isAttacking) // Adicionada verificação !isAttacking
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            if (playerAnimator != null) playerAnimator.SetTrigger("Jump");
            else Debug.LogError("Animator is NULL when trying to Jump!");

            jumpRequested = false;
        }
        // Se um pulo foi requisitado mas está atacando, apenas consome o request
        else if (jumpRequested && isAttacking)
        {
            jumpRequested = false;
        }
    }

    void HandleCrouch()
    {
        // Só permite agachar/levantar se não estiver atacando
        if (!isAttacking)
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
        else // Se está atacando, força o estado de não-agachado no collider (se necessário)
        {
            if (isCrouching) // Se estava agachado quando começou a atacar, força a levantar visualmente
            {
                isCrouching = false; // Atualiza estado lógico
                                     // A altura do collider pode precisar ser ajustada aqui também se o ataque não puder ser feito agachado
            }
        }

    }

    bool CheckObstructionAbove(float targetHeight)
    {
        // Mesma lógica de antes
        if (capsuleCollider == null) return false;
        float radius = capsuleCollider.radius;
        float checkDistance = targetHeight - currentCapsuleHeight;
        Vector3 centerOffset = capsuleCollider.center;
        Vector3 point1 = transform.position + centerOffset + Vector3.up * (currentCapsuleHeight / 2f - radius);
        Vector3 point2 = transform.position + centerOffset - Vector3.up * (currentCapsuleHeight / 2f - radius);
        return Physics.CapsuleCast(point1, point2, radius, Vector3.up, checkDistance + 0.05f, ~groundMask, QueryTriggerInteraction.Ignore);
    }

    void TryInteract()
    {
        // Não interage se estiver atacando
        if (isAttacking || mainCameraTransform == null) return;

        RaycastHit hit;
        if (Physics.Raycast(mainCameraTransform.position, mainCameraTransform.forward, out hit, interactionDistance, interactableMask))
        {
            InteractableItem item = hit.collider.GetComponent<InteractableItem>();
            if (item != null) item.Interact();
            else Debug.LogWarning($"Object {hit.collider.name} is on Interactable layer but has no InteractableItem script.");
        }
    }

    void UpdateAnimatorParameters()
    {
        if (playerAnimator == null) return;

        // --- Lógica de animação de movimento só atualiza se NÃO estiver atacando ---
        if (!isAttacking)
        {
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            float currentHorizontalSpeed = horizontalVelocity.magnitude;
            playerAnimator.SetFloat("Speed", currentHorizontalSpeed, 0.1f, Time.deltaTime); // Adiciona suavização
        }
        else
        {
            // Garante que a velocidade no animator seja 0 enquanto ataca, para não misturar com walk/run
            playerAnimator.SetFloat("Speed", 0f, 0.05f, Time.deltaTime);
        }
        // --------------------------------------------------------------------------

        playerAnimator.SetBool("IsGrounded", isGrounded);
        playerAnimator.SetBool("IsCrouching", isCrouching);

        // O parâmetro AttackIndex é controlado pela lógica de HandleAttackInput e Animation Events
    }

    // --- MÉTODOS CHAMADOS POR ANIMATION EVENTS ---

    // Chamado por um evento na animação de ataque PERTO do fim,
    // definindo a janela onde o próximo clique é registrado para o combo.
    public void EnableComboWindow()
    {
        if (currentComboStep < maxComboSteps) // Só habilita se não for o último ataque
        {
            // Debug.Log($"Combo Window OPEN for step {currentComboStep}");
            // Se o jogador já clicou (nextAttackRequested é true), inicia o próximo ataque IMEDIATAMENTE
            if (nextAttackRequested)
            {
                currentComboStep++;
                // Debug.Log($"Attack {currentComboStep} triggered immediately by buffered input.");
                playerAnimator.SetInteger("AttackIndex", currentComboStep);
                nextAttackRequested = false; // Consome a requisição
            }
            // Se não clicou ainda, apenas permite que o próximo clique seja registrado em HandleAttackInput
            // (Aqui não precisamos de uma flag extra 'canQueue', a flag 'nextAttackRequested' serve)
        }
    }

    // Chamado por um evento EXATAMENTE no final da animação de ataque.
    public void FinishAttack()
    {
        // Se um próximo ataque foi solicitado e processado em EnableComboWindow,
        // currentComboStep já terá sido incrementado. Se não foi solicitado,
        // significa que o combo parou aqui.
        if (!nextAttackRequested) // Se NENHUM clique foi bufferizado durante a janela
        {
            // Debug.Log($"Combo Finished or Interrupted at step {currentComboStep}. Resetting.");
            ResetCombo();
        }
        else
        {
            // Se nextAttackRequested ainda é true aqui, significa que EnableComboWindow não foi chamado
            // ou o clique aconteceu depois dela. Reseta o combo por segurança.
            // Debug.Log($"Next attack was requested but maybe too late? Resetting combo.");
            // nextAttackRequested = false; // Limpa a flag
            // ResetCombo();
            // Nota: A transição no animator baseada em AttackIndex=0 cuidará de voltar ao Idle/Locomotion
            // se ResetCombo for chamado. Se EnableComboWindow já setou o próximo AttackIndex,
            // a transição para o próximo ataque ocorrerá.
        }

        // Limpa a flag de qualquer forma ao final da animação
        nextAttackRequested = false;
    }

    // Função para resetar completamente o estado do combo
    private void ResetCombo()
    {
        isAttacking = false;
        currentComboStep = 0;
        nextAttackRequested = false;
        if (playerAnimator != null)
        {
            playerAnimator.SetInteger("AttackIndex", 0); // Diz ao animator para voltar ao estado normal
        }
    }


    // --- Gizmos (sem alterações) ---
    private void OnDrawGizmosSelected()
    {
        if (groundCheckOrigin != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheckOrigin.position, groundCheckDistance);
        }
        Gizmos.color = Color.yellow;
        if (mainCameraTransform != null)
        {
            Gizmos.DrawRay(mainCameraTransform.position, mainCameraTransform.forward * interactionDistance);
        }
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, worldMoveDirection * 1.5f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, transform.forward * 1.0f);
    }
}

// --- Classe InteractableItem (sem alterações) ---
public class InteractableItem : MonoBehaviour { /* ... */ }