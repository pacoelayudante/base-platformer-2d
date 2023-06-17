using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MicroscopioBehav : MonoBehaviour
{
    private static RaycastHit2D[] _hitsCache = new RaycastHit2D[10];

    [SerializeField]
    private Vector2 _capsuleSize = new Vector2(1f, 1f);
    [SerializeField]
    private CapsuleDirection2D _capsuleDirection = CapsuleDirection2D.Vertical;

    [SerializeField]
    private ContactFilter2D _filtro;

    [SerializeField]
    [UnitVector]
    private Vector2 _vectorDeSalida = Vector2.right;
    [SerializeField]
    private float _toleranciaAngular = 45f;
    [SerializeField]
    private bool _salidaBilateral = false;
    [SerializeField]
    private bool _soloSalidaPorVector = true;

    // [SerializeField]
    // private Vector2 _castVector;
    // [SerializeField]
    // private bool _lateral;
    // [SerializeField]
    // private float _distanciaPisoDebajo = .01f;

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.blue * 0.25f;
        Gizmos.DrawCube(Vector3.zero, new Vector3(_capsuleSize.x, _capsuleSize.y, 0f));
    }

#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MicroscopioBehav))]
    private class EsteEditor : Editor
    {
        private void OnSceneGUI()
        {
            var esto = target as MicroscopioBehav;

            var origin = (Vector2)esto.transform.position;
            var size = esto._capsuleSize;
            var capsuleDirection = esto._capsuleDirection;

            var radio = size[(int)esto._capsuleDirection] / 2f;
            var lhalf = size[((int)esto._capsuleDirection + 1) % 2] / 2f - radio;

            var centroCirculoAbajo = origin - (Vector2)esto.transform.up * lhalf;
            var centroCirculoArriba = origin + (Vector2)esto.transform.up * lhalf;

            float handleSize = radio * HandleUtility.GetHandleSize(origin);
            using (new Handles.DrawingScope(Color.Lerp(Color.green, Color.red, 0.5f), esto.transform.localToWorldMatrix))
            {
                EditorUtils.DrawCapsule2D(Vector3.zero, Vector3.forward, size, capsuleDirection, 3f);
                Handles.DrawLine(Vector3.zero, esto._vectorDeSalida * handleSize);
                Handles.DrawLine(Vector3.zero, Quaternion.Euler(0f, 0f, esto._toleranciaAngular) * esto._vectorDeSalida * handleSize / 2f);
                Handles.DrawLine(Vector3.zero, Quaternion.Euler(0f, 0f, -esto._toleranciaAngular) * esto._vectorDeSalida * handleSize / 2f);
                // Handles.DrawLine(centroCirculoAbajo + Vector2.up * handleSize, centroCirculoAbajo + Vector2.down * handleSize);

                var p = (Vector2)Vector2.up * (lhalf + radio);
                Handles.DrawDottedLine(p - esto._vectorDeSalida * 5f, p + esto._vectorDeSalida * 5f, 3f);
                p = -(Vector2)Vector2.up * (lhalf + radio);
                Handles.DrawDottedLine(p - esto._vectorDeSalida * 5f, p + esto._vectorDeSalida * 5f, 3f);
            }

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                var newval = (Vector2)Handles.Slider(origin + esto._vectorDeSalida * handleSize, esto.transform.rotation * esto._vectorDeSalida) - esto._vectorDeSalida * handleSize;
                if (change.changed)
                {
                    Undo.RecordObject(esto.transform, "Mover por vector de salida");
                    esto.transform.position = newval;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(esto.transform);
                }
            }

            var ang = esto.transform.localEulerAngles.z;
            int casos = Physics2D.CapsuleCast(origin, size, capsuleDirection, angle: ang, -esto._vectorDeSalida, new ContactFilter2D(), Fisica.CacheHit2DSoloPrimero, distance: 0f);
            if (casos > 0)
            {
                var desenterrado = Fisica.DesenterrarCapsula(origin, size, capsuleDirection, esto._vectorDeSalida, Fisica.CacheHit2DSoloPrimero[0], esto._toleranciaAngular, esto._salidaBilateral, esto._soloSalidaPorVector);
                using (new Handles.DrawingScope(Color.red))
                {
                    // EditorUtils.DrawCapsule2D(desenterrado, Vector3.forward, size, capsuleDirection);
                    Handles.DrawLine(Fisica.CacheHit2DSoloPrimero[0].point, Fisica.CacheHit2DSoloPrimero[0].point + Fisica.CacheHit2DSoloPrimero[0].normal * handleSize);
                    Handles.DrawLine(Fisica.CacheHit2DSoloPrimero[0].point, Fisica.CacheHit2DSoloPrimero[0].point + Fisica.CacheHit2DSoloPrimero[0].normal * handleSize);

                    var centrocollid = (Vector2)Fisica.CacheHit2DSoloPrimero[0].collider.bounds.center;
                    var distConSegm = Fisica.DistancePointToLineSegment(centrocollid,
                        centroCirculoAbajo, centroCirculoArriba);
                    var c = centrocollid + Fisica.CacheHit2DSoloPrimero[0].normal * distConSegm;
                    Handles.DrawWireDisc(c, Vector3.forward, radio);
                    Handles.DrawWireDisc(c - Fisica.CacheHit2DSoloPrimero[0].normal * radio, Vector3.forward, radio * 0.1f);
                    // var movnormal = 

                    using (var change = new EditorGUI.ChangeCheckScope())
                    {
                        var newval = (Vector2)Handles.Slider(Fisica.CacheHit2DSoloPrimero[0].point, Fisica.CacheHit2DSoloPrimero[0].normal);
                        if (change.changed)
                        {
                            Undo.RecordObject(esto.transform, "Mover por vector de salida");
                            esto.transform.position = newval - Fisica.CacheHit2DSoloPrimero[0].point + origin;
                            PrefabUtility.RecordPrefabInstancePropertyModifications(esto.transform);
                        }
                    }
                    // if (Vector2.Dot(Vector2.up,Fisica.CacheHit2DSoloPrimero[0].normal)==0f)
                    {
                        // var dif = (origin.x - Fisica.CacheHit2DSoloPrimero[0].point.x) + radio;
                        // Handles.DrawWireDisc(Fisica.CacheHit2DSoloPrimero[0].point, Vector3.forward, dif);

                        // dif *= 2f;
                        // var newpos = origin + Fisica.CacheHit2DSoloPrimero[0].normal * dif;
                        // var fact = (esto._vectorDeSalida.y)/(esto._vectorDeSalida.x);
                        // newpos -= Vector2.up * fact * dif;

                        // EditorUtils.DrawCapsule2D(newpos, Vector3.forward, size, capsuleDirection);

                    }
                }
            }

            // var desenterrado = Fisica.DesenterrarCapsula(origin, size, capsuleDirection, esto._lateral);
            // using (new Handles.DrawingScope(Color.red))
            //     EditorUtils.DrawCapsule2D(desenterrado, Vector3.forward, size, capsuleDirection);

            // // calcular y mostrar punto en la capsula
            // int hits = Physics2D.CapsuleCast(origin, size, capsuleDirection, angle: 0f, Vector2.zero, esto._filtro, _hitsCache);
            // for (int i = 0; i < hits; i++)
            // {
            //     bool overlap = _hitsCache[i].distance <= 0f;
            //     float handleSize = size.magnitude / 2f * HandleUtility.GetHandleSize(_hitsCache[i].point);
            //     Quaternion rot = Quaternion.FromToRotation(Vector3.forward, _hitsCache[i].normal);

            //     var puntoExternoACapsula = origin - 2*lhalf * _hitsCache[i].normal;
            //     var distPuntoExterno = Fisica.DistancePointToLineSegment(puntoExternoACapsula, centroCirculoAbajo, centroCirculoArriba);

            //     var vecLong = (centroCirculoAbajo-centroCirculoArriba);
            //     var dot = Vector2.Dot(vecLong, _hitsCache[i].normal);
            //     var dotsign = dot>0?1:-1;


            //     // Vector3 puntoEnCapsula = puntoExternoACapsula + distPuntoExterno * _hitsCache[i].normal;
            //     Vector2 puntoEnCapsula = origin+(Vector2)esto.transform.up*dotsign * lhalf-radio*_hitsCache[i].normal*0f;
            //     using (new Handles.DrawingScope(overlap ? Color.red : Color.green))
            //     {
            //         // Handles.ArrowHandleCap(-1, _hitsCache[i].point, rot, handleSize, Event.current.type);

            //         Handles.DrawWireDisc(_hitsCache[i].point, Vector3.forward, handleSize * 0.1f);
            //         Handles.DrawLine(_hitsCache[i].point, _hitsCache[i].point + radio * _hitsCache[i].normal);
            //         Handles.DrawWireDisc(puntoEnCapsula, Vector3.forward, handleSize * 0.05f);
            //     }
            // }

            // hits = Physics2D.CapsuleCast(desenterrado, size, capsuleDirection, angle: 0f, Vector2.down, esto._filtro, _hitsCache, esto._distanciaPisoDebajo);
            // var sueloSnapPos = desenterrado;
            // for (int i = 0; i < hits; i++)
            // {
            //     if (i == 0)
            //         sueloSnapPos = desenterrado + Vector2.down * _hitsCache[i].distance;

            //     using (new Handles.DrawingScope(Color.green))
            //     {
            //         Handles.DrawLine(_hitsCache[i].point, _hitsCache[i].point + Vector2.up * _hitsCache[i].distance);
            //         EditorUtils.DrawCapsule2D(desenterrado + Vector2.down * _hitsCache[i].distance, Vector3.forward, size, capsuleDirection);

            //         float handleSize = size.magnitude / 2f * HandleUtility.GetHandleSize(_hitsCache[i].point);
            //         Quaternion rot = Quaternion.LookRotation(Vector2.Perpendicular(_hitsCache[i].normal));
            //         Handles.ArrowHandleCap(-1, _hitsCache[i].point, rot, handleSize, Event.current.type);
            //     }
            // }
        }
    }
#endif
}
