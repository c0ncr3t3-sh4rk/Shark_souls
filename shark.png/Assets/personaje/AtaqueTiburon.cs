using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class AtaqueTiburon : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite spriteNormal; 
    [SerializeField] private Sprite spriteMordisco; 
    [SerializeField] private float duracionMordisco = 0.15f; 

    [Header("Daño")]
    [SerializeField] private int danoMordisco = 1;
    [SerializeField] private Collider2D colliderBoca;

    private SpriteRenderer spriteRenderer; 
    public static bool estaOcupado = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = spriteNormal;
        
        colliderBoca.enabled = false;
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed && !estaOcupado)
        {
            StartCoroutine(Mordisco());
        }
    }

    private IEnumerator Mordisco()
    {
        estaOcupado = true;
        spriteRenderer.sprite = spriteMordisco;
        colliderBoca.enabled = true;

        yield return new WaitForSeconds(duracionMordisco);

        colliderBoca.enabled = false;
        spriteRenderer.sprite = spriteNormal;
        estaOcupado = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!colliderBoca.enabled) return;

        if (collision.gameObject.CompareTag("Enemigo") && collision.IsTouching(colliderBoca))
        {
            VidaEnemigo enemigo = collision.GetComponent<VidaEnemigo>();
            if (enemigo != null)
            {
                Debug.Log("¡Atacaste con la boca con éxito!");
                enemigo.RecibirDano(danoMordisco);

                colliderBoca.enabled = false;
            }
        }
    }

    public void addDamage(int nuevoDano)
    {
        danoMordisco += nuevoDano;
    }
}