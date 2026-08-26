using KanbanBoard.Data;
using KanbanBoard.Models;
using KanbanBoard.Models.Requests;
using KanbanBoard.Models.Responses;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.Conventions;
using System.Threading.Tasks;

namespace KanbanBoard.Services
{
    public class AttachmentService
    {
        private readonly KanbanBoardDbContext _db;

        public AttachmentService(KanbanBoardDbContext dbContext)
        {
            _db = dbContext;
        }

        private async Task<bool> IsUserBoardMemberAsync(int boardId, int userId, CancellationToken ct)
        {
            return await _db.BoardUsers.AnyAsync(bu => bu.BoardId == boardId && bu.UserId == userId, ct);


        }
    }
}
