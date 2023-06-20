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
    public static bool LineLineIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, ref Vector2 result)
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
    public static bool SegmentSegmentIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, ref Vector2 result)
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

    // Unity's Mathf.cs (modificado para que sea realmente un segmento y una linea)
    // Line Segment Intersection (line1 is p1-p2 and line2 is p3-p4)
    public static bool SegmentLineIntersection(Vector2 segment1, Vector2 segment2, Vector2 line1, Vector2 line2, ref Vector2 result)
    {
        float bx = segment2.x - segment1.x;
        float by = segment2.y - segment1.y;
        float dx = line2.x - line1.x;
        float dy = line2.y - line1.y;
        float bDotDPerp = bx * dy - by * dx;
        if (bDotDPerp == 0)
        {
            return false;
        }
        float cx = line1.x - segment1.x;
        float cy = line1.y - segment1.y;
        float t = (cx * dy - cy * dx) / bDotDPerp;
        if (t < 0 || t > 1)
        {
            return false;
        }
        // igual que segment-segment pero sin limitar el segundo
        // float u = (cx * by - cy * bx) / bDotDPerp;
        // if (u < 0 || u > 1)
        // {
        //     return false;
        // }

        result.x = segment1.x + t * bx;
        result.y = segment1.y + t * by;
        return true;
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
    static object llaveRayoContraCapsulaInspectKey = new object();
    public static bool RayoContraCapsula(Ray2D rayo, Vector2 centroCapsula, Vector2 vectorEspinal, float radioDeCapsula, float mediaLongitudEspinal, ref Vector2 result)
    {
        Plasmar.Tarjeta tarjeta = Plasmar.GetTarjeta(llaveRayoContraCapsulaInspectKey);
        tarjeta.DataCruda.Clear();

        var perpEspinal = Vector2.Perpendicular(vectorEspinal);
        var origenRayoLocal = rayo.origin - centroCapsula;

        var crossEspinaOrigenRayo = (origenRayoLocal.x) * (vectorEspinal.y) - (origenRayoLocal.y) * (vectorEspinal.x);
        var crossEspinaDirRayo = (vectorEspinal.x * rayo.direction.y) - (vectorEspinal.y * rayo.direction.x);

        // usado solo cuando se chequea contra circulo final (indica si queremos el arco cercano el lejano)
        var extremoDelCirculo = -1f;

        tarjeta.DataCruda.Add(("crossEspinaOrigenRayo", crossEspinaOrigenRayo.ToString()));
        tarjeta.DataCruda.Add(("crossEspinaDirRayo", crossEspinaDirRayo.ToString()));

        if (radioDeCapsula > 0f) // el radio tiene que ser mayor a cero, sino es solo un segmento (ver el else)
        {
            if (mediaLongitudEspinal == 0f) // y si la espina es cero, la capsula es un circulo
            {
                // interseccion rayo-circulo!

                //dentroDeCapsula == true, sencillamente chequeando distancia contra radio
            }

            bool ladoPositivoColumna = crossEspinaOrigenRayo > radioDeCapsula;
            bool ladoNegativoColumna = crossEspinaOrigenRayo < -radioDeCapsula;
            if (ladoPositivoColumna || ladoNegativoColumna) // estoy fuera de columna
            {
                tarjeta.DataCruda.Add(("fuera de columna", null));
                if ((crossEspinaDirRayo > 0f) ^ (crossEspinaOrigenRayo > 0f))
                {
                    // el origen esta de un lado y el rayo apunta hacia "afuera" de la columna
                    tarjeta.DataCruda.Add(("rayo apunta para otro lado", null));
                    return false;
                }

                // ya que estoy fuera de la columna principal
                // puedo hacer esto para hacer como un offset
                // de la espina, y hacer un chequeo del segmento
                // que corresponde a que lado estoy de la columna
                // este chequeo se realiza abajo del todo (mismo flow que si la capsula fuera radio cero)
                crossEspinaOrigenRayo += ladoNegativoColumna ? radioDeCapsula : -radioDeCapsula;
            }
            else
            {
                // estoy dentro de la columna, primero me fijo si estoy tocando el circulo "mas cercano"
                tarjeta.DataCruda.Add(("rayo contra apuntado", null));
                var dotEspinaDirRayo = Vector2.Dot(rayo.direction, vectorEspinal);

                var circuloCercano = vectorEspinal * (dotEspinaDirRayo < 0f ? +mediaLongitudEspinal : -mediaLongitudEspinal) - origenRayoLocal;
                tarjeta.DataCruda.Add(("dotEspinaDirRayo", dotEspinaDirRayo.ToString()));
                tarjeta.DataCruda.Add(("circulo cercano", circuloCercano.ToString()));

                var dotCirculoCercanoDirRayo = Vector2.Dot(circuloCercano, rayo.direction);

                var distInternaSq = circuloCercano.sqrMagnitude - dotCirculoCercanoDirRayo * dotCirculoCercanoDirRayo;
                var radioSq = radioDeCapsula * radioDeCapsula;
                if (distInternaSq > radioSq)
                {
                    tarjeta.DataCruda.Add(("sin interseccion", null));
                }
                else
                {
                    var distExterna = dotCirculoCercanoDirRayo - Mathf.Sqrt(radioSq - distInternaSq);

                    if (distExterna > 0f)
                    {
                        result = rayo.origin + rayo.direction * distExterna;
                        return true;
                    }
                    else
                    {
                        tarjeta.DataCruda.Add(("interseccion en lado interno", null));
                    }
                }

                // que hacer respecto al alerta, meter lo anterior en una misma branch "true"
                // ya resolver la interseccion lado interno + sin interseccion para el caso de seguir
                // generar offset basado en cross dir
                crossEspinaOrigenRayo += crossEspinaDirRayo > 0f ? radioDeCapsula : -radioDeCapsula;
                if (crossEspinaDirRayo == 0f) crossEspinaOrigenRayo *= -1f;// rayo paralelo a espina
                extremoDelCirculo = 1f;

            }
        }
        else if (mediaLongitudEspinal == 0f) // si la espina es cero (y el radio tambien), la capsula es un punto
        {
            // emm...
            return false; // creo que podemos asumir que nunca la va a tocar (aunque eso no esta del todo bien)
            // podria fijarme si el origen es igual al centro... o algo asi
            // if (rayo.origin == centroCapsula) return true;
        }
        else // (esto es un segmento)
        {
            if (crossEspinaOrigenRayo == 0f) // el origen del rayo es colinear a la columna!
            { // puede haber colision (ver abajo)
                // if (origen tocando segmento) // return true
                // else if (dir rayo paralelo  a columna, y apuntando hacia centro de segmento) // return true
                // else (no toca y apunta para el otro lado, o direccion no es paralela a segmento)
                tarjeta.DataCruda.Add(("origen colineal", null));
                return false;
            }
            else if (crossEspinaDirRayo == 0f)
            { // rayo es paralelo y origen no sobre la lina del segmento
                tarjeta.DataCruda.Add(("rayo paralelo, origen no colineal", null));
                return false;
            }
            else if ((crossEspinaDirRayo > 0f) ^ (crossEspinaOrigenRayo > 0f))
            { // el origen esta de un lado y el rayo apunta hacia "afuera" del segmento
                tarjeta.DataCruda.Add(("rayo apunta para otro lado", null));
                return false;
            }
        }

        // tiene radio, y o bien ya chequeamos si esta adentro, o esta afuera

        // rayo vs segmento, veamos si puedo aprovechar las variables ya definidas
        tarjeta.DataCruda.Add(("rayo-segmento posible", null));

        // este calculo seguro se puede mejorar, sobretodo esa parte de dot espina cross espina
        // crossEspinaOrigenRayo es un escalar
        var nuevaPos = origenRayoLocal + rayo.direction * crossEspinaOrigenRayo / crossEspinaDirRayo;
        var dotColumna = Vector2.Dot(nuevaPos, vectorEspinal);
        tarjeta.DataCruda.Add(("dotColumna", dotColumna.ToString()));
        bool trasExtremoPositivo = dotColumna > mediaLongitudEspinal;
        bool trasExtremoNegativo = dotColumna < -mediaLongitudEspinal;
        if (trasExtremoPositivo || trasExtremoNegativo)
        {
            tarjeta.DataCruda.Add(("interseccion cae fuera de segmento", null));

            if (radioDeCapsula > 0f)
            {
                var circuloCercano = vectorEspinal * (trasExtremoPositivo ? +mediaLongitudEspinal : -mediaLongitudEspinal) - origenRayoLocal;
                tarjeta.DataCruda.Add(("probando contra circulo", circuloCercano.ToString()));
                var dotCirculoCercanoDirRayo = Vector2.Dot(circuloCercano, rayo.direction);
                var distInternaSq = circuloCercano.sqrMagnitude - dotCirculoCercanoDirRayo * dotCirculoCercanoDirRayo;
                var radioSq = radioDeCapsula * radioDeCapsula;
                if (distInternaSq > radioSq)
                {
                    tarjeta.DataCruda.Add(("sin interseccion", null));
                    return false;
                }

                var distExterna = dotCirculoCercanoDirRayo + Mathf.Sqrt(radioSq - distInternaSq) * extremoDelCirculo;
                if (distExterna > 0f)
                {
                    result = rayo.origin + rayo.direction * distExterna;
                    return true;
                }
            }

            return false;
        }

        result = nuevaPos + centroCapsula;
        return true;
    }
}
