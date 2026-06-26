using UnityEngine;
using UnityEngine.InputSystem;

public class MovimientoTiburon : MonoBehaviour
{
    [SerializeField] private float velocidad = 15f; 

    private Rigidbody2D rb;
    private Vector2 inputMovimiento;
    float rotY = 0f;
    float rotZ = 0f;

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
        // HORIZONTAL
        if (inputMovimiento.x > 0f)
        {
            rotY = 180f; // Derecha
        }
        else if (inputMovimiento.x < 0f)
        {
            rotY = 0f; // Izquierda
        }

        // VERTICAL
        if (inputMovimiento.y > 0f)
        {
            rotZ = -45f; // Diagonal hacia arriba
        }
        else if (inputMovimiento.y < 0f)
        {
            rotZ = 45f;  // Diagonal hacia abajo
        }

        // APLICACION
        transform.eulerAngles = new Vector3(0f, rotY, rotZ);
    }

    public Vector2 getImput()
    {
        return inputMovimiento;
    }
}