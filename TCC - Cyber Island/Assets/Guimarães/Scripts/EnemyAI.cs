using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum AIState { PATROL, CHASE }

    [Header("References")]
    [Tooltip("O Transform do jogador a ser perseguido (deve ter a tag 'Player').")]
    public Transform player;
    private NavMeshAgent agent;
    // --- REFERÊNCIA PARA O ANIMATOR ---
    [Tooltip("Assign the Animator component from the enemy model.")]
    [SerializeField] Animator enemyAnimator; // Arraste o componente Animator aqui

    [Header("AI Settings")]
    public AIState currentState = AIState.PATROL;
    public float chaseRange = 15f;
    public float loseSightRange = 20f;
    // --- VELOCIDADES ESPECÍFICAS ---
    [Tooltip("Velocidade do inimigo ao patrulhar.")]
    public float patrolSpeed = 3f;
    [Tooltip("Velocidade do inimigo ao perseguir.")]
    public float chaseSpeed = 5f;
    // public float attackRange = 2f;

    [Header("Patrol Settings")]
    public float patrolRadius = 10f;
    public float patrolWaitTimeMin = 2f;
    public float patrolWaitTimeMax = 5f;

    // Variáveis Privadas
    private Vector3 startPosition;
    private float patrolTimer;
    private bool waitingAtPatrolPoint = false;
    private float currentMaxSpeed; // Para normalizar a velocidade para o Animator

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // --- OBTÉM ANIMATOR ---
        if (enemyAnimator == null)
            enemyAnimator = GetComponentInChildren<Animator>();
        if (enemyAnimator == null)
            Debug.LogWarning($"EnemyAI ({gameObject.name}): Animator não encontrado. Animações não funcionarão.", this);
        // -----------------------

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogError($"EnemyAI ({gameObject.name}): Jogador não encontrado! Tag 'Player' necessária.", this);
            enabled = false;
            return;
        }

        startPosition = transform.position;

        // --- DEFINE VELOCIDADE INICIAL BASEADA NO ESTADO INICIAL ---
        if (currentState == AIState.PATROL)
        {
            agent.speed = patrolSpeed;
            currentMaxSpeed = patrolSpeed; // Inicializa max speed
        }
        else // CHASE (ou outros futuros)
        {
            agent.speed = chaseSpeed;
            currentMaxSpeed = chaseSpeed;
        }
        // ---------------------------------------------------------
    }

    void Start()
    {
        if (currentState == AIState.PATROL) // Só começa a patrulhar se o estado inicial for PATROL
        {
            GoToNewPatrolPoint();
        }
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // --- Lógica de Transição de Estados ---
        switch (currentState)
        {
            case AIState.PATROL:
                PatrolBehavior();
                if (distanceToPlayer <= chaseRange) ChangeState(AIState.CHASE);
                break;

            case AIState.CHASE:
                ChaseBehavior();
                if (distanceToPlayer > loseSightRange) ChangeState(AIState.PATROL);
                // (Add attack logic later)
                break;
        }

        // --- ATUALIZA ANIMATOR ---
        UpdateAnimator();
        // -----------------------
    }

    void ChangeState(AIState newState)
    {
        if (currentState == newState) return;

        // Debug.Log($"{gameObject.name}: Mudando de {currentState} para {newState}");
        currentState = newState;
        waitingAtPatrolPoint = false;

        // --- AJUSTA VELOCIDADE DO AGENT AO MUDAR DE ESTADO ---
        switch (currentState)
        {
            case AIState.PATROL:
                agent.speed = patrolSpeed;
                currentMaxSpeed = patrolSpeed;
                GoToNewPatrolPoint(); // Recomeça a patrulha
                break;
            case AIState.CHASE:
                agent.speed = chaseSpeed;
                currentMaxSpeed = chaseSpeed;
                // Não precisa definir destino aqui, ChaseBehavior faz isso no Update
                break;
        }
        // ----------------------------------------------------
    }

    void PatrolBehavior()
    {
        // Se chegou ao destino ou não tem caminho
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
                if (patrolTimer <= 0)
                {
                    GoToNewPatrolPoint();
                    waitingAtPatrolPoint = false;
                }
            }
        }
        else
        {
            waitingAtPatrolPoint = false; // Garante que não está esperando enquanto move
        }
    }

    void GoToNewPatrolPoint()
    {
        Vector3 randomPoint;
        if (FindRandomPointOnNavMesh(startPosition, patrolRadius, out randomPoint))
        {
            agent.SetDestination(randomPoint);
        }
        else
        {
            agent.SetDestination(startPosition); // Fallback
        }
        waitingAtPatrolPoint = false; // Garante que não está esperando ao iniciar novo caminho
    }

    bool FindRandomPointOnNavMesh(Vector3 center, float radius, out Vector3 result)
    {
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * radius;
            randomDirection += center;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, 1.0f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }
        result = Vector3.zero;
        return false;
    }


    void ChaseBehavior()
    {
        if (player != null)
        {
            agent.SetDestination(player.position);
        }
        waitingAtPatrolPoint = false; // Garante que não está esperando ao perseguir
    }

    // --- MÉTODO PARA ATUALIZAR O ANIMATOR ---
    void UpdateAnimator()
    {
        if (enemyAnimator == null) return;

        // Calcula a velocidade atual do agente
        float currentAgentSpeed = agent.velocity.magnitude;

        // Normaliza a velocidade (0 a 1) baseada na velocidade máxima ATUAL (patrol ou chase)
        // Evita divisão por zero se currentMaxSpeed for 0
        float normalizedSpeed = (currentMaxSpeed > 0.01f) ? currentAgentSpeed / currentMaxSpeed : 0f;

        // Garante que a velocidade normalizada não exceda 1 (pode acontecer brevemente com aceleração)
        normalizedSpeed = Mathf.Clamp01(normalizedSpeed);

        // Define o parâmetro "Speed" no Animator, com suavização (damp time)
        enemyAnimator.SetFloat("Speed", normalizedSpeed, 0.1f, Time.deltaTime);

        // Você pode adicionar mais parâmetros aqui depois, como:
        // enemyAnimator.SetBool("IsChasing", currentState == AIState.CHASE);
        // enemyAnimator.SetBool("IsWaiting", waitingAtPatrolPoint);
    }
    // -----------------------------------------


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startPosition == Vector3.zero ? transform.position : startPosition, patrolRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseSightRange);
        // Gizmos.color = Color.magenta;
        // Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}