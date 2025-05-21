using UnityEngine;

public class NPCAiWave : MonoBehaviour
{
    public Transform playerTransform; // Arraste o GameObject do Jogador aqui no Inspector
    public float detectionRadius = 5f; // Dist�ncia para o NPC come�ar a acenar
    public string waveTriggerName = "StartWaving"; // Nome do trigger no Animator

    private Animator npcAnimator;
    private bool hasWavedThisEncounter = false; // Para garantir que acene apenas uma vez por encontro

    void Start()
    {
        npcAnimator = GetComponent<Animator>();

        if (playerTransform == null)
        {
            // Tenta encontrar o jogador pela tag "Player" se n�o for atribu�do
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
            else
            {
                Debug.LogError("Player Transform n�o atribu�do ao NPCAiWave e n�o encontrado pela tag 'Player'.");
                enabled = false; // Desabilita o script se n�o houver jogador
                return;
            }
        }

        if (npcAnimator == null)
        {
            Debug.LogError("Animator n�o encontrado no NPC. O script NPCAiWave n�o funcionar�.");
            enabled = false; // Desabilita o script se n�o houver Animator
        }
    }

    void Update()
    {
        if (playerTransform == null || npcAnimator == null)
            return;

        // Calcula a dist�ncia entre o NPC e o Jogador
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= detectionRadius)
        {
            // Se o jogador est� perto E o NPC ainda n�o acenou nesta "aproxima��o"
            if (!hasWavedThisEncounter)
            {
                npcAnimator.SetTrigger(waveTriggerName);
                hasWavedThisEncounter = true; // Marca que j� acenou
                Debug.Log("NPC: Ol�, jogador!");
            }
        }
        else
        {
            // Se o jogador se afasta, reseta a flag para que o NPC possa acenar novamente na pr�xima aproxima��o
            if (hasWavedThisEncounter)
            {
                hasWavedThisEncounter = false;
                Debug.Log("NPC: Jogador se afastou, posso acenar de novo.");
            }
        }
    }

    // Opcional: Desenhar o raio de detec��o no Editor para visualiza��o
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}