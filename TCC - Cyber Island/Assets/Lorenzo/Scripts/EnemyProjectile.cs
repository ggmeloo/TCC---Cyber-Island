// EnemyProjectile.cs
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    private Transform target;
    private float speed;
    private int damage;
    private LayerMask playerLayerMask;
    private bool initialized = false;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false; // A menos que queira um proj�til bal�stico
            Debug.LogWarning($"Rigidbody adicionado a {gameObject.name}. Configure suas propriedades.");
        }
    }


    public void Initialize(Transform playerTarget, float projSpeed, int projDamage, LayerMask pLayer)
    {
        target = playerTarget;
        speed = projSpeed;
        damage = projDamage;
        playerLayerMask = pLayer;
        initialized = true;

        if (target != null)
        {
            Vector3 direction = (target.position + Vector3.up * 0.5f - transform.position).normalized; // Mira um pouco acima da base
            transform.rotation = Quaternion.LookRotation(direction);
            if (rb != null) rb.linearVelocity = direction * speed;
        }
        else
        {
            if (rb != null) rb.linearVelocity = transform.forward * speed; // Dispara reto se n�o houver alvo
        }
        Destroy(gameObject, 7f); // Autodestrui��o para evitar proj�teis perdidos
    }

    void FixedUpdate() // Use FixedUpdate para movimento baseado em f�sica se usar Rigidbody.velocity
    {
        if (!initialized) return;
        // O movimento j� foi iniciado em Initialize com rb.velocity.
        // Se quisesse um proj�til teleguiado, a l�gica de persegui��o viria aqui.
    }


    void OnTriggerEnter(Collider other)
    {
        HandleCollision(other.gameObject);
    }
    void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision.gameObject);
    }

    void HandleCollision(GameObject collidedObject)
    {
        if (!initialized) return;

        // Verifica se colidiu com algo na layer do jogador
        if (((1 << collidedObject.layer) & playerLayerMask) != 0)
        {
            PlayerHealth playerHealth = collidedObject.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                //Debug.Log($"Proj�til acertou {collidedObject.name}, causando {damage} de dano.");
            }
            Destroy(gameObject); // Destr�i o proj�til ao acertar o jogador
        }
        // Destruir em colis�o com outros objetos (exceto o pr�prio inimigo ou outros proj�teis)
        else if (!collidedObject.CompareTag("Enemy") && !collidedObject.CompareTag("Projectile")) // Adicione tags aos seus inimigos e proj�teis
        {
            //Debug.Log($"Proj�til colidiu com {collidedObject.name} e foi destru�do.");
            Destroy(gameObject);
        }
    }
}