using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;

//HOLA LOS ODIO :)
public class OllamaConnector : MonoBehaviour
{
    [Header("UI Referencias")]
    public TMP_InputField commandInputField;
    public TextMeshProUGUI statusText;
    public UnityEngine.UI.Button sendButton;

    [Header("Configuración Ollama")]
    public string ollamaUrl = "http://localhost:11434/api/generate";
    public string modelName = "mistral";

    [Header("Referencias a Managers")]
    public CubeManager cubeManager;

    [Header("Referencias para Control de Caminos")]
    public CaminoAbridor caminoAbridor;
    public CaminoCerrador caminoCerrador;

    private void Start()
    {
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(EnviarComandoAOllama);
        }

        
        if (cubeManager == null)
        {
            cubeManager = FindObjectOfType<CubeManager>();
        }

        
        if (caminoAbridor == null)
        {
            caminoAbridor = FindObjectOfType<CaminoAbridor>();
        }

        if (caminoCerrador == null)
        {
            caminoCerrador = FindObjectOfType<CaminoCerrador>();
        }

        if (cubeManager == null)
        {
            Debug.LogError("No se encontró CubeManager en la escena.");
            if (statusText != null)
                statusText.text = "Error: No se encontró CubeManager en la escena.";
            return;
        }

        
        AsignarReferenciasACaminoControladores();

        
        string cuboInicial = "ninguno";
        if (cubeManager != null && cubeManager.cubos != null && cubeManager.cubos.Count > 0)
        {
            cuboInicial = cubeManager.cubos[0].name;
        }

        if (statusText != null)
            statusText.text = $"Cubo inicial: {cuboInicial}";
    }

    private void AsignarReferenciasACaminoControladores()
    {
        if (caminoAbridor != null)
        {
            caminoAbridor.commandInputField = commandInputField;
            caminoAbridor.statusText = statusText;
        }

        if (caminoCerrador != null)
        {
            caminoCerrador.commandInputField = commandInputField;
            caminoCerrador.statusText = statusText;
        }
    }

    public void EnviarComandoAOllama()
    {
        if (commandInputField == null || string.IsNullOrEmpty(commandInputField.text.Trim()))
        {
            MostrarEstado("Por favor ingresa un comando.");
            return;
        }

        string comando = commandInputField.text.Trim();
        MostrarEstado("Procesando comando con Mistral...");

        StartCoroutine(ProcesarComandoConOllama(comando));
    }

    private IEnumerator ProcesarComandoConOllama(string comandoUsuario)
    {
        
        string prompt = CrearPromptCompleto(comandoUsuario);

       
        var requestData = new OllamaRequest
        {
            model = modelName,
            prompt = prompt,
            stream = false
        };

        string jsonPayload = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        
        using (UnityWebRequest request = new UnityWebRequest(ollamaUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string responseText = request.downloadHandler.text;
                    var response = JsonUtility.FromJson<OllamaResponse>(responseText);

                    if (response != null && !string.IsNullOrEmpty(response.response))
                    {
                        string comandoExtraido = ExtraerComandoDeRespuesta(response.response);
                        EjecutarComando(comandoExtraido);
                    }
                    else
                    {
                        MostrarEstado("Error: Respuesta vacía de Mistral");
                    }
                }
                catch (System.Exception e)
                {
                    MostrarEstado($"Error al procesar respuesta: {e.Message}");
                }
            }
            else
            {
                MostrarEstado($"Error de conexión: {request.error}");
            }
        }

        
        commandInputField.text = "";
    }

    private string CrearPromptCompleto(string comandoUsuario)
    {
        string nombresCubos = cubeManager != null && cubeManager.cubos != null ?
            string.Join(", ", cubeManager.cubos.Select(c => c.name)) : "N/A";

        string cuboActual = cubeManager != null && cubeManager.cubos != null && cubeManager.cubos.Count > 0 ?
            cubeManager.cubos[0].name : "ninguno"; 

        
        string nombresNodos = "N/A";
        if (caminoAbridor != null && caminoAbridor.nodos != null && caminoAbridor.nodos.Count > 0)
        {
            nombresNodos = string.Join(", ", caminoAbridor.nodos.Select(n => n.id));
        }

        return $@"Eres un asistente que convierte comandos en lenguaje natural a comandos específicos.

Los comandos válidos son:

MOVIMIENTO BÁSICO:
- adelante (para: adelante, avanzar, mover adelante, ir adelante, una cuadra adelante, etc.)
- atras (para: atrás, retroceder, mover atrás, ir atrás, etc.)
- derecha (para: derecha, girar derecha, mover derecha, ir derecha, etc.)
- izquierda (para: izquierda, girar izquierda, mover izquierda, ir izquierda, etc.)

NAVEGACIÓN INTELIGENTE:
- ir_a_[nodo] (para: ir a [nodo], navegar a [nodo], mover a [nodo], dirigirse a [nodo], etc.)

CAMBIO DE CUBO:
- cambiar (para cambiar al siguiente cubo)

CONTROL DE CAMINOS:
- abrir_camino_[nodo1]_[nodo2] (para abrir un camino entre dos nodos)
- cerrar_camino_[nodo1]_[nodo2] (para cerrar un camino entre dos nodos)

COMANDOS COMBINADOS:
- Si el comando tiene MÚLTIPLES acciones, responde con todas separadas por ' ; '
- Ejemplo: 'cambiar ; ir_a_b2' o 'abrir_camino_a1_b2 ; ir_a_b2'

INFORMACIÓN ACTUAL:
- Cubos disponibles: {nombresCubos}
- Nodos disponibles: {nombresNodos}

Analiza el comando: '{comandoUsuario}'

REGLAS:
1. Comando simple de movimiento → responde: adelante, atras, derecha, izquierda
2. Comando de navegación → responde: ir_a_[nodo]
3. Comando simple de cambio → responde: cambiar
4. Comando de abrir camino → responde: abrir_camino_[nodo1]_[nodo2]
5. Comando de cerrar camino → responde: cerrar_camino_[nodo1]_[nodo2]
6. Comando combinado → responde: [acción1] ; [acción2] ; [acción3]
7. Si no es válido → responde: invalido

EJEMPLOS:
- 'avanzar' → 'adelante'
- 'ir a b2' → 'ir_a_b2'
- 'navegar hasta la salida' → 'ir_a_salida'
- 'dirigirse a nodo1' → 'ir_a_nodo1'
- 'cambiar cubo' → 'cambiar'
- 'abre el camino entre nodo1 y nodo2' → 'abrir_camino_nodo1_nodo2'
- 'cierra el paso de A a B' → 'cerrar_camino_A_B'
- 'abre camino de entrada a salida y luego ve hacia salida' → 'abrir_camino_entrada_salida ; ir_a_salida'
- 'cambia de cubo, abre camino A B y navega a B' → 'cambiar ; abrir_camino_A_B ; ir_a_B'

Responde SOLAMENTE con el comando exacto:";
    }

    private string ExtraerComandoDeRespuesta(string respuesta)
    {
        
        string comando = respuesta.Trim().ToLower();

        
        if (comando.Contains(";"))
        {
            return comando; 
        }

        
        if (comando.Contains("ir_a_"))
        {
            return ExtraerComandoNavegacion(comando);
        }

        
        else if (comando.Contains("abrir_camino_"))
        {
            return ExtraerComandoCamino(comando, "abrir_camino_");
        }
        else if (comando.Contains("cerrar_camino_"))
        {
            return ExtraerComandoCamino(comando, "cerrar_camino_");
        }

        
        else if (comando == "adelante")
            return "adelante";
        else if (comando == "atras" || comando == "atrás")
            return "atras";
        else if (comando == "derecha")
            return "derecha";
        else if (comando == "izquierda")
            return "izquierda";
        else if (comando == "cambiar")
            return "cambiar";


        
        else if ((comando.Contains("adelante") || comando.Contains("avanzar") || comando.Contains("forward"))
                 && !comando.Contains("derecha") && !comando.Contains("izquierda") && !comando.Contains("atras") && !comando.Contains("atrás"))
            return "adelante";
        else if ((comando.Contains("atras") || comando.Contains("atrás") || comando.Contains("back"))
                 && !comando.Contains("derecha") && !comando.Contains("izquierda") && !comando.Contains("adelante"))
            return "atras";
        else if (comando.Contains("derecha") || comando.Contains("right"))
            return "derecha";
        else if (comando.Contains("izquierda") || comando.Contains("left"))
            return "izquierda";

        return "invalido";
    }

    private string ExtraerComandoNavegacion(string comando)
    {
        int index = comando.IndexOf("ir_a_");
        if (index != -1)
        {
            string nodoDestino = comando.Substring(index + 5).Trim(); // 5 = longitud de "ir_a_"

            
            nodoDestino = nodoDestino.Replace("_", "").Replace("-", "").Replace(" ", "");

            return $"ir_a_{nodoDestino}";
        }
        return "invalido";
    }

    private string ExtraerComandoCamino(string comando, string prefijo)
    {
        int index = comando.IndexOf(prefijo);
        if (index != -1)
        {
            string parametros = comando.Substring(index + prefijo.Length).Trim();

            
            string[] partes = parametros.Split('_');
            if (partes.Length >= 2)
            {
                string nodo1 = partes[0].Trim();
                string nodo2 = partes[1].Trim();
                return $"{prefijo}{nodo1}_{nodo2}";
            }
        }
        return "invalido";
    }

    private void EjecutarComando(string comando)
    {
        if (comando == "invalido")
        {
            MostrarEstado("Comando no reconocido por Mistral");
            return;
        }

        
        if (comando.Contains(";"))
        {
            EjecutarComandoCombinado(comando);
            return;
        }

        
        EjecutarComandoSimple(comando);
    }

    private void EjecutarComandoCombinado(string comandoCombinado)
    {
        string[] comandos = comandoCombinado.Split(';');

        MostrarEstado($"Ejecutando {comandos.Length} comandos combinados...");

        StartCoroutine(EjecutarComandosSecuenciales(comandos));
    }

    private IEnumerator EjecutarComandosSecuenciales(string[] comandos)
    {
        for (int i = 0; i < comandos.Length; i++)
        {
            string comando = comandos[i].Trim();
            MostrarEstado($"Ejecutando comando {i + 1}/{comandos.Length}: {comando}");

            EjecutarComandoSimple(comando);

            
            yield return new WaitForSeconds(0.5f);
        }

        MostrarEstado("Secuencia de comandos completada.");
    }

    private void EjecutarComandoSimple(string comando)
    {
        
        if (comando.StartsWith("ir_a_"))
        {
            EjecutarComandoNavegacion(comando);
            return;
        }

        
        else if (comando.StartsWith("abrir_camino_"))
        {
            EjecutarComandoCamino(comando, true);
            return;
        }
        else if (comando.StartsWith("cerrar_camino_"))
        {
            EjecutarComandoCamino(comando, false);
            return;
        }

        
        else if (comando == "cambiar")
        {
            if (cubeManager != null)
            {
                cubeManager.CambiarCubo();
                MostrarEstado("Comando de cambio procesado por Mistral.");
            }
            else
            {
                MostrarEstado("Error: CubeManager no disponible");
            }
            return;
        }

        
        if (cubeManager != null)
        {
            
            string textoOriginal = commandInputField.text;

            
            string comandoMovimiento = "";
            switch (comando)
            {
                case "adelante":
                    comandoMovimiento = "adelante";
                    break;
                case "atras":
                    comandoMovimiento = "atras";
                    break;
                case "izquierda":
                    comandoMovimiento = "izquierda";
                    break;
                case "derecha":
                    comandoMovimiento = "derecha";
                    break;
                default:
                    MostrarEstado($"Comando de movimiento no reconocido: {comando}");
                    return;
            }

            
            commandInputField.text = comandoMovimiento;

            
            cubeManager.EnviarComando();

            
            if (statusText != null && !statusText.text.Contains("no permitido"))
            {
                statusText.text += " (Comando procesado por Mistral)";
            }

           
            commandInputField.text = textoOriginal;
        }
        else
        {
            MostrarEstado("Error: CubeManager no disponible");
        }
    }

    private void EjecutarComandoNavegacion(string comando)
    {
        string nodoDestino = comando.Substring(5); 

        if (cubeManager == null || cubeManager.cubos == null || cubeManager.cubos.Count == 0)
        {
            MostrarEstado("Error: No hay cubos disponibles para navegar");
            return;
        }

        
        CubeMover cuboActivo = ObtenerCuboActivo();
        if (cuboActivo == null)
        {
            MostrarEstado("Error: No se pudo obtener el cubo activo");
            return;
        }

        if (cuboActivo.nodoActual == null)
        {
            MostrarEstado("Error: El cubo no tiene un nodo actual asignado");
            return;
        }

        
        Nodo[] todosLosNodos = FindObjectsOfType<Nodo>();
        Nodo nodoObjetivo = todosLosNodos.FirstOrDefault(n =>
            n.id != null && n.id.Equals(nodoDestino, System.StringComparison.OrdinalIgnoreCase));

        if (nodoObjetivo == null)
        {
            MostrarEstado($"Nodo destino '{nodoDestino}' no encontrado.");
            return;
        }

        if (nodoObjetivo == cuboActivo.nodoActual)
        {
            MostrarEstado("¡Ya estás en ese nodo!");
            return;
        }

        
        List<Nodo> ruta = CalcularRutaDijkstra(cuboActivo.nodoActual, nodoObjetivo);

        if (ruta == null || ruta.Count == 0)
        {
            MostrarEstado($"No se encontró una ruta hacia {nodoDestino}. Verifica que no haya caminos cerrados bloqueando el paso.");
            return;
        }

        MostrarEstado($"Navegando hacia {nodoDestino}...");

        
        StartCoroutine(MoverCuboEnRuta(cuboActivo, ruta));
    }

    private CubeMover ObtenerCuboActivo()
    {
        return cubeManager != null ? cubeManager.CuboActivo : null;
    }


    private List<Nodo> CalcularRutaDijkstra(Nodo origen, Nodo destino)
    {
        List<Nodo> todosNodos = FindObjectsOfType<Nodo>().ToList();

        if (!todosNodos.Contains(origen)) todosNodos.Add(origen);
        if (!todosNodos.Contains(destino)) todosNodos.Add(destino);

        var dist = new Dictionary<Nodo, float>();
        var prev = new Dictionary<Nodo, Nodo>();
        var nodosNoVisitados = new List<Nodo>();

        
        foreach (var nodo in todosNodos)
        {
            dist[nodo] = float.PositiveInfinity;
            prev[nodo] = null;
            nodosNoVisitados.Add(nodo);
        }

        dist[origen] = 0f;

        
        while (nodosNoVisitados.Count > 0)
        {
            
            Nodo nodoActual = nodosNoVisitados.OrderBy(n => dist[n]).First();
            nodosNoVisitados.Remove(nodoActual);

            
            if (nodoActual == destino)
                break;

            
            foreach (var camino in nodoActual.conexiones)
            {
                if (camino == null || camino.destino == null || camino.cerrado)
                    continue;

                Nodo vecino = camino.destino;

                
                if (!dist.ContainsKey(vecino)) continue;

                float nuevaDistancia = dist[nodoActual] + camino.peso;

                if (nuevaDistancia < dist[vecino])
                {
                    dist[vecino] = nuevaDistancia;
                    prev[vecino] = nodoActual;
                }
            }
        }

        
        List<Nodo> ruta = new List<Nodo>();
        Nodo actual = destino;

        
        while (actual != null)
        {
            ruta.Insert(0, actual);
            actual = prev[actual];
        }

       
        if (ruta.Count == 0 || ruta[0] != origen)
            return null;

        
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

    private IEnumerator MoverCuboEnRuta(CubeMover cubo, List<Nodo> ruta)
    {
        for (int i = 1; i < ruta.Count; i++) 
        {
            Nodo siguienteNodo = ruta[i];
            Nodo nodoAnterior = ruta[i - 1];

            
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

            cubo.transform.position = siguienteNodo.transform.position;
            cubo.nodoActual = siguienteNodo;

            MostrarEstado($"Avanzando a {siguienteNodo.id}");
        }

        MostrarEstado($"Llegaste al destino: {ruta[ruta.Count - 1].id} (Navegación completada por Mistral)");
    }

    private void EjecutarComandoCamino(string comando, bool abrir)
    {
        string prefijo = abrir ? "abrir_camino_" : "cerrar_camino_";
        string parametros = comando.Substring(prefijo.Length);

        string[] partes = parametros.Split('_');
        if (partes.Length < 2)
        {
            MostrarEstado("Error: Formato de comando de camino inválido");
            return;
        }

        string id1 = partes[0].Trim();
        string id2 = partes[1].Trim();

        
        string comandoCamino = $"{id1} {id2}";

        if (abrir && caminoAbridor != null)
        {
            
            string textoOriginal = commandInputField.text;
            commandInputField.text = comandoCamino;

            caminoAbridor.AbrirCaminoDesdeTexto();

            commandInputField.text = textoOriginal;
        }
        else if (!abrir && caminoCerrador != null)
        {
            
            string textoOriginal = commandInputField.text;
            commandInputField.text = comandoCamino;

            caminoCerrador.CerrarCaminoDesdeTexto();

            commandInputField.text = textoOriginal;
        }
        else
        {
            string accion = abrir ? "abrir" : "cerrar";
            MostrarEstado($"Error: No se encontró el controlador para {accion} caminos");
        }
    }

    private void MostrarEstado(string mensaje)
    {
        Debug.Log(mensaje);
        if (statusText != null)
            statusText.text = mensaje;
    }

    
    public string ObtenerEstadoActual()
    {
        if (cubeManager == null || cubeManager.cubos == null || cubeManager.cubos.Count == 0)
            return "No hay cubos disponibles";

       
        return "Sistema de IA activo y conectado";
    }
}

[System.Serializable]
public class OllamaRequest
{
    public string model;
    public string prompt;
    public bool stream;
}

[System.Serializable]
public class OllamaResponse
{
    public string model;
    public string created_at;
    public string response;
    public bool done;
}