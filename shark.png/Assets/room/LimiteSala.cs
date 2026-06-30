using UnityEngine;

namespace SharkSouls.Dungeon
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class LimiteSala : MonoBehaviour
    {
        private int layerMaskEnemigos;

        private void Awake()
        {
            layerMaskEnemigos = LayerMask.GetMask("Enemigos");
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (((1 << collision.gameObject.layer) & layerMaskEnemigos) == 0) return;

            VidaEnemigo vida = collision.GetComponent<VidaEnemigo>();
            if (vida != null)
                vida.Morir();
            else
                Destroy(collision.gameObject);
        }
    }
}
