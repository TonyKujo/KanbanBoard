using System.ComponentModel.DataAnnotations;

namespace KanbanBoard.Models.Requests;

public class LoginRequest
{
    [Required]
    [MaxLength(100)]
    public string Login { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}
