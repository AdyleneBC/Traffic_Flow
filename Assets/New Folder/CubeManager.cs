using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class CubeManager : MonoBehaviour
{
    public List<CubeMover> cubos;
    public TMP_InputField commandInputField;
    public TextMeshProUGUI statusText;
    public GameObject prefabCubo;
    public int maxCubos = 6;
    public int minCubos = 2;
    public List<Nodo> nodosDisponibles;
    
    private Dictionary<string, Color> coloresDisponibles = new Dictionary<string, Color>()
    {
        {"Rojo", Color.red},
        {"Verde", Color.green},
        {"Azul", Color.blue},
        {"Amarillo", Color.yellow},
        {"Cian", Color.cyan},
        {"Magenta", Color.magenta},
        {"Gris", Color.gray},
        {"Naranja", new Color(1f, 0.5f, 0f)},
        {"Morado", new Color(0.5f, 0f, 1f)},
        {"Rosa", new Color(1f, 0.4f, 0.7f)}
    };



    private int cuboActivoIndex = 0;

    void Start()
    {
        nodosDisponibles = new List<Nodo>(FindObjectsByType<Nodo>(FindObjectsSortMode.None));
        Debug.Log("Nodos disponibles encontrados: " + nodosDisponibles.Count);
        if (cubos.Count == 0)
        {
            Debug.LogError("No se han asignado cubos al CubeManager.");
            return;
        }
        AsignarReferencias(cubos[cuboActivoIndex]);
    }

    public void EnviarComando()
    {
        if (cubos.Count == 0) return;
        cubos[cuboActivoIndex].ProcessCommand();
    }

    public void CambiarCubo()
    {
        if (cubos.Count <= 1) return;
        cuboActivoIndex = (cuboActivoIndex + 1) % cubos.Count;

        AsignarReferencias(cubos[cuboActivoIndex]);

        if (statusText != null)
            statusText.text = $"Cambiaste al cubo {cubos[cuboActivoIndex].name}";
    }

    private void AsignarReferencias(CubeMover mover)
    {
        mover.commandInputField = commandInputField;
        mover.statusText = statusText;
    }
    public CubeMover CuboActivo => cubos[cuboActivoIndex];
    public void AgregarCubo()
    {
        if (cubos.Count >= maxCubos)
        {
            statusText.text = "Ya tienes el número máximo de cubos.";
            return;
        }

        Nodo nodoLibre = ObtenerNodoLibre();

        if (nodoLibre == null)
        {
            statusText.text = "No hay nodos libres para colocar un nuevo cubo.";
            return;
        }

        Vector3 posicion = nodoLibre.transform.position;
        string nombreColor = coloresDisponibles.Keys
            .Except(cubos.Select(c => c.name))
            .OrderBy(_ => Random.value)
            .FirstOrDefault();

        if (string.IsNullOrEmpty(nombreColor))
        {
            statusText.text = "Ya no hay colores disponibles únicos.";
            return;
        }

        GameObject nuevoCubo = Instantiate(prefabCubo, posicion, Quaternion.identity);

        Renderer rend = nuevoCubo.GetComponent<Renderer>();
        Material nuevoMaterial = new Material(rend.material);
        nuevoMaterial.color = coloresDisponibles[nombreColor];
        rend.material = nuevoMaterial;

        nuevoCubo.name = nombreColor;

        CubeMover mover = nuevoCubo.GetComponent<CubeMover>();
        mover.commandInputField = commandInputField;
        mover.statusText = statusText;
        mover.nodoActual = nodoLibre;

        cubos.Add(mover);
        statusText.text = $"Cubo '{nombreColor}' agregado en {nodoLibre.name}. Total: {cubos.Count}";
    }



    public void EliminarCubo()
    {
        if (cubos.Count <= minCubos)
        {
            statusText.text = "No puedes tener menos cubos.";
            return;
        }

        CubeMover aEliminar = cubos[cubos.Count - 1];
        cubos.RemoveAt(cubos.Count - 1);
        Destroy(aEliminar.gameObject);

        cuboActivoIndex = Mathf.Clamp(cuboActivoIndex, 0, cubos.Count - 1);
        statusText.text = $"Cubo eliminado. Total: {cubos.Count}";
    }
    private Nodo ObtenerNodoLibre()
    {
        foreach (Nodo nodo in nodosDisponibles)
        {
            bool ocupado = false;

            foreach (CubeMover mover in cubos)
            {
                if (mover.nodoActual == nodo)
                {
                    ocupado = true;
                    break;
                }
            }

            if (!ocupado)
                return nodo;
        }

        return null; // No hay nodos libres
    }


}