using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class CubeManager : MonoBehaviour
{
    public List<CubeMover> cubos;
    public TMP_InputField commandInputField;
    public TextMeshProUGUI statusText;

    private int cuboActivoIndex = 0;

    void Start()
    {
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
}