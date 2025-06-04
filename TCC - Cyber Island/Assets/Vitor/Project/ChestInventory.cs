using UnityEngine;
using UnityEngine.UI;

public class ChestInventory : MonoBehaviour
{
    public Transform itemsParent;  // Referência ao ChestPanel
    public GameObject chestUI;     // Referência ao objeto UI completo

    void Start()
    {


        // Inicialmente esconde o baú
        chestUI.SetActive(false);
    }

}