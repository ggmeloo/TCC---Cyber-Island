using UnityEngine;

public class NPCAiWave : MonoBehaviour
{
    public Transform playerTransform; // Arraste o GameObject do Jogador aqui no Inspector
    public float detectionRadius = 5f; // Distância para o NPC começar a acenar
    public string waveTriggerName = "StartWaving"; // Nome do trigger no Animator

    private Animator npcAnimator;
    private bool hasWavedThisEncounter = false; // Para garantir que acene apenas uma vez por encontro

    void Start()
    {
        npcAnimator = GetComponent<Animator>();

        if (playerTransform == null)
        {
            // Tenta encontrar o jogador pela tag "Player" se não for atribuído
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
            else
            {
                Debug.LogError("Player Transform não atribuído ao NPCAiWave e não encontrado pela tag 'Player'.");
                enabled = false; // Desabilita o script se não houver jogador
                return;
            }
        }

        if (npcAnimator == null)
        {
            Debug.LogError("Animator não encontrado no NPC. O script NPCAiWave não funcionará.");
            enabled = false; // Desabilita o script se não houver Animator
        }
    }

    void Update()
    {
        if (playerTransform == null || npcAnimator == null)
            return;

        // Calcula a distância entre o NPC e o Jogador
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= detectionRadius)
        {
            // Se o jogador está perto E o NPC ainda não acenou nesta "aproximação"
            if (!hasWavedThisEncounter)
            {
                npcAnimator.SetTrigger(waveTriggerName);
                hasWavedThisEncounter = true; // Marca que já acenou
                Debug.Log("NPC: Olá, jogador!");
            }
        }
        else
        {
            // Se o jogador se afasta, reseta a flag para que o NPC possa acenar novamente na próxima aproximação
            if (hasWavedThisEncounter)
            {
                hasWavedThisEncounter = false;
                Debug.Log("NPC: Jogador se afastou, posso acenar de novo.");
            }
        }
    }

    // Opcional: Desenhar o raio de detecção no Editor para visualização
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}