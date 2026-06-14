using UnityEngine;
using UnityEngine.InputSystem;

public class MovimientoTiburon : MonoBehaviour
{
    [SerializeField] private float velocidad = 15f; 

    private Rigidbody2D rb;
    private Vector2 inputMovimiento;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void OnMove(InputValue value)
    {
        inputMovimiento = value.Get<Vector2>();
        GirarSprite();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = inputMovimiento * velocidad;
    }

    private void GirarSprite()
    {
        // Si pulsas la A (izquierda), rotamos el objeto 180 grados en el eje Y
        if (inputMovimiento.x > 0f)
        {
            transform.eulerAngles = new Vector3(0f, 180f, 0f);
        }
        // Si pulsas la D (derecha), restauramos la rotación original (0 grados)
        else if (inputMovimiento.x < 0f)
        {
            transform.eulerAngles = new Vector3(0f, 0f, 0f);
        }
    }

    public Vector2 ObtenerDireccionInput()
    {
        return inputMovimiento;
    }
}