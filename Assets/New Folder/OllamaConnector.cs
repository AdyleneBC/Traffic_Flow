using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;

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

        // Buscar CubeManager si no está asignado
        if (cubeManager == null)
        {
            cubeManager = FindObjectOfType<CubeManager>();
        }

        // Buscar CaminoAbridor y CaminoCerrador si no están asignados
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

        // Asignar referencias a los controladores de caminos
        AsignarReferenciasACaminoControladores();

        // Obtener información del cubo inicial
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
        // Crear el prompt para Mistral
        string prompt = CrearPromptCompleto(comandoUsuario);

        // Crear el payload JSON para Ollama
        var requestData = new OllamaRequest
        {
            model = modelName,
            prompt = prompt,
            stream = false
        };

        string jsonPayload = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        // Crear la petición HTTP
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

        // Limpiar el campo de entrada
        commandInputField.text = "";
    }

    private string CrearPromptCompleto(string comandoUsuario)
    {
        string nombresCubos = cubeManager != null && cubeManager.cubos != null ?
            string.Join(", ", cubeManager.cubos.Select(c => c.name)) : "N/A";

        string cuboActual = cubeManager != null && cubeManager.cubos != null && cubeManager.cubos.Count > 0 ?
            cubeManager.cubos[0].name : "ninguno"; // Simplificado, el CubeManager maneja el activo

        // Obtener nodos disponibles desde CaminoAbridor si existe
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
        // Limpiar la respuesta
        string comando = respuesta.Trim().ToLower();

        // Verificar si es un comando combinado (contiene ';')
        if (comando.Contains(";"))
        {
            return comando; // Devolver el comando combinado tal como está
        }

        // PRIORIDAD 1: Comandos de navegación inteligente
        if (comando.Contains("ir_a_"))
        {
            return ExtraerComandoNavegacion(comando);
        }

        // PRIORIDAD 2: Comandos de caminos
        else if (comando.Contains("abrir_camino_"))
        {
            return ExtraerComandoCamino(comando, "abrir_camino_");
        }
        else if (comando.Contains("cerrar_camino_"))
        {
            return ExtraerComandoCamino(comando, "cerrar_camino_");
        }

        // PRIORIDAD 3: Comandos exactos de movimiento
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


        // PRIORIDAD 4: Comandos con palabras adicionales - CON VERIFICACIÓN DE DIRECCIÓN
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

            // Limpiar caracteres no deseados
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

            // Separar los dos nodos
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

        // Verificar si es un comando combinado
        if (comando.Contains(";"))
        {
            EjecutarComandoCombinado(comando);
            return;
        }

        // Ejecutar comando simple
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

            // Pausa entre comandos para que se procesen correctamente
            yield return new WaitForSeconds(0.5f);
        }

        MostrarEstado("Secuencia de comandos completada.");
    }

    private void EjecutarComandoSimple(string comando)
    {
        // NUEVO: Comandos de navegación inteligente
        if (comando.StartsWith("ir_a_"))
        {
            EjecutarComandoNavegacion(comando);
            return;
        }

        // Comandos de control de caminos usando las clases existentes
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

        // Comando de cambio de cubo usando CubeManager
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

        // Comandos de movimiento usando CubeManager -> CubeMover
        if (cubeManager != null)
        {
            // Simular entrada de texto para usar la lógica existente
            string textoOriginal = commandInputField.text;

            // Mapear comandos de IA a comandos de movimiento
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

            // Establecer el comando en el campo de entrada
            commandInputField.text = comandoMovimiento;

            // Ejecutar usando CubeManager (que usa CubeMover)
            cubeManager.EnviarComando();

            // Agregar mensaje de confirmación
            if (statusText != null && !statusText.text.Contains("no permitido"))
            {
                statusText.text += " (Comando procesado por Mistral)";
            }

            // Restaurar texto original
            commandInputField.text = textoOriginal;
        }
        else
        {
            MostrarEstado("Error: CubeManager no disponible");
        }
    }

    private void EjecutarComandoNavegacion(string comando)
    {
        string nodoDestino = comando.Substring(5); // Quitar "ir_a_"

        if (cubeManager == null || cubeManager.cubos == null || cubeManager.cubos.Count == 0)
        {
            MostrarEstado("Error: No hay cubos disponibles para navegar");
            return;
        }

        // Obtener el cubo activo actual
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

        // Buscar el nodo destino
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

        // Calcular la ruta usando Dijkstra
        List<Nodo> ruta = CalcularRutaDijkstra(cuboActivo.nodoActual, nodoObjetivo);

        if (ruta == null || ruta.Count == 0)
        {
            MostrarEstado($"No se encontró una ruta hacia {nodoDestino}. Verifica que no haya caminos cerrados bloqueando el paso.");
            return;
        }

        MostrarEstado($"Navegando hacia {nodoDestino}...");

        // Iniciar el movimiento usando el CubeMover
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

            // Verificar que el camino no esté cerrado
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

        // Verificar que toda la ruta esté disponible (sin caminos cerrados)
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
        for (int i = 1; i < ruta.Count; i++) // Empezar desde 1 para saltar el nodo actual
        {
            Nodo siguienteNodo = ruta[i];
            Nodo nodoAnterior = ruta[i - 1];

            // Verificar antes de cada movimiento que el camino sigue abierto
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

        // Formar el comando como lo esperan las clases CaminoAbridor/CaminoCerrador
        string comandoCamino = $"{id1} {id2}";

        if (abrir && caminoAbridor != null)
        {
            // Usar la clase CaminoAbridor existente
            string textoOriginal = commandInputField.text;
            commandInputField.text = comandoCamino;

            caminoAbridor.AbrirCaminoDesdeTexto();

            commandInputField.text = textoOriginal;
        }
        else if (!abrir && caminoCerrador != null)
        {
            // Usar la clase CaminoCerrador existente
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

    // Método público para obtener información del estado actual
    public string ObtenerEstadoActual()
    {
        if (cubeManager == null || cubeManager.cubos == null || cubeManager.cubos.Count == 0)
            return "No hay cubos disponibles";

        // Obtener información del cubo activo desde CubeManager
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