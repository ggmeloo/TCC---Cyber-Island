using UnityEngine;

// Usando o mesmo enum do PlayerAttack para consist�ncia
// Se PlayerAttack.WeaponAnimType n�o estiver acess�vel diretamente (por exemplo, se estiver em outro namespace
// ou assembly sem refer�ncia), voc� pode duplicar o enum aqui ou torn�-lo p�blico e acess�vel.
// Assumindo que PlayerAttack est� no escopo global ou acess�vel:

public class CollectibleItemInfo : MonoBehaviour
{
    public PlayerAttack.WeaponAnimType itemWeaponType = PlayerAttack.WeaponAnimType.Melee; // Padr�o para Melee
    // Voc� pode adicionar outras informa��es espec�ficas do item aqui, como:
    // public int itemDamage = 10;
    // public float itemAttackSpeed = 1.0f;
}