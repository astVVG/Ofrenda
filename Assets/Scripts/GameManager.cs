using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Tipos de espanto. Se usa para contar cuántos de cada uno ahuyentaste (resumen de Game Over).
public enum TipoEspanto { Fantasma, Murcielago, Calabaza, CalabazaChica }

/// <summary>
/// Cerebro del juego: guarda puntos, tiempo, pétalos, estadísticas y decide cuándo es Game Over.
/// NIVEL 1: muestra todo con un HUD de depuración (OnGUI), sin Canvas.
/// </summary>
public class GameManager : MonoBehaviour
{
    // "Singleton": cualquier script puede escribir GameManager.Instance para hablar con él
    public static GameManager Instance { get; private set; }

    [Header("Referencias")]
    public Ofrenda ofrenda;

    [Header("Dificultad acumulable")]
    [Tooltip("Cuánto sube la velocidad de los espantos por segundo jugado. 0.01 = +1% cada segundo")]
    public float aumentoDificultadPorSegundo = 0.01f;
    [Tooltip("Tope del multiplicador de velocidad")]
    public float multiplicadorMaximo = 2f;

    [Header("Lluvia de pétalos")]
    public int petalosNecesarios = 20;

    [Header("Depuración (Nivel 1)")]
    public bool mostrarHUDDebug = true;

    // ---------- Estado de la partida (solo lectura desde fuera) ----------
    public bool Jugando { get; private set; } = true;
    public int Puntos { get; private set; }
    public float TiempoJugado { get; private set; }
    public int Petalos { get; private set; }
    public int ObjetosRescatados { get; private set; }
    public int Disparos { get; private set; }
    public int Aciertos { get; private set; }

    // Un contador por cada tipo de espanto
    private int[] ahuyentadosPorTipo;

    public float MultiplicadorDificultad =>
        Mathf.Min(1f + TiempoJugado * aumentoDificultadPorSegundo, multiplicadorMaximo);

    public bool LluviaLista => Petalos >= petalosNecesarios;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f; // por si venimos de un Game Over
        ahuyentadosPorTipo = new int[System.Enum.GetValues(typeof(TipoEspanto)).Length];
    }

    void Update()
    {
        if (Jugando)
        {
            TiempoJugado += Time.deltaTime;
        }
        else if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            // NIVEL 4: esto lo hará el botón "Reintentar" de la pantalla de Game Over
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    // ---------- Métodos que llaman los demás scripts ----------

    public void RegistrarDisparo() => Disparos++;
    public void RegistrarAcierto() => Aciertos++;

    public void RegistrarEspantoAhuyentado(TipoEspanto tipo, int puntos, int petalos)
    {
        Puntos += puntos;
        ahuyentadosPorTipo[(int)tipo]++;
        Petalos = Mathf.Min(Petalos + petalos, petalosNecesarios);
    }

    public void RegistrarRescate(int bonus)
    {
        ObjetosRescatados++;
        Puntos += bonus;
        Debug.Log("¡Objeto rescatado! +" + bonus);
    }

    /// <summary>Gasta los pétalos si el medidor está lleno. Regresa true si se pudo usar.</summary>
    public bool UsarLluvia()
    {
        if (!LluviaLista) return false;
        Petalos = 0;
        return true;
    }

    /// <summary>Lo llama un Espanto justo después de robar un objeto.</summary>
    public void ObjetoRobado()
    {
        Debug.Log("Robaron un objeto. Quedan " + ofrenda.ObjetosEnAltar);
        if (ofrenda.ObjetosEnAltar <= 0)
        {
            GameOver();
        }
    }

    void GameOver()
    {
        if (!Jugando) return;
        Jugando = false;
        Time.timeScale = 0f;
        Debug.Log("GAME OVER\n" + TextoResumen());
        // NIVEL 4: aquí se activará el panel de Game Over y se guardará el récord
    }

    public int Ahuyentados(TipoEspanto tipo) => ahuyentadosPorTipo[(int)tipo];

    public int TotalAhuyentados()
    {
        int total = 0;
        foreach (int n in ahuyentadosPorTipo) total += n;
        return total;
    }

    public float Precision => Disparos == 0 ? 0f : (float)Aciertos / Disparos;

    public string TextoResumen()
    {
        return
            "Puntos: " + Puntos + "\n" +
            "Tiempo sobrevivido: " + TiempoJugado.ToString("0.0") + " s\n" +
            "Espantos ahuyentados: " + TotalAhuyentados() + "\n" +
            "   Fantasmas: " + Ahuyentados(TipoEspanto.Fantasma) + "\n" +
            "   Murciélagos: " + Ahuyentados(TipoEspanto.Murcielago) + "\n" +
            "   Calabazas: " + Ahuyentados(TipoEspanto.Calabaza) + "\n" +
            "   Calabazas chicas: " + Ahuyentados(TipoEspanto.CalabazaChica) + "\n" +
            "Objetos rescatados: " + ObjetosRescatados + "\n" +
            "Precisión: " + (Precision * 100f).ToString("0") + "%";
    }

    // ---------- HUD de depuración (se borra en el Nivel 4) ----------
    void OnGUI()
    {
        if (!mostrarHUDDebug || ofrenda == null) return;

        GUIStyle estilo = new GUIStyle(GUI.skin.label) { fontSize = 20 };
        estilo.normal.textColor = Color.white;

        string hud =
            "Puntos: " + Puntos + "\n" +
            "Tiempo: " + TiempoJugado.ToString("0.0") + " s\n" +
            "Ofrenda: " + ofrenda.ObjetosEnAltar + " / " + ofrenda.TotalObjetos + "\n" +
            "Pétalos: " + Petalos + " / " + petalosNecesarios +
                (LluviaLista ? "   ¡Presiona Q!" : "") + "\n" +
            "Dificultad: x" + MultiplicadorDificultad.ToString("0.00");

        GUI.Label(new Rect(15, 10, 600, 200), hud, estilo);

        if (!Jugando)
        {
            GUIStyle centro = new GUIStyle(estilo) { alignment = TextAnchor.MiddleCenter, fontSize = 22 };
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height),
                "GAME OVER\n\n" + TextoResumen() + "\n\nPresiona R para reiniciar", centro);
        }
    }
}