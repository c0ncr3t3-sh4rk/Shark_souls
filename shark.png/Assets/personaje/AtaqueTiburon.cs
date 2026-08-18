using System.Collections;
using UnityEngine;
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

    private SpriteRenderer spriteRenderer;
    public static bool estaOcupado = false;

    private IAgarrable pezAgarrado = null;
    private GameObject pezAgarradoGO = null;
    private PlayerInput input;

    public bool tienePezAgarrado => pezAgarrado != null;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = spriteNormal;
        colliderBoca.enabled = false;
        input = GetComponent<PlayerInput>();
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
            // Lógica unificada para soltar el botón
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
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (estadoActual != EstadoBoca.Abierta) return;

        if (collision.gameObject.CompareTag("Enemigo") || collision.attachedRigidbody?.CompareTag("Enemigo") == true)
        {
            IAgarrable agarrable = collision.GetComponent<IAgarrable>() ?? collision.GetComponentInParent<IAgarrable>();
            VidaEnemigo enemigo = collision.GetComponent<VidaEnemigo>() ?? collision.GetComponentInParent<VidaEnemigo>();

            if (enemigo != null)
            {
                enemigo.RecibirDano(danoMordisco);

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
    }

    public void addDamage(int nuevoDano)
    {
        danoMordisco += nuevoDano;
    }
}