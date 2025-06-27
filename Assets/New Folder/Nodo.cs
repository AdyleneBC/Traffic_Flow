using System.Collections.Generic;
using UnityEngine;

public class Nodo : MonoBehaviour
{
    public string id;
    public List<Camino> conexiones = new List<Camino>();

    public Nodo ObtenerVecino(Vector3 direccion)
    {
        Nodo mejor = null;
        float mejorPuntaje = 0f;

        foreach (var c in conexiones)
        {
            if (c.cerrado) continue;

            Vector3 dir = (c.destino.transform.position - transform.position).normalized;
            float puntaje = Vector3.Dot(dir, direccion.normalized);

            if (puntaje > mejorPuntaje && puntaje > 0.7f)
            {
                mejor = c.destino;
                mejorPuntaje = puntaje;
            }
        }

        return mejor;
    }
}

