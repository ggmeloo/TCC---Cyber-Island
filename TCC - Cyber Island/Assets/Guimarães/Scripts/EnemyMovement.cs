using UnityEngine.AI;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public enum AIMoveState { PATROL, ENGAGED, HIT_STUNNED, DEAD } // ATTACKING é implícito por EnemyAttack.IsCurrentlyAttacking
    [Header("Referências")]
    public Transform player;
    private NavMeshAgent agent;
    public Animator characterAnimator;
    public EnemyHealth enemyHealth;
    public EnemyAttack enemyAttackScript;

    [Header("Configurações de Movimento")]
    public AIMoveState currentMoveState = AIMoveState.PATROL;
    private AIMoveState previousMoveStateBeforeHit;
    public float aggroRange = 10f;
    public float patrolSpeed = 3f;
    public float engagedSpeed = 4.5f;
    public float attackRange = 2f;
    public float stoppingDistanceEngaged = 1.5f;

    [Header("Configurações de Patrulha")]
    public float patrolRadius = 10f;
    public float patrolWaitTimeMin = 2f;
    public float patrolWaitTimeMax = 5f;

    private Vector3 startPosition;
    private float patrolTimer;
    private bool waitingAtPatrolPoint = false;
    private float currentAnimatorSpeedBase;

    [Header("Verificação de Chão (Ground Check)")]
    public Transform groundCheckPoint;
    public float groundCheckDistance = 0.3f;
    public LayerMask groundLayer;
    public bool adjustToGround = true; // Manteve, mas o ajuste manual de Y ainda é problemático com NavMeshAgent
    private bool isGrounded = true;


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null) { Debug.LogError($"[{gameObject.name}] EnemyMovement: NavMeshAgent não encontrado!", this); enabled = false; return; }

        if (characterAnimator == null) characterAnimator = GetComponentInChildren<Animator>();
        if (enemyHealth == null) enemyHealth = GetComponent<EnemyHealth>();
        if (enemyAttackScript == null) enemyAttackScript = GetComponent<EnemyAttack>();
        // ... Verificações de nulidade ...

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
        else { Debug.LogError($"[{gameObject.name}] EnemyMovement: Jogador não encontrado (Tag 'Player')!", this); enabled = false; return; }

        startPosition = transform.position;

        if (groundCheckPoint == null)
        {
            GameObject gcp = new GameObject(gameObject.name + "_GroundCheckPoint");
            gcp.transform.SetParent(transform);
            CapsuleCollider cap = GetComponent<CapsuleCollider>();
            float yOffset = cap != null ? (-cap.height / 2f) * 0.9f : -0.9f; // Um pouco acima da base
            gcp.transform.localPosition = new Vector3(0, yOffset, 0);
            groundCheckPoint = gcp.transform;
        }
        if (groundLayer == 0) groundLayer = LayerMask.GetMask("Default");

        // Garante que o stoppingDistance não impeça de chegar no attackRange
        if (stoppingDistanceEngaged >= attackRange)
        {
            stoppingDistanceEngaged = attackRange * 0.8f; // Deixa uma margem
            Debug.LogWarning($"[{gameObject.name}] Ajustando stoppingDistanceEngaged para {stoppingDistanceEngaged} para ser menor que attackRange ({attackRange})");
        }

        ChangeMoveState(currentMoveState, true);
    }

    void Update()
    {
        if (player == null || enemyHealth == null || enemyHealth.IsDead())
        {
            if (currentMoveState != AIMoveState.DEAD) ChangeMoveState(AIMoveState.DEAD);
            UpdateAnimatorParameters();
            return;
        }

        if (adjustToGround && currentMoveState != AIMoveState.DEAD) PerformGroundCheck();


        bool isAttacking = (enemyAttackScript != null && enemyAttackScript.IsCurrentlyAttacking());

        if (currentMoveState == AIMoveState.HIT_STUNNED || isAttacking)
        {
            if (agent.isActiveAndEnabled && agent.isOnNavMesh && !agent.isStopped)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            UpdateAnimatorParameters(isAttacking); // Passa a flag de ataque
            return;
        }
        // Se não está atacando ou stunado, e o agente estava parado, permite mover (a menos que esperando na patrulha)
        else if (agent.isActiveAndEnabled && agent.isOnNavMesh && agent.isStopped &&
                 currentMoveState != AIMoveState.DEAD && currentMoveState != AIMoveState.HIT_STUNNED)
        {
            if (!(currentMoveState == AIMoveState.PATROL && waitingAtPatrolPoint))
            {
                agent.isStopped = false;
            }
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentMoveState)
        {
            case AIMoveState.PATROL:
                PatrolBehavior();
                if (distanceToPlayer <= aggroRange && CanSeePlayer())
                {
                    ChangeMoveState(AIMoveState.ENGAGED);
                }
                break;

            case AIMoveState.ENGAGED:
                EngagedBehavior(distanceToPlayer);
                // A decisão de atacar é do EnemyAttack.cs.
                // Se o jogador sair do range/visão E NÃO estivermos no meio de um ataque, volta a patrulhar.
                if (!isAttacking) // Só considera voltar a patrulhar se não estiver no meio de um ataque
                {
                    if (distanceToPlayer > aggroRange * 1.2f || (distanceToPlayer > attackRange && !CanSeePlayer())) // Se muito longe OU fora do range de ataque e sem visão
                    {
                        ChangeMoveState(AIMoveState.PATROL);
                    }
                }
                break;
        }
        UpdateAnimatorParameters(isAttacking);
    }

    bool CanSeePlayer()
    {
        if (player == null || groundCheckPoint == null) return false; // Adicionado groundCheckPoint null check
        RaycastHit hit;
        // Origem do raycast pode ser o groundCheckPoint ou um "ponto de olho" dedicado
        Vector3 rayOrigin = groundCheckPoint.position + Vector3.up * 0.5f; // Um pouco acima do chão
        Vector3 directionToPlayer = (player.position + Vector3.up * 0.5f - rayOrigin).normalized;
        float sightDistance = aggroRange * 1.5f; // Quão longe pode ver

        // Debug.DrawRay(rayOrigin, directionToPlayer * sightDistance, Color.cyan);
        if (Physics.Raycast(rayOrigin, directionToPlayer, out hit, sightDistance, ~(1 << gameObject.layer))) // Ignora a própria layer
        {
            if (hit.transform.CompareTag("Player")) return true;
        }
        return false;
    }

    void PerformGroundCheck() // Removido ajuste manual de Y por enquanto
    {
        if (groundCheckPoint == null) return;
        isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, groundCheckDistance, groundLayer);
    }


    void ChangeMoveState(AIMoveState newState, bool forceInitialSetup = false)
    {
        if (!forceInitialSetup && currentMoveState == newState && currentMoveState != AIMoveState.DEAD) return;
        if (currentMoveState == AIMoveState.DEAD && newState != AIMoveState.DEAD) return;

        if (newState == AIMoveState.HIT_STUNNED && currentMoveState != AIMoveState.DEAD)
        {
            previousMoveStateBeforeHit = currentMoveState;
        }

        //Debug.Log($"[{gameObject.name}] MovState: {currentMoveState} -> {newState}");
        currentMoveState = newState;
        waitingAtPatrolPoint = false;

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            switch (currentMoveState)
            {
                case AIMoveState.PATROL:
                    agent.speed = patrolSpeed;
                    agent.stoppingDistance = 0.1f;
                    currentAnimatorSpeedBase = patrolSpeed;
                    agent.isStopped = false;
                    GoToNewPatrolPoint(); // Inicia nova patrulha ao entrar no estado
                    break;
                case AIMoveState.ENGAGED:
                    agent.speed = engagedSpeed;
                    agent.stoppingDistance = stoppingDistanceEngaged;
                    currentAnimatorSpeedBase = engagedSpeed;
                    agent.isStopped = false;
                    break;
                case AIMoveState.HIT_STUNNED:
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                    currentAnimatorSpeedBase = 0;
                    break;
                case AIMoveState.DEAD:
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                    currentAnimatorSpeedBase = 0;
                    if (agent.enabled) agent.enabled = false;
                    break;
            }
        }
    }

    void PatrolBehavior()
    {
        if (agent == null || !agent.isOnNavMesh || !agent.isActiveAndEnabled || agent.isStopped) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!waitingAtPatrolPoint)
            {
                waitingAtPatrolPoint = true;
                patrolTimer = Random.Range(patrolWaitTimeMin, patrolWaitTimeMax);
                agent.velocity = Vector3.zero;
            }
            else
            {
                patrolTimer -= Time.deltaTime;
                if (patrolTimer <= 0) GoToNewPatrolPoint();
            }
        }
    }

    void GoToNewPatrolPoint()
    {
        if (agent == null || !agent.isOnNavMesh || !agent.isActiveAndEnabled) return;
        waitingAtPatrolPoint = false; // Reseta aqui
        agent.isStopped = false; // Garante que pode mover

        Vector3 randomPoint;
        if (FindRandomPointOnNavMesh(startPosition, patrolRadius, out randomPoint))
            agent.SetDestination(randomPoint);
        else
            agent.SetDestination(startPosition);
    }

    bool FindRandomPointOnNavMesh(Vector3 center, float radius, out Vector3 result)
    {
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * radius;
            randomDirection += center;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }
        result = Vector3.zero;
        return false;
    }

    void EngagedBehavior(float distanceToPlayer)
    {
        if (player != null && agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh && !agent.isStopped)
        {
            // Só define destino se não estiver muito perto (já na stoppingDistance)
            // para evitar que o agente "dance" se já estiver no local de ataque.
            // A verificação de `IsCurrentlyAttacking` no Update principal já para o agente.
            if (distanceToPlayer > agent.stoppingDistance)
            {
                agent.SetDestination(player.position);
            }
            else // Está perto o suficiente, pode ter parado por stoppingDistance
            {
                // Se parou por stoppingDistance mas não está atacando, pode precisar encarar o jogador.
                if (!(enemyAttackScript != null && enemyAttackScript.IsCurrentlyAttacking()))
                {
                    FacePlayer();
                }
            }
        }
    }

    void FacePlayer()
    {
        if (player == null) return;
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Mantém a rotação apenas no eixo Y
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * agent.angularSpeed * 0.1f); // Ajuste a velocidade de rotação
        }
    }


    void UpdateAnimatorParameters(bool isCurrentlyAttackingFlag = false) // Adicionado parâmetro
    {
        if (characterAnimator == null || agent == null) return;

        float actualAgentSpeed = 0f;
        if (agent.isActiveAndEnabled && agent.isOnNavMesh && !agent.isStopped && !isCurrentlyAttackingFlag) // Não pega velocidade se estiver parado ou atacando
        {
            actualAgentSpeed = agent.velocity.magnitude;
        }

        float normalizedSpeed = 0f;
        // Só calcula normalizedSpeed se não estiver atacando e tiver uma base de velocidade
        if (!isCurrentlyAttackingFlag && currentAnimatorSpeedBase > 0.01f && !agent.isStopped)
        {
            normalizedSpeed = actualAgentSpeed / currentAnimatorSpeedBase;
        }

        normalizedSpeed = Mathf.Clamp01(normalizedSpeed);

        characterAnimator.SetFloat("Speed", normalizedSpeed, 0.1f, Time.deltaTime);
        // IsEngaged é true se o estado de movimento for ENGAGED E não estiver atualmente atacando
        characterAnimator.SetBool("IsEngaged", currentMoveState == AIMoveState.ENGAGED && !isCurrentlyAttackingFlag);
        characterAnimator.SetBool("IsGrounded", isGrounded);
    }

    public void EnterHitState() { ChangeMoveState(AIMoveState.HIT_STUNNED); }
    public void ExitHitState()
    {
        if (currentMoveState == AIMoveState.DEAD) return;
        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= aggroRange && CanSeePlayer()) ChangeMoveState(AIMoveState.ENGAGED);
            else ChangeMoveState(previousMoveStateBeforeHit == AIMoveState.DEAD ? AIMoveState.PATROL : previousMoveStateBeforeHit);
        }
        else ChangeMoveState(AIMoveState.PATROL);
    }
    public void EnterDeadState() { ChangeMoveState(AIMoveState.DEAD); }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(Application.isPlaying ? startPosition : transform.position, patrolRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);

        if (groundCheckPoint != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(groundCheckPoint.position, groundCheckPoint.position + Vector3.down * groundCheckDistance);
        }
    }
}