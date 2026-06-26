using UnityEngine;

public class AnimacionEnemigo : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("Meneíto Rígido (Al Nadar)")]
    [SerializeField] private float velocidadMeneito = 25f;
    [SerializeField] private float anguloMeneito = 6f;

    [Header("Balanceo Boya (Al Estar Quieto)")]
    [SerializeField] private float velocidadIdle = 3f;
    [SerializeField] private float anguloIdle = 2.5f;

    private float ultimoModificadorZ = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void LateUpdate()
    {
        if (rb == null) return;

        // 1. Eliminamos el efecto del frame anterior para dejar la rotación limpia
        Vector3 eulerActual = transform.localEulerAngles;
        eulerActual.z -= ultimoModificadorZ;

        // 2. Calculamos el nuevo meneíto basándonos en la velocidad actual
        if (rb.linearVelocity.magnitude < 0.2f)
        {
            ultimoModificadorZ = Mathf.Sin(Time.time * velocidadIdle) * anguloIdle;
        }
        else
        {
            ultimoModificadorZ = Mathf.Sin(Time.time * velocidadMeneito) * anguloMeneito;
        }

        // 3. Aplicamos el meneíto sobre la rotación ya establecida por la IA
        eulerActual.z += ultimoModificadorZ;
        transform.localEulerAngles = eulerActual;
    }
}