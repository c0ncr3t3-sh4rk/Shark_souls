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
        
        // Cambiamos al sprite de morder (se adaptará a la rotación del objeto automáticamente)
        if (spriteMordisco != null) spriteRenderer.sprite = spriteMordisco;

        yield return new WaitForSeconds(duracionMordisco);

        // Volvemos al sprite normal
        if (spriteNormal != null) spriteRenderer.sprite = spriteNormal;

        estaMordiendo = false;
    }
}