// EnemyAttack.cs
using UnityEngine;
using System.Collections;

public class EnemyAttack : MonoBehaviour
{
    public enum AttackType { Melee, RangedProjectile, RangedRaycast, Throwable }

    [Header("Referências")]
    public Transform playerTarget; // O jogador
    public Animator characterAnimator;
    public EnemyMovement enemyMovement; // Para coordenar com o movimento
    public EnemyHealth enemyHealth;     // Para saber se está vivo

    [Header("Configurações Gerais de Ataque")]
    public AttackType currentAttackType = AttackType.Melee;
    public float attackRange = 2f;         // Distância para iniciar o ataque
    public float attackCooldown = 2f;      // Tempo entre os ataques
    public int attackDamage = 10;          // Dano base do ataque do inimigo
    private bool canAttack = true;         // Flag para cooldown
    private bool isCurrentlyAttacking = false; // Flag para saber se uma sequência de ataque está em progresso

    [Header("Configurações Específicas de Ataque")]
    [Tooltip("Ponto de origem para ataques melee ou raycast ranged.")]
    public Transform attackOriginPoint;
    // Melee
    public float meleeAttackRadius = 0.7f; // Para OverlapSphere no melee
    public LayerMask playerLayer;          // Para identificar o jogador na detecção de acerto
    // Ranged Projectile
    public GameObject projectilePrefab;
    public float projectileSpeed = 15f;
    public Transform projectileSpawnPoint; // Pode ser o mesmo que attackOriginPoint ou diferente
    // Ranged Raycast (similar a uma arma de fogo instantânea)
    // Nenhum parâmetro extra aqui, usa attackOriginPoint e attackRange
    // Throwable (Granada, Magia de Área)
    public GameObject throwablePrefab;
    public float throwForce = 10f;
    public Transform throwSpawnPoint;      // Pode ser o mesmo que attackOriginPoint ou diferente

    [Header("Animação")]
    [Tooltip("Nome do Trigger no Animator para o ataque principal.")]
    public string attackAnimationTrigger = "AttackTrigger"; // Ex: "AttackMelee", "AttackRanged", "Throw"
    // Você pode ter triggers diferentes por tipo de ataque se as animações forem muito distintas
    // public string meleeAnimTrigger = "MeleeAttack";
    // public string rangedAnimTrigger = "RangedAttack";


    void Start()
    {
        if (playerTarget == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) playerTarget = playerObject.transform;
            else Debug.LogError($"[{gameObject.name}] EnemyAttack: PlayerTarget não encontrado!", this);
        }

        if (characterAnimator == null) characterAnimator = GetComponentInChildren<Animator>();
        if (enemyMovement == null) enemyMovement = GetComponent<EnemyMovement>();
        if (enemyHealth == null) enemyHealth = GetComponent<EnemyHealth>();

        if (attackOriginPoint == null) attackOriginPoint = transform; // Fallback
        if (projectileSpawnPoint == null) projectileSpawnPoint = attackOriginPoint;
        if (throwSpawnPoint == null) throwSpawnPoint = attackOriginPoint;

        // Pega a layer do player automaticamente se não definida, mas é melhor setar no Inspector
        if (playerLayer == 0 && playerTarget != null) // Se layerMask é 0 (Nothing)
        {
            playerLayer = 1 << playerTarget.gameObject.layer; // Cria uma LayerMask com apenas a layer do player
            if (playerLayer == 0) Debug.LogWarning($"[{gameObject.name}] EnemyAttack: Não foi possível obter a layer do Player. Defina manualmente a Player Layer.");
        }
    }

    void Update()
    {
        if (playerTarget == null || characterAnimator == null || enemyMovement == null || enemyHealth == null || enemyHealth.IsDead())
        {
            return; // Sai se alguma referência crucial falta ou se está morto
        }

        // Só tenta atacar se estiver no estado ENGAGED do EnemyMovement e não estiver já atacando ou em cooldown
        if (enemyMovement.currentMoveState == EnemyMovement.AIMoveState.ENGAGED && canAttack && !isCurrentlyAttacking)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
            if (distanceToPlayer <= attackRange)
            {
                // Verifica se há linha de visão antes de atacar (especialmente para ranged)
                if (HasLineOfSightToPlayer())
                {
                    StartCoroutine(PerformAttackSequence());
                }
            }
        }
    }

    bool HasLineOfSightToPlayer()
    {
        if (playerTarget == null || attackOriginPoint == null) return false;

        Vector3 directionToPlayer = (playerTarget.position - attackOriginPoint.position).normalized;
        float distanceToPlayer = Vector3.Distance(attackOriginPoint.position, playerTarget.position);

        RaycastHit hit;
        // Dispara um Raycast do ponto de origem do ataque na direção do jogador
        // Ignora o próprio colisor do inimigo (você pode precisar de uma LayerMask para obstáculos)
        if (Physics.Raycast(attackOriginPoint.position, directionToPlayer, out hit, distanceToPlayer, ~LayerMask.GetMask(LayerMask.LayerToName(gameObject.layer)))) // Exclui a própria layer do inimigo
        {
            // Se o primeiro objeto atingido não for o jogador, então há um obstáculo
            if (hit.transform == playerTarget)
            {
                return true; // Linha de visão limpa para o jogador
            }
            //Debug.Log($"Linha de visão para {playerTarget.name} bloqueada por {hit.transform.name}");
            return false; // Algo está no caminho
        }
        // Se o Raycast não atingiu nada (improvável se o player está dentro do range e não há obstáculos),
        // mas para garantir, consideramos que tem linha de visão se o player está perto.
        // No entanto, se o raycast não atingiu NADA ATÉ o player, significa que não tem linha de visão.
        // A condição acima já trata se o player foi o primeiro hit.
        // Se o Raycast não atingiu nada DENTRO DA DISTÂNCIA DO PLAYER, isso é estranho.
        // A checagem acima (hit.transform == playerTarget) é a mais importante.
        // Se o raycast não atingiu NADA até a distância do player, considera-se que não há obstáculos diretos.
        // Mas é mais seguro confiar que o raycast DEVE atingir o player se a visão estiver limpa.
        // O `~LayerMask.GetMask(LayerMask.LayerToName(gameObject.layer))` exclui a layer do próprio inimigo do raycast.
        // Se o Raycast não atingiu nada até o 'distanceToPlayer' e não era o player, então a visão está bloqueada.
        // O código acima com `hit.transform == playerTarget` já cobre isso. Se não for o player, está bloqueado.
        // Se o raycast não atingir NADA (raro se o player está lá), também não tem linha de visão direta.
        // Para simplificar: se o raycast não acertou o player como primeiro objeto, não há LoS.
        return false; // Fallback se o raycast não atingiu o player
    }


    IEnumerator PerformAttackSequence()
    {
        isCurrentlyAttacking = true;
        canAttack = false; // Entra em cooldown

        // 1. Avisa o EnemyMovement para parar e encarar (se ainda não estiver fazendo)
        // O EnemyMovement já deve parar quando a distância é <= attackRange e ele está ENGAGED.
        // Mas podemos forçar aqui se necessário ou para uma lógica de "preparar ataque".
        // enemyMovement.EnterAttackingState(); // Você precisaria criar essa função em EnemyMovement
        // Por ora, vamos assumir que EnemyMovement já o posicionou e parou.

        // Garante que está encarando o jogador
        if (playerTarget != null)
        {
            Vector3 directionToPlayer = (playerTarget.position - transform.position);
            directionToPlayer.y = 0;
            if (directionToPlayer != Vector3.zero) transform.rotation = Quaternion.LookRotation(directionToPlayer);
        }

        // 2. Toca a animação de ataque
        Debug.Log($"[{gameObject.name}] EnemyAttack: Iniciando animação de ataque (Trigger: {attackAnimationTrigger})");
        characterAnimator.SetTrigger(attackAnimationTrigger);

        // 3. Espera um ponto específico da animação para aplicar o efeito do ataque
        // Esta é a parte que Animation Events resolvem melhor.
        // Sem eles, precisamos estimar ou usar a duração do clipe.
        float actionPointDelay = GetActionPointDelayForAttack(); // Tempo até o "impacto"
        yield return new WaitForSeconds(actionPointDelay);

        // 4. Executa a lógica específica do ataque (se ainda estiver vivo e atacando)
        if (!enemyHealth.IsDead() && isCurrentlyAttacking) // Verifica se não morreu/foi interrompido durante a animação
        {
            ExecuteAttackEffect();
        }
        else
        {
            Debug.Log($"[{gameObject.name}] EnemyAttack: Ataque interrompido (morto ou estado mudou).");
        }

        // 5. Espera o resto da animação de ataque (aproximadamente)
        float remainingAnimationTime = GetCurrentAttackAnimationLength() - actionPointDelay;
        if (remainingAnimationTime > 0)
        {
            yield return new WaitForSeconds(remainingAnimationTime);
        }

        // 6. Cooldown
        isCurrentlyAttacking = false; // Terminou a sequência de animação/efeito do ataque
        // enemyMovement.ExitAttackingState(); // Avisa o EnemyMovement para retomar comportamento normal

        // Debug.Log($"[{gameObject.name}] EnemyAttack: Ataque finalizado. Cooldown de {attackCooldown}s iniciado.");
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
        // Debug.Log($"[{gameObject.name}] EnemyAttack: Cooldown finalizado.");
    }

    void ExecuteAttackEffect()
    {
        Debug.Log($"[{gameObject.name}] EnemyAttack: Executando efeito do ataque do tipo {currentAttackType}.");
        switch (currentAttackType)
        {
            case AttackType.Melee:
                PerformMeleeDamage();
                break;
            case AttackType.RangedProjectile:
                FireProjectile();
                break;
            case AttackType.RangedRaycast:
                FireRaycast();
                break;
            case AttackType.Throwable:
                ThrowObject();
                break;
        }
    }

    void PerformMeleeDamage()
    {
        if (playerTarget == null || attackOriginPoint == null) return;

        Collider[] hitPlayers = Physics.OverlapSphere(attackOriginPoint.position, meleeAttackRadius, playerLayer);
        foreach (Collider playerCol in hitPlayers)
        {
            // Verificamos se o colisor realmente pertence ao nosso playerTarget,
            // embora a layerMask já deva filtrar isso.
            if (playerCol.transform == playerTarget)
            {
                PlayerHealth playerHealth = playerCol.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    Debug.Log($"[{gameObject.name}] EnemyAttack: Acertou {playerTarget.name} com MELEE causando {attackDamage} de dano.");
                    playerHealth.TakeDamage(attackDamage);
                }
                break; // Geralmente acerta apenas um player no melee
            }
        }
    }

    void FireProjectile()
    {
        if (projectilePrefab == null || projectileSpawnPoint == null || playerTarget == null)
        {
            Debug.LogWarning($"[{gameObject.name}] EnemyAttack: Faltam referências para FireProjectile (prefab, spawn ou target).");
            return;
        }
        Debug.Log($"[{gameObject.name}] EnemyAttack: Disparando projétil.");
        GameObject projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation);
        // O projétil precisa de um script para se mover e causar dano na colisão.
        // Exemplo simples de direcionamento:
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 direction = (playerTarget.position + Vector3.up * 0.5f - projectileSpawnPoint.position).normalized; // Mira um pouco acima da base do player
            projectile.transform.rotation = Quaternion.LookRotation(direction); // Orienta o projétil
            rb.linearVelocity = direction * projectileSpeed;
        }
        // Adicione um script ao projétil para lidar com colisão e dano.
    }

    void FireRaycast()
    {
        if (playerTarget == null || attackOriginPoint == null) return;
        Debug.Log($"[{gameObject.name}] EnemyAttack: Disparando Raycast.");
        RaycastHit hit;
        Vector3 direction = (playerTarget.position + Vector3.up * 0.5f - attackOriginPoint.position).normalized;
        if (Physics.Raycast(attackOriginPoint.position, direction, out hit, attackRange * 1.5f, playerLayer)) // Aumenta um pouco o range do raycast
        {
            if (hit.transform == playerTarget)
            {
                PlayerHealth playerHealth = hit.transform.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    Debug.Log($"[{gameObject.name}] EnemyAttack: Acertou {playerTarget.name} com RAYCAST causando {attackDamage} de dano.");
                    playerHealth.TakeDamage(attackDamage);
                }
            }
        }
    }

    void ThrowObject()
    {
        if (throwablePrefab == null || throwSpawnPoint == null || playerTarget == null)
        {
            Debug.LogWarning($"[{gameObject.name}] EnemyAttack: Faltam referências para ThrowObject (prefab, spawn ou target).");
            return;
        }
        Debug.Log($"[{gameObject.name}] EnemyAttack: Arremessando objeto.");
        GameObject throwable = Instantiate(throwablePrefab, throwSpawnPoint.position, throwSpawnPoint.rotation);
        Rigidbody rb = throwable.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Lógica para calcular trajetória de arremesso (mais complexa)
            // Exemplo simples: apenas força na direção do player
            Vector3 direction = (playerTarget.position - throwSpawnPoint.position).normalized;
            // Adicionar um pouco de força para cima para um arco
            direction = (direction + Vector3.up * 0.5f).normalized; // Ajuste o 0.5f para o arco desejado
            rb.AddForce(direction * throwForce, ForceMode.VelocityChange);
        }
        // O objeto arremessado (granada, magia) precisará de seu próprio script para explodir/causar dano.
    }


    // --- Funções Auxiliares para Animação (Ajuste conforme necessário) ---
    float GetActionPointDelayForAttack()
    {
        // Retorna o tempo em segundos DENTRO da animação de ataque onde o "acerto" acontece.
        // Idealmente, isso viria de dados da animação ou Animation Events.
        // Exemplo: se a animação dura 1s e o acerto é na metade, retorna 0.5s.
        // Precisa ser específico para a animação de ataque atual se você tiver várias.
        // Por enquanto, um valor fixo ou baseado na duração total.
        return GetCurrentAttackAnimationLength() * 0.4f; // Ex: acerto em 40% da animação
    }

    float GetCurrentAttackAnimationLength()
    {
        if (characterAnimator == null) return 1f; // Fallback

        // Esta é uma forma de obter a duração do clipe ATUALMENTE TOCANDO na camada 0.
        // Pode não ser 100% preciso se você tiver transições longas ou estados complexos.
        AnimatorClipInfo[] clipInfo = characterAnimator.GetCurrentAnimatorClipInfo(0);
        if (clipInfo.Length > 0)
        {
            // Se você tem triggers de ataque diferentes por tipo, pode checar o nome do estado aqui
            // para retornar durações específicas.
            // Ex: if (characterAnimator.GetCurrentAnimatorStateInfo(0).IsName("MeleeAttackState")) return 1.2f;
            return clipInfo[0].clip.length;
        }
        return 1f; // Fallback
    }

    public bool IsCurrentlyAttacking()
    {
        return isCurrentlyAttacking;
    }
}