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
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = inputMovimiento * velocidad;
    }

    public void addVelocidad(float addVelocidad)
    {
        velocidad += addVelocidad;
        if (velocidad == 0) velocidad = 0.1f;
    }

    public Vector2 getImput()
    {
        return inputMovimiento;
    }
}