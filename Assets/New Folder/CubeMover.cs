using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

public class CubeMover : MonoBehaviour
{
    public float moveSpeed = 10f;
    public TMP_InputField commandInputField;
    public TextMeshProUGUI statusText;

    public Nodo nodoActual;

    void Start()
    {
        if (nodoActual == null)
        {
            Debug.LogError("Nodo actual no asignado.");
        }
    }

    public void ProcessCommand()
    {
        if (commandInputField == null || nodoActual == null)
            return;

        string command = commandInputField.text;
        command = command.Trim().ToLower();

        if (Regex.IsMatch(command, @"^(ir a|mover a|caminar a|dirigirse a|arrastrar a|mover|caminar|dirigirse|voltear a|voltear|desplazar)", RegexOptions.IgnoreCase))
        {
            string objetivoId = null;
            var todos = FindObjectsOfType<Nodo>();
            Debug.Log("Nodos en escena:");
            foreach (var n in todos)
            {
                Debug.Log($"Nodo encontrado: {n.name}, id: {n.id}");
                if (n.id != null && ContienePalabrasEnOrdenOInvertido(command, n.id.ToLower()))
                {
                    objetivoId = n.id;
                    break;
                }
            }
            Nodo objetivo = todos.FirstOrDefault(n => n.id != null && n.id.Equals(objetivoId, System.StringComparison.OrdinalIgnoreCase));

            if (objetivo == null)
            {
                MostrarEstado($"Nodo destino '{objetivoId}' no encontrado.");
                return;
            }

            if (objetivo == nodoActual)
            {
                MostrarEstado("¡Ya estás en ese nodo!");
                return;
            }

            List<Nodo> ruta = CalcularRutaDijkstra(nodoActual, objetivo);

            if (ruta == null || ruta.Count == 0)
            {
                MostrarEstado("No se encontró una ruta al nodo destino.");
                return;
            }

            StartCoroutine(MoverRuta(ruta));
            return;
        }

        Vector3 direction = Vector3.zero;

        switch (command)
        {
            case "adelante": direction = Vector3.forward; break;
            case "atras": direction = Vector3.back; break;
            case "izquierda": direction = Vector3.left; break;
            case "derecha": direction = Vector3.right; break;
            default:
                MostrarEstado($"Comando inválido: {command}");
                return;
        }

        // CORREGIDO: Usar ObtenerVecino que ya respeta caminos cerrados
        Nodo siguienteNodo = nodoActual.ObtenerVecino(direction);

        if (siguienteNodo == null)
        {
            MostrarEstado("¡Movimiento no permitido! No hay camino abierto en esa dirección.");
            return;
        }

        transform.position = siguienteNodo.transform.position;
        nodoActual = siguienteNodo;

        MostrarEstado($"Movido a {siguienteNodo.id}. Escribe otro comando.");
    }

    List<Nodo> CalcularRutaDijkstra(Nodo origen, Nodo destino)
    {
        List<Nodo> todosNodos = FindObjectsOfType<Nodo>().ToList();

        if (!todosNodos.Contains(origen)) todosNodos.Add(origen);
        if (!todosNodos.Contains(destino)) todosNodos.Add(destino);

        var dist = new Dictionary<Nodo, float>();
        var prev = new Dictionary<Nodo, Nodo>();
        var nodosNoVisitados = new List<Nodo>();

        // Inicializar estructuras
        foreach (var nodo in todosNodos)
        {
            dist[nodo] = float.PositiveInfinity;
            prev[nodo] = null;
            nodosNoVisitados.Add(nodo);
        }

        dist[origen] = 0f;

        // Algoritmo de Dijkstra
        while (nodosNoVisitados.Count > 0)
        {
            // Nodo con menor distancia estimada
            Nodo nodoActual = nodosNoVisitados.OrderBy(n => dist[n]).First();
            nodosNoVisitados.Remove(nodoActual);

            // Si llegamos al destino, terminamos
            if (nodoActual == destino)
                break;

            // CORREGIDO: Verificar que el camino no esté cerrado
            foreach (var camino in nodoActual.conexiones)
            {
                if (camino == null || camino.destino == null || camino.cerrado)
                    continue;

                Nodo vecino = camino.destino;

                // Si el vecino no está en la lista, sáltalo
                if (!dist.ContainsKey(vecino)) continue;

                float nuevaDistancia = dist[nodoActual] + camino.peso;

                if (nuevaDistancia < dist[vecino])
                {
                    dist[vecino] = nuevaDistancia;
                    prev[vecino] = nodoActual;
                }
            }
        }

        // Reconstrucción del camino
        List<Nodo> ruta = new List<Nodo>();
        Nodo actual = destino;

        // Retroceder desde destino hasta origen
        while (actual != null)
        {
            ruta.Insert(0, actual);
            actual = prev[actual];
        }

        // Validar que la ruta encontrada lleva realmente al destino
        if (ruta.Count == 0 || ruta[0] != origen)
            return null;

        // CORREGIDO: Verificar que toda la ruta esté disponible (sin caminos cerrados)
        for (int i = 0; i < ruta.Count - 1; i++)
        {
            Nodo nodoOrigen = ruta[i];
            Nodo nodoDestino = ruta[i + 1];

            bool caminoDisponible = false;
            foreach (var camino in nodoOrigen.conexiones)
            {
                if (camino.destino == nodoDestino && !camino.cerrado)
                {
                    caminoDisponible = true;
                    break;
                }
            }

            if (!caminoDisponible)
            {
                MostrarEstado($"El camino desde {nodoOrigen.id} hacia {nodoDestino.id} está cerrado.");
                return null;
            }
        }

        return ruta;
    }

    IEnumerator MoverRuta(List<Nodo> ruta)
    {
        for (int i = 1; i < ruta.Count; i++) // Empezar desde 1 para saltar el nodo actual
        {
            Nodo siguienteNodo = ruta[i];
            Nodo nodoAnterior = ruta[i - 1];

            // CORREGIDO: Verificar antes de cada movimiento que el camino sigue abierto
            bool caminoAbierto = false;
            foreach (var camino in nodoAnterior.conexiones)
            {
                if (camino.destino == siguienteNodo && !camino.cerrado)
                {
                    caminoAbierto = true;
                    break;
                }
            }

            if (!caminoAbierto)
            {
                MostrarEstado($"¡El camino hacia {siguienteNodo.id} se cerró durante el movimiento!");
                yield break;
            }

            yield return new WaitForSeconds(0.5f);

            transform.position = siguienteNodo.transform.position;
            nodoActual = siguienteNodo;

            MostrarEstado($"Avanzando a {siguienteNodo.id}");
        }

        MostrarEstado("Llegaste al destino.");
    }

    bool ContienePalabrasEnOrden(string texto, string frase)
    {
        var palabrasTexto = texto.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        var palabrasFrase = frase.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

        int indexTexto = 0;

        foreach (var palabra in palabrasFrase)
        {
            bool encontrada = false;
            while (indexTexto < palabrasTexto.Length)
            {
                if (palabrasTexto[indexTexto] == palabra)
                {
                    encontrada = true;
                    indexTexto++;
                    break;
                }
                indexTexto++;
            }
            if (!encontrada)
                return false;
        }
        return true;
    }

    bool ContienePalabrasEnOrdenOInvertido(string texto, string frase)
    {
        if (ContienePalabrasEnOrden(texto, frase))
            return true;

        var palabrasFrase = frase.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        var fraseInvertida = string.Join(" ", palabrasFrase.Reverse());

        return ContienePalabrasEnOrden(texto, fraseInvertida);
    }

    // Método auxiliar para mostrar estado
    private void MostrarEstado(string mensaje)
    {
        Debug.Log(mensaje);
        if (statusText != null)
            statusText.text = mensaje;
    }
}