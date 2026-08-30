using System.ComponentModel.DataAnnotations;

namespace KanbanBoard.Models.Requests;

public class RegisterRequest
{
    [Required]
    [MaxLength(100)]
    public string Login { get; set; } = null!;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = null!;
}
