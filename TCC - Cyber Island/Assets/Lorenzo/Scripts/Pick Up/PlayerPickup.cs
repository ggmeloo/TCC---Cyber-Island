using UnityEngine;

public class PlayerPickup : MonoBehaviour
{
    [Header("Configura��es de Teclas")]
    public KeyCode pickupKey = KeyCode.F;
    public KeyCode toggleHolsterKey = KeyCode.G;

    [Header("Pontos de Encaixe")]
    public Transform handPoint;
    public Transform standbyPoint;

    [Header("Configura��es de Rota��o do Item Equipado")]
    [Tooltip("Rota��o local do item quando na m�o (�ngulos de Euler em Graus). Ex: (0, 90, 0)")]
    public Vector3 handItemLocalRotationEuler = new Vector3(0f, 90f, 0f); // Padr�o para 90 graus no eixo Y
    [Tooltip("Rota��o local do item quando em standby/cintura (�ngulos de Euler em Graus). Ex: (0, 160, 0)")]
    public Vector3 standbyItemLocalRotationEuler = new Vector3(0f, 160f, 0f); // Padr�o para 160 graus no eixo Y

    [Header("Configura��es de Soltar Item")]
    public float dropForwardForce = 5f;
    public float dropUpwardForce = 2f;

    [Header("Feedback Visual (Opcional)")]
    public Material highlightMaterial;

    [Header("Refer�ncias Externas")]
    public PlayerAttack playerAttack;

    private GameObject itemInRange = null;
    private GameObject heldItem = null;
    private Rigidbody heldItemRb;
    private Collider heldItemCollider;
    private CollectibleItemInfo heldItemInfo;

    private Material originalItemMaterial;
    private Renderer itemInRangeRenderer;

    private bool isItemInHand = false;

    void Start()
    {
        if (playerAttack == null)
        {
            playerAttack = GetComponent<PlayerAttack>();
            if (playerAttack == null)
            {
                Debug.LogError("PLAYER PICKUP: Script PlayerAttack n�o encontrado! Funcionalidade de ataque n�o ser� alterada.", this.gameObject);
            }
        }
        if (handPoint == null) Debug.LogError("PLAYER PICKUP: HandPoint n�o atribu�do!", this.gameObject);
        if (standbyPoint == null) Debug.LogError("PLAYER PICKUP: StandbyPoint n�o atribu�do!", this.gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(pickupKey))
        {
            if (heldItem == null && itemInRange != null && standbyPoint != null)
            {
                PickUpItem(itemInRange);
            }
            else if (heldItem != null)
            {
                DropItem();
            }
        }

        if (Input.GetKeyDown(toggleHolsterKey) && heldItem != null)
        {
            ToggleHolster();
        }
    }

    void PickUpItem(GameObject itemToPickUp)
    {
        if (standbyPoint == null)
        {
            Debug.LogError("PLAYER PICKUP: StandbyPoint n�o configurado!");
            return;
        }

        if (itemInRangeRenderer != null && originalItemMaterial != null && highlightMaterial != null)
        {
            itemInRangeRenderer.material = originalItemMaterial;
        }
        itemInRange = null;
        itemInRangeRenderer = null;
        originalItemMaterial = null;

        heldItem = itemToPickUp;
        heldItemRb = heldItem.GetComponent<Rigidbody>();
        heldItemCollider = heldItem.GetComponent<Collider>();
        heldItemInfo = heldItem.GetComponent<CollectibleItemInfo>();

        if (heldItemInfo == null)
        {
            Debug.LogWarning($"Item {heldItem.name} n�o possui o script CollectibleItemInfo. Assumindo Melee por padr�o se equipado na m�o.", heldItem);
        }

        if (heldItemRb != null)
        {
            heldItemRb.isKinematic = true;
            heldItemRb.detectCollisions = false;
        }
        if (heldItemCollider != null)
        {
            heldItemCollider.enabled = false;
        }

        MoveItemToPoint(standbyPoint); // Ir� para standby e aplicar� a rota��o de standby
        isItemInHand = false;

        Debug.Log($"Pegou o item: {heldItem.name}. Posi��o inicial: Standby ({standbyPoint.position})");

        if (playerAttack != null)
        {
            playerAttack.EquipWeapon(PlayerAttack.WeaponAnimType.Unarmed);
        }
    }

    void DropItem()
    {
        if (heldItem == null) return;
        Debug.Log($"Soltou o item: {heldItem.name} de volta ao mundo.");
        heldItem.transform.SetParent(null);

        // <<< IMPORTANTE: Resetar a rota��o para algo neutro antes de soltar, se necess�rio >>>
        // Se a rota��o customizada afetar como ele cai, voc� pode querer reset�-la aqui:
        // heldItem.transform.rotation = Quaternion.identity; // Ou alguma rota��o padr�o para o item solto

        if (heldItemRb != null)
        {
            heldItemRb.isKinematic = false;
            heldItemRb.detectCollisions = true;
            Vector3 forceDirection = transform.forward;
            heldItemRb.AddForce(forceDirection * dropForwardForce, ForceMode.Impulse);
            heldItemRb.AddForce(Vector3.up * dropUpwardForce, ForceMode.Impulse);
        }
        if (heldItemCollider != null)
        {
            heldItemCollider.enabled = true;
        }

        heldItem = null;
        heldItemRb = null;
        heldItemCollider = null;
        heldItemInfo = null;
        isItemInHand = false;

        if (playerAttack != null)
        {
            playerAttack.EquipWeapon(PlayerAttack.WeaponAnimType.Unarmed);
        }
    }

    void ToggleHolster()
    {
        if (heldItem == null) return;

        if (!isItemInHand)
        {
            if (handPoint != null)
            {
                MoveItemToPoint(handPoint); // Aplicar� a rota��o da m�o
                isItemInHand = true;
                Debug.Log($"Item {heldItem.name} movido para M�o ({handPoint.position})");
                if (playerAttack != null)
                {
                    PlayerAttack.WeaponAnimType typeToEquip = PlayerAttack.WeaponAnimType.Melee;
                    if (heldItemInfo != null)
                    {
                        typeToEquip = heldItemInfo.itemWeaponType;
                    }
                    playerAttack.EquipWeapon(typeToEquip);
                    Debug.Log($"PlayerAttack definido para: {typeToEquip}");
                }
            }
            else Debug.LogWarning("HandPoint n�o configurado.");
        }
        else
        {
            if (standbyPoint != null)
            {
                MoveItemToPoint(standbyPoint); // Aplicar� a rota��o de standby
                isItemInHand = false;
                Debug.Log($"Item {heldItem.name} movido para Standby ({standbyPoint.position})");
                if (playerAttack != null)
                {
                    playerAttack.EquipWeapon(PlayerAttack.WeaponAnimType.Unarmed);
                    Debug.Log("PlayerAttack definido para: Unarmed");
                }
            }
            else Debug.LogWarning("StandbyPoint n�o configurado.");
        }
    }

    // MODIFICADO AQUI
    void MoveItemToPoint(Transform targetPoint)
    {
        if (heldItem == null || targetPoint == null) return;

        heldItem.transform.SetParent(targetPoint);
        heldItem.transform.localPosition = Vector3.zero; // Posi��o local sempre zerada em rela��o ao ponto de encaixe

        // Aplicar rota��o local espec�fica baseada no ponto de encaixe
        if (targetPoint == handPoint)
        {
            heldItem.transform.localRotation = Quaternion.Euler(handItemLocalRotationEuler);
            //Debug.Log($"Aplicando rota��o da m�o: {handItemLocalRotationEuler}");
        }
        else if (targetPoint == standbyPoint)
        {
            heldItem.transform.localRotation = Quaternion.Euler(standbyItemLocalRotationEuler);
            //Debug.Log($"Aplicando rota��o de standby: {standbyItemLocalRotationEuler}");
        }
        else
        {
            // Caso de fallback, se houver outros pontos no futuro
            heldItem.transform.localRotation = Quaternion.identity;
            Debug.LogWarning("MoveItemToPoint chamado com um targetPoint n�o reconhecido para rota��o customizada. Usando rota��o identity.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (heldItem == null && other.CompareTag("Collectible"))
        {
            if (itemInRange == null)
            {
                if (other.GetComponent<CollectibleItemInfo>() == null)
                {
                    Debug.LogWarning($"O item colet�vel '{other.name}' n�o tem o componente CollectibleItemInfo.", other.gameObject);
                }
                itemInRange = other.gameObject;
                //Debug.Log($"Item ao alcance: {itemInRange.name}");
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
            //Debug.Log($"Item saiu do alcance: {itemInRange.name}");
            if (itemInRangeRenderer != null && originalItemMaterial != null && highlightMaterial != null)
            {
                itemInRangeRenderer.material = originalItemMaterial;
            }
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
            Gizmos.DrawLine(transform.position, handPoint.position);
            // Visualizar a rota��o da m�o
            if (Application.isPlaying && heldItem != null && isItemInHand) { } // S� desenha se estiver segurando e na m�o
            else { Gizmos.matrix = Matrix4x4.TRS(handPoint.position, handPoint.rotation * Quaternion.Euler(handItemLocalRotationEuler), handPoint.lossyScale); Gizmos.DrawFrustum(Vector3.zero, 30, 0.3f, 0.01f, 1f); }

        }
        if (standbyPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(standbyPoint.position, 0.1f);
            Gizmos.DrawLine(transform.position, standbyPoint.position);
            // Visualizar a rota��o do standby
            if (Application.isPlaying && heldItem != null && !isItemInHand) { } // S� desenha se estiver segurando e no standby
            else { Gizmos.matrix = Matrix4x4.TRS(standbyPoint.position, standbyPoint.rotation * Quaternion.Euler(standbyItemLocalRotationEuler), standbyPoint.lossyScale); Gizmos.DrawFrustum(Vector3.zero, 30, 0.3f, 0.01f, 1f); }
        }
        Gizmos.matrix = Matrix4x4.identity; // Resetar matrix do Gizmo

        Gizmos.color = Color.red;
        Vector3 forcePreviewOrigin = transform.position + Vector3.up * 0.5f;
        Vector3 forceDirection = transform.forward * dropForwardForce;
        Gizmos.DrawRay(forcePreviewOrigin, forceDirection.normalized * 2f);
    }
}