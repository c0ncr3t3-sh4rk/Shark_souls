using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Combo : MonoBehaviour
{
    public static Combo Instancia;

    [Header("Referencias")]
    private TextMeshProUGUI texto;
    [SerializeField] private GameObject prefabCuadrado; 
    [SerializeField] private Transform contenedorCuadrados;
    private List<Image> listaCuadrados = new List<Image>();

    [Header("Ajustes")]
    private int comboActual = 0;
    private float tiempoUltimaBaja = 0f;
    [SerializeField] private float tiempoParaPerderCombo = 10f;
    
    // Variables para el temblor
    private Vector3 posOriginal;

    private void Awake() 
    { 
        Instancia = this; 
        texto = GetComponent<TextMeshProUGUI>();
        posOriginal = transform.localPosition;
        
        // Crear los cuadrados al iniciar
        for (int i = 0; i < 7; i++) // Ajusta a los que quieras
        {
            GameObject obj = Instantiate(prefabCuadrado, contenedorCuadrados);
            listaCuadrados.Add(obj.GetComponent<Image>());
        }
    }

    private void Update()
    {
        // 1. Lógica de tiempo con cuadrados
        float tiempoRestante = tiempoParaPerderCombo - (Time.time - tiempoUltimaBaja);
        float porcentaje = Mathf.Clamp01(tiempoRestante / tiempoParaPerderCombo);
        
        int cuadradosVisibles = Mathf.CeilToInt(porcentaje * listaCuadrados.Count);
        for (int i = 0; i < listaCuadrados.Count; i++)
        {
            listaCuadrados[i].enabled = (i < cuadradosVisibles && comboActual > 0);
        }

        // 2. Reset combo
        if (comboActual > 0 && Time.time - tiempoUltimaBaja > tiempoParaPerderCombo)
        {
            comboActual = 0;
            texto.text = "";
        }

        // 3. Efecto temblor si es mayor a 50
        if (comboActual >= 50)
        {
            transform.localPosition = posOriginal + (Vector3)Random.insideUnitCircle * 5f;
        }
    }

    public void RegistrarBaja()
    {
        comboActual++;
        tiempoUltimaBaja = Time.time;
        
        texto.text = "X " + comboActual;
        
        // Cambio de colores según hitos
        if (comboActual >= 50) texto.color = Color.red;
        else if (comboActual >= 25) texto.color = Color.magenta;
        else if (comboActual >= 10) texto.color = Color.yellow;
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