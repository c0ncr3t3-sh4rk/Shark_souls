using UnityEngine;
using UnityEngine.InputSystem;

public class MovimientoTiburon : MonoBehaviour
{
    [SerializeField] private float velocidad = 5f; 

    private Rigidbody2D rb;
    // Esto ahora es público para que el script de ataque lo pueda leer
    public SpriteRenderer spriteRenderer { get; private set; } 

    private Vector2 inputMovimiento;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
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
        if (inputMovimiento.x < 0f)
        {
            spriteRenderer.flipX = false; 
        }
        else if (inputMovimiento.x > 0f)
        {
            spriteRenderer.flipX = true; 
        }
    }
}