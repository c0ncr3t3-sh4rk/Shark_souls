using UnityEngine;

public class AnimacionEnemigo : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("Al Nadar")]
    [SerializeField] private float velocidadMeneito = 25f;
    [SerializeField] private float anguloMeneito = 6f;

    [Header("Al Estar Quieto")]
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

        Vector3 eulerActual = transform.localEulerAngles;
        eulerActual.z -= ultimoModificadorZ;

        if (rb.linearVelocity.magnitude < 0.2f)
        {
            ultimoModificadorZ = Mathf.Sin(Time.time * velocidadIdle) * anguloIdle;
        }
        else
        {
            ultimoModificadorZ = Mathf.Sin(Time.time * velocidadMeneito) * anguloMeneito;
        }

        eulerActual.z += ultimoModificadorZ;
        transform.localEulerAngles = eulerActual;
    }
}