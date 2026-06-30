using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Parry : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite spriteNormal; 
    [SerializeField] private Sprite spriteParry; 
    [SerializeField] private float duracionParry = 0.2f; 

    [Header("Configuración")]
    [SerializeField] private Collider2D colliderParry;

    [Header("Efectos de Éxito de Parry")]
    [SerializeField] private GameObject efectoPantalla; 
    [SerializeField] private float duracionEfectoPantalla = 0.27f;
    [SerializeField] private AudioClip sonidoParry;

    private SpriteRenderer spriteRenderer; 
    private AudioSource audioSource;
    public bool estaHaciendoParry { get; private set; } = false;

    private void Awake()
    {
        efectoPantalla.SetActive(false);
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = spriteNormal;
        
        colliderParry.enabled = false;

        audioSource = GetComponent<AudioSource>();
    }

    public void OnAttackSec(InputValue value)
    {
        if (value.isPressed && !AtaqueTiburon.estaOcupado)
        {
            StartCoroutine(IntentarParry());
        }
    }

    private IEnumerator IntentarParry()
    {
        estaHaciendoParry = true;
        AtaqueTiburon.estaOcupado = true;
        
        spriteRenderer.sprite = spriteParry;
        colliderParry.enabled = true;
        yield return new WaitForSeconds(duracionParry);

        colliderParry.enabled = false;
        spriteRenderer.sprite = spriteNormal;

        AtaqueTiburon.estaOcupado = false;
        estaHaciendoParry = false;
    }

    public bool HacerParry(GameObject atacante, int danoDeAtaque)
    {
        if (estaHaciendoParry)
        {
            Debug.Log("¡Parry realizado con éxito contra " + atacante.name + "!");
            audioSource.PlayOneShot(sonidoParry);

            // Reproducir efecto visual en pantalla
            if (efectoPantalla != null)
            {
                StartCoroutine(EfectoPantalla());
            }

            Iframes();

            // Si el atacante es parreable, ejecutar su lógica propia
            IParryable parryable = atacante.GetComponentInParent<IParryable>() ?? 
                                   atacante.GetComponentInChildren<IParryable>() ?? 
                                   atacante.GetComponent<IParryable>();

            if (parryable != null)
            {
                parryable.OnParry(gameObject, danoDeAtaque);
            }

            Combo.Instancia.Kill();
            estaHaciendoParry = false;

            return true;
        }

        return false;
    }

    private IEnumerator EfectoPantalla()
    {
        Animacion animacion = GetComponent<Animacion>();
        animacion.enabled = false;
        efectoPantalla.SetActive(true);
        Time.timeScale = 0f; 

        yield return new WaitForSecondsRealtime(duracionEfectoPantalla);

        Time.timeScale = 1f;
        animacion.enabled = true;
        efectoPantalla.SetActive(false);
        spriteRenderer.sprite = spriteNormal;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!estaHaciendoParry) return;
        if (collision.IsTouching(colliderParry) && (collision.CompareTag("Ataque") || collision.CompareTag("Proyectil")))
        {
            HacerParry(collision.gameObject, 0);
        }
    }

    private void Iframes()
    {
        SaludTiburon scriptSalud = GetComponent<SaludTiburon>();
        scriptSalud.SumarI(1);
    }
}