using UnityEngine;

// Usando o mesmo enum do PlayerAttack para consistência
// Se PlayerAttack.WeaponAnimType não estiver acessível diretamente (por exemplo, se estiver em outro namespace
// ou assembly sem referência), você pode duplicar o enum aqui ou torná-lo público e acessível.
// Assumindo que PlayerAttack está no escopo global ou acessível:

public class CollectibleItemInfo : MonoBehaviour
{
    public PlayerAttack.WeaponAnimType itemWeaponType = PlayerAttack.WeaponAnimType.Melee; // Padrão para Melee
    // Você pode adicionar outras informações específicas do item aqui, como:
    // public int itemDamage = 10;
    // public float itemAttackSpeed = 1.0f;
}