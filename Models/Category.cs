using System.ComponentModel.DataAnnotations;

namespace UltraPokemon.Models;

public class Category
{
    public int Id { get; set; }

    [Display(Name = "Tên Hệ Phái / Bộ Bài")]
    [Required(ErrorMessage = "Tên hệ phái không được để trống!")]
    [StringLength(100, ErrorMessage = "Tên tối đa 100 ký tự")]
    public string Name { get; set; } = string.Empty;
}
