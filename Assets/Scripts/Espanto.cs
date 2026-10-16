using UnityEngine;

/// <summary>
/// Enemigo genérico. Con este MISMO script se hacen los 4 tipos (fantasma, murciélago,
/// calabaza y calabaza chica); solo cambian los valores en el Inspector.
///
/// Comportamiento:
///  1. Acercándose: camina hacia la ofrenda (como tu Asteroid iba hacia el centro).
///  2. Al tocar la ofrenda se roba un objeto y pasa a Huyendo.
///  3. Huyendo: se aleja del altar. Si sale de la pantalla, el objeto se pierde.
///     Si lo destruyes antes, el objeto regresa al altar (+bonus).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Espanto : MonoBehaviour
{
    private enum Estado { Acercandose, Huyendo }

    [Header("Tipo y estadísticas")]
    public TipoEspanto tipo = TipoEspanto.Fantasma;
    public int vida = 1;
    public float velocidad = 1.5f;
    public int puntos = 100;
    public int petalosQueSuelta = 2;

    [Header("Zigzag (murciélago)")]
    public bool zigzag = false;
    public float amplitudZigzag = 2f;
    public float frecuenciaZigzag = 4f;

    [Header("División al morir (calabaza grande)")]
    [Tooltip("Prefab que aparece al morir. Déjalo vacío si no se divide.")]
    public GameObject prefabDivision;
    public int cantidadDivision = 2;

    [Header("Robo")]
    [Tooltip("Hijo que se enciende cuando lleva un objeto robado")]
    public GameObject indicadorObjetoRobado;
    public float multiplicadorHuida = 1.3f;
    public int bonusRescate = 250;
    [Tooltip("Si huyendo pasa estos límites, escapa con el objeto")]
    public float limiteX = 11f;
    public float limiteY = 7f;

    private Rigidbody2D rb;
    private Ofrenda ofrenda;
    private Estado estado = Estado.Acercandose;
    private int objetoRobado = -1;
    private float tiempoVivo;
    private float faseZigzag;
    private bool muerto = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
    }

    void Start()
    {
        ofrenda = GameManager.Instance.ofrenda;
        faseZigzag = Random.Range(0f, 10f); // para que no todos zigzagueen igual
        if (indicadorObjetoRobado != null) indicadorObjetoRobado.SetActive(false);
    }

    void FixedUpdate()
    {
        tiempoVivo += Time.fixedDeltaTime;

        float vel = velocidad * GameManager.Instance.MultiplicadorDificultad;
        Vector2 haciaAltar = ((Vector2)ofrenda.transform.position - rb.position).normalized;
        Vector2 direccion;

        if (estado == Estado.Acercandose)
        {
            direccion = haciaAltar;
        }
        else
        {
            direccion = -haciaAltar;
            vel *= multiplicadorHuida;
        }

        Vector2 movimiento = direccion * vel;

        if (zigzag)
        {
            // Perpendicular a la dirección: se mueve de lado a lado con una onda seno
            Vector2 perpendicular = new Vector2(-direccion.y, direccion.x);
            movimiento += perpendicular *
                          Mathf.Sin((tiempoVivo + faseZigzag) * frecuenciaZigzag) *
                          amplitudZigzag;
        }

        rb.linearVelocity = movimiento;

        if (estado == Estado.Huyendo &&
            (Mathf.Abs(rb.position.x) > limiteX || Mathf.Abs(rb.position.y) > limiteY))
        {
            Escapar();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (muerto || estado != Estado.Acercandose) return;

        // En lugar de Tags, preguntamos si lo que tocamos tiene el script Ofrenda
        if (other.TryGetComponent(out Ofrenda altar))
        {
            int indice = altar.RobarObjeto();

            if (indice < 0)
            {
                Destroy(gameObject); // ya no había nada que robar
                return;
            }

            objetoRobado = indice;
            estado = Estado.Huyendo;
            if (indicadorObjetoRobado != null) indicadorObjetoRobado.SetActive(true);

            GameManager.Instance.ObjetoRobado();
        }
    }

    public void RecibirDanio(int danio)
    {
        if (muerto) return;

        vida -= danio;
        // NIVEL 3: destello blanco/rojo al recibir daño

        if (vida <= 0)
        {
            Morir(false);
        }
    }

    /// <param name="porLluvia">true si murió por la Lluvia de pétalos (no se divide ni suelta pétalos)</param>
    public void Morir(bool porLluvia)
    {
        if (muerto) return;
        muerto = true;

        GameManager.Instance.RegistrarEspantoAhuyentado(tipo, puntos, porLluvia ? 0 : petalosQueSuelta);

        if (objetoRobado >= 0)
        {
            ofrenda.DevolverObjeto(objetoRobado);
            GameManager.Instance.RegistrarRescate(bonusRescate);
        }

        if (!porLluvia && prefabDivision != null)
        {
            for (int i = 0; i < cantidadDivision; i++)
            {
                Vector2 posicion = (Vector2)transform.position + Random.insideUnitCircle * 0.5f;
                Instantiate(prefabDivision, posicion, Quaternion.identity);
            }
        }

        // NIVEL 3: explosión / humo / pétalos + sonido
        Destroy(gameObject);
    }

    void Escapar()
    {
        Debug.Log(tipo + " escapó con un objeto de la ofrenda");
        // NIVEL 3: risa malvada :)
        Destroy(gameObject);
    }
}
