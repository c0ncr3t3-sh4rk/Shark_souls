using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Parry : MonoBehaviour
{
    [Header("Sprites de Animación Cutre")]
    [SerializeField] private Sprite spriteNormal; 
    [SerializeField] private Sprite spriteParry; 
    [SerializeField] private float duracionParry = 0.2f; 

    [Header("Configuración")]
    [SerializeField] private Collider2D colisionadorParry;

    [Header("Efectos de Éxito de Parry")]
    [SerializeField] private GameObject efectoPantallaParry; 
    [SerializeField] private float duracionEfectoPantalla = 0.15f;
    [SerializeField] private AudioClip sonidoParry;

    private SpriteRenderer spriteRenderer; 
    private AudioSource audioSource;

    public bool estaHaciendoParry { get; private set; } = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteNormal != null) spriteRenderer.sprite = spriteNormal;
        
        if (colisionadorParry != null) colisionadorParry.enabled = false;

        // Buscamos o agregamos un AudioSource en el tiburón
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void OnAttackSec(InputValue value)
    {
        if (value.isPressed && !AtaqueTiburon.estaOcupado)
        {
            StartCoroutine(RutinaParry());
        }
    }

    private IEnumerator RutinaParry()
    {
        estaHaciendoParry = true;
        AtaqueTiburon.estaOcupado = true;
        
        if (spriteParry != null) spriteRenderer.sprite = spriteParry;
        if (colisionadorParry != null) colisionadorParry.enabled = true;

        yield return new WaitForSeconds(duracionParry);

        if (colisionadorParry != null) colisionadorParry.enabled = false;
        if (spriteNormal != null) spriteRenderer.sprite = spriteNormal;

        AtaqueTiburon.estaOcupado = false;
        estaHaciendoParry = false;
    }

    public bool IntentarParry(GameObject atacante, int danoDeAtaque)
    {
        if (estaHaciendoParry)
        {
            Debug.Log("¡Parry realizado con éxito contra " + atacante.name + "!");

            // Reproducir efecto visual en pantalla
            if (efectoPantallaParry != null)
            {
                StartCoroutine(RutinaEfectoPantalla());
            }

            // Reproducir efecto de sonido
            if (sonidoParry != null && audioSource != null)
            {
                audioSource.PlayOneShot(sonidoParry);
            }

            // Si el atacante es parreable, ejecutar su lógica propia
            IParryable parryable = atacante.GetComponent<IParryable>();
            if (parryable == null) parryable = atacante.GetComponentInParent<IParryable>();
            if (parryable == null) parryable = atacante.GetComponentInChildren<IParryable>();

            if (parryable != null)
            {
                parryable.OnParry(gameObject, danoDeAtaque);
            }

            return true;
        }

        return false;
    }

    private IEnumerator RutinaEfectoPantalla()
    {
        efectoPantallaParry.SetActive(true);
        yield return new WaitForSeconds(duracionEfectoPantalla);
        efectoPantallaParry.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!estaHaciendoParry) return;

        // Comprobamos si el objeto es parreable
        IParryable parryable = collision.GetComponent<IParryable>();
        if (parryable == null) parryable = collision.GetComponentInParent<IParryable>();
        if (parryable == null) parryable = collision.GetComponentInChildren<IParryable>();

        if (parryable != null)
        {
            // Si es una mina, solo permitimos parry si está por explotar
            MinaSubmarina mina = collision.GetComponent<MinaSubmarina>();
            if (mina == null) mina = collision.GetComponentInParent<MinaSubmarina>();
            if (mina == null) mina = collision.GetComponentInChildren<MinaSubmarina>();

            if (mina != null && !mina.estaPorExplotar)
            {
                return;
            }

            IntentarParry(collision.gameObject, 0);
        }
        else if (collision.CompareTag("Proyectil"))
        {
            Debug.Log("¡Parry realizado contra proyectil!");
            // Lógica de parry aquí
        }
    }
}