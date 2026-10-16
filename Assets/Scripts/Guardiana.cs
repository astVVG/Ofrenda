using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// La jugadora. Controles estilo Asteroids + mouse:
///   A / D o ← / →  : girar
///   W o ↑           : impulso (se mueve con inercia, como la nave)
///   Espacio          : disparar
///   Mouse            : apuntar moviendo el mouse, disparar con clic izquierdo
///   Q                : Lluvia de pétalos (si el medidor está lleno)
/// Si giras con teclado, el teclado manda; si mueves el mouse, apunta al mouse.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Guardiana : MonoBehaviour
{
    public enum ModoControl { TecladoYMouse, SoloTeclado, SoloMouse }

    [Header("Controles")]
    public ModoControl modoControl = ModoControl.TecladoYMouse;
    [Tooltip("Cuántos píxeles debe moverse el mouse para que tome el control de la puntería")]
    public float umbralMovimientoMouse = 2f;

    [Header("Movimiento (como Asteroids)")]
    public float velocidadRotacion = 200f;
    public float fuerzaImpulso = 6f;
    public float velocidadMaxima = 6f;
    [Tooltip("Límites para que no salga de la pantalla")]
    public float limiteX = 8.4f;
    public float limiteY = 4.6f;

    [Header("Disparo")]
    public GameObject prefabLlama;
    public Transform puntoDisparo;
    public float tiempoEntreDisparos = 0.25f;

    private Rigidbody2D rb;
    private Camera camara;
    private float siguienteDisparo = 0f;

    // Entrada leída en Update y usada en FixedUpdate
    private float entradaRotacion;
    private bool entradaImpulso;
    private bool apuntandoConMouse;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        camara = Camera.main;
    }

    void Update()
    {
        if (!GameManager.Instance.Jugando) return;

        LeerEntrada();

        bool disparoTeclado = modoControl != ModoControl.SoloMouse &&
                              Keyboard.current.spaceKey.isPressed;
        bool disparoMouse = modoControl != ModoControl.SoloTeclado &&
                            Mouse.current != null && Mouse.current.leftButton.isPressed;

        if ((disparoTeclado || disparoMouse) && Time.time >= siguienteDisparo)
        {
            Disparar();
            siguienteDisparo = Time.time + tiempoEntreDisparos;
        }

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            LanzarLluvia();
        }
    }

    void LeerEntrada()
    {
        Keyboard teclado = Keyboard.current;

        entradaRotacion = 0f;
        entradaImpulso = false;

        if (modoControl != ModoControl.SoloMouse)
        {
            if (teclado.aKey.isPressed || teclado.leftArrowKey.isPressed) entradaRotacion = 1f;
            if (teclado.dKey.isPressed || teclado.rightArrowKey.isPressed) entradaRotacion = -1f;
            entradaImpulso = teclado.wKey.isPressed || teclado.upArrowKey.isPressed;
        }

        if (modoControl == ModoControl.SoloMouse)
        {
            apuntandoConMouse = true;
        }
        else if (entradaRotacion != 0f)
        {
            apuntandoConMouse = false; // el teclado tiene prioridad mientras giras
        }
        else if (modoControl == ModoControl.TecladoYMouse && Mouse.current != null)
        {
            // Si mueves el mouse o haces clic, vuelve a apuntar con el mouse
            if (Mouse.current.delta.ReadValue().magnitude > umbralMovimientoMouse ||
                Mouse.current.leftButton.wasPressedThisFrame)
            {
                apuntandoConMouse = true;
            }
        }
    }

    void FixedUpdate()
    {
        if (!GameManager.Instance.Jugando) return;

        // ---- Rotación ----
        if (apuntandoConMouse && Mouse.current != null)
        {
            Vector2 mousePantalla = Mouse.current.position.ReadValue();
            Vector2 mouseMundo = camara.ScreenToWorldPoint(mousePantalla);
            Vector2 direccion = mouseMundo - rb.position;

            // Atan2 da el ángulo; restamos 90 porque el sprite "mira" hacia arriba (transform.up)
            float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg - 90f;
            rb.MoveRotation(angulo);
        }
        else
        {
            rb.MoveRotation(rb.rotation + entradaRotacion * velocidadRotacion * Time.fixedDeltaTime);
        }

        // ---- Impulso (igual que tu nave) ----
        if (entradaImpulso)
        {
            rb.AddForce(transform.up * fuerzaImpulso);
            // NIVEL 3: partículas de propulsión (velitas / chispas)
        }

        if (rb.linearVelocity.magnitude > velocidadMaxima)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * velocidadMaxima;
        }

        // ---- No salir de la pantalla (en vez de ScreenWrap, porque hay que cuidar el altar) ----
        Vector2 pos = rb.position;
        Vector2 vel = rb.linearVelocity;

        if (Mathf.Abs(pos.x) > limiteX)
        {
            pos.x = Mathf.Sign(pos.x) * limiteX;
            vel.x = 0f;
        }
        if (Mathf.Abs(pos.y) > limiteY)
        {
            pos.y = Mathf.Sign(pos.y) * limiteY;
            vel.y = 0f;
        }

        rb.position = pos;
        rb.linearVelocity = vel;
    }

    void Disparar()
    {
        Instantiate(prefabLlama, puntoDisparo.position, puntoDisparo.rotation);
        GameManager.Instance.RegistrarDisparo();
        // NIVEL 2: sonido de disparo | NIVEL 3: destello en la boca
    }

    void LanzarLluvia()
    {
        if (!GameManager.Instance.UsarLluvia())
        {
            Debug.Log("Aún no hay suficientes pétalos");
            return;
        }

        Espanto[] espantos = FindObjectsByType<Espanto>(FindObjectsSortMode.None);
        foreach (Espanto espanto in espantos)
        {
            espanto.Morir(true);
        }

        Debug.Log("¡Lluvia de pétalos! Ahuyentados: " + espantos.Length);
        // NIVEL 3: partículas de cempasúchil por toda la pantalla
    }
}