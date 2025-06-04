using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string itemName = "New Item";
    public Sprite icon = null;
    public int maxStack = 1;

    // Método virtual que pode ser sobrescrito por tipos específicos de itens
    public virtual void Use()
    {
        Debug.Log("Usando " + itemName);
    }
}