using UnityEngine;
using System.Collections;

public class PlayerPickup : MonoBehaviour
{
    [Header("Configurações de Teclas")]
    public KeyCode pickupKey = KeyCode.F;
    public KeyCode toggleHolsterKey = KeyCode.G;
    // As teclas de hotbar (Q, Mouse1, 1,2,3...) não são mais necessárias para a coleta do inventário principal.
    // Deixei-as aqui caso queira reativar uma hotbar no futuro.

    [Header("Pontos de Encaixe (for Equippable Items)")]
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

    // <<< MUDANÇA 1: A referência agora é para o InventoryManager >>>
    public InventoryManager inventoryManager;

    [Header("Configurações de Animação")]
    public string pickupAnimationTriggerName = "PickupTrigger";
    public string equipAnimationTriggerName = "EquipTrigger";
    public string holsterAnimationTriggerName = "HolsterTrigger";
    public float pickupAnimationDuration = 0.7f;
    public float equipAnimationDuration = 0.5f;
    public float holsterAnimationDuration = 0.5f;

    private GameObject itemInRange = null;
    private GameObject heldItem = null;
    private Rigidbody heldItemRb;
    private Collider heldItemCollider;
    private CollectibleItemInfo heldItemInfo;
    private Material originalItemMaterial;
    private Renderer itemInRangeRenderer;
    private bool isItemInHand = false;
    private bool isPerformingAction = false;

    void Start()
    {
        if (playerAttack == null) playerAttack = GetComponent<PlayerAttack>();
        if (playerMovementScript == null) playerMovementScript = GetComponent<PlayerMovement>() ?? GetComponentInParent<PlayerMovement>();
        if (playerAnimator == null) playerAnimator = GetComponentInChildren<Animator>();
        if (handPoint == null) Debug.LogError("PLAYER PICKUP: HandPoint não atribuído!");
        if (standbyPoint == null) Debug.LogError("PLAYER PICKUP: StandbyPoint não atribuído!");

        // <<< MUDANÇA 2: Procura pelo InventoryManager, não mais pelo SlotBarManager >>>
        if (inventoryManager == null)
        {
            inventoryManager = FindObjectOfType<InventoryManager>();
            if (inventoryManager == null)
                Debug.LogWarning("PLAYER PICKUP: InventoryManager não encontrado na cena! A coleta de itens não funcionará.");
        }
    }

    void Update()
    {
        if (isPerformingAction) return;

        // Ação 1: Interagir com o mundo (Pegar item do chão ou Dropar a arma equipada)
        if (Input.GetKeyDown(pickupKey)) // Tecla 'F'
        {
            if (itemInRange != null)
            {
                if (itemInRange.GetComponent<CollectibleItemInfo>() != null)
                {
                    StartCoroutine(PickupSequence(itemInRange));
                }
            }
            else if (heldItem != null)
            {
                DropHeldItem();
            }
        }

        // Ação 2: Equipar/Guardar arma
        if (Input.GetKeyDown(toggleHolsterKey) && heldItem != null) // Tecla 'G'
        {
            StartCoroutine(ToggleHolsterSequence());
        }

        // <<< MUDANÇA 3: A lógica da hotbar (Q, 1, 2, 3...) foi removida do fluxo principal >>>
        // Se você quiser uma hotbar no futuro, pode reativar essas chamadas.
        // HandleSlotSelectionInput();
        // HandleItemUsageInput();
    }

    IEnumerator PickupSequence(GameObject itemToPickUp)
    {
        isPerformingAction = true;
        if (playerMovementScript != null) playerMovementScript.SetCanMove(false);

        CollectibleItemInfo prospectiveItemInfo = itemToPickUp.GetComponent<CollectibleItemInfo>();
        if (prospectiveItemInfo == null)
        {
            Debug.LogError($"Item {itemToPickUp.name} não tem CollectibleItemInfo. Abortando coleta.", itemToPickUp);
            ClearItemInRange();
            if (playerMovementScript != null) playerMovementScript.SetCanMove(true);
            isPerformingAction = false;
            yield break;
        }

        try
        {
            if (playerAnimator != null && !string.IsNullOrEmpty(pickupAnimationTriggerName))
            {
                playerAnimator.SetTrigger(pickupAnimationTriggerName);
                yield return new WaitForSeconds(pickupAnimationDuration);
            }
            FinalizePickup(itemToPickUp, prospectiveItemInfo);
        }
        finally
        {
            if (playerMovementScript != null) playerMovementScript.SetCanMove(true);
            isPerformingAction = false;
        }
        yield return null;
    }

    void FinalizePickup(GameObject itemToPickUp, CollectibleItemInfo collectedItemInfo)
    {
        if (itemInRangeRenderer != null && originalItemMaterial != null && highlightMaterial != null)
        {
            itemInRangeRenderer.material = originalItemMaterial;
        }

        if (collectedItemInfo.isDirectlyEquippable)
        {
            if (standbyPoint == null) return;
            if (heldItem != null) DropHeldItem();

            heldItem = itemToPickUp;
            heldItemRb = heldItem.GetComponent<Rigidbody>();
            heldItemCollider = heldItem.GetComponent<Collider>();
            this.heldItemInfo = collectedItemInfo;

            if (heldItemRb != null) { heldItemRb.isKinematic = true; heldItemRb.detectCollisions = false; }
            if (heldItemCollider != null) heldItemCollider.enabled = false;

            MoveItemToPoint(standbyPoint);
            isItemInHand = false;

            if (playerAttack != null) playerAttack.EquipWeapon(PlayerAttack.WeaponAnimType.Unarmed, null);
            ClearItemInRange();
        }
        else
        {
            // <<< MUDANÇA 4: A chamada agora vai para o InventoryManager >>>
            if (inventoryManager != null)
            {
                bool foiAdicionado = inventoryManager.AddItem(collectedItemInfo);
                if (foiAdicionado)
                {
                    Debug.Log($"{itemToPickUp.name} adicionado ao inventário. Destruindo o objeto do mundo.");
                    ClearItemInRange();
                    Destroy(itemToPickUp);
                }
                else
                {
                    Debug.LogWarning($"{itemToPickUp.name} não pôde ser adicionado (inventário cheio).");
                }
            }
            else
            {
                Debug.LogError("InventoryManager não está atribuído no PlayerPickup! Não é possível coletar item para a UI.", this.gameObject);
            }
        }
    }

    // O resto do seu código pode permanecer, mas as funções abaixo não serão mais chamadas a partir do Update.
    // Elas são específicas da hotbar.

    /*
    void HandleSlotSelectionInput()
    {
        if (slotBarManager == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) slotBarManager.SelectSlot(0);
        // ... etc
    }

    void HandleItemUsageInput()
    {
        if (Input.GetKeyDown(useItemKey))
        {
            if (slotBarManager != null)
            {
                slotBarManager.UseSelectedItem();
            }
        }
    }
    */

    void DropHeldItem()
    {
        if (heldItem == null || isPerformingAction) return;

        heldItem.transform.SetParent(null);
        if (heldItemRb != null)
        {
            heldItemRb.isKinematic = false;
            heldItemRb.detectCollisions = true;
            Vector3 forceDirection = (transform.forward + Random.insideUnitSphere * 0.1f);
            forceDirection.y = 0;
            forceDirection.Normalize();
            heldItemRb.AddForce(forceDirection * dropForwardForce, ForceMode.Impulse);
            heldItemRb.AddForce(Vector3.up * dropUpwardForce, ForceMode.Impulse);
        }
        if (heldItemCollider != null) heldItemCollider.enabled = true;

        if (playerAttack != null)
        {
            if (this.heldItemInfo != null && this.heldItemInfo.itemWeaponType == PlayerAttack.WeaponAnimType.Melee)
            {
                playerAttack.SetMeleeWeaponAttackPoint(null);
            }
            playerAttack.EquipWeapon(PlayerAttack.WeaponAnimType.Unarmed, null);
        }

        heldItem = null;
        heldItemRb = null;
        heldItemCollider = null;
        this.heldItemInfo = null;
        isItemInHand = false;
    }

    IEnumerator ToggleHolsterSequence()
    {
        if (heldItem == null) yield break;

        isPerformingAction = true;
        if (playerMovementScript != null) playerMovementScript.SetCanMove(false);

        try
        {
            if (!isItemInHand)
            {
                if (playerAnimator != null && !string.IsNullOrEmpty(equipAnimationTriggerName))
                {
                    playerAnimator.SetTrigger(equipAnimationTriggerName);
                    yield return new WaitForSeconds(equipAnimationDuration);
                }
                FinalizeEquip();
            }
            else
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
        yield return null;
    }

    void FinalizeEquip()
    {
        if (handPoint != null && heldItem != null)
        {
            MoveItemToPoint(handPoint);
            isItemInHand = true;
            if (playerAttack != null && this.heldItemInfo != null)
            {
                playerAttack.EquipWeapon(this.heldItemInfo.itemWeaponType, heldItem.transform.Find("WeaponDamagePoint"));
            }
        }
    }

    void FinalizeHolster()
    {
        if (standbyPoint != null && heldItem != null)
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
        if (other.CompareTag("Collectible"))
        {
            if (itemInRange == null && !isPerformingAction)
            {
                CollectibleItemInfo info = other.GetComponent<CollectibleItemInfo>();
                if (info == null) return;
                if (heldItem != null && info.isDirectlyEquippable) return;

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

    private void ClearItemInRange()
    {
        if (itemInRangeRenderer != null && originalItemMaterial != null && highlightMaterial != null)
        {
            if (itemInRangeRenderer.sharedMaterial == highlightMaterial)
                itemInRangeRenderer.material = originalItemMaterial;
        }
        itemInRange = null;
        itemInRangeRenderer = null;
        originalItemMaterial = null;
    }

    void OnDrawGizmosSelected()
    {
        if (handPoint != null) Gizmos.DrawWireSphere(handPoint.position, 0.1f);
        if (standbyPoint != null) Gizmos.DrawWireSphere(standbyPoint.position, 0.1f);
    }
}