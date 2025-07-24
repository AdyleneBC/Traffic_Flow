using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class CaminoAbridor : MonoBehaviour
{
    public TMP_InputField commandInputField;
    public TextMeshProUGUI statusText;
    public List<Nodo> nodos;

    private void Awake()
    {
        nodos = new List<Nodo>(FindObjectsByType<Nodo>(FindObjectsSortMode.None));

    }

    public void AbrirCaminoDesdeTexto()
    {
        string entrada = commandInputField.text.Trim().ToLower();
        commandInputField.text = "";

        if (string.IsNullOrEmpty(entrada))
        {
            Mostrar("Entrada vacía.");
            return;
        }

        string[] partes = entrada.Split(' ');

        if (partes.Length != 2)
        {
            Mostrar("Debe ingresar exactamente dos IDs de nodos. Ejemplo: nodo1 nodo2");
            return;
        }

        string id1 = partes[0];
        string id2 = partes[1];

        Nodo nodo1 = nodos.Find(n => n.id.ToLower() == id1);
        Nodo nodo2 = nodos.Find(n => n.id.ToLower() == id2);

        if (nodo1 == null || nodo2 == null)
        {
            Mostrar($"Nodo(s) no encontrado(s): {id1}, {id2}");
            return;
        }

        bool cerrado = AbrirCamino(nodo1, nodo2);

        if (!cerrado)
        {
            Mostrar($"Camino abierto entre {id1} y {id2}");
        }
        else
        {
            Mostrar($"No se encontró un camino cerrado entre {id1} y {id2}");
        }
    }

    private bool AbrirCamino(Nodo a, Nodo b)
    {
        bool seCerro = true;

        foreach (var camino in a.conexiones)
        {
            if (camino.destino == b && camino.cerrado)
            {
                camino.cerrado = false;
                seCerro = false;
            }
        }

        foreach (var camino in b.conexiones)
        {
            if (camino.destino == a && camino.cerrado)
            {
                camino.cerrado = false;
            }
        }

        return seCerro;
    }

    private void Mostrar(string msg)
    {
        Debug.Log(msg);
        if (statusText != null)
            statusText.text = msg;
    }
}
