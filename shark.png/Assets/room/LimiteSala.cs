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
            if (!gameObject.activeInHierarchy || !collision.gameObject.activeInHierarchy) return;

            if (((1 << collision.gameObject.layer) & layerMaskEnemigos) == 0) return;

            IAgarrable agarrable = collision.GetComponent<IAgarrable>() ?? collision.GetComponentInParent<IAgarrable>();
            if (agarrable != null && agarrable.EstaAgarrado) return;

            VidaEnemigo vida = collision.GetComponent<VidaEnemigo>() ?? collision.GetComponentInParent<VidaEnemigo>();
            if (vida != null)
                vida.Morir();
            else
                Destroy(collision.gameObject);
        }
    }
}
