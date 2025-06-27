using UnityEngine;

[System.Serializable]
public class Camino
{
    public Nodo destino;
    public float peso = 1f;
    public bool cerrado = false;
}
