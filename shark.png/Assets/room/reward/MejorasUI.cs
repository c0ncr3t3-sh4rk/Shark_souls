using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MejorasUI : MonoBehaviour
{
    private const int   CARD_W      = 300;
    private const int   CARD_H      = 100;
    private const int   BORDER_SIZE = 5;   // grosor del contorno en píxeles
    private const int   SPACING     = 120; // reducido un poco para que quepan hasta 6 opciones

    private GameObject canvasGO;

    /// <summary>
    /// Muestra las mejoras. borderColors debe tener la misma longitud que descripciones.
    /// </summary>
    public void MostrarMejoras(string[] descripciones, System.Action<int> onSelect, Color[] borderColors = null)
    {
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        canvasGO = new GameObject("MejorasCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        Image panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);
        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        for (int i = 0; i < descripciones.Length; i++)
        {
            int index = i;
            Color borderColor = (borderColors != null && i < borderColors.Length)
                ? borderColors[i]
                : Color.white;

            float totalHeight = (descripciones.Length - 1) * SPACING;
            float startY = totalHeight / 2f;
            Vector2 centerPos = new Vector2(0, startY - (i * SPACING));

            // --- Contorno (se dibuja primero, detrás de la tarjeta) ---
            GameObject borderGO = new GameObject($"Border_{i}");
            borderGO.transform.SetParent(panelGO.transform, false);
            Image borderImage = borderGO.AddComponent<Image>();
            borderImage.color = borderColor;
            RectTransform borderRect = borderGO.GetComponent<RectTransform>();
            borderRect.sizeDelta = new Vector2(CARD_W + BORDER_SIZE * 2, CARD_H + BORDER_SIZE * 2);
            borderRect.anchoredPosition = centerPos;

            // --- Tarjeta blanca (encima del contorno) ---
            GameObject btnGO = new GameObject($"Button_{i}");
            btnGO.transform.SetParent(panelGO.transform, false);

            Image btnImage = btnGO.AddComponent<Image>();
            btnImage.color = new Color(0.12f, 0.12f, 0.18f, 1f); // fondo oscuro para que el borde destaque

            Button btn = btnGO.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                onSelect(index);
                Cerrar();
            });

            RectTransform btnRect = btnGO.GetComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(CARD_W, CARD_H);
            btnRect.anchoredPosition = centerPos;

            // Efecto hover: tinte más claro al pasar el cursor
            ColorBlock cb = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = new Color(0.85f, 0.85f, 0.85f);
            cb.pressedColor     = new Color(0.6f, 0.6f, 0.6f);
            btn.colors = cb;

            // --- Texto ---
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform, false);
            Text text = textGO.AddComponent<Text>();
            text.text = descripciones[i];
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 22;

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(8, 0);
            textRect.offsetMax = new Vector2(-8, 0);
        }
    }

    private void Cerrar()
    {
        if (canvasGO != null) Destroy(canvasGO);
        Destroy(gameObject);
    }
}
