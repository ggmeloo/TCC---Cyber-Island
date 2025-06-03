using UnityEngine;
using System.Collections;

public class PlayerPickup : MonoBehaviour
{
    [Header("Configurações de Teclas")]
    public KeyCode pickupKey = KeyCode.F;
    public KeyCode toggleHolsterKey = KeyCode.G;

    [Header("Pontos de Encaixe")]
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
        if (handPoint == null) Debug.LogError("PLAYER PICKUP: HandPoint não atribuído!", this.gameObject);
        if (standbyPoint == null) Debug.LogError("PLAYER PICKUP: StandbyPoint não atribuído!", this.gameObject);
    }

    void Update()
    {
        if (isPerformingAction) return;

        if (Input.GetKeyDown(pickupKey))
        {
            if (heldItem == null && itemInRange != null && standbyPoint != null)
                StartCoroutine(PickupSequence(itemInRange));
            else if (heldItem != null)
                DropItem();
        }

        if (Input.GetKeyDown(toggleHolsterKey) && heldItem != null)
            StartCoroutine(ToggleHolsterSequence());
    }

    IEnumerator PickupSequence(GameObject itemToPickUp)
    {
        isPerformingAction = true;
        if (playerMovementScript != null) playerMovementScript.SetCanMove(false);

        try
        {
            if (playerAnimator != null && !string.IsNullOrEmpty(pickupAnimationTriggerName))
            {
                playerAnimator.SetTrigger(pickupAnimationTriggerName);
                yield return new WaitForSeconds(pickupAnimationDuration);
            }
            FinalizePickup(itemToPickUp);
        }
        finally
        {
            if (playerMovementScript != null) playerMovementScript.SetCanMove(true);
            isPerformingAction = false;
        }
    }

    void FinalizePickup(GameObject itemToPickUp)
    {
        if (standbyPoint == null) return;

        if (itemInRangeRenderer != null && originalItemMaterial != null && highlightMaterial != null)
            itemInRangeRenderer.material = originalItemMaterial;

        itemInRange = null;
        itemInRangeRenderer = null;
        originalItemMaterial = null;

        heldItem = itemToPickUp;
        heldItemRb = heldItem.GetComponent<Rigidbody>();
        heldItemCollider = heldItem.GetComponent<Collider>();
        heldItemInfo = heldItem.GetComponent<CollectibleItemInfo>();

        if (heldItemRb != null)
        {
            heldItemRb.isKinematic = true;
            heldItemRb.detectCollisions = false;
        }
        if (heldItemCollider != null) heldItemCollider.enabled = false;

        MoveItemToPoint(standbyPoint);
        isItemInHand = false;

        if (playerAttack != null)
        {
            playerAttack.EquipWeapon(PlayerAttack.WeaponAnimType.Unarmed, null); // Ao pegar, inicialmente vai para Unarmed
        }
    }

    void DropItem()
    {
        if (heldItem == null || isPerformingAction) return;
        heldItem.transform.SetParent(null);

        if (heldItemRb != null)
        {
            heldItemRb.isKinematic = false;
            heldItemRb.detectCollisions = true;
            Vector3 forceDirection = transform.forward;
            heldItemRb.AddForce(forceDirection * dropForwardForce, ForceMode.Impulse);
            heldItemRb.AddForce(Vector3.up * dropUpwardForce, ForceMode.Impulse);
        }
        if (heldItemCollider != null) heldItemCollider.enabled = true;

        if (playerAttack != null)
        {
            // Limpa o ponto de ataque da arma melee se o item solto era a arma atual
            if (heldItemInfo != null && heldItemInfo.itemWeaponType == PlayerAttack.WeaponAnimType.Melee)
            {
                playerAttack.SetMeleeWeaponAttackPoint(null);
            }
        }

        heldItem = null;
        heldItemRb = null;
        heldItemCollider = null;
        heldItemInfo = null;
        isItemInHand = false;


        if (playerAttack != null)
        {
            playerAttack.EquipWeapon(PlayerAttack.WeaponAnimType.Unarmed, null);
        }
    }

    IEnumerator ToggleHolsterSequence()
    {
        if (heldItem == null) yield break;

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
        if (handPoint != null)
        {
            MoveItemToPoint(handPoint);
            isItemInHand = true;
            if (playerAttack != null)
            {
                PlayerAttack.WeaponAnimType typeToEquip = PlayerAttack.WeaponAnimType.Unarmed; // Default to unarmed
                Transform weaponDamagePointForAttackScript = null;

                if (heldItemInfo != null)
                {
                    typeToEquip = heldItemInfo.itemWeaponType;
                    if (typeToEquip == PlayerAttack.WeaponAnimType.Melee && heldItem != null)
                    {
                        // Tenta encontrar um ponto de dano específico na arma
                        weaponDamagePointForAttackScript = heldItem.transform.Find("WeaponDamagePoint");
                        if (weaponDamagePointForAttackScript == null)
                        {
                            Debug.LogWarning($"Arma Melee '{heldItem.name}' não possui um filho 'WeaponDamagePoint'. PlayerAttack usará seu meleeWeaponAttackPoint padrão se não for nulo, ou o transform da arma.");
                        }
                    }
                }
                playerAttack.EquipWeapon(typeToEquip, weaponDamagePointForAttackScript);
            }
        }
    }

    void FinalizeHolster()
    {
        if (standbyPoint != null)
        {
            MoveItemToPoint(standbyPoint);
            isItemInHand = false;
            if (playerAttack != null)
            {
                // Ao guardar, efetivamente ficamos desarmados em termos de animação de ataque
                // e limpamos o ponto de ataque da arma melee.
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
        if (isPerformingAction || heldItem != null) return;
        if (other.CompareTag("Collectible"))
        {
            if (itemInRange == null)
            {
                if (other.GetComponent<CollectibleItemInfo>() == null)
                    Debug.LogWarning($"Item '{other.name}' não tem CollectibleItemInfo.", other.gameObject);
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
            if (itemInRangeRenderer != null && originalItemMaterial != null && highlightMaterial != null)
                itemInRangeRenderer.material = originalItemMaterial;
            itemInRange = null;
            itemInRangeRenderer = null;
            originalItemMaterial = null;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (handPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(handPoint.position, 0.1f);
            // ... (código do Frustum)
        }
        if (standbyPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(standbyPoint.position, 0.1f);
            // ... (código do Frustum)
        }
        // ... (código do Gizmo de força de soltar)
    }
}