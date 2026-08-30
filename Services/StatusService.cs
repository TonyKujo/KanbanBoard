using KanbanBoard.Data;
using KanbanBoard.Models;
using KanbanBoard.Models.Requests;
using KanbanBoard.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace KanbanBoard.Services
{
    public enum DeleteStatusResult
    {
        NotFound,
        LastStatus,
        HasTasks,
        Deleted
    }

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

        private async Task<bool> IsUserBoardOwnerAsync(int boardId, int userId, CancellationToken ct)
        {
            return await _db.Boards.AnyAsync(b => b.BoardId == boardId && b.AuthorId == userId, ct);
        }

        public async Task<List<StatusResponse>?> GetBoardStatusesAsync(int boardId, int userId, CancellationToken ct)
        {
            if (!await IsUserBoardMemberAsync(boardId, userId, ct))
            {
                return null;
            }

            var boardStatuses = await _db.Statuses
                .Where(s => s.BoardId == boardId)
                .OrderBy(s => s.Order)
                .ThenBy(s => s.StatusId)
                .Select(s => new StatusResponse
                {
                    StatusId = s.StatusId,
                    StatusName = s.StatusName,
                    Order = s.Order,
                })
                .ToListAsync(ct);


            return boardStatuses;

        }

        public async Task<StatusResponse?> CreateStatusAsync(int boardId, int userId, StatusRequest request, CancellationToken ct)
        {
            if (!await IsUserBoardOwnerAsync(boardId, userId, ct))
                return null;

            var maxOrder = await _db.Statuses
                .Where(s => s.BoardId == boardId)
                .MaxAsync(s => (int?)s.Order, ct) ?? -1;

            var status = new Status
            {
                BoardId = boardId,
                StatusName = request.Name,
                Order = maxOrder + 1
            };

            _db.Statuses.Add(status);
            await _db.SaveChangesAsync(ct);

            return new StatusResponse
            {
                StatusId = status.StatusId,
                StatusName = status.StatusName,
                Order = status.Order
            };
        }

        public async Task<StatusResponse?> UpdateStatusAsync(int boardId, int userId, int statusId, StatusRequest request, CancellationToken ct)
        {
            if (!await IsUserBoardOwnerAsync(boardId, userId, ct))
                return null;

            var status = await _db.Statuses
                .FirstOrDefaultAsync(s => s.StatusId == statusId && s.BoardId == boardId, ct);

            if (status == null)
                return null;

            status.StatusName = request.Name;

            await _db.SaveChangesAsync(ct);

            return new StatusResponse
            {
                StatusId = status.StatusId,
                StatusName = status.StatusName,
                Order = status.Order
            };
        }

        public async Task<DeleteStatusResult> DeleteStatusAsync(int boardId, int userId, int statusId, CancellationToken ct)
        {
            if (!await IsUserBoardOwnerAsync(boardId, userId, ct))
                return DeleteStatusResult.NotFound;

            var status = await _db.Statuses
                .FirstOrDefaultAsync(s => s.StatusId == statusId && s.BoardId == boardId, ct);

            if (status == null)
                return DeleteStatusResult.NotFound;

            var statusesCount = await _db.Statuses.CountAsync(s => s.BoardId == boardId, ct);
            if (statusesCount <= 1)
                return DeleteStatusResult.LastStatus;

            var hasTasks = await _db.Tasks.AnyAsync(t => t.StatusId == statusId, ct);
            if (hasTasks)
                return DeleteStatusResult.HasTasks;

            _db.Statuses.Remove(status);
            await _db.SaveChangesAsync(ct);

            var restStatuses = await _db.Statuses
                .Where(s => s.BoardId == boardId)
                .OrderBy(s => s.Order)
                .ThenBy(s => s.StatusId)
                .ToListAsync(ct);

            for (var i = 0; i < restStatuses.Count; i++)
                restStatuses[i].Order = i;

            await _db.SaveChangesAsync(ct);

            return DeleteStatusResult.Deleted;
        }

        public async System.Threading.Tasks.Task CreateDefaultStatusesAsync(int boardId, CancellationToken ct)
        {
            var newStatuses = new List<Status>
            {
                new Status { BoardId = boardId, StatusName = "To Do", Order = 0 },
                new Status { BoardId = boardId, StatusName = "In Progress", Order = 1 },
                new Status { BoardId = boardId, StatusName = "Done", Order = 2 },
            };


            _db.Statuses.AddRange(newStatuses);

            await _db.SaveChangesAsync(ct);
            
        }
    }
}
