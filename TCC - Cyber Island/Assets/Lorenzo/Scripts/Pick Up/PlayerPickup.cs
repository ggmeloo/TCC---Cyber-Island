using UnityEngine;
using System.Collections;

public class PlayerPickup : MonoBehaviour
{
    [Header("Configura��es de Teclas")]
    public KeyCode pickupKey = KeyCode.F;
    public KeyCode toggleHolsterKey = KeyCode.G;

    [Header("Pontos de Encaixe (for Equippable Items)")] // Pontos de Encaixe (para Itens Equip�veis)
    public Transform handPoint;
    public Transform standbyPoint;

    [Header("Configura��es de Rota��o do Item Equipado")]
    public Vector3 handItemLocalRotationEuler = new Vector3(0f, 90f, 0f);
    public Vector3 standbyItemLocalRotationEuler = new Vector3(0f, 160f, 0f);

    [Header("Configura��es de Soltar Item")]
    public float dropForwardForce = 5f;
    public float dropUpwardForce = 2f;

    [Header("Feedback Visual (Opcional)")]
    public Material highlightMaterial;

    [Header("Refer�ncias Externas")]
    public PlayerAttack playerAttack;
    public Animator playerAnimator;
    public PlayerMovement playerMovementScript;
    public PlayerInventoryDisplay inventoryDisplay; // <<< NOVA REFER�NCIA

    [Header("Configura��es de Anima��o")]
    public string pickupAnimationTriggerName = "PickupTrigger";
    public string equipAnimationTriggerName = "EquipTrigger";
    public string holsterAnimationTriggerName = "HolsterTrigger";
    public float pickupAnimationDuration = 0.7f;
    public float equipAnimationDuration = 0.5f;
    public float holsterAnimationDuration = 0.5f;

    private GameObject itemInRange = null;
    private GameObject heldItem = null; // Isto agora ser� primariamente para itens 'isDirectlyEquippable'
    private Rigidbody heldItemRb;
    private Collider heldItemCollider;
    private CollectibleItemInfo heldItemInfo; // Informa��o para o item equip�vel atualmente 'segurado'

    private Material originalItemMaterial;
    private Renderer itemInRangeRenderer;

    private bool isItemInHand = false;
    private bool isPerformingAction = false;

    void Start()
    {
        // --- Bloco de inicializa��o existente ---
        if (playerAttack == null)
        {
            playerAttack = GetComponent<PlayerAttack>();
            if (playerAttack == null) Debug.LogError("PLAYER PICKUP: PlayerAttack n�o encontrado!", this.gameObject);
        }
        if (playerMovementScript == null)
        {
            playerMovementScript = GetComponent<PlayerMovement>() ?? GetComponentInParent<PlayerMovement>();
            if (playerMovementScript == null) Debug.LogError("PLAYER PICKUP: PlayerMovement n�o encontrado!", this.gameObject);
        }
        if (playerAnimator == null)
        {
            if (playerMovementScript != null && playerMovementScript.characterAnimator != null)
                playerAnimator = playerMovementScript.characterAnimator;
            else
                playerAnimator = GetComponentInChildren<Animator>();
            if (playerAnimator == null) Debug.LogError("PLAYER PICKUP: Animator n�o encontrado!", this.gameObject);
        }
        if (handPoint == null) Debug.LogError("PLAYER PICKUP: HandPoint n�o atribu�do para itens equip�veis!", this.gameObject);
        if (standbyPoint == null) Debug.LogError("PLAYER PICKUP: StandbyPoint n�o atribu�do para itens equip�veis!", this.gameObject);
        // --- Fim do bloco ---

        // <<< NOVA INICIALIZA��O para InventoryDisplay >>>
        if (inventoryDisplay == null)
        {
            inventoryDisplay = GetComponent<PlayerInventoryDisplay>();
            if (inventoryDisplay == null)
                inventoryDisplay = GetComponentInParent<PlayerInventoryDisplay>(); // Verifica o pai se estiver em um objeto filho
            if (inventoryDisplay == null)
                Debug.LogWarning("PLAYER PICKUP: PlayerInventoryDisplay n�o encontrado! Itens n�o equip�veis n�o ir�o para os slots de invent�rio.", this.gameObject);
        }
    }

    void Update()
    {
        if (isPerformingAction) return;

        if (Input.GetKeyDown(pickupKey))
        {
            // Se n�o estiver segurando nada E houver um item ao alcance
            // OU se estiver segurando um item (para solt�-lo)
            if ((heldItem == null && itemInRange != null) || heldItem != null)
            {
                if (heldItem == null && itemInRange != null) // Pegar novo item
                {
                    // Verificar se itemInRange ainda � v�lido (n�o foi pego por outra coisa)
                    if (itemInRange.GetComponent<CollectibleItemInfo>() != null)
                    {
                        StartCoroutine(PickupSequence(itemInRange));
                    }
                    else
                    {
                        // Item pode ter sido destru�do ou alterado, limpar refer�ncias
                        ClearItemInRange();
                    }
                }
                else if (heldItem != null) // Soltar item segurado (que � equip�vel)
                {
                    DropHeldItem(); // Renomeado para clareza
                }
            }
        }

        if (Input.GetKeyDown(toggleHolsterKey) && heldItem != null) // Apenas para itens equip�veis
            StartCoroutine(ToggleHolsterSequence());
    }

    IEnumerator PickupSequence(GameObject itemToPickUp)
    {
        isPerformingAction = true;
        if (playerMovementScript != null) playerMovementScript.SetCanMove(false);

        // <<< MODIFICADO: Obter CollectibleItemInfo ANTES da anima��o >>>
        CollectibleItemInfo prospectiveItemInfo = itemToPickUp.GetComponent<CollectibleItemInfo>();
        if (prospectiveItemInfo == null)
        {
            Debug.LogError($"Item {itemToPickUp.name} n�o tem CollectibleItemInfo. Abortando coleta.", itemToPickUp);
            ClearItemInRange(); // Limpar refer�ncia ao item problem�tico
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
        // itemInRange ser� limpo abaixo SE a coleta for bem-sucedida

        if (collectedItemInfo.isDirectlyEquippable)
        {
            // L�gica para itens EQUIP�VEIS (como a espada)
            if (standbyPoint == null)
            {
                Debug.LogError("PLAYER PICKUP: StandbyPoint n�o atribu�do! N�o � poss�vel pegar item equip�vel.", this.gameObject);
                ClearItemInRange(); // Falha ao pegar, limpar refer�ncia
                return;
            }

            // Se j� estivermos segurando um item equip�vel, solte-o primeiro.
            if (heldItem != null)
            {
                DropHeldItem();
            }

            // Agora, pegue o novo item equip�vel
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

            MoveItemToPoint(standbyPoint); // Itens equip�veis v�o para o standbyPoint
            isItemInHand = false;

            if (playerAttack != null)
            {
                // Ao pegar um novo item equip�vel, o jogador fica "desarmado" at� apertar G
                playerAttack.EquipWeapon(PlayerAttack.WeaponAnimType.Unarmed, null);
            }
            ClearItemInRange(); // Item pego com sucesso
        }
        else
        {
            // L�gica para itens N�O EQUIP�VEIS (como o coco verde) -> V�o para o PlayerInventoryDisplay
            if (inventoryDisplay != null)
            {
                if (inventoryDisplay.AddItemToDesignatedSlot(itemToPickUp, collectedItemInfo.itemIdentifier))
                {
                    // Item adicionado ao slot de invent�rio com sucesso
                    // N�o se torna 'heldItem'
                    Debug.Log($"{itemToPickUp.name} enviado para o slot de invent�rio {collectedItemInfo.itemIdentifier}.");
                    ClearItemInRange(); // Item pego com sucesso
                }
                else
                {
                    Debug.LogWarning($"{itemToPickUp.name} n�o p�de ser adicionado ao slot de invent�rio (slot cheio ou n�o definido). Item n�o foi pego.");
                    // N�o limpar itemInRange aqui, pois a coleta falhou, o jogador pode tentar de novo
                }
            }
            else
            {
                Debug.LogWarning("PlayerInventoryDisplay n�o est� atribu�do. N�o � poss�vel enviar item para o slot de invent�rio.", this.gameObject);
                // N�o limpar itemInRange aqui, pois a coleta falhou
            }
        }
    }

    // Renomeado de DropItem para DropHeldItem para ser espec�fico sobre o item gerenciado por PlayerPickup
    void DropHeldItem()
    {
        if (heldItem == null || isPerformingAction) return; // S� solta itens equip�veis que est�o sendo 'held'

        heldItem.transform.SetParent(null);

        if (heldItemRb != null)
        {
            heldItemRb.isKinematic = false;
            heldItemRb.detectCollisions = true;
            Vector3 forceDirection = transform.forward;
            // Adiciona um pouco de varia��o aleat�ria para n�o cair sempre no mesmo lugar
            forceDirection += Random.insideUnitSphere * 0.1f;
            forceDirection.y = 0; // N�o queremos que a aleatoriedade afete muito a altura inicial
            forceDirection.Normalize();

            heldItemRb.AddForce(forceDirection * dropForwardForce, ForceMode.Impulse);
            heldItemRb.AddForce(Vector3.up * dropUpwardForce, ForceMode.Impulse);
        }
        if (heldItemCollider != null) heldItemCollider.enabled = true;

        if (playerAttack != null)
        {
            // Limpa o ponto de ataque da arma melee se o item solto era a arma atual
            // Usar this.heldItemInfo que � a info do item que ESTAVA sendo segurado
            if (this.heldItemInfo != null && this.heldItemInfo.itemWeaponType == PlayerAttack.WeaponAnimType.Melee)
            {
                playerAttack.SetMeleeWeaponAttackPoint(null);
            }
            playerAttack.EquipWeapon(PlayerAttack.WeaponAnimType.Unarmed, null); // Sempre desarmado ap�s soltar
        }

        // Limpar refer�ncias do item que foi solto
        heldItem = null;
        heldItemRb = null;
        heldItemCollider = null;
        this.heldItemInfo = null; // Limpar a info do item segurado
        isItemInHand = false;
    }

    IEnumerator ToggleHolsterSequence()
    {
        if (heldItem == null) yield break; // S� funciona para itens equip�veis 'held'

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
        if (handPoint != null && heldItem != null) // Verifica heldItem aqui tamb�m
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
                        Debug.LogWarning($"Arma Melee '{heldItem.name}' n�o possui um filho 'WeaponDamagePoint'. PlayerAttack usar� seu meleeWeaponAttackPoint padr�o se n�o for nulo, ou o transform da arma.");
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
        // A l�gica foi simplificada para refletir as mudan�as no Update e FinalizePickup.
        // Agora, OnTriggerEnter apenas se preocupa em *detectar* um item se n�o houver um
        // itemInRange j� detectado e o jogador n�o estiver no meio de uma a��o.
        // A decis�o de pegar um item equip�vel vs. n�o equip�vel, ou soltar o atual,
        // � gerenciada no Update e no PickupSequence/FinalizePickup.

        if (other.CompareTag("Collectible"))
        {
            if (itemInRange == null && !isPerformingAction) // S� detecta novo item se n�o houver um j� detectado e n�o estiver em a��o
            {
                CollectibleItemInfo info = other.GetComponent<CollectibleItemInfo>();
                if (info == null)
                {
                    Debug.LogWarning($"Item '{other.name}' com tag 'Collectible' n�o tem CollectibleItemInfo.", other.gameObject);
                    return;
                }

                // Se j� estou segurando um item equip�vel (heldItem != null), e o item ao alcance (info)
                // tamb�m � equip�vel, eu n�o o destaco como "itemInRange".
                // Isso porque a a��o de 'F' seria soltar o heldItem atual, n�o pegar este novo.
                // Eu s� quero destacar um novo item equip�vel se eu n�o estiver segurando nada equip�vel.
                // Se o item ao alcance n�o for equip�vel, eu sempre posso destac�-lo,
                // pois ele iria para o invent�rio sem conflitar com o heldItem.
                if (heldItem != null && info.isDirectlyEquippable)
                {
                    return; // N�o destacar um novo item equip�vel se j� estou segurando um.
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

    // Helper para limpar refer�ncias do item ao alcance
    private void ClearItemInRange()
    {
        if (itemInRangeRenderer != null && originalItemMaterial != null && highlightMaterial != null)
        {
            // Apenas restaure se o material ainda for o de highlight
            // (Pode ter sido pego e o renderer destru�do, ou o material mudado por outra raz�o)
            if (itemInRangeRenderer.sharedMaterial == highlightMaterial) // Use sharedMaterial para compara��o
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
        if (Camera.main != null) // Evita erro se n�o houver c�mera principal
        {
            Vector3 dropDirection = transform.position + transform.forward * 0.5f; // Ponto de partida para o gizmo
            Gizmos.DrawLine(dropDirection, dropDirection + (transform.forward.normalized * dropForwardForce / 10f) + (Vector3.up * dropUpwardForce / 10f));
        }
    }
}