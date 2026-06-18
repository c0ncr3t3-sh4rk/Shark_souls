using UnityEngine;
using TMPro;
using System.Collections;

public class Combo : MonoBehaviour
{
    public static Combo Instancia;

    private TextMeshProUGUI texto;
    private int comboActual = 0;
    private float tiempoUltimaBaja = 0f;
    [SerializeField] private float tiempoParaPerderCombo = 10f;

    private void Awake() 
    { 
        Instancia = this; 
        texto = GetComponent<TextMeshProUGUI>();
        texto.text = ""; 
    }

    private void Update()
    {
        // Lógica de reset del combo
        if (comboActual > 0 && Time.time - tiempoUltimaBaja > tiempoParaPerderCombo)
        {
            comboActual = 0;
            texto.text = "";
        }
    }

    public void Refrescar()
    {
        tiempoUltimaBaja = Time.time;
    }

    public void RegistrarBaja()
    {
        comboActual++;
        tiempoUltimaBaja = Time.time;
        
        texto.text = "X " + comboActual;
        
        // Animación de pulso rápida
        StopAllCoroutines();
        StartCoroutine(EfectoPulso());
    }

    private IEnumerator EfectoPulso()
    {
        transform.localScale = Vector3.one * 1.4f;
        yield return new WaitForSeconds(0.1f);
        transform.localScale = Vector3.one;
    }
}