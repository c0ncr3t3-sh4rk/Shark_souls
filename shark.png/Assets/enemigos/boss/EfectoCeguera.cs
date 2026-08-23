using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Se añade al Player. Gestiona un overlay de pantalla oscuro para simular ceguera.
/// El overlay se busca por tag "OverlayCeguera" o se crea dinámicamente.
/// </summary>
public class EfectoCeguera : MonoBehaviour
{
    private Image overlayCeguera;
    private Coroutine rutinaCeguera;

    /// <summary>
    /// Aplica el efecto de ceguera durante la duración indicada.
    /// Si ya hay una ceguera activa, la reinicia con la nueva duración.
    /// </summary>
    public void AplicarCeguera(float duracion)
    {
        if (overlayCeguera == null)
        {
            BuscarOCrearOverlay();
        }

        if (rutinaCeguera != null)
        {
            StopCoroutine(rutinaCeguera);
        }

        rutinaCeguera = StartCoroutine(RutinaCeguera(duracion));
    }

    private void BuscarOCrearOverlay()
    {
        // Buscar por tag primero
        GameObject overlayGO = GameObject.FindWithTag("OverlayCeguera");

        if (overlayGO != null)
        {
            overlayCeguera = overlayGO.GetComponent<Image>();
        }
        else
        {
            // Crear uno dinámicamente
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("CanvasCeguera");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
            }

            GameObject overlay = new GameObject("OverlayCeguera");
            overlay.transform.SetParent(canvas.transform, false);
            overlay.tag = "OverlayCeguera";

            overlayCeguera = overlay.AddComponent<Image>();
            overlayCeguera.color = new Color(0f, 0f, 0f, 0f);
            overlayCeguera.raycastTarget = false;

            RectTransform rt = overlayCeguera.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        overlayCeguera.gameObject.SetActive(false);
    }

    private IEnumerator RutinaCeguera(float duracion)
    {
        overlayCeguera.gameObject.SetActive(true);

        // Fade in rápido (0.2s)
        float fadeInTime = 0.2f;
        float t = 0f;
        while (t < fadeInTime)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 0.85f, t / fadeInTime);
            overlayCeguera.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        overlayCeguera.color = new Color(0f, 0f, 0f, 0.85f);

        // Mantener la ceguera
        yield return new WaitForSeconds(duracion - fadeInTime - 0.5f);

        // Fade out (0.5s)
        float fadeOutTime = 0.5f;
        t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(0.85f, 0f, t / fadeOutTime);
            overlayCeguera.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }

        overlayCeguera.color = new Color(0f, 0f, 0f, 0f);
        overlayCeguera.gameObject.SetActive(false);
        rutinaCeguera = null;
    }
}
