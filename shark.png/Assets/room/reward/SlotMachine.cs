using UnityEngine;
using System.Collections.Generic;

public class RecompensaData
{
    public int vida;
    public int ataque;
    public int velocidad;
    public float asfixia;
    public float invencibilidad;
    public string textoUI;
    public Color colorUI;
}

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
        int suerte = 0;
        int sabiduria = 0;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            SuerteTiburon suerteObj = player.GetComponent<SuerteTiburon>();
            if (suerteObj != null) suerte = suerteObj.GetSuerte();
            
            SabiduriaTiburon sabiduriaObj = player.GetComponent<SabiduriaTiburon>();
            if (sabiduriaObj != null) sabiduria = sabiduriaObj.GetSabiduria();
        }

        CalidadPool poolElegido = ElegirCalidad(suerte);
        int baseStat = ObtenerStatPorCalidad(poolElegido.calidad);
        bool esComun = poolElegido.calidad == Calidad.Looser || poolElegido.calidad == Calidad.Comun;

        Calidad calidadEspecial = (int)poolElegido.calidad < (int)Calidad.Raro ? Calidad.Raro : poolElegido.calidad;
        (float bonusAsfixia, float bonusInvenc, string labelCalidad) = ObtenerBonusEspecialPorCalidad(calidadEspecial);
        
        Color colorBase = ColorPorCalidad(poolElegido.calidad);
        Color colorEspecial = ColorPorCalidad(calidadEspecial);

        // Probabilidad de obtener opciones adicionales (Base 3, Max 6)
        // Sabiduría: 1% por punto. Suerte: 0.5% por punto.
        float probExtraOption = sabiduria * 0.01f + suerte * 0.005f;
        int extraOptions = Mathf.FloorToInt(probExtraOption) + (Random.value < (probExtraOption % 1f) ? 1 : 0);
        int numOpciones = Mathf.Clamp(3 + extraOptions, 3, 6);

        List<RecompensaData> elegidas = new List<RecompensaData>();

        for (int i = 0; i < numOpciones; i++)
        {
            RecompensaData recompensa = GenerarOpcion(poolElegido.calidad, baseStat, esComun, bonusAsfixia, bonusInvenc, labelCalidad, colorBase, colorEspecial, suerte, sabiduria);
            elegidas.Add(recompensa);
        }

        string[] opcionesText = new string[numOpciones];
        Color[] coloresElegidos = new Color[numOpciones];

        for (int i = 0; i < numOpciones; i++)
        {
            opcionesText[i] = elegidas[i].textoUI;
            coloresElegidos[i] = elegidas[i].colorUI;
        }

        MejorasUI ui = gameObject.AddComponent<MejorasUI>();
        ui.MostrarMejoras(opcionesText, (slotElegido) => {
            AplicarMejora(elegidas[slotElegido]);
        }, coloresElegidos);
    }

    private RecompensaData GenerarOpcion(Calidad calidadPool, int baseStat, bool esComun, float bonusA, float bonusI, string label, Color colorB, Color colorE, int suerte, int sabiduria)
    {
        RecompensaData rec = new RecompensaData();
        
        int maxRnd = esComun ? 3 : 5;
        int rndStatBase = Random.Range(0, maxRnd);
        
        // 1) Asignar mejora base
        AplicarStat(rec, rndStatBase, baseStat, bonusA, bonusI, true);
        rec.colorUI = (rndStatBase >= 3) ? colorE : colorB;
        rec.textoUI = TextoGenericoPorStat(rndStatBase, baseStat, bonusA, bonusI, label);
        
        // Probabilidad de que la recompensa sea extraordinaria
        // Con 20 suerte da aprox ~9% por opción (lo que equivale a 1 por cada 3-4 tragaperras).
        float probExtraordinaria = (suerte * 0.0045f) + (sabiduria * 0.002f);
        bool esExtraordinaria = Random.value < probExtraordinaria;

        if (esExtraordinaria)
        {
            int numExtras = Random.Range(1, 4); // 1 a 3 extras garantizados
            
            int oldVida = rec.vida;
            int oldAtq = rec.ataque;
            int oldVel = rec.velocidad;
            float oldAsf = rec.asfixia;
            float oldInv = rec.invencibilidad;

            for (int i = 0; i < numExtras; i++)
            {
                int extraRndStat = Random.Range(0, maxRnd);
                bool esPositivo = Random.value > 0.5f;
                AplicarStatAleatorio(rec, extraRndStat, baseStat, bonusA, bonusI, esPositivo);
            }
            
            float netVida = rec.vida - oldVida;
            float netAtq = rec.ataque - oldAtq;
            float netVel = rec.velocidad - oldVel;
            float netAsf = rec.asfixia - oldAsf;
            float netInv = rec.invencibilidad - oldInv;

            float revelationRoll = Random.Range(0f, 100f) + sabiduria * 5f;
            int tierRevelacion = 0;
            if (revelationRoll >= 90f) tierRevelacion = 3;
            else if (revelationRoll >= 60f) tierRevelacion = 2;
            else if (revelationRoll >= 30f) tierRevelacion = 1;
            
            rec.textoUI += "\n✨ ";
            
            if (tierRevelacion == 0)
            {
                rec.textoUI += "Sientes un poder extraño...";
            }
            else if (tierRevelacion == 1)
            {
                rec.textoUI += "Alteración: " + ConstruirTextoNivel1(netVida, netAtq, netVel, netAsf, netInv);
            }
            else if (tierRevelacion == 2)
            {
                rec.textoUI += "Efectos: " + ConstruirTextoNivel2(netVida, netAtq, netVel, netAsf, netInv);
            }
            else if (tierRevelacion == 3)
            {
                rec.textoUI += "Detalle: " + ConstruirTextoNivel3(netVida, netAtq, netVel, netAsf, netInv);
            }
        }
        
        return rec;
    }

    private void AplicarStat(RecompensaData rec, int rndStat, int baseStat, float bonusA, float bonusI, bool positivo)
    {
        int sign = positivo ? 1 : -1;
        if (rndStat == 0) rec.vida += baseStat * sign;
        else if (rndStat == 1) rec.ataque += baseStat * sign;
        else if (rndStat == 2) rec.velocidad += baseStat * sign;
        else if (rndStat == 3) rec.asfixia += bonusA * sign;
        else if (rndStat == 4) rec.invencibilidad += bonusI * sign;
    }

    private void AplicarStatAleatorio(RecompensaData rec, int rndStat, int baseStat, float bonusA, float bonusI, bool positivo)
    {
        int sign = positivo ? 1 : -1;
        
        // Calcular valores aleatorios basados en la calidad (baseStat o bonus)
        int randomInt = Random.Range(baseStat, (baseStat * 3) + 1);
        float randomFloatA = Random.Range(bonusA, bonusA * 3f);
        float randomFloatI = Random.Range(bonusI, bonusI * 3f);

        if (rndStat == 0) rec.vida += randomInt * sign;
        else if (rndStat == 1) rec.ataque += randomInt * sign;
        else if (rndStat == 2) rec.velocidad += randomInt * sign;
        else if (rndStat == 3) rec.asfixia += randomFloatA * sign;
        else if (rndStat == 4) rec.invencibilidad += randomFloatI * sign;
    }

    private string TextoGenericoPorStat(int rndStat, int baseStat, float bonusA, float bonusI, string label)
    {
        if (rndStat == 0) return $"❤️ Vida Máxima +{baseStat}";
        if (rndStat == 1) return $"⚔️ Ataque +{baseStat}";
        if (rndStat == 2) return $"💨 Velocidad +{baseStat}";
        if (rndStat == 3) return $"🔵 [{label}] Pulmones de Acero — Asfixia +{bonusA:0.##}s";
        if (rndStat == 4) return $"🛡️ [{label}] Escamas Endurecidas — Invenc. +{bonusI:0.##}s";
        return "";
    }

    private string ConstruirTextoNivel1(float dVida, float dAtq, float dVel, float dAsf, float dInv)
    {
        List<string> stats = new List<string>();
        if (Mathf.Abs(dVida) > 0.01f) stats.Add("Vida");
        if (Mathf.Abs(dAtq) > 0.01f) stats.Add("Ataque");
        if (Mathf.Abs(dVel) > 0.01f) stats.Add("Velocidad");
        if (Mathf.Abs(dAsf) > 0.01f) stats.Add("Asfixia");
        if (Mathf.Abs(dInv) > 0.01f) stats.Add("Invencibilidad");
        
        if (stats.Count == 0) return "Sientes que algo cambió...";
        return string.Join(", ", stats);
    }

    private string ConstruirTextoNivel2(float dVida, float dAtq, float dVel, float dAsf, float dInv)
    {
        List<string> stats = new List<string>();
        if (Mathf.Abs(dVida) > 0.01f) stats.Add("Vida" + (dVida > 0 ? "(+)" : "(-)"));
        if (Mathf.Abs(dAtq) > 0.01f) stats.Add("Atq" + (dAtq > 0 ? "(+)" : "(-)"));
        if (Mathf.Abs(dVel) > 0.01f) stats.Add("Vel" + (dVel > 0 ? "(+)" : "(-)"));
        if (Mathf.Abs(dAsf) > 0.01f) stats.Add("Asf" + (dAsf > 0 ? "(+)" : "(-)"));
        if (Mathf.Abs(dInv) > 0.01f) stats.Add("Inv" + (dInv > 0 ? "(+)" : "(-)"));
        
        if (stats.Count == 0) return "Cambios ocultos";
        return string.Join(", ", stats);
    }

    private string ConstruirTextoNivel3(float dVida, float dAtq, float dVel, float dAsf, float dInv)
    {
        List<string> stats = new List<string>();
        if (Mathf.Abs(dVida) > 0.01f) stats.Add((dVida > 0 ? $"+{dVida}" : $"{dVida}") + " Vida");
        if (Mathf.Abs(dAtq) > 0.01f) stats.Add((dAtq > 0 ? $"+{dAtq}" : $"{dAtq}") + " Atq");
        if (Mathf.Abs(dVel) > 0.01f) stats.Add((dVel > 0 ? $"+{dVel}" : $"{dVel}") + " Vel");
        if (Mathf.Abs(dAsf) > 0.01f) stats.Add((dAsf > 0 ? $"+{dAsf:0.##}" : $"{dAsf:0.##}") + " Asf");
        if (Mathf.Abs(dInv) > 0.01f) stats.Add((dInv > 0 ? $"+{dInv:0.##}" : $"{dInv:0.##}") + " Inv");
        
        if (stats.Count == 0) return "Sin cambios adicionales";
        return string.Join(" | ", stats);
    }

    private Color ColorPorCalidad(Calidad calidad)
    {
        switch (calidad)
        {
            case Calidad.Looser:     return new Color(0.40f, 0.40f, 0.40f);
            case Calidad.Comun:      return new Color(0.85f, 0.85f, 0.85f);
            case Calidad.PocoComun:  return new Color(0.30f, 0.80f, 0.30f);
            case Calidad.Raro:       return new Color(0.20f, 0.50f, 1.00f);
            case Calidad.Epico:      return new Color(0.65f, 0.20f, 0.90f);
            case Calidad.Legendario: return new Color(1.00f, 0.60f, 0.10f);
            case Calidad.Mitico:     return new Color(1.00f, 0.15f, 0.15f);
            default:                 return Color.white;
        }
    }

    private (float bonusAsfixia, float bonusInvenc, string label) ObtenerBonusEspecialPorCalidad(Calidad calidad)
    {
        switch (calidad)
        {
            case Calidad.Raro:        return (0.50f, 0.20f, "Raro");
            case Calidad.Epico:       return (0.65f, 0.27f, "Épico");
            case Calidad.Legendario:  return (0.85f, 0.35f, "Legendario");
            case Calidad.Mitico:      return (1.10f, 0.45f, "Mítico");
            default:                  return (0.50f, 0.20f, "Raro");
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

    private CalidadPool ElegirCalidad(int suerte)
    {
        float total = 0f;
        foreach (var pool in data.pools)
            total += pool.probabilidadGlobal;

        float suerteOffset = Mathf.Clamp(suerte * total * 0.05f, 0f, total * 0.5f);
        float rng = Random.Range(0f, total) + suerteOffset;

        float acumulado = 0f;
        foreach (var pool in data.pools)
        {
            acumulado += pool.probabilidadGlobal;
            if (rng <= acumulado)
                return pool;
        }

        return data.pools[data.pools.Length - 1];
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

    private void AplicarMejora(RecompensaData recompensa)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            if (recompensa.vida != 0)
            {
                SaludTiburon salud = player.GetComponent<SaludTiburon>();
                if (salud != null) salud.addMaxHealth(recompensa.vida);
            }
            if (recompensa.ataque != 0)
            {
                AtaqueTiburon ataque = player.GetComponent<AtaqueTiburon>();
                if (ataque != null) ataque.addDamage(recompensa.ataque);
            }
            if (recompensa.velocidad != 0)
            {
                MovimientoTiburon mov = player.GetComponent<MovimientoTiburon>();
                if (mov != null) mov.addVelocidad((float)recompensa.velocidad);
            }
            if (recompensa.asfixia != 0)
            {
                AsfixiaTiburon asfixia = player.GetComponent<AsfixiaTiburon>();
                if (asfixia != null) asfixia.addTiempoLimiteQuieto(recompensa.asfixia);
            }
            if (recompensa.invencibilidad != 0)
            {
                SaludTiburon salud = player.GetComponent<SaludTiburon>();
                if (salud != null) salud.addDuracionInvencibilidad(recompensa.invencibilidad);
            }
        }

        Debug.Log($"Mejora aplicada. Se destruye la tragaperras.");
        Destroy(gameObject);
    }
}
