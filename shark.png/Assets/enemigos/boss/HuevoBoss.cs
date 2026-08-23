using UnityEngine;
using System.Collections;

/// <summary>
/// Huevo que el boss suelta mientras se mueve.
/// Tiene vida propia y eclosiona en una CarpitaMinion tras un tiempo.
/// Si es destruido antes de eclosionar, no spawnea nada.
/// </summary>
public class HuevoBoss : MonoBehaviour
{
    [Header("Eclosión")]
    [SerializeField] private float tiempoEclosion = 5f;
    [SerializeField] private GameObject prefabCarpitaMinion;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private bool haEclosionado = false;
    private VidaEnemigo vidaEnemigo;

    private void Awake()
    {
        vidaEnemigo = GetComponent<VidaEnemigo>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        haEclosionado = false;
        StartCoroutine(RutinaEclosion());
    }

    private IEnumerator RutinaEclosion()
    {
        float t = 0f;

        while (t < tiempoEclosion)
        {
            t += Time.deltaTime;

            // Efecto visual: temblor progresivo conforme se acerca la eclosión
            float progreso = t / tiempoEclosion;
            if (progreso > 0.5f)
            {
                float intensidad = (progreso - 0.5f) * 2f; // de 0 a 1
                float temblor = Mathf.Sin(Time.time * (10f + intensidad * 30f)) * intensidad * 0.1f;
                transform.localPosition += new Vector3(temblor, 0f, 0f);
            }

            // Cambio de color antes de eclosionar
            if (progreso > 0.7f && spriteRenderer != null)
            {
                float lerpT = (progreso - 0.7f) / 0.3f;
                spriteRenderer.color = Color.Lerp(Color.white, new Color(1f, 0.8f, 0.6f), lerpT);
            }

            yield return null;
        }

        Eclosionar();
    }

    private void Eclosionar()
    {
        if (haEclosionado) return;
        haEclosionado = true;

        if (prefabCarpitaMinion != null)
        {
            Instantiate(prefabCarpitaMinion, transform.position, Quaternion.identity);
        }

        // Destruir el huevo
        if (vidaEnemigo != null)
        {
            vidaEnemigo.Morir(VidaEnemigo.TipoMuerte.Normal);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }
}
