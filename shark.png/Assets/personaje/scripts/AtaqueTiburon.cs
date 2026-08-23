using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class AtaqueTiburon : MonoBehaviour
{
    public enum EstadoBoca { Reposo, Abierta, Bloqueada, Presa }

    [Header("Estado (Solo lectura)")]
    public EstadoBoca estadoActual = EstadoBoca.Reposo;

    [Header("Sprites")]
    [SerializeField] private Sprite spriteNormal;
    [SerializeField] private Sprite spriteMordisco;

    [Header("Daño")]
    [SerializeField] private int danoMordisco = 1;
    [SerializeField] private Collider2D colliderBoca;

    [Header("Agarre")]
    [SerializeField] private Transform puntoBoca;

    [Header("Bapuleo (Sacudida)")]
    [SerializeField] private int girosParaBapuleo = 4;
    [SerializeField] private float tiempoMaximoEntreGiros = 0.5f;
    [SerializeField] private int danoBapuleo = 1;

    [Header("Eventos de Bapuleo")]
    public UnityEvent onGolpeBapuleo;
    public UnityEvent onMuerteBapuleo;

    private SpriteRenderer spriteRenderer;
    public static bool estaOcupado = false;

    private IAgarrable pezAgarrado = null;
    private GameObject pezAgarradoGO = null;
    private PlayerInput input;

    private MovimientoTiburon scriptMovimiento;
    private int contadorGiros = 0;
    private float ultimoTiempoGiro = 0f;
    private int ultimoLadoInput = 0;

    public bool tienePezAgarrado => pezAgarrado != null;
    private bool haMatado = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = spriteNormal;
        colliderBoca.enabled = false;
        input = GetComponent<PlayerInput>();
        scriptMovimiento = GetComponent<MovimientoTiburon>();
    }

    public void OnAttack(InputValue value)
    {
        bool presionado = value.isPressed;

        if (presionado)
        {
            if (estadoActual == EstadoBoca.Reposo)
            {
                AbrirBoca();
            }
        }
        else
        {
            switch (estadoActual)
            {
                case EstadoBoca.Abierta:
                    CerrarBoca(EstadoBoca.Reposo);
                    break;
                case EstadoBoca.Presa:
                    SoltarPez();
                    CerrarBoca(EstadoBoca.Reposo);
                    break;
                case EstadoBoca.Bloqueada:
                    estadoActual = EstadoBoca.Reposo;
                    estaOcupado = false;
                    break;
            }
        }
    }

    private void AbrirBoca()
    {
        estadoActual = EstadoBoca.Abierta;
        estaOcupado = true;
        spriteRenderer.sprite = spriteMordisco;
        colliderBoca.enabled = true;
    }

    private void CerrarBoca(EstadoBoca nuevoEstado)
    {
        estadoActual = nuevoEstado;
        spriteRenderer.sprite = spriteNormal;
        colliderBoca.enabled = false;

        if (nuevoEstado == EstadoBoca.Reposo)
        {
            estaOcupado = false;
        }
    }

    private void Update()
    {
        if (estadoActual == EstadoBoca.Presa)
        {
            // Si el objeto desaparece, forzamos el cierre y liberamos el estado
            if (pezAgarradoGO == null || !pezAgarradoGO.activeInHierarchy)
            {
                LimpiarAgarre();
                CerrarBoca(EstadoBoca.Reposo); 
                return;
            }

            ProcesarBapuleo();
        }
    }

    private void ProcesarBapuleo()
    {
        if (scriptMovimiento == null) return;

        Vector2 inputMov = scriptMovimiento.getImput();
        int ladoActual = 0;

        if (inputMov.x > 0.1f) ladoActual = 1;
        else if (inputMov.x < -0.1f) ladoActual = -1;

        if (ladoActual != 0 && ladoActual != ultimoLadoInput)
        {
            if (ultimoLadoInput != 0)
            {
                float tiempoPasado = Time.time - ultimoTiempoGiro;
                if (tiempoPasado <= tiempoMaximoEntreGiros)
                {
                    contadorGiros++;
                }
                else
                {
                    contadorGiros = 1;
                }

                if (contadorGiros >= girosParaBapuleo)
                {
                    EjecutarGolpeBapuleo();
                }
            }
            else
            {
                contadorGiros = 1;
            }

            ultimoLadoInput = ladoActual;
            ultimoTiempoGiro = Time.time;
        }
    }

    private void EjecutarGolpeBapuleo()
    {
        if (pezAgarradoGO == null) return;

        VidaEnemigo enemigo = pezAgarradoGO.GetComponent<VidaEnemigo>() ?? pezAgarradoGO.GetComponentInParent<VidaEnemigo>();

        if (enemigo != null)
        {
            enemigo.RecibirDano(danoBapuleo, VidaEnemigo.TipoMuerte.Bapuleo);
            onGolpeBapuleo?.Invoke();

            if (pezAgarradoGO == null || !pezAgarradoGO.activeInHierarchy)
            {
                onMuerteBapuleo?.Invoke();
                LimpiarAgarre();
                CerrarBoca(EstadoBoca.Reposo);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        haMatado = false;
        if (estadoActual != EstadoBoca.Abierta) return;

        if (collision.gameObject.CompareTag("Enemigo") || collision.attachedRigidbody?.CompareTag("Enemigo") == true)
        {
            IAgarrable agarrable = collision.GetComponent<IAgarrable>() ?? collision.GetComponentInParent<IAgarrable>();
            VidaEnemigo enemigo = collision.GetComponent<VidaEnemigo>() ?? collision.GetComponentInParent<VidaEnemigo>();
            SaludTiburon saludJugador = GetComponent<SaludTiburon>() ?? GetComponentInParent<SaludTiburon>();

            if (enemigo != null)
            {
                haMatado = enemigo.RecibirDano(danoMordisco, VidaEnemigo.TipoMuerte.Mordisco);

                if (haMatado)
                {
                    if (Random.Range(0f, 1f) < 1f)
                    {
                        saludJugador.Curar(1);
                    }
                }

                GameObject objetivoGO = (agarrable as MonoBehaviour)?.gameObject ?? collision.gameObject;

                if (agarrable != null && objetivoGO.activeInHierarchy)
                {
                    AgarrarPez(agarrable, objetivoGO);
                    estadoActual = EstadoBoca.Presa;
                    colliderBoca.enabled = false;
                }
                else
                {
                    CerrarBoca(EstadoBoca.Bloqueada);
                }
            }
        }
        estaOcupado = false;
    }

    private void AgarrarPez(IAgarrable agarrable, GameObject go)
    {
        pezAgarrado = agarrable;
        pezAgarradoGO = go;
        Transform parentTransform = puntoBoca != null ? puntoBoca : transform;
        pezAgarrado.EnAgarrar(parentTransform);
    }

    public void SoltarPez()
    {
        if (pezAgarrado != null)
        {
            pezAgarrado.EnSoltar();
            LimpiarAgarre();
        }
    }

    private void LimpiarAgarre()
    {
        pezAgarrado = null;
        pezAgarradoGO = null;
        contadorGiros = 0;
        ultimoLadoInput = 0;
        ultimoTiempoGiro = 0f;
    }

    public GameObject SoltarPezParaParry()
    {
        if (pezAgarrado == null) return null;

        GameObject go = pezAgarradoGO;
        pezAgarrado.EnSoltar();
        LimpiarAgarre();
        CerrarBoca(EstadoBoca.Reposo);
        return go;
    }

    public void addDamage(int nuevoDano)
    {
        danoMordisco += nuevoDano;
    }
}