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

        public override int SaveChanges()
        {
            DeleteAttachmentFiles();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            DeleteAttachmentFiles();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void DeleteAttachmentFiles()
        {
            var deletedAttachments = ChangeTracker.Entries<Attachment>()
                .Where(e => e.State == EntityState.Deleted)
                .Select(e => e.Entity)
                .ToList();

            foreach (var attachment in deletedAttachments)
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), attachment.FilePath);
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BoardUser>()
            .HasIndex(bu => new { bu.UserId, bu.BoardId })
            .IsUnique();

            modelBuilder.Entity<BoardUser>().HasQueryFilter(bu => !bu.IsDeleted);

            modelBuilder.Entity<Attachment>().ToTable(t => t
            .HasCheckConstraint(
                    "CK_Attachment_OneAttach",
                    "(\"TaskId\" IS NOT NULL AND \"CommentId\" IS NULL) OR (\"TaskId\" IS NULL AND \"CommentId\" IS NOT NULL)"
            ));

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Login)
                .IsUnique();
        }
    }
}
