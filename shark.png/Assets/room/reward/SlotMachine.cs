using UnityEngine;
using System.Collections.Generic;

public class SlotMachine : MonoBehaviour
{
    public SlotMachineData data;
    public Animator anim;

    private bool interactuado = false;

    public void Interactuar()
    {
        if (interactuado) return;
        interactuado = true;
        
        anim.SetTrigger("Interact");

        // Wait a small delay for animation if needed, but we'll show UI directly
        MostrarOpcionesMejora();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!interactuado && collision.CompareTag("Player"))
        {
            Interactuar();
        }
    }

    private void MostrarOpcionesMejora()
    {
        CalidadPool poolElegido = ElegirCalidad();
        int baseStat = ObtenerStatPorCalidad(poolElegido.calidad);
        bool esComun = poolElegido.calidad == Calidad.Looser || poolElegido.calidad == Calidad.Comun;

        // Las opciones especiales tienen calidad mínima Raro.
        // Si el pool global sacó algo mejor, también se aplica a esas opciones.
        Calidad calidadEspecial = (int)poolElegido.calidad < (int)Calidad.Raro
            ? Calidad.Raro
            : poolElegido.calidad;

        (float bonusAsfixia, float bonusInvenc, string labelCalidad) = ObtenerBonusEspecialPorCalidad(calidadEspecial);
        Color colorBase      = ColorPorCalidad(poolElegido.calidad);
        Color colorEspecial  = ColorPorCalidad(calidadEspecial);

        var todasLasOpciones = new List<(int indice, string texto, float bonusA, float bonusI, Color color)>
        {
            (0, $"❤️ Vida Máxima +{baseStat}",           0f, 0f, colorBase),
            (1, $"⚔️ Ataque +{baseStat}",                0f, 0f, colorBase),
            (2, $"💨 Velocidad +{baseStat}",              0f, 0f, colorBase),
        };

        // Solo añadir opciones especiales si la calidad NO es Común / Looser
        if (!esComun)
        {
            todasLasOpciones.Add((3, $"🔵 [{labelCalidad}] Pulmones de Acero — Tiempo de asfixia +{bonusAsfixia:0.##}s", bonusAsfixia, 0f, colorEspecial));
            todasLasOpciones.Add((4, $"🛡️ [{labelCalidad}] Escamas Endurecidas — Invencibilidad +{bonusInvenc:0.##}s",   0f, bonusInvenc, colorEspecial));
        }

        // Barajar y elegir exactamente 3
        Barajar(todasLasOpciones);
        var elegidas = todasLasOpciones.GetRange(0, 3);

        string[] opcionesText   = new string[3];
        int[]    indicesElegidos = new int[3];
        float[]  bonusAElegidos  = new float[3];
        float[]  bonusIElegidos  = new float[3];
        Color[]  coloresElegidos = new Color[3];

        for (int i = 0; i < 3; i++)
        {
            opcionesText[i]    = elegidas[i].texto;
            indicesElegidos[i] = elegidas[i].indice;
            bonusAElegidos[i]  = elegidas[i].bonusA;
            bonusIElegidos[i]  = elegidas[i].bonusI;
            coloresElegidos[i] = elegidas[i].color;
        }

        MejorasUI ui = gameObject.AddComponent<MejorasUI>();
        ui.MostrarMejoras(opcionesText, (slotElegido) => {
            AplicarMejora(indicesElegidos[slotElegido], baseStat,
                          bonusAElegidos[slotElegido], bonusIElegidos[slotElegido]);
        }, coloresElegidos);
    }

    /// <summary>
    /// Color de borde según la calidad del pool.
    /// </summary>
    private Color ColorPorCalidad(Calidad calidad)
    {
        switch (calidad)
        {
            case Calidad.Looser:     return new Color(0.40f, 0.40f, 0.40f); // gris apagado
            case Calidad.Comun:      return new Color(0.85f, 0.85f, 0.85f); // blanco roto
            case Calidad.PocoComun:  return new Color(0.30f, 0.80f, 0.30f); // verde
            case Calidad.Raro:       return new Color(0.20f, 0.50f, 1.00f); // azul
            case Calidad.Epico:      return new Color(0.65f, 0.20f, 0.90f); // púrpura
            case Calidad.Legendario: return new Color(1.00f, 0.60f, 0.10f); // naranja dorado
            case Calidad.Mitico:     return new Color(1.00f, 0.15f, 0.15f); // rojo intenso
            default:                 return Color.white;
        }
    }

    /// <summary>
    /// Devuelve (bonusAsfixia, bonusInvencibilidad, etiqueta) para las opciones especiales.
    /// Calidad mínima garantizada: Raro.
    /// </summary>
    private (float bonusAsfixia, float bonusInvenc, string label) ObtenerBonusEspecialPorCalidad(Calidad calidad)
    {
        switch (calidad)
        {
            case Calidad.Raro:        return (0.50f, 0.20f, "Raro");
            case Calidad.Epico:       return (0.65f, 0.27f, "Épico");
            case Calidad.Legendario:  return (0.85f, 0.35f, "Legendario");
            case Calidad.Mitico:      return (1.10f, 0.45f, "Mítico");
            default:                  return (0.50f, 0.20f, "Raro"); // fallback seguro
        }
    }

    private void Barajar<T>(List<T> lista)
    {
        for (int i = lista.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T tmp = lista[i];
            lista[i] = lista[j];
            lista[j] = tmp;
        }
    }

    private CalidadPool ElegirCalidad()
    {
        int suerte = 0;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            SuerteTiburon suerteObj = player.GetComponent<SuerteTiburon>();
            if (suerteObj != null) suerte = suerteObj.GetSuerte();
        }

        // Calcular el total real de probabilidades del ScriptableObject (puede no sumar 100)
        float total = 0f;
        foreach (var pool in data.pools)
            total += pool.probabilidadGlobal;

        // La suerte desplaza el roll hacia calidades más altas.
        // Cada punto resta hasta un 5% del total (clampea al 50% del total máximo).
        float suerteOffset = Mathf.Clamp(suerte * total * 0.05f, 0f, total * 0.5f);
        float rng = Random.Range(0f, total) - suerteOffset;

        float acumulado = 0f;
        foreach (var pool in data.pools)
        {
            acumulado += pool.probabilidadGlobal;
            if (rng <= acumulado)
                return pool;
        }

        return data.pools[data.pools.Length - 1]; // fallback a la máxima si el rng superó todo
    }

    private int ObtenerStatPorCalidad(Calidad calidad)
    {
        switch (calidad)
        {
            case Calidad.Comun:      return 1;
            case Calidad.PocoComun:  return 2;
            case Calidad.Raro:       return 3;
            case Calidad.Epico:      return 4;
            case Calidad.Legendario: return 5;
            case Calidad.Mitico:     return 7;
            default:                 return 1;
        }
    }

    private void AplicarMejora(int opcionIndex, int cantidad, float bonusA, float bonusI)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            if (opcionIndex == 0)
            {
                SaludTiburon salud = player.GetComponent<SaludTiburon>();
                if (salud != null) salud.addMaxHealth(cantidad);
            }
            else if (opcionIndex == 1)
            {
                AtaqueTiburon ataque = player.GetComponent<AtaqueTiburon>();
                if (ataque != null) ataque.addDamage(cantidad);
            }
            else if (opcionIndex == 2)
            {
                MovimientoTiburon mov = player.GetComponent<MovimientoTiburon>();
                if (mov != null) mov.addVelocidad((float)cantidad);
            }
            else if (opcionIndex == 3)
            {
                // 🔵 Pulmones de Acero — tiempo de asfixia escalado por calidad (mín. Raro)
                AsfixiaTiburon asfixia = player.GetComponent<AsfixiaTiburon>();
                if (asfixia != null) asfixia.addTiempoLimiteQuieto(bonusA);
            }
            else if (opcionIndex == 4)
            {
                // 🛡️ Escamas Endurecidas — invencibilidad escalada por calidad (mín. Raro)
                SaludTiburon salud = player.GetComponent<SaludTiburon>();
                if (salud != null) salud.addDuracionInvencibilidad(bonusI);
            }
        }

        Debug.Log($"Mejora de estadística aplicada. Se destruye la tragaperras.");
        Destroy(gameObject);
    }
}
