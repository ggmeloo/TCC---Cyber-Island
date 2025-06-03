// Crie este novo script, por exemplo, "EnemyAttackSMB.cs"
using UnityEngine;

public class EnemyAttackSMB : StateMachineBehaviour
{
    private EnemyAttack attackScript;

    // OnStateEnter é chamado quando uma transição começa e a máquina de estados começa a avaliar este estado
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (attackScript == null)
        {
            // Tenta pegar no pai, pois o Animator pode estar em um objeto filho do objeto principal do inimigo
            attackScript = animator.GetComponentInParent<EnemyAttack>();
        }
        // attackScript?.SetIsCurrentlyAttacking(true); // Você já faz isso no início da corrotina
    }

    // OnStateExit é chamado quando uma transição termina e a máquina de estados para de avaliar este estado
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (attackScript == null)
        {
            attackScript = animator.GetComponentInParent<EnemyAttack>();
        }
        attackScript?.ResetIsAttackingFlag();
    }
}