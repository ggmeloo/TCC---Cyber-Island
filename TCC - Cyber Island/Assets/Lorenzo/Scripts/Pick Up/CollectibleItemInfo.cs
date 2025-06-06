using UnityEngine;

public class CollectibleItemInfo : MonoBehaviour
{
    [Header("Item Identification")] // Identificação do Item
    public SpecificItemType itemIdentifier = SpecificItemType.None; // Use o novo enum

    [Header("For Equippable Items (Used by PlayerPickup)")] // Para Itens Equipáveis (Usado por PlayerPickup)
    // Se verdadeiro, PlayerPickup irá gerenciá-lo com handPoint/standbyPoint.
    // Se falso, ele tentará ir para um slot do PlayerInventoryDisplay.
    public bool isDirectlyEquippable = false;
    public PlayerAttack.WeaponAnimType itemWeaponType = PlayerAttack.WeaponAnimType.Melee; // Relevante se isDirectlyEquippable for verdadeiro

    // Você pode adicionar outros dados específicos do item aqui, se necessário no futuro
    // public int value;
    // public string description;
}