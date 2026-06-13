using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class AtaqueTiburon : MonoBehaviour
{
    [Header("Sprites de Animación Cutre")]
    [SerializeField] private Sprite spriteNormal; 
    [SerializeField] private Sprite spriteMordisco; 
    [SerializeField] private float duracionMordisco = 0.15f; 

    private SpriteRenderer spriteRenderer; 
    private bool estaMordiendo = false; 

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteNormal != null) spriteRenderer.sprite = spriteNormal;
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed && !estaMordiendo)
        {
            StartCoroutine(RutinaMordisco());
        }
    }

    private IEnumerator RutinaMordisco()
    {
        estaMordiendo = true;
        
        // Buscamos el script de movimiento en el tiburón
        MovimientoTiburon movimiento = GetComponent<MovimientoTiburon>();
        // Si lo encuentra, mira su flipX. Si no, por defecto es false.
        bool mirarIzquierda = (movimiento != null) ? movimiento.spriteRenderer.flipX : false;

        if (spriteMordisco != null) 
        {
            spriteRenderer.sprite = spriteMordisco;
            spriteRenderer.flipX = mirarIzquierda; // Mantiene la dirección
        }

        yield return new WaitForSeconds(duracionMordisco);

        if (spriteNormal != null) 
        {
            spriteRenderer.sprite = spriteNormal;
            spriteRenderer.flipX = mirarIzquierda; // Mantiene la dirección al volver
        }

        estaMordiendo = false;
    }
}