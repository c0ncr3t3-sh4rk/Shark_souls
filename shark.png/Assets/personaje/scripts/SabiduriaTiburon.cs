using UnityEngine;

public class SabiduriaTiburon : MonoBehaviour
{
    [SerializeField] private int sabiduria = 0;

    public int GetSabiduria()
    {
        return sabiduria;
    }

    public void AddSabiduria(int cantidad)
    {
        sabiduria += cantidad;
        Debug.Log($"Sabiduría incrementada. Sabiduría actual: {sabiduria}");
    }
}
