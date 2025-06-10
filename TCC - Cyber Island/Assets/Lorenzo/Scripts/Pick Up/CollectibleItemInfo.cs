using UnityEngine;

public class CollectibleItemInfo : MonoBehaviour
{
    [Header("Item Identification")]
    public SpecificItemType itemIdentifier = SpecificItemType.None;

    [Header("For Equippable Items (Used by PlayerPickup)")]
    public bool isDirectlyEquippable = false;
    public PlayerAttack.WeaponAnimType itemWeaponType = PlayerAttack.WeaponAnimType.Melee;

    [Header("Visuals for UI / Hotbar")]
    public Sprite itemIcon = null;

    [Header("World Representation")]
    // Refer�ncia ao Prefab deste pr�prio item.
    // Essencial para que o sistema de invent�rio saiba qual objeto 3D criar no mundo ao "dropar" o item.
    // Arraste o prefab correspondente da sua pasta de Assets para este campo.
    public GameObject itemPrefab;
}