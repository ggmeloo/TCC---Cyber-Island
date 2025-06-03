using UnityEngine;
using System.Collections;

public class EnemyAttack : MonoBehaviour
{
    public enum AttackType { Melee, RangedProjectile /*, RangedRaycast, Throwable */ } // Simplificado para o exemplo

    [Header("Referências")]
    public Transform playerTarget;
    public Animator characterAnimator;
    public EnemyMovement enemyMovement;
    public EnemyHealth enemyHealth;

    [Header("Configurações Gerais de Ataque")]
    public AttackType currentAttackType = AttackType.Melee;
    public float attackRange = 2f;
    public float attackCooldown = 2f;
    public int attackDamage = 10;
    private bool canAttack = true;
    private bool isCurrentlyAttacking = false;

    [Header("Configurações Específicas de Ataque")]
    [Tooltip("Ponto de origem para ataques melee.")]
    public Transform attackOriginPoint; // Para Melee e Raycast
    // Melee
    public float meleeAttackRadius = 0.7f;
    public LayerMask playerLayer; // MUITO IMPORTANTE: Defina esta Layer no Inspector para a Layer do seu Player
    // Ranged Projectile
    public GameObject projectilePrefab;
    public float projectileSpeed = 15f;
    public Transform projectileSpawnPoint;


    [Header("Animação")]
    public string attackAnimationTrigger = "AttackTrigger";
    [Tooltip("Tempo (em segundos) dentro da animação de ataque onde o dano/efeito deve ser aplicado.")]
    public float damageApplicationDelay = 0.5f; // Ajuste isso para sua animação!


    void Start()
    {
        if (playerTarget == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) playerTarget = playerObject.transform;
            else Debug.LogError($"[{gameObject.name}] EnemyAttack: PlayerTarget não encontrado (Tag 'Player')!", this);
        }

        if (characterAnimator == null) characterAnimator = GetComponentInChildren<Animator>();
        if (characterAnimator == null) Debug.LogError($"[{gameObject.name}] EnemyAttack: Animator não encontrado!", this);

        if (enemyMovement == null) enemyMovement = GetComponent<EnemyMovement>();
        if (enemyMovement == null) Debug.LogError($"[{gameObject.name}] EnemyAttack: EnemyMovement não encontrado!", this);

        if (enemyHealth == null) enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth == null) Debug.LogError($"[{gameObject.name}] EnemyAttack: EnemyHealth não encontrado!", this);


        if (attackOriginPoint == null)
        {
            // Cria um ponto de ataque padrão se não definido
            GameObject defaultOrigin = new GameObject(gameObject.name + "_AttackOrigin");
            defaultOrigin.transform.SetParent(transform);
            CapsuleCollider cap = GetComponent<CapsuleCollider>();
            if (cap != null)
                defaultOrigin.transform.localPosition = new Vector3(0, cap.height * 0.75f, cap.radius + 0.1f);
            else
                defaultOrigin.transform.localPosition = new Vector3(0, 1f, 0.5f);
            attackOriginPoint = defaultOrigin.transform;
            Debug.LogWarning($"[{gameObject.name}] EnemyAttack: attackOriginPoint não atribuído. Criado um padrão. Ajuste sua posição.");
        }

        if (currentAttackType == AttackType.RangedProjectile && projectileSpawnPoint == null)
        {
            projectileSpawnPoint = attackOriginPoint; // Fallback
            Debug.LogWarning($"[{gameObject.name}] EnemyAttack: projectileSpawnPoint não atribuído para RangedProjectile. Usando attackOriginPoint como fallback.");
        }


        // Pega a layer do player automaticamente se playerLayer não estiver setada e playerTarget existir
        // É MELHOR CONFIGURAR playerLayer NO INSPECTOR.
        if (playerLayer == 0 && playerTarget != null) // LayerMask 0 é "Nothing"
        {
            int targetLayer = playerTarget.gameObject.layer;
            if (targetLayer != 0) // Não "Default" layer
            {
                playerLayer = 1 << targetLayer; // Cria uma LayerMask com apenas a layer do player
            }
            if (playerLayer == 0) Debug.LogWarning($"[{gameObject.name}] EnemyAttack: Não foi possível obter a layer do Player automaticamente. Defina manualmente a Player Layer no Inspector.");
        }
        else if (playerLayer == 0)
        {
            Debug.LogError($"[{gameObject.name}] EnemyAttack: Player Layer NÃO ESTÁ DEFINIDA no Inspector! O ataque pode não detectar o jogador.");
        }
    }

    void Update()
    {
        if (playerTarget == null || characterAnimator == null || enemyMovement == null || enemyHealth == null || enemyHealth.IsDead())
            return;

        if (enemyMovement.currentMoveState == EnemyMovement.AIMoveState.ENGAGED && canAttack && !isCurrentlyAttacking)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
            if (distanceToPlayer <= attackRange)
            {
                if (HasLineOfSightToPlayer()) // Importante para ranged, bom para melee também
                {
                    StartCoroutine(PerformAttackSequence());
                }
            }
        }
    }

    bool HasLineOfSightToPlayer()
    {
        if (playerTarget == null || attackOriginPoint == null) return false;

        Vector3 targetCenter = playerTarget.position + Vector3.up * 1.0f; // Mira no centro do jogador
        Vector3 originPosition = attackOriginPoint.position;
        Vector3 directionToPlayer = (targetCenter - originPosition).normalized;
        float distanceToPlayerActual = Vector3.Distance(originPosition, targetCenter);

        // Define uma LayerMask para tudo exceto a layer do próprio inimigo
        // Isso evita que o raycast do inimigo acerte a si mesmo.
        int enemyLayerValue = gameObject.layer;
        LayerMask obstaclesAndPlayerLayerMask = ~(1 << enemyLayerValue); // Tudo exceto a layer do inimigo

        RaycastHit hit;
        // Debug.DrawRay(originPosition, directionToPlayer * distanceToPlayerActual, Color.magenta, 1f);
        if (Physics.Raycast(originPosition, directionToPlayer, out hit, distanceToPlayerActual, obstaclesAndPlayerLayerMask))
        {
            if (hit.transform.gameObject.CompareTag("Player")) // Verifica se o que foi atingido é o jogador pela tag
            {
                return true;
            }
            //Debug.Log($"[{gameObject.name}] Linha de visão para {playerTarget.name} bloqueada por {hit.transform.name} (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");
            return false;
        }
        // Se o raycast não atingiu nada DENTRO da distância ao jogador, mas o jogador ESTÁ nessa distância,
        // isso implica uma linha de visão limpa (pode acontecer se o colisor do jogador for complexo ou trigger).
        // Para ser mais robusto, se o raycast não acertar NADA ATÉ O JOGADOR, mas a distância está correta,
        // e não há outros obstáculos, podemos assumir LoS.
        // No entanto, a condição `hit.transform.gameObject.CompareTag("Player")` é geralmente suficiente.
        // Se o raycast não atingiu nada, mas o player está no range (sem obstáculos entre eles),
        // pode ser que o colisor do player não seja detectado pelo raycast por algum motivo.
        // Nesse caso, e se a distância estiver correta, podemos considerar LoS.
        // Mas é mais seguro confiar que o Raycast DEVE atingir o player se a visão for limpa.
        // Se o raycast não atingiu nada, isso significa que não há obstáculos, e o player está (presumivelmente) lá.
        // Essa linha é um fallback caso o raycast não atinja o colisor do player mas não haja outros obstáculos.
        // if (!Physics.Raycast(originPosition, directionToPlayer, distanceToPlayerActual, ~( (1 << enemyLayerValue) | playerLayer) ) ) return true;

        return false; // Fallback: se o raycast não acertou o jogador, não há LoS
    }


    IEnumerator PerformAttackSequence()
    {
        isCurrentlyAttacking = true;
        canAttack = false;

        // Garante que está encarando o jogador
        if (playerTarget != null)
        {
            Vector3 directionToPlayer = (playerTarget.position - transform.position);
            directionToPlayer.y = 0;
            if (directionToPlayer != Vector3.zero) transform.rotation = Quaternion.LookRotation(directionToPlayer);
        }

        //Debug.Log($"[{gameObject.name}] Iniciando ataque. Trigger: {attackAnimationTrigger}");
        characterAnimator.SetTrigger(attackAnimationTrigger);

        // Espera o ponto de dano da animação
        yield return new WaitForSeconds(damageApplicationDelay);

        if (!enemyHealth.IsDead() && isCurrentlyAttacking) // Verifica se não morreu/foi interrompido
        {
            ExecuteAttackEffect();
        }

        // Espera o resto da animação (aproximadamente) - PODE SER REMOVIDO SE A ANIMAÇÃO VOLTA SOZINHA PARA IDLE
        // float animationLength = GetCurrentAttackAnimationLength(); // Você precisaria de uma forma de pegar a duração do clipe de ataque
        // float remainingAnimTime = Mathf.Max(0, animationLength - damageApplicationDelay);
        // if (remainingAnimTime > 0) yield return new WaitForSeconds(remainingAnimTime);

        // O isCurrentlyAttacking será resetado quando a animação de ataque terminar (via Animation Event ou StateMachineBehaviour)
        // Por enquanto, vamos resetar após um tempo fixo para simular o fim da animação.
        // Ideal: Use um StateMachineBehaviour no estado de ataque do Animator para setar isCurrentlyAttacking = false em OnStateExit.
        yield return new WaitForSeconds(0.5f); // Tempo para a animação "terminar" visualmente após o dano. Ajuste!

        isCurrentlyAttacking = false;

        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    void ExecuteAttackEffect()
    {
        //Debug.Log($"[{gameObject.name}] Executando efeito do ataque: {currentAttackType}");
        switch (currentAttackType)
        {
            case AttackType.Melee:
                PerformMeleeDamage();
                break;
            case AttackType.RangedProjectile:
                FireProjectile();
                break;
                // Adicione outros tipos de ataque aqui
        }
    }

    void PerformMeleeDamage()
    {
        if (playerTarget == null || attackOriginPoint == null || playerLayer == 0)
        {
            Debug.LogError($"[{gameObject.name}] PerformMeleeDamage: Faltam referências ou PlayerLayer não definida. Ponto: {attackOriginPoint}, Layer: {LayerMask.LayerToName(playerLayer)}");
            return;
        }

        Collider[] hitPlayers = Physics.OverlapSphere(attackOriginPoint.position, meleeAttackRadius, playerLayer);
        foreach (Collider playerCol in hitPlayers)
        {
            // playerCol.transform pode ser um filho do objeto Player real se o colisor estiver em um filho.
            // É mais seguro pegar o PlayerHealth do objeto raiz do colisor.
            PlayerHealth playerHealth = playerCol.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                //Debug.Log($"[{gameObject.name}] Acertou {playerCol.name} (PlayerHealth encontrado) com MELEE causando {attackDamage} de dano.");
                playerHealth.TakeDamage(attackDamage);
                break; // Geralmente acerta apenas um jogador por vez no melee
            }
            else
            {
                //Debug.LogWarning($"[{gameObject.name}] Colidiu com {playerCol.name} na playerLayer, mas não encontrou PlayerHealth.");
            }
        }
    }

    void FireProjectile()
    {
        if (projectilePrefab == null || projectileSpawnPoint == null || playerTarget == null) return;

        GameObject projectileInstance = Instantiate(projectilePrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation);
        EnemyProjectile projectileScript = projectileInstance.GetComponent<EnemyProjectile>(); // Assumindo que o projétil tem esse script

        if (projectileScript != null)
        {
            projectileScript.Initialize(playerTarget, projectileSpeed, attackDamage, playerLayer); // Passa o dano e a layer do jogador
        }
        else
        {
            // Fallback se não houver script EnemyProjectile (movimento simples)
            Rigidbody rb = projectileInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 direction = (playerTarget.position + Vector3.up * 0.5f - projectileSpawnPoint.position).normalized;
                projectileInstance.transform.rotation = Quaternion.LookRotation(direction);
                rb.linearVelocity = direction * projectileSpeed;
            }
            // O projétil precisaria de um script para detectar colisão com o player e aplicar dano
            // Ex: OnCollisionEnter, verifica se colidiu com playerLayer, pega PlayerHealth e aplica attackDamage.
            // E se autodestruir.
            Debug.LogWarning($"[{gameObject.name}] Projétil {projectileInstance.name} não tem script EnemyProjectile. Movimento básico aplicado. Dano e destruição precisam ser implementados no projétil.");
            Destroy(projectileInstance, 5f); // Autodestrói após 5s como fallback
        }
    }

    float GetCurrentAttackAnimationLength()
    {
        if (characterAnimator == null) return 0f;
        AnimatorClipInfo[] clipInfo = characterAnimator.GetCurrentAnimatorClipInfo(0); // Camada 0
        if (clipInfo.Length > 0)
        {
            // Assume que o primeiro clipe é o de ataque se estivermos em um estado de ataque.
            // Isso pode precisar de mais lógica se você tiver blend trees complexas.
            return clipInfo[0].clip.length;
        }
        return 0f; // Fallback
    }

    public void ResetIsAttackingFlag()
    {
        isCurrentlyAttacking = false;
        // Debug.Log($"[{gameObject.name}] Attack SMB: isCurrentlyAttacking set to false.");
    }

    public bool IsCurrentlyAttacking()
    {
        return isCurrentlyAttacking;
    }

    // Idealmente, usar um Animation State Machine Behaviour para resetar isCurrentlyAttacking
    // ao sair do estado de ataque no Animator.
    // Exemplo (adicionar este script a cada estado de ataque no Animator do inimigo):
    /*
    public class EnemyAttackSMB : StateMachineBehaviour
    {
        EnemyAttack attackScript;
        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (attackScript == null) attackScript = animator.GetComponentInParent<EnemyAttack>();
            // attackScript?.SetIsAttacking(true); // Se precisar setar no Enter
        }

        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (attackScript == null) attackScript = animator.GetComponentInParent<EnemyAttack>();
            attackScript?.ResetIsAttackingFlag(); // Método público em EnemyAttack
        }
    }
    // Em EnemyAttack.cs:
    // public void ResetIsAttackingFlag() { isCurrentlyAttacking = false; }
    */
}