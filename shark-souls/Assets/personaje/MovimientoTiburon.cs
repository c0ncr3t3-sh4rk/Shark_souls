using UnityEngine;
using UnityEngine.InputSystem;

public class MovimientoTiburon : MonoBehaviour
{
    [SerializeField] private float velocidad = 15f; 

    private Rigidbody2D rb;
    private Vector2 inputMovimiento;
    private bool direccion = false; // false = Izquierda, true = Derecha

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
        // 1. MEMORIA HORIZONTAL: Solo actualiza si realmente estás pulsando izquierda o derecha
        if (inputMovimiento.x > 0f)
        {
            direccion = true; // Derecha
        }
        else if (inputMovimiento.x < 0f)
        {
            direccion = false; // Izquierda
        }

        // 2. GIRO (Eje Y): Aplicamos los 180º o 0º según la memoria de la dirección
        float rotY = direccion ? 180f : 0f;

        // 3. INCLINACIÓN (Eje Z): Calculamos si sube, baja o está en horizontal puro
        float rotZ = 0f;

        if (inputMovimiento.y > 0f)
        {
            rotZ = -45f; // Diagonal hacia arriba (tanto si vas solo arriba como en diagonal)
        }
        else if (inputMovimiento.y < 0f)
        {
            rotZ = 45f;  // Diagonal hacia abajo (tanto si vas solo abajo como en diagonal)
        }

        // 4. APLICACIÓN: Un único cambio de eulerAngles sin conflictos de ifs
        transform.eulerAngles = new Vector3(0f, rotY, rotZ);
    }

    public Vector2 ObtenerDireccionInput()
    {
        return inputMovimiento;
    }
}