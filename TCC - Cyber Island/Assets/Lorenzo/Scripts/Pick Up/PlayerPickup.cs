using UnityEngine;
using System.Collections;

public class PlayerPickup : MonoBehaviour
{
    [Header("Configurações de Teclas")]
    public KeyCode pickupKey = KeyCode.F;
    public KeyCode toggleHolsterKey = KeyCode.G;

    [Header("Pontos de Encaixe (for Equippable Items)")] // Pontos de Encaixe (para Itens Equipáveis)
    public Transform handPoint;
    public Transform standbyPoint;

    [Header("Configurações de Rotação do Item Equipado")]
    public Vector3 handItemLocalRotationEuler = new Vector3(0f, 90f, 0f);
    public Vector3 standbyItemLocalRotationEuler = new Vector3(0f, 160f, 0f);

    [Header("Configurações de Soltar Item")]
    public float dropForwardForce = 5f;
    public float dropUpwardForce = 2f;

    [Header("Feedback Visual (Opcional)")]
    public Material highlightMaterial;

    [Header("Referências Externas")]
    public PlayerAttack playerAttack;
    public Animator playerAnimator;
    public PlayerMovement playerMovementScript;
    public PlayerInventoryDisplay inventoryDisplay; // <<< NOVA REFERÊNCIA

    [Header("Configurações de Animação")]
    public string pickupAnimationTriggerName = "PickupTrigger";
    public string equipAnimationTriggerName = "EquipTrigger";
    public string holsterAnimationTriggerName = "HolsterTrigger";
    public float pickupAnimationDuration = 0.7f;
    public float equipAnimationDuration = 0.5f;
    public float holsterAnimationDuration = 0.5f;

    private GameObject itemInRange = null;
    private GameObject heldItem = null; // Isto agora será primariamente para itens 'isDirectlyEquippable'
    private Rigidbody heldItemRb;
    private Collider heldItemCollider;
    private CollectibleItemInfo heldItemInfo; // Informação para o item equipável atualmente 'segurado'

    private Material originalItemMaterial;
    private Renderer itemInRangeRenderer;

    private bool isItemInHand = false;
    private bool isPerformingAction = false;

    void Start()
    {
        // --- Bloco de inicialização existente ---
        if (playerAttack == null)
        {
            playerAttack = GetComponent<PlayerAttack>();
            if (playerAttack == null) Debug.LogError("PLAYER PICKUP: PlayerAttack não encontrado!", this.gameObject);
        }
        if (playerMovementScript == null)
        {
            playerMovementScript = GetComponent<PlayerMovement>() ?? GetComponentInParent<PlayerMovement>();
            if (playerMovementScript == null) Debug.LogError("PLAYER PICKUP: PlayerMovement não encontrado!", this.gameObject);
        }
        if (playerAnimator == null)
        {
            if (playerMovementScript != null && playerMovementScript.characterAnimator != null)
                playerAnimator = playerMovementScript.characterAnimator;
            else
                playerAnimator = GetComponentInChildren<Animator>();
            if (playerAnimator == null) Debug.LogError("PLAYER PICKUP: Animator não encontrado!", this.gameObject);
        }
        if (handPoint == null) Debug.LogError("PLAYER PICKUP: HandPoint não atribuído para itens equipáveis!", this.gameObject);
        if (standbyPoint == null) Debug.LogError("PLAYER PICKUP: StandbyPoint não atribuído para itens equipáveis!", this.gameObject);
        // --- Fim do bloco ---

        // <<< NOVA INICIALIZAÇÃO para InventoryDisplay >>>
        if (inventoryDisplay == null)
        {
            inventoryDisplay = GetComponent<PlayerInventoryDisplay>();
            if (inventoryDisplay == null)
                inventoryDisplay = GetComponentInParent<PlayerInventoryDisplay>(); // Verifica o pai se estiver em um objeto filho
            if (inventoryDisplay == null)
                Debug.LogWarning("PLAYER PICKUP: PlayerInventoryDisplay não encontrado! Itens não equipáveis não irão para os slots de inventário.", this.gameObject);
        }
    }

    void Update()
    {
        if (isPerformingAction) return;

        if (Input.GetKeyDown(pickupKey))
        {
            // Se não estiver segurando nada E houver um item ao alcance
            // OU se estiver segurando um item (para soltá-lo)
            if ((heldItem == null && itemInRange != null) || heldItem != null)
            {
                if (heldItem == null && itemInRange != null) // Pegar novo item
                {
                    // Verificar se itemInRange ainda é válido (não foi pego por outra coisa)
                    if (itemInRange.GetComponent<CollectibleItemInfo>() != null)
                    {
                        StartCoroutine(PickupSequence(itemInRange));
                    }
                    else
                    {
                        // Item pode ter sido destruído ou alterado, limpar referências
                        ClearItemInRange();
                    }
                }
                else if (heldItem != null) // Soltar item segurado (que é equipável)
                {
                    DropHeldItem(); // Renomeado para clareza
                }
            }
        }

        if (Input.GetKeyDown(toggleHolsterKey) && heldItem != null) // Apenas para itens equipáveis
            StartCoroutine(ToggleHolsterSequence());
    }

    IEnumerator PickupSequence(GameObject itemToPickUp)
    {
        isPerformingAction = true;
        if (playerMovementScript != null) playerMovementScript.SetCanMove(false);

        // <<< MODIFICADO: Obter CollectibleItemInfo ANTES da animação >>>
        CollectibleItemInfo prospectiveItemInfo = itemToPickUp.GetComponent<CollectibleItemInfo>();
        if (prospectiveItemInfo == null)
        {
            Debug.LogError($"Item {itemToPickUp.name} não tem CollectibleItemInfo. Abortando coleta.", itemToPickUp);
            ClearItemInRange(); // Limpar referência ao item problemático
            if (playerMovementScript != null) playerMovementScript.SetCanMove(true);
            isPerformingAction = false;
            yield break; // Sair da corrotina
        }

        try
        {
            if (playerAnimator != null && !string.IsNullOrEmpty(pickupAnimationTriggerName))
            {
                playerAnimator.SetTrigger(pickupAnimationTriggerName);
                yield return new WaitForSeconds(pickupAnimationDuration);
            }
            FinalizePickup(itemToPickUp, prospectiveItemInfo); // Passar info
        }
        finally
        {
            if (playerMovementScript != null) playerMovementScript.SetCanMove(true);
            isPerformingAction = false;
        }
    }

    // <<< MODIFICADO: Recebe CollectibleItemInfo >>>
    void FinalizePickup(GameObject itemToPickUp, CollectibleItemInfo collectedItemInfo)
    {
        // Remover o highlight do item que estava ao alcance, pois estamos processando-o
        if (itemInRangeRenderer != null && originalItemMaterial != null && highlightMaterial != null)
            itemInRangeRenderer.material = originalItemMaterial;
        // itemInRange será limpo abaixo SE a coleta for bem-sucedida

        if (collectedItemInfo.isDirectlyEquippable)
        {
            // Lógica para itens EQUIPÁVEIS (como a espada)
            if (standbyPoint == null)
            {
                Debug.LogError("PLAYER PICKUP: StandbyPoint não atribuído! Não é possível pegar item equipável.", this.gameObject);
                ClearItemInRange(); // Falha ao pegar, limpar referência
                return;
            }

            // Se já estivermos segurando um item equipável, solte-o primeiro.
            if (heldItem != null)
            {
                DropHeldItem();
            }

            // Agora, pegue o novo item equipável
            heldItem = itemToPickUp;
            heldItemRb = heldItem.GetComponent<Rigidbody>();
            heldItemCollider = heldItem.GetComponent<Collider>();
            this.heldItemInfo = collectedItemInfo; // Armazena info do item segurado

            if (heldItemRb != null)
            {
                heldItemRb.isKinematic = true;
                heldItemRb.detectCollisions = false;
            }
            if (heldItemCollider != null) heldItemCollider.enabled = false;

            MoveItemToPoint(standbyPoint); // Itens equipáveis vão para o standbyPoint
            isItemInHand = false;

            if (playerAttack != null)
            {
                // Ao pegar um novo item equipável, o jogador fica "desarmado" até apertar G
                playerAttack.EquipWeapon(PlayerAttack.WeaponAnimType.Unarmed, null);
            }
            ClearItemInRange(); // Item pego com sucesso
        }
        else
        {
            // Lógica para itens NÃO EQUIPÁVEIS (como o coco verde) -> Vão para o PlayerInventoryDisplay
            if (inventoryDisplay != null)
            {
                if (inventoryDisplay.AddItemToDesignatedSlot(itemToPickUp, collectedItemInfo.itemIdentifier))
                {
                    // Item adicionado ao slot de inventário com sucesso
                    // Não se torna 'heldItem'
                    Debug.Log($"{itemToPickUp.name} enviado para o slot de inventário {collectedItemInfo.itemIdentifier}.");
                    ClearItemInRange(); // Item pego com sucesso
                }
                else
                {
                    Debug.LogWarning($"{itemToPickUp.name} não pôde ser adicionado ao slot de inventário (slot cheio ou não definido). Item não foi pego.");
                    // Não limpar itemInRange aqui, pois a coleta falhou, o jogador pode tentar de novo
                }
            }
            else
            {
                Debug.LogWarning("PlayerInventoryDisplay não está atribuído. Não é possível enviar item para o slot de inventário.", this.gameObject);
                // Não limpar itemInRange aqui, pois a coleta falhou
            }
        }
    }

    // Renomeado de DropItem para DropHeldItem para ser específico sobre o item gerenciado por PlayerPickup
    void DropHeldItem()
    {
        if (heldItem == null || isPerformingAction) return; // Só solta itens equipáveis que estão sendo 'held'

        heldItem.transform.SetParent(null);

        if (heldItemRb != null)
        {
            heldItemRb.isKinematic = false;
            heldItemRb.detectCollisions = true;
            Vector3 forceDirection = transform.forward;
            // Adiciona um pouco de variação aleatória para não cair sempre no mesmo lugar
            forceDirection += Random.insideUnitSphere * 0.1f;
            forceDirection.y = 0; // Não queremos que a aleatoriedade afete muito a altura inicial
            forceDirection.Normalize();

            heldItemRb.AddForce(forceDirection * dropForwardForce, ForceMode.Impulse);
            heldItemRb.AddForce(Vector3.up * dropUpwardForce, ForceMode.Impulse);
        }
        if (heldItemCollider != null) heldItemCollider.enabled = true;

        if (playerAttack != null)
        {
            // Limpa o ponto de ataque da arma melee se o item solto era a arma atual
            // Usar this.heldItemInfo que é a info do item que ESTAVA sendo segurado
            if (this.heldItemInfo != null && this.heldItemInfo.itemWeaponType == PlayerAttack.WeaponAnimType.Melee)
            {
                playerAttack.SetMeleeWeaponAttackPoint(null);
            }
            playerAttack.EquipWeapon(PlayerAttack.WeaponAnimType.Unarmed, null); // Sempre desarmado após soltar
        }

        // Limpar referências do item que foi solto
        heldItem = null;
        heldItemRb = null;
        heldItemCollider = null;
        this.heldItemInfo = null; // Limpar a info do item segurado
        isItemInHand = false;
    }

    IEnumerator ToggleHolsterSequence()
    {
        if (heldItem == null) yield break; // Só funciona para itens equipáveis 'held'

        isPerformingAction = true;
        if (playerMovementScript != null) playerMovementScript.SetCanMove(false);

        try
        {
            if (!isItemInHand) // Equipar
            {
                if (playerAnimator != null && !string.IsNullOrEmpty(equipAnimationTriggerName))
                {
                    playerAnimator.SetTrigger(equipAnimationTriggerName);
                    yield return new WaitForSeconds(equipAnimationDuration);
                }
                FinalizeEquip();
            }
            else // Guardar
            {
                if (playerAnimator != null && !string.IsNullOrEmpty(holsterAnimationTriggerName))
                {
                    playerAnimator.SetTrigger(holsterAnimationTriggerName);
                    yield return new WaitForSeconds(holsterAnimationDuration);
                }
                FinalizeHolster();
            }
        }
        finally
        {
            if (playerMovementScript != null) playerMovementScript.SetCanMove(true);
            isPerformingAction = false;
        }
    }

    void FinalizeEquip()
    {
        if (handPoint != null && heldItem != null) // Verifica heldItem aqui também
        {
            MoveItemToPoint(handPoint);
            isItemInHand = true;
            if (playerAttack != null && this.heldItemInfo != null) // Usa this.heldItemInfo
            {
                PlayerAttack.WeaponAnimType typeToEquip = this.heldItemInfo.itemWeaponType;
                Transform weaponDamagePointForAttackScript = null;

                if (typeToEquip == PlayerAttack.WeaponAnimType.Melee)
                {
                    weaponDamagePointForAttackScript = heldItem.transform.Find("WeaponDamagePoint");
                    if (weaponDamagePointForAttackScript == null)
                    {
                        Debug.LogWarning($"Arma Melee '{heldItem.name}' não possui um filho 'WeaponDamagePoint'. PlayerAttack usará seu meleeWeaponAttackPoint padrão se não for nulo, ou o transform da arma.");
                    }
                }
                playerAttack.EquipWeapon(typeToEquip, weaponDamagePointForAttackScript);
            }
        }
    }

    void FinalizeHolster()
    {
        if (standbyPoint != null && heldItem != null) // Verifica heldItem
        {
            MoveItemToPoint(standbyPoint);
            isItemInHand = false;
            if (playerAttack != null)
            {
                playerAttack.EquipWeapon(PlayerAttack.WeaponAnimType.Unarmed, null);
            }
        }
    }

    void MoveItemToPoint(Transform targetPoint)
    {
        if (heldItem == null || targetPoint == null) return;
        heldItem.transform.SetParent(targetPoint);
        heldItem.transform.localPosition = Vector3.zero;
        if (targetPoint == handPoint)
            heldItem.transform.localRotation = Quaternion.Euler(handItemLocalRotationEuler);
        else if (targetPoint == standbyPoint)
            heldItem.transform.localRotation = Quaternion.Euler(standbyItemLocalRotationEuler);
        else
            heldItem.transform.localRotation = Quaternion.identity;
    }

    private void OnTriggerEnter(Collider other)
    {
        // A lógica foi simplificada para refletir as mudanças no Update e FinalizePickup.
        // Agora, OnTriggerEnter apenas se preocupa em *detectar* um item se não houver um
        // itemInRange já detectado e o jogador não estiver no meio de uma ação.
        // A decisão de pegar um item equipável vs. não equipável, ou soltar o atual,
        // é gerenciada no Update e no PickupSequence/FinalizePickup.

        if (other.CompareTag("Collectible"))
        {
            if (itemInRange == null && !isPerformingAction) // Só detecta novo item se não houver um já detectado e não estiver em ação
            {
                CollectibleItemInfo info = other.GetComponent<CollectibleItemInfo>();
                if (info == null)
                {
                    Debug.LogWarning($"Item '{other.name}' com tag 'Collectible' não tem CollectibleItemInfo.", other.gameObject);
                    return;
                }

                // Se já estou segurando um item equipável (heldItem != null), e o item ao alcance (info)
                // também é equipável, eu não o destaco como "itemInRange".
                // Isso porque a ação de 'F' seria soltar o heldItem atual, não pegar este novo.
                // Eu só quero destacar um novo item equipável se eu não estiver segurando nada equipável.
                // Se o item ao alcance não for equipável, eu sempre posso destacá-lo,
                // pois ele iria para o inventário sem conflitar com o heldItem.
                if (heldItem != null && info.isDirectlyEquippable)
                {
                    return; // Não destacar um novo item equipável se já estou segurando um.
                }

                itemInRange = other.gameObject;
                if (highlightMaterial != null)
                {
                    itemInRangeRenderer = itemInRange.GetComponent<Renderer>();
                    if (itemInRangeRenderer != null)
                    {
                        originalItemMaterial = itemInRangeRenderer.material;
                        itemInRangeRenderer.material = highlightMaterial;
                    }
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == itemInRange)
        {
            ClearItemInRange();
        }
    }

    // Helper para limpar referências do item ao alcance
    private void ClearItemInRange()
    {
        if (itemInRangeRenderer != null && originalItemMaterial != null && highlightMaterial != null)
        {
            // Apenas restaure se o material ainda for o de highlight
            // (Pode ter sido pego e o renderer destruído, ou o material mudado por outra razão)
            if (itemInRangeRenderer.sharedMaterial == highlightMaterial) // Use sharedMaterial para comparação
                itemInRangeRenderer.material = originalItemMaterial;
        }
        itemInRange = null;
        itemInRangeRenderer = null;
        originalItemMaterial = null;
    }


    void OnDrawGizmosSelected()
    {
        if (handPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(handPoint.position, 0.1f);
        }
        if (standbyPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(standbyPoint.position, 0.1f);
        }
        Gizmos.color = Color.red;
        if (Camera.main != null) // Evita erro se não houver câmera principal
        {
            Vector3 dropDirection = transform.position + transform.forward * 0.5f; // Ponto de partida para o gizmo
            Gizmos.DrawLine(dropDirection, dropDirection + (transform.forward.normalized * dropForwardForce / 10f) + (Vector3.up * dropUpwardForce / 10f));
        }
    }
}