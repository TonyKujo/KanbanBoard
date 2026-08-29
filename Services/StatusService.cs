using KanbanBoard.Data;
using KanbanBoard.Models;
using KanbanBoard.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace KanbanBoard.Services
{
    public class StatusService
    {
        private readonly KanbanBoardDbContext _db;

        public StatusService(KanbanBoardDbContext dbContext)
        {
            _db = dbContext;
        }

        private async Task<bool> IsUserBoardMemberAsync(int boardId, int userId, CancellationToken ct)
        {
            return await _db.BoardUsers.AnyAsync(bu => bu.BoardId == boardId && bu.UserId == userId && !bu.IsDeleted, ct);
        }

        public async Task<List<StatusResponse>?> GetBoardStatusesAsync(int boardId, int userId, CancellationToken ct)
        {
            if (!await IsUserBoardMemberAsync(boardId, userId, ct))
            {
                return null;
            }

            var boardStatuses = await _db.Statuses
                .Where(s => s.BoardId == boardId)
                .Select(s => new StatusResponse
                {
                    StatusId = s.StatusId,
                    StatusName = s.StatusName,
                })
                .ToListAsync(ct);


            return boardStatuses;

        }

        public async System.Threading.Tasks.Task CreateDefaultStatusesAsync(int boardId, CancellationToken ct)
        {
            var newStatuses = new List<Status>
            {
                new Status { BoardId = boardId, StatusName = "To Do" },
                new Status { BoardId = boardId, StatusName = "In Progress" },
                new Status { BoardId = boardId, StatusName = "Done" },
            };


            _db.Statuses.AddRange(newStatuses);

            await _db.SaveChangesAsync(ct);
            
        }
    }
}
