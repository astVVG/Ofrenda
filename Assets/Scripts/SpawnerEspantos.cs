using UnityEngine;

/// <summary>
/// Una fila de la lista del spawner: qué espanto, desde qué segundo aparece y qué tan seguido.
/// </summary>
[System.Serializable]
public class EntradaSpawn
{
    public string nombre;
    public GameObject prefab;
    [Tooltip("Segundo de juego a partir del cual puede aparecer")]
    public float desbloqueaEnSegundo = 0f;
    [Tooltip("Mientras más alto, más seguido aparece comparado con los demás")]
    [Min(1)] public int peso = 1;
}

/// <summary>
/// Tu AsteroidSpawner, mejorado: sigue acelerando con el tiempo (dificultad acumulable)
/// y además va desbloqueando tipos de espanto conforme pasan los segundos.
/// </summary>
public class SpawnerEspantos : MonoBehaviour
{
    public EntradaSpawn[] espantos;

    [Header("Ritmo")]
    public float intervaloInicial = 2f;
    public float intervaloMinimo = 0.4f;
    public float reduccionIntervalo = 0.03f;

    [Header("Bordes (un poco fuera de cámara)")]
    public float limiteX = 10f;
    public float limiteY = 6f;

    private float intervalo;
    private float siguienteEspanto;
    private bool[] anunciado;

    void Start()
    {
        intervalo = intervaloInicial;
        siguienteEspanto = Time.time + 1f;
        anunciado = new bool[espantos.Length];
    }

    void Update()
    {
        if (!GameManager.Instance.Jugando) return;

        AnunciarDesbloqueos();

        if (Time.time >= siguienteEspanto)
        {
            CrearEspanto();
            siguienteEspanto = Time.time + intervalo;
            intervalo = Mathf.Max(intervaloMinimo, intervalo - reduccionIntervalo);
        }
    }

    void CrearEspanto()
    {
        GameObject prefab = ElegirPrefab();
        if (prefab == null) return;

        Instantiate(prefab, PosicionEnBorde(), Quaternion.identity);
    }

    /// <summary>Sorteo con pesos entre los espantos ya desbloqueados.</summary>
    GameObject ElegirPrefab()
    {
        float tiempo = GameManager.Instance.TiempoJugado;

        int pesoTotal = 0;
        foreach (EntradaSpawn e in espantos)
        {
            if (tiempo >= e.desbloqueaEnSegundo) pesoTotal += e.peso;
        }
        if (pesoTotal == 0) return null;

        int sorteo = Random.Range(0, pesoTotal);
        foreach (EntradaSpawn e in espantos)
        {
            if (tiempo < e.desbloqueaEnSegundo) continue;
            sorteo -= e.peso;
            if (sorteo < 0) return e.prefab;
        }
        return null;
    }

    Vector2 PosicionEnBorde()
    {
        switch (Random.Range(0, 4))
        {
            case 0: return new Vector2(-limiteX, Random.Range(-limiteY, limiteY)); // izquierda
            case 1: return new Vector2(limiteX, Random.Range(-limiteY, limiteY));  // derecha
            case 2: return new Vector2(Random.Range(-limiteX, limiteX), limiteY);  // arriba
            default: return new Vector2(Random.Range(-limiteX, limiteX), -limiteY); // abajo
        }
    }

    void AnunciarDesbloqueos()
    {
        float tiempo = GameManager.Instance.TiempoJugado;
        for (int i = 0; i < espantos.Length; i++)
        {
            if (!anunciado[i] && tiempo >= espantos[i].desbloqueaEnSegundo)
            {
                anunciado[i] = true;
                Debug.Log("Nuevo espanto desbloqueado: " + espantos[i].nombre);
                // NIVEL 4: aviso en pantalla "¡Llegaron los murciélagos!"
            }
        }
    }
}