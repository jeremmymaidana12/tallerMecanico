namespace TallerMecanico.Modelos;

/// <summary>Base de los modelos que se guardan con un ServicioBase.</summary>
public abstract class EntidadBase
{
    /// <summary>0 = todavia no se guardo en la base.</summary>
    public long Id { get; set; }
}
