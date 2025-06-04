using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string itemName = "New Item";
    public Sprite icon = null;
    public int maxStack = 1;

    // M�todo virtual que pode ser sobrescrito por tipos espec�ficos de itens
    public virtual void Use()
    {
        Debug.Log("Usando " + itemName);
    }
}