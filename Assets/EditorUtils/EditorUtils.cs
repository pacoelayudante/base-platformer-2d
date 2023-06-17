using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class EditorUtils
{
    // public static event System.Action SiguienteSceneDraw;

    // [InitializeOnLoadMethod]
    // private static void Init()
    // {
    //     SceneView.duringSceneGui -= EjecutarAccionesEnCola;
    //     SceneView.duringSceneGui += EjecutarAccionesEnCola;
    // }

    // private static void EjecutarAccionesEnCola(SceneView sceneView)
    // {
    //     SiguienteSceneDraw?.Invoke();
    //     SiguienteSceneDraw = null;
    // }

    public static void DrawCapsule2D(Vector3 center, Vector3 normal, Vector2 extension, CapsuleDirection2D capsuleDirection, float thickness = 0f)
    {
        var halfDiam = 0.5f * (capsuleDirection == CapsuleDirection2D.Vertical ? extension.x : extension.y);
        var halfInterCenterDist = 0.5f * (capsuleDirection == CapsuleDirection2D.Vertical ? extension.y : extension.x) - halfDiam;
        if (halfInterCenterDist < 0f)
        {
            Handles.DrawWireDisc(center, normal, halfDiam, thickness);
            return;
        }

        var offCenterVec = halfInterCenterDist * (capsuleDirection == CapsuleDirection2D.Vertical ? Vector3.up : Vector3.right);
        var offCenterVecSide = halfDiam * (capsuleDirection == CapsuleDirection2D.Vertical ? Vector3.right : Vector3.up);

        Vector3 tangent = Vector3.Cross(normal, Vector3.up);
        if (tangent.sqrMagnitude < .001f)
            tangent = Vector3.Cross(normal, Vector3.right);

        if (capsuleDirection == CapsuleDirection2D.Horizontal)
            tangent = Quaternion.AngleAxis(-90f, normal) * tangent;

        Handles.DrawWireArc(center - offCenterVec, normal, tangent, 180f, halfDiam, thickness);
        Handles.DrawWireArc(center + offCenterVec, normal, -tangent, 180f, halfDiam, thickness);

        Handles.DrawLine(center - offCenterVec - offCenterVecSide, center + offCenterVec - offCenterVecSide, thickness);
        Handles.DrawLine(center - offCenterVec + offCenterVecSide, center + offCenterVec + offCenterVecSide, thickness);
    }
    // public static void DrawCapsule2D(Vector3 center, Vector3 normal, Vector3 from, float angle, float radius, float thickness = 0f)
    // {
    //     if (Event.current.type != EventType.Repaint)
    //         return;

    //     thickness = ThicknessToPixels(thickness);

    //     var mat = SetupArcMaterial();
    //     if (mat == null) // can't do arcs or thick lines (only on GLES2), fallback to thin arc via CPU path
    //     {
    //         SetDiscSectionPoints(s_WireArcPoints, center, normal, from, angle, radius);
    //         Handles.DrawPolyLine(s_WireArcPoints);
    //         return;
    //     }

    //     mat.SetVector(kPropArcCenterRadius, new Vector4(center.x, center.y, center.z, radius));
    //     mat.SetVector(kPropArcNormalAngle, new Vector4(normal.x, normal.y, normal.z, angle * Mathf.Deg2Rad));
    //     mat.SetVector(kPropArcFromCount, new Vector4(from.x, from.y, from.z, kArcSegments));
    //     mat.SetVector(kPropArcThicknessSides, new Vector4(thickness, kArcSides, 0, 0));
    //     mat.SetPass(0);

    //     if (thickness <= 0.0f)
    //         Graphics.DrawProceduralNow(MeshTopology.LineStrip, kArcSegments);
    //     else
    //     {
    //         var indexBuffer = HandleUtility.GetArcIndexBuffer(kArcSegments, kArcSides);
    //         Graphics.DrawProceduralNow(MeshTopology.Triangles, indexBuffer, indexBuffer.count);
    //     }
    // }
}
