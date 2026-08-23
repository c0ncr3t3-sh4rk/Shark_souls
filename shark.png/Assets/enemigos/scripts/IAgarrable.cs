using UnityEngine;

public interface IAgarrable
{
    bool EstaAgarrado { get; }
    void EnAgarrar(Transform boca);
    void EnSoltar();
}
