using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class Fisica
{
    public static readonly RaycastHit2D[] CacheHit2DSoloPrimero = new RaycastHit2D[1];
    public static readonly RaycastHit2D[] CacheHit2DMultiple = new RaycastHit2D[10];

    public static readonly ContactFilter2D FiltroParedIzquierda = new ContactFilter2D()
    {
        useNormalAngle = true,
        useOutsideNormalAngle = true,
        minNormalAngle = 45f,
        maxNormalAngle = 315f,
    };
    public static readonly ContactFilter2D FiltroParedDerecha = new ContactFilter2D()
    {
        useNormalAngle = true,
        minNormalAngle = 135f,
        maxNormalAngle = 225f,
    };
    public static readonly ContactFilter2D FiltroSuelo = new ContactFilter2D()
    {
        useNormalAngle = true,
        minNormalAngle = 45f,
        maxNormalAngle = 135f,
    };
    public static readonly ContactFilter2D FiltroTecho = new ContactFilter2D()
    {
        useNormalAngle = true,
        minNormalAngle = 225f,
        maxNormalAngle = 315f,
    };

    // public struct DataProyeccion
    // {
    //     public Vector2 TamCapsula;
    //     public CapsuleDirection2D DirCapsula;
    //     public float AnguloCapsula;

    //     public float FactorEscalon;

    //     public Vector2 PosicionOriginal;
    //     public Vector2 VelocidadOriginal;
    //     // public bool EnSuelo;
    //     public Vector2 Gravedad;

    //     public float MaxPaso;
    //     public float TiempoRestante;
    //     public bool Interrumpido;
    // }

    // public static DataProyeccion Proyeccion(DataProyeccion proy)
    // {
    //     bool buscarSuelo = proy.VelocidadOriginal.y <= 0f;
    //     Vector2 origen = proy.PosicionOriginal;
    //     Vector2 dirSuelo = Vector2.down;
    //     float distSueloInmediato = 0.001f;

    //     // buscar suelo inicial
    //     int casos = Physics2D.CapsuleCast(origen, proy.TamCapsula, proy.DirCapsula, proy.AnguloCapsula, dirSuelo, FiltroSuelo, CacheHit2DSoloPrimero, distSueloInmediato);
    //     bool sinSuelo = casos == 0;
    //     if (casos > 0)
    //     {

    //     }

    //     return proy;
    // }

    public static Vector2 DesenterrarCapsula(Vector2 capsOrig, Vector2 capsTam, CapsuleDirection2D capsDir, float anguloMaximoDeNormal = 45f, bool salidaBiLateral = false, bool soloExtraerPorVectorDeSalida = false)
        => DesenterrarCapsula(capsOrig, capsTam, capsDir, Vector2.up, anguloMaximoDeNormal, salidaBiLateral, soloExtraerPorVectorDeSalida);

    public static Vector2 DesenterrarCapsula(Vector2 capsOrig, Vector2 capsTam, CapsuleDirection2D capsDir, RaycastHit2D hit, float anguloMaximoDeNormal = 45f, bool salidaBiLateral = false, bool soloExtraerPorVectorDeSalida = false)
        => DesenterrarCapsula(capsOrig, capsTam, capsDir, Vector2.up, hit, anguloMaximoDeNormal, salidaBiLateral, soloExtraerPorVectorDeSalida);

    public static Vector2 DesenterrarCapsula(Vector2 capsOrig, Vector2 capsTam, CapsuleDirection2D capsDir, Vector2 vectorDeSalidaLineal, float anguloMaximoDeNormal = 45f, bool salidaBiLateral = false, bool soloExtraerPorVectorDeSalida = false)
    {
        int casos = Physics2D.CapsuleCast(capsOrig, capsTam, capsDir, angle: 0f, -vectorDeSalidaLineal, new ContactFilter2D(), CacheHit2DSoloPrimero, distance: 0f);

        if (casos > 0)
            return DesenterrarCapsula(capsOrig, capsTam, capsDir, vectorDeSalidaLineal, CacheHit2DSoloPrimero[0], anguloMaximoDeNormal, salidaBiLateral, soloExtraerPorVectorDeSalida);
        else
            return capsOrig;
    }

    public static Vector2 DesenterrarCapsula(Vector2 capsOrig, Vector2 capsTam, CapsuleDirection2D capsDir, Vector2 vectorDeSalidaLineal, RaycastHit2D hit, float anguloMaximoDeNormal = 45f, bool salidaBiLateral = false, bool soloExtraerPorVectorDeSalida = false)
    {
        var anguloDeSalida = Vector2.Angle(vectorDeSalidaLineal, hit.normal);
        if (salidaBiLateral && anguloDeSalida > 90f)
        {
            anguloDeSalida = 180f - anguloDeSalida;
            vectorDeSalidaLineal *= -1f;
        }

        // salida en eje
        if (anguloDeSalida <= anguloMaximoDeNormal)
        {
            var radioCapsula = (capsDir == CapsuleDirection2D.Vertical ? capsTam.x : capsTam.y) / 2f;
            var mediaAlturaColumnaCapsula = (capsDir == CapsuleDirection2D.Vertical ? capsTam.y : capsTam.x) / 2f - radioCapsula;
            if (mediaAlturaColumnaCapsula < 0f)
                mediaAlturaColumnaCapsula = 0f;

            var capsEjeColumna = (capsDir == CapsuleDirection2D.Vertical ? Vector2.up : Vector2.right);//esto ignora el posible "angle" de la capsula
            // var capsVerticeNegativo = capsOrig - capsEjeColumna * mediaAlturaColumnaCapsula;
            // var capsVerticePositivo = capsOrig + capsEjeColumna * mediaAlturaColumnaCapsula;

            if (hit.collider is CircleCollider2D circleCollider)
            {
                var centroCollider = (Vector2)circleCollider.bounds.center;
                var radioCollider = circleCollider.bounds.extents.x;
                var radioSumSq = radioCollider + radioCapsula;
                radioSumSq *= radioSumSq;

                // usar el dot del hit normal y la columna para saber eje de circulo capsula que quiero
                var dot = Vector2.Dot(capsEjeColumna, hit.normal); // que basicamente es la proyeccion de un vector sobre otro ¿ok?
                var signoDelDot = dot >= 0 ? -1 : 1;
                var centroDeCirculoDeSalida = capsOrig + signoDelDot * capsEjeColumna * mediaAlturaColumnaCapsula;
                if (dot == 0f)
                    centroDeCirculoDeSalida.y = centroCollider.y;

                var posCentroCapsulaRespectoCollider = centroDeCirculoDeSalida - centroCollider;
                // perpendicular a salida linal (debe ser unit vector!)
                var difPerpendicularSq = Vector2.Dot(Vector2.Perpendicular(vectorDeSalidaLineal), posCentroCapsulaRespectoCollider);
                // var xDifSq = capsVerticeNegativo[perpendicularAMovilidad] - centroCollider[perpendicularAMovilidad];
                difPerpendicularSq *= difPerpendicularSq;

                // paralelo a salida lineal
                var difParalelo = Vector2.Dot(vectorDeSalidaLineal, posCentroCapsulaRespectoCollider);
                // var yDif = Mathf.Abs(capsVerticeNegativo[ejeDeMovilidad] - centroCollider[ejeDeMovilidad]);
                var deltaParaNuevaPos = Mathf.Sqrt(radioSumSq - difPerpendicularSq) - difParalelo;

                return capsOrig + vectorDeSalidaLineal * deltaParaNuevaPos;
            }
            else
            {

            }

            return capsOrig;
        }
        else if (!soloExtraerPorVectorDeSalida)// salida comun por la normal
        {
            return DesenterrarCapsulaPorNormal(capsOrig, capsTam, capsDir, new Ray2D(hit.point, hit.normal));
        }
        else
        {
            return capsOrig;
        }
    }

    public static Vector2 DesenterrarCapsulaPorNormal(Vector2 capsOrig, Vector2 capsTam, CapsuleDirection2D capsDir, Ray2D normal)
    {
        var radioCapsula = (capsDir == CapsuleDirection2D.Vertical ? capsTam.x : capsTam.y) / 2f;
        var mediaAlturaColumnaCapsula = (capsDir == CapsuleDirection2D.Vertical ? capsTam.y : capsTam.x) / 2f - radioCapsula;
        if (mediaAlturaColumnaCapsula < 0f)
            mediaAlturaColumnaCapsula = 0f;

        var capsEjeColumna = (capsDir == CapsuleDirection2D.Vertical ? Vector2.up : Vector2.right);//esto ignora el posible "angle" de la capsula
        var capsVerticeNegativo = capsOrig - capsEjeColumna * mediaAlturaColumnaCapsula;
        var capsVerticePositivo = capsOrig + capsEjeColumna * mediaAlturaColumnaCapsula;

        var puntoExternoACapsula = normal.origin - Mathf.Max(capsTam.x, capsTam.y) * normal.direction;
        var distPuntoExterno = Fisica.DistancePointToLineSegment(puntoExternoACapsula, capsVerticePositivo, capsVerticeNegativo) - radioCapsula;

        Vector2 puntoEnCapsula = puntoExternoACapsula + distPuntoExterno * normal.direction;
        var dobleDistPenetracion = Vector2.Distance(puntoEnCapsula, normal.origin) * 2f;

        return capsOrig + dobleDistPenetracion * normal.direction;
    }

    // deberia devolver un bool "habia overlap" y luego el out (o sino, un objeto mas complejo con mas data, saber si salio para el piso o para otro lado)
    public static Vector2 DesenterrarCapsula(Vector2 capsOrig, Vector2 capsTam, CapsuleDirection2D capsDir, bool esLateral = false, float angulo = 45f)
    {
        // un "angulo" mayor a 90° trae problemas
        int casos = Physics2D.CapsuleCast(capsOrig, capsTam, capsDir, angle: 0f, Vector2.down, new ContactFilter2D(), CacheHit2DSoloPrimero, distance: 0f);

        if (casos > 0)
            return DesenterrarCapsula(capsOrig, capsTam, capsDir, CacheHit2DSoloPrimero[0], esLateral, angulo);
        else
            return capsOrig;
    }

    public static Vector2 DesenterrarCapsula(Vector2 capsOrig, Vector2 capsTam, CapsuleDirection2D capsDir, RaycastHit2D hit, bool esLateral = false, float angulo = 45f)
    {
        // un "angulo" mayor a 90° trae problemas
        var anguloDeSalida = Vector2.Angle(Vector2.up, hit.normal);
        var radioCapsula = (capsDir == CapsuleDirection2D.Vertical ? capsTam.x : capsTam.y) / 2f;
        var mediaAlturaColumnaCapsula = (capsDir == CapsuleDirection2D.Vertical ? capsTam.y : capsTam.x) / 2f - radioCapsula;
        if (mediaAlturaColumnaCapsula < 0f)
            mediaAlturaColumnaCapsula = 0f;

        var capsEjeColumna = (capsDir == CapsuleDirection2D.Vertical ? Vector2.up : Vector2.right);//esto no es tan real
        var capsVerticeNegativo = capsOrig - capsEjeColumna * mediaAlturaColumnaCapsula;

        // expulsion por la normal
        bool expulsionPorLaNormal = esLateral ? (anguloDeSalida < 90f - angulo || anguloDeSalida > 90f + angulo) : anguloDeSalida > angulo;
        if (expulsionPorLaNormal)
        {
            if (esLateral)
                return capsOrig;

            var capsVerticePositivo = capsOrig + capsEjeColumna * mediaAlturaColumnaCapsula;
            var puntoExternoACapsula = hit.point - Mathf.Max(capsTam.x, capsTam.y) * hit.normal;
            var distPuntoExterno = Fisica.DistancePointToLineSegment(puntoExternoACapsula, capsVerticePositivo, capsVerticeNegativo) - radioCapsula;

            Vector2 puntoEnCapsula = puntoExternoACapsula + distPuntoExterno * hit.normal;
            var dobleDistPenetracion = Vector2.Distance(puntoEnCapsula, hit.point) * 2f;

            return capsOrig + dobleDistPenetracion * hit.normal;
        }
        else // es piso, expulsion para "arriba"
        {
            int ejeDeMovilidad = esLateral ? 0 : 1;
            int perpendicularAMovilidad = esLateral ? 1 : 0;

            if (hit.collider is CircleCollider2D circle)
            {
                var centroCollider = circle.bounds.center;
                var radioCollider = circle.bounds.extents.x;
                var radioSumSq = radioCollider + radioCapsula;
                radioSumSq *= radioSumSq;

                var direccionDeExpulsion = Vector2.up;
                if (esLateral)
                {
                    direccionDeExpulsion.x = capsOrig[ejeDeMovilidad] > centroCollider[ejeDeMovilidad] ? 1f : -1f;
                    direccionDeExpulsion.y = 0f;
                    if (capsOrig[perpendicularAMovilidad] < centroCollider[perpendicularAMovilidad])
                        capsVerticeNegativo[perpendicularAMovilidad] += mediaAlturaColumnaCapsula * 2f;
                }

                var xDifSq = capsVerticeNegativo[perpendicularAMovilidad] - centroCollider[perpendicularAMovilidad];
                xDifSq *= xDifSq;

                var yDif = Mathf.Abs(capsVerticeNegativo[ejeDeMovilidad] - centroCollider[ejeDeMovilidad]);
                var dy = Mathf.Sqrt(radioSumSq - xDifSq) - yDif;

                return capsOrig + direccionDeExpulsion * dy;
            }
            else // para capsula puedo ver la normal y decidir desde ahi (si es perpendicular a la capsula, tratar de lineal)
            {
                var direccionDeExpulsion = Vector2.up;
                if (esLateral)
                {
                    direccionDeExpulsion.x = hit.normal.x > 0f ? 1f : -1f;
                    direccionDeExpulsion.y = 0f;
                    if (hit.normal.y < 0f)
                        capsVerticeNegativo[perpendicularAMovilidad] += mediaAlturaColumnaCapsula * 2f;
                }

                Vector3 puntoEnCapsula = capsVerticeNegativo - radioCapsula * hit.normal;
                float distBordeColision = Vector2.Distance(hit.point, puntoEnCapsula);
                float anguloAdyacente = Vector2.Angle(hit.normal, direccionDeExpulsion) * Mathf.Deg2Rad;
                float hipotenusa = distBordeColision * 2f / Mathf.Cos(anguloAdyacente);

                return capsOrig + direccionDeExpulsion * hipotenusa;
            }
        }
    }

    // Unity's Mathf.cs
    // Infinite Line Intersection (line1 is p1-p2 and line2 is p3-p4)
    public static bool LineIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, ref Vector2 result)
    {
        float bx = p2.x - p1.x;
        float by = p2.y - p1.y;
        float dx = p4.x - p3.x;
        float dy = p4.y - p3.y;
        float bDotDPerp = bx * dy - by * dx;
        if (bDotDPerp == 0)
        {
            return false;
        }
        float cx = p3.x - p1.x;
        float cy = p3.y - p1.y;
        float t = (cx * dy - cy * dx) / bDotDPerp;

        result.x = p1.x + t * bx;
        result.y = p1.y + t * by;
        return true;
    }

    // Unity's Mathf.cs
    // Line Segment Intersection (line1 is p1-p2 and line2 is p3-p4)
    public static bool LineSegmentIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, ref Vector2 result)
    {
        float bx = p2.x - p1.x;
        float by = p2.y - p1.y;
        float dx = p4.x - p3.x;
        float dy = p4.y - p3.y;
        float bDotDPerp = bx * dy - by * dx;
        if (bDotDPerp == 0)
        {
            return false;
        }
        float cx = p3.x - p1.x;
        float cy = p3.y - p1.y;
        float t = (cx * dy - cy * dx) / bDotDPerp;
        if (t < 0 || t > 1)
        {
            return false;
        }
        float u = (cx * by - cy * bx) / bDotDPerp;
        if (u < 0 || u > 1)
        {
            return false;
        }

        result.x = p1.x + t * bx;
        result.y = p1.y + t * by;
        return true;
    }

    // Unity's Mathf.cs
    // Line Segment Intersection (line1 is p1-p2 and line2 is p3-p4)
    public static bool LineCircleIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, ref Vector2 result)
    {
        return false;
    }

    // Unity's Mathf.cs
    // Distance from a point /p/ in 2d to a line segment defined by two s_Points /a/ and /b/
    public static float DistancePointToLineSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        float l2 = (b - a).sqrMagnitude;    // i.e. |b-a|^2 -  avoid a sqrt
        if (l2 == 0.0)
            return (p - a).magnitude;       // a == b case
        float t = Vector2.Dot(p - a, b - a) / l2;
        if (t < 0.0)
            return (p - a).magnitude;       // Beyond the 'a' end of the segment
        if (t > 1.0)
            return (p - b).magnitude;         // Beyond the 'b' end of the segment
        Vector2 projection = a + t * (b - a); // Projection falls on the segment
        return (p - projection).magnitude;
    }

    // Unity's HandleUtility.cs
    // Calculate distance between a point and a line.
    public static float DistancePointLine(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        return (ProjectPointLine(point, lineStart, lineEnd) - point).magnitude;
    }

    // Unity's HandleUtility.cs
    // Project /point/ onto a line.
    public static Vector2 ProjectPointLine(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 relativePoint = point - lineStart;
        Vector2 lineDirection = lineEnd - lineStart;
        float length = lineDirection.magnitude;
        Vector2 normalizedLineDirection = lineDirection.normalized;
        // if (length > .000001f) // nah es necesaario
        //     normalizedLineDirection /= length;

        float dot = Vector2.Dot(normalizedLineDirection, relativePoint);
        dot = Mathf.Clamp(dot, 0.0F, length); // line segment!

        return lineStart + normalizedLineDirection * dot;
    }

    // inspirado en Unity's HandleUtility.cs
    // Project /point/ onto a capsule surface.
    ///<summary> el punto es relativo al centro de la capsula
    /// la orientacion de la capsula la sacamos del vector espinal.
    /// </summary>
    ///<returns>Devuelve un punto relativo</returns>
    public static Vector2 ProjectPointCapsule(Vector2 puntoRelativo, Vector2 vectorEspinal, float radioDeCapsula, float mediaLongitudEspinal)
    {
        if (mediaLongitudEspinal <= 0f) // es un circulo nomas
        {
            // ¿es un punto?
            if (radioDeCapsula <= 0f)
                return Vector2.zero;

            return puntoRelativo.normalized * radioDeCapsula;
        }
        else
        {
            float dot = Vector2.Dot(vectorEspinal, puntoRelativo);

            // ¿la capsula es un segmento sin grosor en realidad?
            if (radioDeCapsula <= 0f)
                return vectorEspinal * Mathf.Clamp(dot, -mediaLongitudEspinal, mediaLongitudEspinal);

            if (dot <= -mediaLongitudEspinal) // extremo inferior
            {
                // circulo con centro en el extremo negativo
                var centroExtremoInferior = -vectorEspinal * mediaLongitudEspinal;
                puntoRelativo -= centroExtremoInferior;
                return puntoRelativo.normalized * radioDeCapsula + centroExtremoInferior;
            }
            else if (dot >= mediaLongitudEspinal) // extremo superior
            {
                // circulo con centro en el extremo positivo
                var centroExtremoSuperior = vectorEspinal * mediaLongitudEspinal;
                puntoRelativo -= centroExtremoSuperior;
                return puntoRelativo.normalized * radioDeCapsula + centroExtremoSuperior;
            }
            else
            { // proyectado sobre la espina
                var puntoSobreEspina = vectorEspinal * dot;

                var perpendicularAEspina = Vector2.Perpendicular(vectorEspinal);
                // ¿esta del lado "negativo" de la espina?
                if (Vector2.Dot(perpendicularAEspina, puntoRelativo) < 0f)
                    perpendicularAEspina *= -1f;

                return puntoSobreEspina + perpendicularAEspina * radioDeCapsula;
            }
        }
    }

    // inspirado por Unity's Mathf.cs y HandleUtility.cs
    ///<summary> el rayo esta en coordenadas globales
    /// la orientacion de la capsula la sacamos del vector espinal.
    /// </summary>
    ///<returns>Devuelve global sobre la superficie de la capsula</returns>
    // public static bool RayAgainstCapsule(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, ref Vector2 result)
    public static bool RayoContraCapsula(Ray2D rayo, Vector2 centroCapsula, Vector2 vectorEspinal, float radioDeCapsula, float mediaLongitudEspinal, ref Vector2 result)
    {
        var perpEspinal = Vector2.Perpendicular(vectorEspinal);
        var origenRayoLocal = centroCapsula-rayo.origin;

        var centroDeSegmentoLateralCercano = centroCapsula;

        // bool dentroDeRiel = 
        var crossEspinaRayo = (vectorEspinal.x)*(origenRayoLocal.y) - (vectorEspinal.y)*(origenRayoLocal.x);
        var crossEspinaRayoDir = (vectorEspinal.x*rayo.direction.y)-(vectorEspinal.y*rayo.direction.x);

        if (radioDeCapsula > 0f) // si el radio es menor que cero, la capsula es un segmento
        {
            // emm lo que estoy haciendo acá es detectar de que lado esta el origen del rayo
            // y agarrar el segmento espinal y desplazarlo perpendicularmente hasta el lado de la capsula
            // de esa manera ya puedo estar seguro si el rayo cae sobre uno de los circulos o sobre uno de los lados
            var crossProdOrigenRayo = ((vectorEspinal.x) * (rayo.origin.y - centroCapsula.y) - (vectorEspinal.y) * (rayo.origin.x - centroCapsula.x));
            // si el cross es == 0f, tonces es colinear
            var magnitudOffset = crossProdOrigenRayo > 0f ? radioDeCapsula : -radioDeCapsula;
            centroDeSegmentoLateralCercano.x += -vectorEspinal.y * magnitudOffset;
            centroDeSegmentoLateralCercano.y += vectorEspinal.x * magnitudOffset;
        }

        var p1 = centroDeSegmentoLateralCercano + vectorEspinal * mediaLongitudEspinal;
        var p2 = centroDeSegmentoLateralCercano - vectorEspinal * mediaLongitudEspinal;
        var p3 = rayo.origin;
        var p4 = rayo.origin + rayo.direction;

        float bx = p2.x - p1.x;
        float by = p2.y - p1.y;
        float dx = p4.x - p3.x;
        float dy = p4.y - p3.y;
        float bDotDPerp = bx * dy - by * dx;
        if (bDotDPerp == 0) // chequeo si son paralelos
        {
            return false;
        }

        float cx = p3.x - p1.x;
        float cy = p3.y - p1.y;

        float u = (cx * by - cy * bx) / bDotDPerp;
        // u = Mathf.Clamp(u,0f,1f);
        // if (u < 0) // rayo apunta para el otro lado (seguro puedo hacer este return antes si pienso un poco)
        // {
        //     return false;
        // }

        float t = (cx * dy - cy * dx) / bDotDPerp;
        // t = Mathf.Clamp(t, 0f, 1f); // limito rayo los extremos, 
        if (t < 0 || t > 1) // la interseccion cae fuera del segmento espinal (aun debo chequear la distancia con el circulo)
        {
            // return false;
            var circuloCercano = centroCapsula + vectorEspinal * mediaLongitudEspinal * (t <= 0?1:-1) - rayo.origin;
            var proyectado = Vector2.Dot(circuloCercano, rayo.direction);
            var distInternaSq = circuloCercano.sqrMagnitude-proyectado*proyectado;
            var radioSq = radioDeCapsula*radioDeCapsula;
            if (distInternaSq > radioSq)
                return false;

            var distExterna = proyectado-Mathf.Sqrt(radioSq-distInternaSq);
            // if (distExterna > 0f)
            {
                result = rayo.origin + rayo.direction * distExterna;
                return true;
            }
            // else return false;
        }

        result.x = p1.x + t * bx;
        result.y = p1.y + t * by;
        return true;
    }

    
}
