using UnityEngine;

public class Player : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Rigidbody rb;
    public float velocidade;
    public float x;
    public float z;
    public float y;
    public float forcaPulo;
    public bool noChao;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        x = Input.GetAxis("Horizontal"); // aqui ele representa para um lado e para o outro lado
        z = Input.GetAxis("Vertical"); //axis é as setas pra cima e para baixo
        y = Input.GetAxis("Jump");//jump representa a tecla espaço
    }
    private void FixedUpdate()
    {
        //vai manter a velocidade y do corpo, a que ele já tem(rb.linearVelocity.y).
        rb.linearVelocity = new Vector3(x * velocidade, rb.linearVelocity.y,z * velocidade);
        if(noChao == true && y != 0)
        {
            rb.AddForce(new Vector3(0, forcaPulo, 0));
            noChao = false;
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Chão"))
        {
            noChao = true;
        }
    }
}
