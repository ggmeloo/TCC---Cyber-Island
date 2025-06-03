// Crie este novo script, por exemplo, "EnemyAttackSMB.cs"
using UnityEngine;

public class EnemyAttackSMB : StateMachineBehaviour
{
    private EnemyAttack attackScript;

    // OnStateEnter � chamado quando uma transi��o come�a e a m�quina de estados come�a a avaliar este estado
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (attackScript == null)
        {
            // Tenta pegar no pai, pois o Animator pode estar em um objeto filho do objeto principal do inimigo
            attackScript = animator.GetComponentInParent<EnemyAttack>();
        }
        // attackScript?.SetIsCurrentlyAttacking(true); // Voc� j� faz isso no in�cio da corrotina
    }

    // OnStateExit � chamado quando uma transi��o termina e a m�quina de estados para de avaliar este estado
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (attackScript == null)
        {
            attackScript = animator.GetComponentInParent<EnemyAttack>();
        }
        attackScript?.ResetIsAttackingFlag();
    }
}