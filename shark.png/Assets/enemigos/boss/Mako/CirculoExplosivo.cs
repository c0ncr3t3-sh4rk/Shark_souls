using System.Collections;
using UnityEngine;

public class CirculoExplosivo : MonoBehaviour
{
    [Header("Explosión")]
    public float tExplosion = 1.2f;
    public float radio = 2.5f;

    [Header("Referencias Visuales")]
    public SpriteRenderer spriteRenderer;
    public GameObject efectoExplosion;
    public GameObject dano;

    private void Start()
    {
        StartCoroutine(Explotar());
    }

    private IEnumerator Explotar()
    {
        float S = UnityEngine.Random.value;
        yield return new WaitForSeconds(1f + S);
        
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.5f);

        // efecto, ya lo pondre 
        if (efectoExplosion != null)
        {
            Instantiate(efectoExplosion, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radio);
    }
}