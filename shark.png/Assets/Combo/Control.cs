using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Combo : MonoBehaviour
{
    public static Combo Instancia;
    private bool esMulticolor = false;

    [Header("Referencias")]
    private TextMeshProUGUI texto;
    [SerializeField] private GameObject prefabDientes; 
    [SerializeField] private Transform contenedorDientes;

    [Header("Ajustes")]
    [SerializeField] private float tiempoParaPerderCombo = 10f;
    
    private List<Image> listaDientes = new List<Image>();
    private int comboActual = 0;
    private float tiempoUltimaBaja = 0f;
    private Vector3 posOriginal;

    private void Awake() 
    { 
        Instancia = this; 
        texto = GetComponent<TextMeshProUGUI>();
        posOriginal = transform.localPosition;
        
        for (int i = 0; i < 7; i++)
        {
            GameObject obj = Instantiate(prefabDientes, contenedorDientes);
            listaDientes.Add(obj.GetComponent<Image>());
        }
    }

    private void Update()
    {
        float tiempoRestante = tiempoParaPerderCombo - (Time.time - tiempoUltimaBaja);
        float porcentaje = Mathf.Clamp01(tiempoRestante / tiempoParaPerderCombo);
        
        int cuadradosVisibles = Mathf.CeilToInt(porcentaje * listaDientes.Count);
        for (int i = 0; i < listaDientes.Count; i++)
        {
            listaDientes[i].enabled = (i < cuadradosVisibles && comboActual > 0);
        }

        if (comboActual > 0 && Time.time - tiempoUltimaBaja > tiempoParaPerderCombo)
        {
            comboActual = 0;
            texto.text = "";
        }

        if (comboActual >= 100)
        {
            transform.localPosition = posOriginal + (Vector3)Random.insideUnitCircle * 10f;
        }
        else if (comboActual >= 50)
        {
            transform.localPosition = posOriginal + (Vector3)Random.insideUnitCircle * 5f;
        }
        else if (comboActual >= 25)
        {
            transform.localPosition = posOriginal + (Vector3)Random.insideUnitCircle * 2f;
        }
        else if (comboActual >= 10)
        {
            transform.localPosition = posOriginal + (Vector3)Random.insideUnitCircle * 1f;
        }

        if (comboActual >= 100)
        {
            float hue = Mathf.PingPong(Time.time * 2f, 1f); 
            texto.color = Color.HSVToRGB(hue, 1f, 1f);
            
            texto.fontSize = 170 + Mathf.Sin(Time.time * 10f) * 10f; 
        }
        else
        {
            texto.fontSize = 170; 
        }
    }

    public void Kill()
    {
        comboActual++;
        tiempoUltimaBaja = Time.time;
        
        texto.text = "X " + comboActual;
        
        // Cambio de colores según hitos
        if (comboActual >= 100) texto.color = Color.magenta;
        else if (comboActual >= 50) texto.color = Color.red;
        else if (comboActual >= 25) texto.color = Color.magenta;
        else if (comboActual >= 10) texto.color = Color.green;
        else if (comboActual >= 5) texto.color = Color.yellow;
        else texto.color = Color.white;

        StopAllCoroutines();
        StartCoroutine(EfectoPulso());
    }

    private IEnumerator EfectoPulso()
    {
        transform.localScale = Vector3.one * 1.1f;
        yield return new WaitForSeconds(0.1f);
        transform.localScale = Vector3.one;
    }
}