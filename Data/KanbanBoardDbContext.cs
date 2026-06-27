using KanbanBoard.Models;
using Microsoft.EntityFrameworkCore;
using Task = KanbanBoard.Models.Task;

namespace KanbanBoard.Data
{
    public class KanbanBoardDbContext : DbContext
    {
        public KanbanBoardDbContext(DbContextOptions<KanbanBoardDbContext> options) : base(options) { }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Board> Boards { get; set; }
        public DbSet<BoardUser> BoardUsers { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Status> Statuses { get; set; }
        public DbSet<Task> Tasks { get; set; }
        public DbSet<TaskStatusHistory> TaskStatusHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Attachment>().ToTable(t => 
                t.HasCheckConstraint(
                    "CK_Attachment_OneAttach",
                    "(\"TaskId\" IS NOT NULL AND \"CommentId\" IS NULL) OR (\"TaskId\" IS NULL AND \"CommentId\" IS NOT NULL)"
                ));

        }
    }
}
