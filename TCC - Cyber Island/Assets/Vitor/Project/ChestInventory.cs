using UnityEngine;
using UnityEngine.UI;

public class ChestInventory : MonoBehaviour
{
    public Transform itemsParent;  // Refer�ncia ao ChestPanel
    public GameObject chestUI;     // Refer�ncia ao objeto UI completo

    void Start()
    {


        // Inicialmente esconde o ba�
        chestUI.SetActive(false);
    }

}