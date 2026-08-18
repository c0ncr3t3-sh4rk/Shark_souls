using UnityEngine;

public class SuerteTiburon : MonoBehaviour
{
    [SerializeField] private int suerte = 0;

    public int GetSuerte()
    {
        return suerte;
    }

    public void AddSuerte(int cantidad)
    {
        suerte += cantidad;
        Debug.Log($"Suerte incrementada. Suerte actual: {suerte}");
    }
}
