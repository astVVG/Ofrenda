using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// El altar. Sus objetos son las "vidas" del juego: cuando un espanto lo toca, se roba uno
/// (el objeto se apaga). Si lo rescatas, vuelve a encenderse.
/// Requiere un Collider2D marcado como "Is Trigger" en este mismo GameObject.
/// </summary>
public class Ofrenda : MonoBehaviour
{
    [Tooltip("Los objetos de la ofrenda (pan, vela, foto, flor, calaverita). Arrástralos aquí.")]
    public GameObject[] objetos;

    public int TotalObjetos => objetos.Length;

    public int ObjetosEnAltar
    {
        get
        {
            int cantidad = 0;
            foreach (GameObject objeto in objetos)
            {
                if (objeto.activeSelf) cantidad++;
            }
            return cantidad;
        }
    }

    /// <summary>
    /// Apaga un objeto al azar de los que siguen en el altar y regresa su índice.
    /// Regresa -1 si ya no queda ninguno.
    /// </summary>
    public int RobarObjeto()
    {
        List<int> disponibles = new List<int>();
        for (int i = 0; i < objetos.Length; i++)
        {
            if (objetos[i].activeSelf) disponibles.Add(i);
        }

        if (disponibles.Count == 0) return -1;

        int indice = disponibles[Random.Range(0, disponibles.Count)];
        objetos[indice].SetActive(false);
        // NIVEL 3: efecto/sonido de "robo"
        return indice;
    }

    public void DevolverObjeto(int indice)
    {
        if (indice < 0 || indice >= objetos.Length) return;
        objetos[indice].SetActive(true);
        // NIVEL 3: destello/sonido de "rescate"
    }
}