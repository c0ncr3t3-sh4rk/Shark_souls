using UnityEngine;
using System.Collections.Generic;

public enum Calidad
{
    Looser,
    Comun,
    PocoComun,
    Raro,
    Epico,
    Legendario,
    Mitico
}

[System.Serializable]
public class DropEntry
{
    public string nombre;

    public Calidad calidad;

    [Range(0, 100)]
    public float probabilidadDentroDeCalidad; 
    // Ej: dentro de "Común", vida = 25%, ataque = 30%

    public float ataque;
    public float vida;
    public float velocidad;

    public GameObject objetoPrefab;

    [Header("Requisitos opcionales")]
    public string requiereNombre; // si necesita otro drop previo
    public bool requiereTenerObjeto; // si debe estar en inventario
}

[System.Serializable]
public class CalidadPool
{
    public Calidad calidad;

    [Range(0, 100)]
    public float probabilidadGlobal; 
    // Ej: Común = 35%

    public DropEntry[] drops;
}

[CreateAssetMenu(fileName = "SlotMachineData", menuName = "SlotMachine/Data")]
public class SlotMachineData : ScriptableObject
{
    public CalidadPool[] pools;
}
