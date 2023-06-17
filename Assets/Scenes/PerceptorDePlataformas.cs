using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PerceptorDePlataformas : MonoBehaviour
{
    private static RaycastHit2D[] _hitsCache = new RaycastHit2D[10];
    private static List<ResultadoPaso> _cacheDePasos = new(100);

    [SerializeField]
    private float _maxStepDist = .01f;
    [SerializeField]
    private float _tiempoSimulado = 1f;
    [SerializeField]
    private float _velocidadLateral = 1f;

    [SerializeField]
    private Vector2 _capsuleSize = new Vector2(1f, 1f);
    [SerializeField]
    private CapsuleDirection2D _capsuleDirection = CapsuleDirection2D.Vertical;
    [SerializeField]
    private float _porcentajeTrepa = .1f;
    [SerializeField]
    private float _distPisoInmediato = .02f;
    [SerializeField]
    private float _distBuscoPisoSnap = .2f;
    [SerializeField, System.Obsolete]
    private ContactFilter2D _leftContact, _rightContact, _filtroPiso;

    private struct ResultadoPaso
    {
        public enum Tipo
        {
            DesenterrarInicial,
            SnapPiso,
            DesentierroDePiso,
            AvanzoIninterrumpido,
            ChocoParedOEscalon
        }
        public Vector2 posPrincipal;
        public Vector2 posSecundaria;
        public Vector2 vector;

        public Tipo tipo;

        [System.Obsolete]
        public RaycastHit2D[] colisiones;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green * 0.25f;
        Gizmos.DrawCube(transform.position, new Vector3(_capsuleSize.x, _capsuleSize.y, 0f));
    }

    private List<ResultadoPaso> Proyectar()
    {
        _cacheDePasos.Clear();

        var posActual = (Vector2)transform.position;
        var tam = _capsuleSize;
        var dir = _capsuleDirection;
        ResultadoPaso resultadoPaso = new ResultadoPaso()
        {
            tipo = ResultadoPaso.Tipo.DesenterrarInicial,
            posPrincipal = posActual,
        };

        // primero desenterrar por las dudas
        posActual = Fisica.DesenterrarCapsula(posActual, tam, dir, esLateral: false);
        resultadoPaso.posSecundaria = posActual;
        _cacheDePasos.Add(resultadoPaso);

        Vector2 pendiente = Vector2.right;
        float velocidadVertical = 0f;
        // enpisar (solo si no estoy subiendo?)          >>>>>>>>>>>>>??<<<<<<<<<<<<<

        // NOTA: sobre uso del filtro aqui... podria suceder que haya una pared "antes" que el piso, pero despues de haber desentarrado
        // esa situacion seria extraña ademas de ser menor a la distancia del "escalon"
        int toques = Physics2D.CapsuleCast(posActual, tam, dir, angle: 0f, Vector2.down, Fisica.FiltroSuelo, Fisica.CacheHit2DSoloPrimero, _distPisoInmediato);
        Collider2D sueloActual = null;
        if (toques > 0)
        {
            sueloActual = Fisica.CacheHit2DSoloPrimero[0].collider;
            velocidadVertical = 0f;
            pendiente = Vector2.Perpendicular(Fisica.CacheHit2DSoloPrimero[0].normal);
            if (Fisica.CacheHit2DSoloPrimero[0].distance > 0f)
            {//bajar al piso (snap)
                posActual += Vector2.down * Fisica.CacheHit2DSoloPrimero[0].distance;
            }

            resultadoPaso = new ResultadoPaso()
            {
                tipo = ResultadoPaso.Tipo.SnapPiso,
                posPrincipal = posActual,
                vector = pendiente,
            };
            _cacheDePasos.Add(resultadoPaso);
            // else // -------------------------------------- no necesario la primera vez (?)
            // {
            //     //desenterrar del piso
            //     posActual = Fisica.DesenterrarCapsula(posActual, tam, dir, Fisica.CacheHit2DSoloPrimero[0]);
            //     resultadoPaso = new ResultadoPaso()
            //     {
            //         tipo = ResultadoPaso.Tipo.DesentierroDePiso,
            //         posPrincipal = posActual,
            //     };
            // }
        }
        // else no hay suelo (la pendiente tendria que ya tener la gravedad incluida?)  no, caida solo sucede cuando no hay pendiente>>>>>>>>>>>>>??<<<<<<<<<<<<<
        // ¿y sino ejecuto una caida acá?                                                       >>>>>>>>>>>>>??<<<<<<<<<<<<<

        float porcentajeTrepaTam = 1f - _porcentajeTrepa;
        Vector2 capsulaSizeTrepa = new Vector2(_capsuleSize.x, _capsuleSize.y * porcentajeTrepaTam); // ¿deberia depender direccion capsula?
        Vector2 offsetCentroNormalCentroTrepa = Vector2.up * _capsuleSize.y * _porcentajeTrepa / 2f; // IDEM

        // la pendiente tiene que apuntar hacia donde nos movemos
        if (pendiente.x == 0f)
        {
            Debug.LogError($"la pendiente no puede tener X zero, como hizo para registrar un piso en primer lugar??");
            pendiente = _velocidadLateral < 0f ? Vector2.left : Vector2.right;
        }
        else if (pendiente.x * _velocidadLateral < 0f)
            pendiente *= -1f;

        var filtroParedActivo = _velocidadLateral > 0f ? Fisica.FiltroParedDerecha : Fisica.FiltroParedIzquierda;

        var tiempoRestante = _tiempoSimulado; // <- ojo con esto
        while (tiempoRestante > 0f && _maxStepDist > 0f && _velocidadLateral != 0f)
        {
            // avanzo hasta pared u escalon
            //float maxStepHorizontal = 
            toques = Physics2D.CapsuleCast(posActual, tam, dir, angle: 0f, pendiente, filtroParedActivo, Fisica.CacheHit2DSoloPrimero, _maxStepDist);
            float tiempoTranscurrido = 0f; // MUCHO OJO CON ESTO!
            if (toques == 0)
            { // sin interrupcion
                var vectorRecorrido = pendiente * _maxStepDist;

                posActual += vectorRecorrido;
                tiempoTranscurrido = vectorRecorrido.x / _velocidadLateral;

                resultadoPaso = new ResultadoPaso()
                {
                    tipo = ResultadoPaso.Tipo.AvanzoIninterrumpido,
                    posPrincipal = posActual,
                    vector = pendiente,
                };
                _cacheDePasos.Add(resultadoPaso);
            }
            else
            { // encontramos pared u escalon
                var distanciaRecorrida = Fisica.CacheHit2DSoloPrimero[0].distance;
                var vectorRecorrido = pendiente * distanciaRecorrida;

                posActual += vectorRecorrido;
                resultadoPaso = new ResultadoPaso()
                {
                    tipo = ResultadoPaso.Tipo.ChocoParedOEscalon,
                    posPrincipal = posActual,
                };

                tiempoTranscurrido = vectorRecorrido.x / _velocidadLateral;

                if (distanciaRecorrida == 0f) // SINO, <= 0.001f
                { } // me aplaste contra la pared/escalon

                // intentare subir escalon (¿en el proximo step tal vez?)          >>>>>>>>>>>>>??<<<<<<<<<<<<<
                var posCapsulaEscalon = posActual + offsetCentroNormalCentroTrepa;
                // VER QUE ONDA ESTO DESPUES //pendiente = Vector2.right;
                toques = Physics2D.CapsuleCast(posCapsulaEscalon, capsulaSizeTrepa, dir, angle: 0f, pendiente, filtroParedActivo, Fisica.CacheHit2DSoloPrimero, _maxStepDist);

                resultadoPaso.posSecundaria = posCapsulaEscalon + pendiente * _maxStepDist;
                _cacheDePasos.Add(resultadoPaso);
                if (toques > 0)
                { // ES UNA PARED, me freno (¿y caigo?)
                  // agrego tiempo transcurrido // esto lo estoy haciendo despues al parecer
                }
                else
                { // era un escalon
                    // me snapeo al piso nuevo (¿o caigo?) // o mas bien busco nuevo piso y veo si "entro"
                    vectorRecorrido = pendiente * _maxStepDist;
                    posActual += vectorRecorrido;
                    tiempoTranscurrido += vectorRecorrido.x / _velocidadLateral;

                    //posCapsulaEscalon += vectorRecorrido - offsetCentroNormalCentroTrepa;
                    toques = Physics2D.CapsuleCast(posActual, tam, dir, angle: 0f, Vector2.down, Fisica.FiltroSuelo, Fisica.CacheHit2DSoloPrimero, _distPisoInmediato);
                    if (toques > 0)
                    {
                        sueloActual = Fisica.CacheHit2DSoloPrimero[0].collider;
                        velocidadVertical = 0f;
                        pendiente = Vector2.Perpendicular(Fisica.CacheHit2DSoloPrimero[0].normal);
                        if (Fisica.CacheHit2DSoloPrimero[0].distance > 0f)
                        {//bajar al piso (snap)
                            posActual = posCapsulaEscalon + Vector2.down * Fisica.CacheHit2DSoloPrimero[0].distance;
                            resultadoPaso = new ResultadoPaso()
                            {
                                tipo = ResultadoPaso.Tipo.SnapPiso,
                                posPrincipal = posCapsulaEscalon,
                            };
                        }
                        // else
                        // {
                        //     //desenterrar del piso
                        //     posActual = Fisica.DesenterrarCapsula(posActual, tam, dir, Fisica.CacheHit2DSoloPrimero[0]);
                        //     resultadoPaso = new ResultadoPaso()
                        //     {
                        //         tipo = ResultadoPaso.Tipo.DesentierroDePiso,
                        //         posPrincipal = posActual,
                        //     };
                        // }

                        _cacheDePasos.Add(resultadoPaso);
                    }
                    else // era un escalon pero del otro lado no hay asidero
                    {
                        sueloActual = null;
                        pendiente = _velocidadLateral < 0f ? Vector2.left : Vector2.right;

                    }
                    posActual = Fisica.DesenterrarCapsula(posActual, tam, dir, esLateral: false);
                    resultadoPaso = new ResultadoPaso()
                    {
                        tipo = ResultadoPaso.Tipo.DesentierroDePiso,
                        posPrincipal = posActual,
                    };
                    _cacheDePasos.Add(resultadoPaso);
                    // agrego tiempo transcurrido
                    // continuo sobre nuevo piso/aire
                }

            }

            // tal vez tiempo transcurrido debo calcularlo recien ahora basado en cuanto y efectivamente me moví?
            // busco nuevo piso (si estoy en el aire, no busco lejos, si estoy en suelo busco escalon que baja)
            var distBuscoPiso = sueloActual == null ? _distPisoInmediato + velocidadVertical : _distBuscoPisoSnap;
            toques = Physics2D.CapsuleCast(posActual, tam, dir, angle: 0f, Vector2.down, Fisica.FiltroSuelo, Fisica.CacheHit2DSoloPrimero, distBuscoPiso);
            if (toques > 0)
            {
                sueloActual = Fisica.CacheHit2DSoloPrimero[0].collider;
                velocidadVertical = 0f;
                pendiente = Vector2.Perpendicular(Fisica.CacheHit2DSoloPrimero[0].normal);

                if (pendiente.x * _velocidadLateral < 0f)
                    pendiente *= -1f;

                if (Fisica.CacheHit2DSoloPrimero[0].distance > 0f)
                {//bajar al piso (snap)
                    posActual += Vector2.down * Fisica.CacheHit2DSoloPrimero[0].distance;
                    resultadoPaso = new ResultadoPaso()
                    {
                        tipo = ResultadoPaso.Tipo.SnapPiso,
                        posPrincipal = posActual,
                        vector = pendiente,
                    };
                }
                else
                {
                    //desenterrar del piso
                    posActual = Fisica.DesenterrarCapsula(posActual, tam, dir, Fisica.CacheHit2DSoloPrimero[0], esLateral: false);
                    resultadoPaso = new ResultadoPaso()
                    {
                        tipo = ResultadoPaso.Tipo.DesentierroDePiso,
                        posPrincipal = posActual,
                        vector = pendiente,
                    };
                }

                _cacheDePasos.Add(resultadoPaso);
            }
            else // en el aire
            {
                sueloActual = null;
                pendiente = _velocidadLateral < 0f ? Vector2.left : Vector2.right;
                if (tiempoTranscurrido < Time.fixedDeltaTime / 2f) // if menor que fixed timestep?
                {
                    tiempoTranscurrido = Time.fixedDeltaTime / 2f;
                }
                velocidadVertical += Physics2D.gravity.y * tiempoTranscurrido;
                posActual.y += velocidadVertical * tiempoTranscurrido; // tengo que detectar colisiones antes de caer como un desaforado

                resultadoPaso = new ResultadoPaso()
                {
                    tipo = ResultadoPaso.Tipo.DesenterrarInicial,
                    posPrincipal = posActual,
                };
                // ¿desenterrar solo cuando hay colisiones?
                posActual = Fisica.DesenterrarCapsula(posActual, tam, dir, esLateral: true); // desenterrar lateral
                resultadoPaso.posSecundaria = posActual;

                _cacheDePasos.Add(resultadoPaso);
            }

            // si no hay piso (colliderPiso==0) caigo + busco piso
            // si tiempo transcurrido == 0, la "caida" tiene que tomar la posta
            // me despego de las paredes de forma lateral (izq o der, no desentierro automatico)
            // si aun no hay piso, acelero la caida

            if (tiempoTranscurrido == 0f) // if menor que fixed timestep?
            { // NO SUCEDIO NADA, termino la simulacion? (hay pared, y hay piso)
                break;
            }
            else
            {
                tiempoRestante -= tiempoTranscurrido;
            }
        }

        return _cacheDePasos;
    }

#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(PerceptorDePlataformas))]
    private class EsteEditor : Editor
    {
        private void OnSceneGUI()
        {
            var perc = target as PerceptorDePlataformas;

            using (new Handles.DrawingScope(Color.white * 0.7f))
                EditorUtils.DrawCapsule2D(perc.transform.position, Vector3.forward, perc._capsuleSize, perc._capsuleDirection, 3f);

            var resultados = perc.Proyectar();
            foreach (var paso in resultados)
                DrawResultadoPaso(paso, perc);
        }

        private void DrawResultadoPaso(ResultadoPaso paso, PerceptorDePlataformas perc)
        {
            var size = perc._capsuleSize;
            float radio = .5f * (perc._capsuleDirection == CapsuleDirection2D.Vertical ? size.x : size.y);

            var sizeTrepa = new Vector2(size.x, size.y * (1f - perc._porcentajeTrepa)); // ¿deberia depender direccion capsula?

            var offsetPiesCapsula = Vector2.down * (.5f * (perc._capsuleDirection == CapsuleDirection2D.Vertical ? size.y : size.x) - radio);
            if (paso.tipo == ResultadoPaso.Tipo.DesenterrarInicial)
            {
                using (new Handles.DrawingScope(Color.red * 1.25f))
                {
                    Handles.DrawLine(paso.posPrincipal, paso.posSecundaria);
                    EditorUtils.DrawCapsule2D(paso.posSecundaria, Vector3.forward, size, perc._capsuleDirection);
                }
            }
            else if (paso.tipo == ResultadoPaso.Tipo.SnapPiso || paso.tipo == ResultadoPaso.Tipo.DesentierroDePiso)
            {
                var col = Color.Lerp(Color.red, Color.yellow, paso.tipo == ResultadoPaso.Tipo.SnapPiso ? .5f : .1f);
                using (new Handles.DrawingScope(col))
                {
                    //EditorUtils.DrawCapsule2D(paso.posPrincipal, Vector3.forward, size, perc._capsuleDirection);
                    Handles.DrawWireArc(paso.posPrincipal + offsetPiesCapsula, Vector3.forward, Vector3.left, 180f, radio);

                    float handleSize = size.magnitude / 2f * HandleUtility.GetHandleSize(paso.posPrincipal);
                    if (paso.vector != Vector2.zero)
                    {
                        Quaternion rot = Quaternion.LookRotation(paso.vector);
                        Handles.ArrowHandleCap(-1, paso.posPrincipal + offsetPiesCapsula, rot, handleSize, Event.current.type);
                    }
                }
            }
            else if (paso.tipo == ResultadoPaso.Tipo.AvanzoIninterrumpido)
            {
                using (new Handles.DrawingScope(Color.white))
                {
                    EditorUtils.DrawCapsule2D(paso.posPrincipal, Vector3.forward, size, perc._capsuleDirection);

                    float handleSize = size.magnitude / 2f * HandleUtility.GetHandleSize(paso.posPrincipal);
                    Quaternion rot = Quaternion.LookRotation(paso.vector);
                    // Handles.ArrowHandleCap(-1, paso.posPrincipal, rot, handleSize, Event.current.type);
                }

            }
            else if (paso.tipo == ResultadoPaso.Tipo.ChocoParedOEscalon)
            {
                using (new Handles.DrawingScope(Color.red * 0.25f))
                {
                    EditorUtils.DrawCapsule2D(paso.posPrincipal, Vector3.forward, size, perc._capsuleDirection, 4f);
                    EditorUtils.DrawCapsule2D(paso.posSecundaria, Vector3.forward, sizeTrepa, perc._capsuleDirection, 2f);
                }

            }

        }
    }
#endif
}
