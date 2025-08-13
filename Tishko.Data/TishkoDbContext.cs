namespace Tishko.Data;

public sealed class TishkoDbContext : DbContext
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Session> Sessions => Set<Session>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tishko");
        if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);
        var dbPath = Path.Combine(baseDir, "data.db");

        var cs = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Cache = SqliteCacheMode.Shared,
            Pooling = true,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        var conn = new SqliteConnection(cs);
        conn.Open();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA foreign_keys=ON;";
            cmd.ExecuteNonQuery();
        }

        optionsBuilder.UseSqlite(conn);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TaskItem
        var t = modelBuilder.Entity<TaskItem>();
        t.ToTable("Tasks");
        t.HasKey(x => x.Id);
        t.Property(x => x.Title).IsRequired().HasMaxLength(200);
        t.Property(x => x.Notes).HasMaxLength(4000);

        // Concurrency token: Version
        t.Property(x => x.Version).IsConcurrencyToken();

        // Indices
        t.HasIndex(x => new { x.IsCompleted, x.DueAtUtcTicks });      
        t.HasIndex(x => x.CompletedAtUtcTicks);                       
        t.HasIndex(x => x.CreatedAtUtcTicks);                        
        t.HasIndex(x => x.UpdatedAtUtcTicks);                         

        // Partial index: only completed tasks by completion date 
        t.HasIndex(nameof(TaskItem.CompletedAtUtcTicks))
         .HasFilter($"{nameof(TaskItem.CompletedAtUtcTicks)} IS NOT NULL");

        // Session
        var s = modelBuilder.Entity<Session>();
        s.ToTable("Sessions");
        s.HasKey(x => x.Id);
        s.HasOne(x => x.Task)
         .WithMany()
         .HasForeignKey(x => x.TaskId)
         .OnDelete(DeleteBehavior.SetNull);

        s.HasIndex(x => x.DayKey);              
        s.HasIndex(x => x.StartUtcTicks);      
        s.HasIndex(x => new { x.TaskId, x.StartUtcTicks });

        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges()
    {
        UpdateTimestampsAndVersion();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestampsAndVersion();
        return base.SaveChangesAsync(cancellationToken);
    }

    private static long UtcNowTicks() => DateTime.UtcNow.Ticks;

    private void UpdateTimestampsAndVersion()
    {
        var nowTicks = UtcNowTicks();

        foreach (var entry in ChangeTracker.Entries<TaskItem>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtcTicks = nowTicks;
                entry.Entity.UpdatedAtUtcTicks = nowTicks;
                entry.Entity.Version = 1;

                if (entry.Entity.IsCompleted && entry.Entity.CompletedAtUtcTicks is null)
                    entry.Entity.CompletedAtUtcTicks = nowTicks;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtcTicks = nowTicks;
                entry.Entity.Version++; 

                if (entry.Property(x => x.IsCompleted).IsModified)
                {
                    var was = entry.Property(x => x.IsCompleted).OriginalValue!;
                    var now = entry.Property(x => x.IsCompleted).CurrentValue!;
                    if (!was && now) entry.Entity.CompletedAtUtcTicks = nowTicks;
                    if (was && !now) entry.Entity.CompletedAtUtcTicks = null;
                }
            }
        }

        foreach (var entry in ChangeTracker.Entries<Session>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.DayKey == 0)
                    entry.Entity.DayKey = DayKeyFromUtcTicks(entry.Entity.StartUtcTicks);
            }
        }
    }
    public static int DayKeyFromUtcTicks(long utcTicks)
    {
        var dt = new DateTime(utcTicks, DateTimeKind.Utc);
        var y = dt.Year; var m = dt.Month; var d = dt.Day;
        return (y * 10000) + (m * 100) + d;
    }
}
