using UnityEngine;

/// <summary>
/// Proyectil de la guardiana. Es casi tu Bullet.cs de Asteroids, pero
/// en vez de destruir al enemigo directamente le hace daño (las calabazas aguantan 3).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class LlamaVela : MonoBehaviour
{
    public float velocidad = 12f;
    public float tiempoVida = 2f;
    public int danio = 1;

    private bool usada = false; // evita pegarle a 2 espantos en el mismo frame

    void Start()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearVelocity = transform.up * velocidad;

        Destroy(gameObject, tiempoVida);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (usada) return;

        if (other.TryGetComponent(out Espanto espanto))
        {
            usada = true;
            GameManager.Instance.RegistrarAcierto();
            espanto.RecibirDanio(danio);
            // NIVEL 3: chispa de impacto
            Destroy(gameObject);
        }
    }
}
