using System.ComponentModel.DataAnnotations;
namespace PowerUp.Models;

public class User
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Lütfen isim giriniz.")]
    public required string Name { get; set; }

    [EmailAddress(ErrorMessage = "Email adresini kontrol ediniz.")]
    [Required(ErrorMessage = "Lütfen bir email giriniz.")]
    public required string Email { get; set; }

    [MinLength(6, ErrorMessage = "Şifreniz en az 6 karakterden oluşmalıdır.")]
    [Required(ErrorMessage = "Lütfen bir şifre oluşturunuz.")]
    public required string Password { get; set; }
}