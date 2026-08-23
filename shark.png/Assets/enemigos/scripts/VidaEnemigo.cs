using UnityEngine;
using System;
using System.Collections;

public class VidaEnemigo : MonoBehaviour
{
    public enum TipoMuerte
    {
        Normal,
        Mordisco,
        Bapuleo,
        proyectil
    }

    [SerializeField] private int vidaMaxima = 3;
    private int vidaActual;

    public event Action<int, int> OnVidaCambiada;
    public event Action OnMuerto;
    public int VidaActual => vidaActual;
    public int VidaMaxima => vidaMaxima;

    [Header("Visuales")]
    [SerializeField] private GameObject sangre;
    [SerializeField] private GameObject sangreMuerte;
    public SharkSouls.Dungeon.SalaBase salaAsignada;
    [HideInInspector] public int costePresupuesto;
    [HideInInspector] public GameObject prefabOrigen;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        vidaActual = vidaMaxima;
        if (sr != null) sr.color = Color.white;
    }

    public bool RecibirDano(int cantidad, TipoMuerte tipoDaño)
    {
        vidaActual -= cantidad;
        OnVidaCambiada?.Invoke(vidaActual, vidaMaxima);

        if (Combo.Instancia != null)
            Combo.Instancia.Hit();

        if (sangre != null)
        {
            GameObject objSangre = Instantiate(sangre, transform.position, Quaternion.identity);
            Destroy(objSangre, 1f); 
        }

        if (sr != null)
            StartCoroutine(EfectoDano());

        if (vidaActual <= 0)
        {
            OnMuerto?.Invoke();
            Morir(tipoDaño);
            return true;
        } else
        {
            IEnemigo enemigo = GetComponent<IEnemigo>();

            if (enemigo != null)
            {
                Debug.Log("stun de 0.5 por ataque");
                enemigo.SumarAturdimiento(0.5f);
            }
        }
        return false;
    }

    private IEnumerator EfectoDano()
    {
        sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        sr.color = Color.white;
    }

    public void Morir(TipoMuerte tipoMuerte)
    {
        if (sangreMuerte != null)
        {
            GameObject objSangre = Instantiate(sangreMuerte, transform.position, Quaternion.identity);
            Destroy(objSangre, 1f); 
        }

        if (Combo.Instancia != null)
            Combo.Instancia.Kill();
        
        if (salaAsignada != null)
        {
            salaAsignada.EnemigoEliminado(gameObject);
        }

        if (SharkSouls.Utils.SimpleObjectPool.Instance != null)
            SharkSouls.Utils.SimpleObjectPool.Instance.ReturnToPool(gameObject);
        else
            Destroy(gameObject);
    }

    public void Curar(int cantidad)
    {
        vidaActual = Mathf.Min(vidaActual + cantidad, vidaMaxima);
        OnVidaCambiada?.Invoke(vidaActual, vidaMaxima);
    }
}