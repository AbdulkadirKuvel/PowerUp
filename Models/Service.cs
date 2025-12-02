using System.ComponentModel.DataAnnotations;

namespace PowerUp.Models;

public class Service
{
    public int Id {get; set;}

    [Required(ErrorMessage = "Lütfen İsim giriniz.")]
    [MaxLength(50, ErrorMessage = "İsim En Fazla 50 Karakter Olabilir.")]
    public required string Name {get; set;}

    public ICollection<TrainerService> TrainerServices { get; set; } = [];
}