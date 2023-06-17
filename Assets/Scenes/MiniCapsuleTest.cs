using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MiniCapsuleTest : MonoBehaviour
{
    private static RaycastHit2D[] _hitsCache = new RaycastHit2D[10];

    [SerializeField]
    private Vector2 _capsuleSize = new Vector2(1f, 1f);
    [SerializeField]
    private CapsuleDirection2D _capsuleDirection = CapsuleDirection2D.Vertical;

    [SerializeField]
    [UnitVector]
    private Vector2 _vectorDeSalida = Vector2.right;
    [SerializeField]
    private float _toleranciaAngular = 45f;
    [SerializeField]
    private bool _salidaBilateral = false;
    [SerializeField]
    private bool _soloSalidaPorVector = true;

    public Vector2 _origenPuntoGlobal = Vector2.zero;

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.green * 0.35f;

        var tam = new Vector3(_capsuleSize.x, _capsuleSize.y, 0f);
        if (_capsuleDirection == CapsuleDirection2D.Vertical)
            tam.y = Mathf.Max(_capsuleSize.x, _capsuleSize.y);
        else
            tam.x = Mathf.Max(_capsuleSize.x, _capsuleSize.y);

        Gizmos.DrawCube(Vector3.zero, tam);
#if UNITY_EDITOR
        using (new Handles.DrawingScope(Color.green * .5f, transform.localToWorldMatrix))
            EditorUtils.DrawCapsule2D(Vector3.zero, Vector3.forward, _capsuleSize, _capsuleDirection, 4f);
#endif
    }

#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MiniCapsuleTest))]
    private class EsteEditor : Editor
    {
        private void OnSceneGUI()
        {
            var esto = target as MiniCapsuleTest;
            var centro = (Vector2)esto.transform.position;
            var handSize = HandleUtility.GetHandleSize(centro) * .1f;

            var capsulaVertical = esto._capsuleDirection == CapsuleDirection2D.Vertical;
            var columna = (Vector2)(capsulaVertical ? esto.transform.up : esto.transform.right);
            var perpColumna = Vector2.Perpendicular(columna);//(Vector2)(capsulaVertical ? esto.transform.right : esto.transform.up);
            var puntoBuscadoLocal = esto._origenPuntoGlobal - centro;

            using (new Handles.DrawingScope(Color.red))
            {
                var capsTam = esto._capsuleSize;
                var radioCapsula = (capsulaVertical ? capsTam.x : capsTam.y) / 2f;
                var radioColumna = Mathf.Max((capsulaVertical ? capsTam.y : capsTam.x) / 2f, radioCapsula) - radioCapsula;

                var puntoProyectado = Fisica.ProjectPointCapsule(puntoBuscadoLocal, columna, radioCapsula, radioColumna);

                var rayoHit = centro;
                var rayo = new Ray2D(esto._origenPuntoGlobal, esto.transform.TransformVector(esto._vectorDeSalida));
                if (Fisica.RayoContraCapsula(rayo, centro, columna, radioCapsula, radioColumna, ref rayoHit))
                {
                    using (new Handles.DrawingScope(Color.white))
                        Handles.DrawWireDisc(rayoHit, esto.transform.forward, radioCapsula, 2f);
                }

                Handles.DrawDottedLine(centro + columna * 1000f, centro - columna * 1000f, 5f);
                
                using (new Handles.DrawingScope(Color.white))
                    Handles.DrawDottedLine(rayo.origin + rayo.direction * 1000f, rayo.origin - rayo.direction * 1000f, 5f);

                var crossEspinaRayo = ((columna.x)*(puntoBuscadoLocal.y) - (columna.y)*(puntoBuscadoLocal.x));
                var crossEspinaRayoDir = (columna.x*rayo.direction.y)-(columna.y*rayo.direction.x);
                var lado = crossEspinaRayoDir < 0f ^ Mathf.Abs(crossEspinaRayo)<radioCapsula;
                Handles.DrawLine(centro + columna * 1000f + perpColumna * radioCapsula, centro - columna * 1000f + perpColumna * radioCapsula, lado? 5f:1f);
                Handles.DrawLine(centro + columna * 1000f - perpColumna * radioCapsula, centro - columna * 1000f - perpColumna * radioCapsula, !lado? 5f:1f);

                Handles.DrawWireDisc(centro + columna * radioColumna, esto.transform.forward, radioCapsula);
                Handles.DrawWireDisc(centro - columna * radioColumna, esto.transform.forward, radioCapsula);

                Handles.DrawSolidDisc(puntoProyectado + centro, esto.transform.forward, handSize);
            }

            using (new Handles.DrawingScope(esto.transform.localToWorldMatrix))
            {
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    var nuevoOrigPuntoLocal = (Vector2)Handles.Slider2D(esto.transform.InverseTransformPoint(esto._origenPuntoGlobal), esto.transform.forward, esto.transform.up, esto.transform.right, handSize, Handles.SphereHandleCap, Vector2.zero);
                    var nuevoRayo = (Vector2)Handles.Slider2D(nuevoOrigPuntoLocal+esto._vectorDeSalida.normalized, esto.transform.forward, esto.transform.up, esto.transform.right, handSize, Handles.CircleHandleCap, Vector2.zero);
                    if (change.changed)
                    {
                        Undo.RecordObject(esto, "Movio punto origen");
                        esto._origenPuntoGlobal = esto.transform.TransformPoint(nuevoOrigPuntoLocal);
                        esto._vectorDeSalida = (nuevoRayo-nuevoOrigPuntoLocal).normalized;
                    }
                }
            }
        }
    }
#endif
}
