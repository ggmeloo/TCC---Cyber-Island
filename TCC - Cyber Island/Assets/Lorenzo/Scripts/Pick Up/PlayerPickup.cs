using UnityEngine;
using System.Collections;

public class PlayerPickup : MonoBehaviour
{
    [Header("Configurações de Teclas")]
    public KeyCode pickupKey = KeyCode.F; // Tecla para PEGAR itens do mundo
    public KeyCode dropKey = KeyCode.Q;   // <<< NOVA TECLA para DROPAR item da hotbar
    public KeyCode toggleHolsterKey = KeyCode.G;
    public KeyCode useItemKey = KeyCode.Mouse1;

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
    public SlotBarManager slotBarManager;

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

        if (slotBarManager == null)
        {
            slotBarManager = FindObjectOfType<SlotBarManager>();
            if (slotBarManager == null)
                Debug.LogWarning("PLAYER PICKUP: SlotBarManager não encontrado na cena! Itens da hotbar não funcionarão.");
        }
    }

    void Update()
    {
        if (isPerformingAction) return;

        // <<< LÓGICA DE INPUT REESTRUTURADA >>>

        // Ação 1: Interagir com o mundo (Pegar item do chão ou Dropar a arma equipada)
        if (Input.GetKeyDown(pickupKey)) // Tecla 'F'
        {
            // Se tem um item ao alcance, a prioridade é pegar
            if (itemInRange != null)
            {
                if (itemInRange.GetComponent<CollectibleItemInfo>() != null)
                {
                    StartCoroutine(PickupSequence(itemInRange));
                }
            }
            // Se não tem item ao alcance, mas estamos segurando uma arma, drope-a
            else if (heldItem != null)
            {
                DropHeldItem();
            }
        }

        // Ação 2: Dropar item da hotbar (tecla separada)
        if (Input.GetKeyDown(dropKey)) // Tecla 'Q'
        {
            if (slotBarManager != null)
            {
                slotBarManager.DropSelectedItem();
            }
        }

        // Ação 3: Equipar/Guardar arma
        if (Input.GetKeyDown(toggleHolsterKey) && heldItem != null) // Tecla 'G'
        {
            StartCoroutine(ToggleHolsterSequence());
        }

        // Ação 4: Selecionar e Usar itens da hotbar
        HandleSlotSelectionInput(); // Teclas 1, 2, 3...
        HandleItemUsageInput();     // Botão direito do mouse
    }

    // O resto do seu código (FinalizePickup, DropHeldItem, etc.) permanece EXATAMENTE O MESMO.
    // Nenhuma outra modificação é necessária.

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
            if (slotBarManager != null)
            {
                bool foiAdicionado = slotBarManager.AddItem(collectedItemInfo);
                if (foiAdicionado)
                {
                    Debug.Log($"{itemToPickUp.name} adicionado à barra de slots da UI. Destruindo o objeto do mundo.");
                    ClearItemInRange();
                    Destroy(itemToPickUp);
                }
                else
                {
                    Debug.LogWarning($"{itemToPickUp.name} não pôde ser adicionado à barra de slots (cheia). Item não foi pego.");
                }
            }
            else
            {
                Debug.LogError("SlotBarManager não está atribuído no PlayerPickup! Não é possível coletar item para a UI.", this.gameObject);
            }
        }
    }

    void HandleSlotSelectionInput()
    {
        if (slotBarManager == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) slotBarManager.SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) slotBarManager.SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) slotBarManager.SelectSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) slotBarManager.SelectSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) slotBarManager.SelectSlot(4);
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