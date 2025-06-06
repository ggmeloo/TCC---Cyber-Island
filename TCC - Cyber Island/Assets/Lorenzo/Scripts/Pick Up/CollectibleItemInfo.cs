using UnityEngine;

public class CollectibleItemInfo : MonoBehaviour
{
    [Header("Item Identification")] // Identifica��o do Item
    public SpecificItemType itemIdentifier = SpecificItemType.None; // Use o novo enum

    [Header("For Equippable Items (Used by PlayerPickup)")] // Para Itens Equip�veis (Usado por PlayerPickup)
    // Se verdadeiro, PlayerPickup ir� gerenci�-lo com handPoint/standbyPoint.
    // Se falso, ele tentar� ir para um slot do PlayerInventoryDisplay.
    public bool isDirectlyEquippable = false;
    public PlayerAttack.WeaponAnimType itemWeaponType = PlayerAttack.WeaponAnimType.Melee; // Relevante se isDirectlyEquippable for verdadeiro

    // Voc� pode adicionar outros dados espec�ficos do item aqui, se necess�rio no futuro
    // public int value;
    // public string description;
}