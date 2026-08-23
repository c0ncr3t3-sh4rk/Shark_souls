using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Agarrable : MonoBehaviour, IAgarrable
{
    private Transform puntoBocaActual = null;
    public bool EstaAgarrado => puntoBocaActual != null;

    private Vector3 escalaOriginal;
    private Rigidbody2D rb;
    private Collider2D[] misColliders;
    private AnimacionEnemigo animEnemigo;
    private MonoBehaviour scriptEnemigo;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        misColliders = GetComponentsInChildren<Collider2D>();
        escalaOriginal = transform.localScale;
        animEnemigo = GetComponent<AnimacionEnemigo>();
        scriptEnemigo = GetComponent<MonoBehaviour>();
    }

    private void LateUpdate()
    {
        if (puntoBocaActual != null)
        {
            transform.rotation = Quaternion.identity;
        }
    }

    public void EnAgarrar(Transform boca)
    {
        puntoBocaActual = boca;
        escalaOriginal = transform.localScale;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        foreach (var col in misColliders)
        {
            if (col != null) col.enabled = false;
        }

        if (animEnemigo != null) animEnemigo.enabled = false;

        Vector3 escalaWorld = transform.lossyScale;

        transform.SetParent(boca);
        transform.localPosition = Vector3.right * -0.7f; 
        transform.localRotation = Quaternion.Euler(0, 0, 180);   

        Vector3 lossyBoca = boca.lossyScale;
        transform.localScale = new Vector3(
            Mathf.Abs(escalaWorld.x) / Mathf.Abs(lossyBoca.x),
            Mathf.Abs(escalaWorld.y) / Mathf.Abs(lossyBoca.y),
            Mathf.Abs(escalaWorld.z) / Mathf.Abs(lossyBoca.z)
        );
    }

    public void EnSoltar()
    {
        puntoBocaActual = null;

        transform.SetParent(null);
        transform.localScale = escalaOriginal;

        rb.bodyType = RigidbodyType2D.Dynamic;

        foreach (var col in misColliders)
        {
            if (col != null) col.enabled = true;
        }

        if (animEnemigo != null) animEnemigo.enabled = true;
        if (scriptEnemigo != null) scriptEnemigo.enabled = true;
    }

    private void OnDisable()
    {
        if (puntoBocaActual != null)
        {
            transform.SetParent(null);
            transform.localScale = escalaOriginal;
        }
        puntoBocaActual = null;

        if (rb != null) rb.bodyType = RigidbodyType2D.Dynamic;

        if (misColliders != null)
        {
            foreach (var col in misColliders)
            {
                if (col != null) col.enabled = true;
            }
        }

        if (animEnemigo != null) animEnemigo.enabled = true;
    }
}