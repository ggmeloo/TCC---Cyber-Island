using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))] // Mesmo que o Animator esteja no filho, esta dependência no pai não prejudica.
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Referências")]
    public Transform cameraTransform;
    public Animator characterAnimator; // Arraste o objeto PlayerModel aqui
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;

    [Header("Configurações de Movimento")]
    public float walkSpeed = 3.0f;
    public float sprintSpeed = 6.0f;
    public float crouchSpeed = 1.5f;
    public float rotationSpeed = 720f;
    public float jumpForce = 7f;

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


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        if (characterAnimator == null)
        {
            characterAnimator = GetComponentInChildren<Animator>();
            if (characterAnimator == null)
            {
                Debug.LogError("ANIMATOR NÃO ENCONTRADO no PlayerMovement ou em seus filhos! As animações não funcionarão.", this.gameObject);
            }
            else
            {
                Debug.Log("Animator encontrado via GetComponentInChildren em PlayerMovement.", this.gameObject);
            }
        }
        else
        {
            Debug.Log("Animator referenciado via Inspector em PlayerMovement: " + (characterAnimator != null), this.gameObject);
        }


        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }

        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (Mathf.Abs(capsuleCollider.height - standingHeight) > 0.01f)
        {
            //Debug.LogWarning($"Altura inicial do CapsuleCollider ({capsuleCollider.height}) não bate com standingHeight ({standingHeight}). Ajustando.");
        }
        capsuleCollider.height = standingHeight;
        standingCenterY = capsuleCollider.center.y;
        crouchingCenterY = standingCenterY - (standingHeight - crouchingHeight) / 2.0f;
        capsuleCollider.center = new Vector3(capsuleCollider.center.x, standingCenterY, capsuleCollider.center.z);

        if (groundCheckPoint == null)
        {
            Debug.LogError("GroundCheckPoint não atribuído no PlayerMovement!", this.gameObject);
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
        HandleInput();
        ProcessCrouch();
        if (characterAnimator != null) UpdateAnimatorParameters();
    }

    void FixedUpdate()
    {
        CheckGrounded();
        CalculateFinalMoveVelocity();
        ApplyMovementAndRotation();
        ApplyJump();
    }

    void HandleInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        moveInputDirection = (camForward * vertical + camRight * horizontal).normalized;

        isSprinting = Input.GetKey(KeyCode.LeftShift) && !isCrouching && moveInputDirection.magnitude > 0.1f;

        wantsToCrouch = Input.GetKey(KeyCode.LeftControl);
        //Debug.Log("HandleInput - wantsToCrouch: " + wantsToCrouch); // DEBUG CROUCH INTENT

        if (isGrounded && Input.GetKeyDown(KeyCode.Space) && !isCrouching)
        {
            if (characterAnimator != null) characterAnimator.SetTrigger("JumpTrigger");
            isJumpingOrFalling = true;
        }
    }

    void CheckGrounded()
    {
        bool previouslyGrounded = isGrounded;
        if (groundCheckPoint == null)
        {
            isGrounded = false;
            return;
        }
        isGrounded = Physics.CheckSphere(groundCheckPoint.position, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);

        if (!previouslyGrounded && isGrounded && isJumpingOrFalling)
        {
            isJumpingOrFalling = false;
            if (characterAnimator != null) characterAnimator.SetTrigger("LandTrigger");
        }
        if (!isGrounded && !isJumpingOrFalling && rb.linearVelocity.y < -0.1f)
        {
            isJumpingOrFalling = true;
        }
    }

    void ProcessCrouch()
    {
        //Debug.Log($"ProcessCrouch - Início: wantsToCrouch={wantsToCrouch}, isCrouching={isCrouching}"); // DEBUG CROUCH LOGIC
        if (wantsToCrouch)
        {
            if (!isCrouching)
            {
                isCrouching = true;
                //Debug.Log("ProcessCrouch - Agachando. isCrouching agora é: " + isCrouching); // DEBUG CROUCH STATE CHANGE
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
                    //Debug.Log("ProcessCrouch - Levantando. isCrouching agora é: " + isCrouching); // DEBUG CROUCH STATE CHANGE
                    capsuleCollider.height = standingHeight;
                    capsuleCollider.center = new Vector3(capsuleCollider.center.x, standingCenterY, capsuleCollider.center.z);
                    if (groundCheckPoint != null)
                        groundCheckPoint.localPosition = new Vector3(groundCheckPoint.localPosition.x, (-standingHeight / 2f) + capsuleCollider.radius * 0.5f, groundCheckPoint.localPosition.z);
                }
                //else Debug.Log("ProcessCrouch - Queria levantar, mas CanStandUp() retornou false."); // DEBUG CAN STAND UP
            }
        }
    }

    bool CanStandUp()
    {
        Vector3 currentCapsuleCenter = transform.TransformPoint(capsuleCollider.center);
        Vector3 castOrigin = currentCapsuleCenter + transform.up * (crouchingHeight / 2f - capsuleCollider.radius);
        float castDistance = standingHeight - crouchingHeight;

        if (Physics.SphereCast(castOrigin, capsuleCollider.radius * 0.9f, transform.up, out RaycastHit hit, castDistance, obstructionLayers, QueryTriggerInteraction.Ignore))
        {
            //Debug.Log("CanStandUp: Obstruído por " + hit.collider.name); // DEBUG OBSTRUCTION
            return false;
        }
        return true;
    }

    void CalculateFinalMoveVelocity()
    {
        if (isCrouching) currentSpeed = crouchSpeed;
        else if (isSprinting) currentSpeed = sprintSpeed;
        else currentSpeed = walkSpeed;

        Vector3 targetVelocity = moveInputDirection * currentSpeed;
        finalMoveVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
    }

    void ApplyMovementAndRotation()
    {
        rb.linearVelocity = finalMoveVelocity;

        if (moveInputDirection.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveInputDirection);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }
    }

    void ApplyJump()
    {
        if (isJumpingOrFalling && isGrounded && rb.linearVelocity.y < jumpForce * 0.5f)
        {
            if (Time.timeSinceLevelLoad < 0.1f || isGrounded)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            }
        }
    }

    void UpdateAnimatorParameters()
    {
        if (characterAnimator == null)
        {
            //Debug.LogError("UpdateAnimatorParameters: characterAnimator é NULO!"); // DEBUG ANIMATOR REF
            return;
        }

        float targetSpeedPercent = 0f;
        if (moveInputDirection.magnitude > 0.1f)
        {
            if (isSprinting) targetSpeedPercent = 1.0f;
            else targetSpeedPercent = 0.5f;
        }

        // DEBUG VALORES ENVIADOS AO ANIMATOR
        //Debug.Log($"Animator Update: Speed={targetSpeedPercent}, Crouch={isCrouching}, Grounded={isGrounded}, JumpFall={isJumpingOrFalling}");

        characterAnimator.SetFloat("SpeedPercent", targetSpeedPercent, 0.1f, Time.deltaTime);
        characterAnimator.SetBool("IsCrouching", isCrouching);
        characterAnimator.SetBool("IsGrounded", isGrounded);
        characterAnimator.SetBool("IsJumpingOrFalling", isJumpingOrFalling);
    }
}