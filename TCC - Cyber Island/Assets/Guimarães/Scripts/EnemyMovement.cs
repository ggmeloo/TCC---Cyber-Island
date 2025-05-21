
using UnityEngine.AI;

using UnityEngine;

// EnemyMovement.cs

public class EnemyMovement : MonoBehaviour
{
    public enum AIMoveState { PATROL, ENGAGED, HIT_STUNNED, DEAD }
    [Header("Referências")]
    public Transform player;
    private NavMeshAgent agent;
    public Animator characterAnimator;
    public EnemyHealth enemyHealth;

    [Header("Configurações de Movimento")]
    public AIMoveState currentMoveState = AIMoveState.PATROL;
    private AIMoveState previousMoveStateBeforeHit;
    public float aggroRange = 10f;
    public float patrolSpeed = 3f;
    public float engagedSpeed = 4.5f; // NOVA: Velocidade ao perseguir/engajar ativamente
    public float attackRange = 2f; // Para saber quando parar de perseguir e talvez atacar
    public float stoppingDistanceEngaged = 1.5f; // Distância para parar do jogador ao engajar

    [Header("Configurações de Patrulha")]
    public float patrolRadius = 10f;
    public float patrolWaitTimeMin = 2f;
    public float patrolWaitTimeMax = 5f;

    private Vector3 startPosition;
    private float patrolTimer;
    private bool waitingAtPatrolPoint = false;
    private float currentAnimatorSpeedBase;

    [Header("Enemy Attack")]
    public EnemyAttack enemyAttackScript; // Adicione esta referência

    void Start() // MUDANÇA: Awake para Start para garantir que outras Awakes (como do player) possam ter rodado
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null) { Debug.LogError($"[{gameObject.name}] EnemyMovement: NavMeshAgent não encontrado!", this); enabled = false; return; }

        // Com Root Motion no Animator, o NavMeshAgent não deve atualizar a posição ou rotação do transform.
        // O Animator fará isso. O NavMeshAgent serve para pathfinding e velocidade desejada.
        // No entanto, para que o NavMeshAgent siga o caminho corretamente mesmo com root motion,
        // pode ser necessário deixar updatePosition e updateRotation como true e sincronizar
        // a posição do transform com agent.nextPosition, ou usar OnAnimatorMove.
        // Por simplicidade inicial, vamos deixar como está e ver o comportamento.
        // Se o personagem não seguir o caminho direito, precisaremos de OnAnimatorMove.
        // agent.updatePosition = false; // Considere se o movimento com root motion não seguir o path
        // agent.updateRotation = false; // Considere se o root motion da animação já faz a rotação



        if (characterAnimator == null)
        {
            characterAnimator = GetComponentInChildren<Animator>();
            if (characterAnimator == null) Debug.LogWarning($"[{gameObject.name}] EnemyMovement: Animator não encontrado.", this);
        }
        if (enemyHealth == null)
        {
            enemyHealth = GetComponent<EnemyHealth>();
            if (enemyHealth == null) Debug.LogError($"[{gameObject.name}] EnemyMovement: Script EnemyHealth não encontrado!", this);
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
        else
        {
            Debug.LogError($"[{gameObject.name}] EnemyMovement: Jogador não encontrado! Desabilitando.", this);
            enabled = false; return;
        }
        startPosition = transform.position;
        ChangeMoveState(currentMoveState, true);

        if (enemyAttackScript == null) enemyAttackScript = GetComponent<EnemyAttack>();
        if (enemyAttackScript == null) Debug.LogError($"[{gameObject.name}] EnemyMovement: Script EnemyAttack não encontrado!", this);


    }


    void Update()
    {
        if (player == null ||
        currentMoveState == AIMoveState.DEAD ||
        currentMoveState == AIMoveState.HIT_STUNNED ||
        (enemyAttackScript != null && enemyAttackScript.IsCurrentlyAttacking())) // << NOVA CONDIÇÃO
        {
            UpdateAnimatorParameters(); // Ainda atualiza o animator para refletir Speed = 0
            return;
        }

        if (player == null || currentMoveState == AIMoveState.DEAD || currentMoveState == AIMoveState.HIT_STUNNED)
        {
            UpdateAnimatorParameters();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentMoveState)
        {
            case AIMoveState.PATROL:
                PatrolBehavior();
                if (distanceToPlayer <= aggroRange) ChangeMoveState(AIMoveState.ENGAGED);
                break;

            case AIMoveState.ENGAGED:
                EngagedBehavior(distanceToPlayer); // Agora EngagedBehavior vai controlar o movimento
                if (distanceToPlayer > aggroRange * 1.2f) ChangeMoveState(AIMoveState.PATROL);
                break;
        }
        UpdateAnimatorParameters();
    }

    void ChangeMoveState(AIMoveState newState, bool forceInitialSetup = false)
    {
        if (!forceInitialSetup && (currentMoveState == newState || currentMoveState == AIMoveState.DEAD)) return;

        if (newState == AIMoveState.HIT_STUNNED && currentMoveState != AIMoveState.DEAD)
        {
            previousMoveStateBeforeHit = currentMoveState;
        }

        currentMoveState = newState;
        waitingAtPatrolPoint = false;

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            if (agent.isActiveAndEnabled)
            { // Checagem redundante, mas segura
                switch (currentMoveState)
                {
                    case AIMoveState.PATROL:
                        agent.speed = patrolSpeed;
                        agent.stoppingDistance = 0.1f; // Stopping distance padrão para patrulha
                        agent.isStopped = false;
                        currentAnimatorSpeedBase = patrolSpeed;
                        // Verifica se não estava já patrulhando ou se é setup inicial para evitar chamar GoToNewPatrolPoint desnecessariamente
                        if (forceInitialSetup || (previousMoveStateBeforeHit != AIMoveState.PATROL && (!forceInitialSetup && newState == AIMoveState.PATROL)))
                        {
                            GoToNewPatrolPoint();
                        }
                        break;
                    case AIMoveState.ENGAGED:
                        agent.speed = engagedSpeed; // <<< USA A NOVA VELOCIDADE DE ENGAGED
                        agent.stoppingDistance = stoppingDistanceEngaged; // Para parar perto do player
                        agent.isStopped = false;    // <<< PERMITE MOVIMENTO
                        currentAnimatorSpeedBase = engagedSpeed; // <<< BASE PARA ANIMAÇÃO DE CORRIDA/MOVIMENTO
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
    }


    void PatrolBehavior()
    {
        if (agent == null || !agent.isOnNavMesh || !agent.isActiveAndEnabled || agent.isStopped) return;

        // Se o agente não tem um caminho ou chegou ao destino
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!waitingAtPatrolPoint)
            {
                waitingAtPatrolPoint = true;
                patrolTimer = Random.Range(patrolWaitTimeMin, patrolWaitTimeMax);
            }
            else
            {
                patrolTimer -= Time.deltaTime;
                if (patrolTimer <= 0) GoToNewPatrolPoint();
            }
        }
        else waitingAtPatrolPoint = false;
    }

    void GoToNewPatrolPoint()
    {
        if (agent == null || !agent.isOnNavMesh || !agent.isActiveAndEnabled) return;
        agent.isStopped = false;

        Vector3 randomPoint;
        if (FindRandomPointOnNavMesh(startPosition, patrolRadius, out randomPoint))
            agent.SetDestination(randomPoint);
        else
            agent.SetDestination(startPosition);
        waitingAtPatrolPoint = false;
    }

    bool FindRandomPointOnNavMesh(Vector3 center, float radius, out Vector3 result)
    {
        // ... (código sem alteração)
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * radius;
            randomDirection += center;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, radius * 0.5f, NavMesh.AllAreas))
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
        if (player != null && agent != null && agent.isActiveAndEnabled && !agent.isStopped)
        {
            agent.SetDestination(player.position); // <<< PERSEGUE O JOGADOR

            // A rotação PODE ser controlada pelo NavMeshAgent se agent.updateRotation = true
            // Se você desabilitou agent.updateRotation, ou quer uma rotação mais suave/customizada:
            /*
            Vector3 directionToPlayer = (player.position - transform.position);
            directionToPlayer.y = 0;
            if (directionToPlayer != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * agent.angularSpeed * 0.1f);
            }
            */
        }
        // Se distanceToPlayer <= attackRange, você poderia mudar para um sub-estado de ATAQUE
        // e definir agent.isStopped = true;
    }

    void UpdateAnimatorParameters()
    {
        if (characterAnimator == null || agent == null) return;

        float actualAgentSpeed = 0f;
        if (agent.isActiveAndEnabled && agent.isOnNavMesh) // Não checa mais isStopped aqui, pois queremos animar mesmo se parado por stoppingDistance
        {
            actualAgentSpeed = agent.velocity.magnitude;
        }

        float normalizedSpeed = (currentAnimatorSpeedBase > 0.01f) ? actualAgentSpeed / currentAnimatorSpeedBase : 0f;
        // Se o agente está "parado" por causa do stoppingDistance mas tem um path, ele ainda pode ter uma pequena velocidade desejada.
        // Se isStopped é true, a velocidade é 0.
        if (agent.isStopped)
        {
            normalizedSpeed = 0;
        }
        normalizedSpeed = Mathf.Clamp01(normalizedSpeed);

        characterAnimator.SetFloat("Speed", normalizedSpeed, 0.1f, Time.deltaTime);
        characterAnimator.SetBool("IsEngaged", currentMoveState == AIMoveState.ENGAGED);
    }

    public void EnterHitState() { ChangeMoveState(AIMoveState.HIT_STUNNED); }
    public void ExitHitState()
    {
        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= aggroRange) ChangeMoveState(AIMoveState.ENGAGED);
            else ChangeMoveState(previousMoveStateBeforeHit == AIMoveState.DEAD ? AIMoveState.PATROL : previousMoveStateBeforeHit);
        }
        else ChangeMoveState(AIMoveState.PATROL);
    }
    public void EnterDeadState() { ChangeMoveState(AIMoveState.DEAD); }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(startPosition == Vector3.zero ? transform.position : startPosition, patrolRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
    }
}