using System;

/// <summary>
/// Interfaz común para todos los bosses del juego.
/// Permite que SalaBoss funcione con cualquier tipo de boss
/// sin depender de una clase concreta.
/// </summary>
public interface IBoss
{
    /// <summary>Evento que se dispara cuando el boss muere.</summary>
    event Action OnBossMuerto;

    /// <summary>Activa el boss e inicia el combate.</summary>
    void IniciarCombate();
}
