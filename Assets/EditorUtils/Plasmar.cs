using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// ummm hay que pensar un poco mejor esto
// pero creo que tiene mas que ver con tener instancias
// de debugeo que se actualizan, en vez de fire y forget
// aunque el fire and forget puede ser, con expiracion incluso
// pero el fire and forget tambien taria bueno que tenga algun tipo de
// trackeo para poder actualizar si se ejecuta algo 5 veces antes que llegue
// a plasmarse, incluso podria haber un "modo" de plasmar con historia

[InitializeOnLoad]
public static class Plasmar
{
    public class Tarjeta
    {
        public bool EnMundo = false;
        public Vector3 PosEnMundo = Vector3.zero;

        public readonly List<(string nombre, string valor)> DataCruda = new();
        public event System.Action DrawQueue;

        public bool Dibujar;

        public void OnGUI()
        {
            foreach (var data in DataCruda)
            {
                if (data.valor == null)
                    GUILayout.Label(data.nombre);
                else
                    GUILayout.Label($"{data.nombre}:\t{data.valor}");
            }
        }

        public void Draw()
        {
            DrawQueue?.Invoke();
            DrawQueue = null;
        }
    }

    // static List<Tarjeta> _tarjetas = new();
    static Dictionary<object, Tarjeta> _tarjetas = new();

    static Plasmar()
    {
        SceneView.beforeSceneGui -= BeforeSceneGui;
        SceneView.beforeSceneGui += BeforeSceneGui;
        SceneView.duringSceneGui -= DuringSceneGui;
        SceneView.duringSceneGui += DuringSceneGui;
    }

    static void BeforeSceneGui(SceneView view)
    {

    }

    static void DuringSceneGui(SceneView view)
    {
        Handles.BeginGUI();
        {
            using (new GUILayout.AreaScope(view.position))
            {
                foreach (var draw in _tarjetas.Values)
                {
                    if (draw.Dibujar)
                    {
                        draw.OnGUI();
                        draw.Dibujar = false;
                    }
                }
            }
        }
        Handles.EndGUI();
    }

    public static Tarjeta GetTarjeta(object llave)
    {
        if (!_tarjetas.ContainsKey(llave))
            _tarjetas.Add(llave, new Tarjeta());

        var draw = _tarjetas[llave];
        draw.Dibujar = true;

        return draw;
    }

    // public static void AddDrawToQueue(Tarjeta tarjeta)
    // {
    //     _tarjetas.Add
    // }

    // public static void AddPanel
}
