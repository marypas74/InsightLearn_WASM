using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using InsightLearn.Core.Entities;
using System.Reflection;

namespace InsightLearn.Infrastructure.Data;

public class InsightLearnDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public InsightLearnDbContext(DbContextOptions<InsightLearnDbContext> options) : base(options)
    {
    }

    public DbSet<Course> Courses { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Section> Sections { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<Enrollment> Enrollments { get; set; }
    public DbSet<LessonProgress> LessonProgress { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<ReviewVote> ReviewVotes { get; set; }
    public DbSet<Discussion> Discussions { get; set; }
    public DbSet<DiscussionComment> DiscussionComments { get; set; }
    public DbSet<DiscussionVote> DiscussionVotes { get; set; }
    public DbSet<DiscussionCommentVote> DiscussionCommentVotes { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentMethod> PaymentMethods { get; set; }
    public DbSet<PaymentMethodAuditLog> PaymentMethodAuditLogs { get; set; }
    public DbSet<Coupon> Coupons { get; set; }
    public DbSet<Certificate> Certificates { get; set; }
    public DbSet<Note> Notes { get; set; }
    
    // Logging entities for Admin Console
    public DbSet<LogEntry> LogEntries { get; set; }
    public DbSet<AccessLog> AccessLogs { get; set; }
    public DbSet<ErrorLog> ErrorLogs { get; set; }
    public DbSet<AdminAuditLog> AdminAuditLogs { get; set; }
    
    // Enhanced Logging System entities
    public DbSet<LoginAttempt> LoginAttempts { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<LoginMethod> LoginMethods { get; set; }
    public DbSet<SecurityEvent> SecurityEvents { get; set; }
    public DbSet<ApiRequestLog> ApiRequestLogs { get; set; }
    public DbSet<DatabaseErrorLog> DatabaseErrorLogs { get; set; }
    public DbSet<ValidationErrorLog> ValidationErrorLogs { get; set; }
    public DbSet<PerformanceMetric> PerformanceMetrics { get; set; }
    public DbSet<EntityAuditLog> EntityAuditLogs { get; set; }
    
    // SEO and Management entities
    public DbSet<SeoSettings> SeoSettings { get; set; }
    public DbSet<ApplicationSetting> ApplicationSettings { get; set; }
    public DbSet<SettingChangeLog> SettingChangeLogs { get; set; }
    public DbSet<GoogleIntegration> GoogleIntegrations { get; set; }
    public DbSet<SeoAudit> SeoAudits { get; set; }
    public DbSet<Sitemap> Sitemaps { get; set; }

    // Chatbot entities
    public DbSet<ChatbotContact> ChatbotContacts { get; set; }
    public DbSet<ChatbotMessage> ChatbotMessages { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Configure default query behavior
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
        
        // Enable sensitive data logging in development only
        #if DEBUG
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();
        #endif
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Apply all configurations from the current assembly
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        
        // Configure Identity tables
        builder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.WalletBalance).HasColumnType("decimal(10,2)");
        });

        builder.Entity<IdentityRole<Guid>>(entity =>
        {
            entity.ToTable("Roles");
        });

        builder.Entity<IdentityUserRole<Guid>>(entity =>
        {
            entity.ToTable("UserRoles");
        });

        builder.Entity<IdentityUserClaim<Guid>>(entity =>
        {
            entity.ToTable("UserClaims");
        });

        builder.Entity<IdentityUserLogin<Guid>>(entity =>
        {
            entity.ToTable("UserLogins");
        });

        builder.Entity<IdentityRoleClaim<Guid>>(entity =>
        {
            entity.ToTable("RoleClaims");
        });

        builder.Entity<IdentityUserToken<Guid>>(entity =>
        {
            entity.ToTable("UserTokens");
        });

        // Apply EF Core optimizations using extension methods
        builder.ConfigureGlobalQueryFilters();
        builder.ConfigurePerformanceOptimizations();
        builder.ApplyOptimalIndexes();

        // Apply OAuth-specific optimizations for Google authentication
        builder.ConfigureOAuthOptimizations();
        builder.CreateOAuthViews();

        // Seed data
        SeedData(builder);
    }

    private void SeedData(ModelBuilder builder)
    {
        // Seed Categories
        var categories = new[]
        {
            new Category { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Development", Slug = "development", IconUrl = "fas fa-code", ColorCode = "#007bff", OrderIndex = 1 },
            new Category { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Business", Slug = "business", IconUrl = "fas fa-briefcase", ColorCode = "#28a745", OrderIndex = 2 },
            new Category { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "IT & Software", Slug = "it-software", IconUrl = "fas fa-laptop", ColorCode = "#17a2b8", OrderIndex = 3 },
            new Category { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Design", Slug = "design", IconUrl = "fas fa-paint-brush", ColorCode = "#e83e8c", OrderIndex = 4 },
            new Category { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "Marketing", Slug = "marketing", IconUrl = "fas fa-bullhorn", ColorCode = "#fd7e14", OrderIndex = 5 },
            new Category { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), Name = "Photography & Video", Slug = "photography-video", IconUrl = "fas fa-camera", ColorCode = "#6f42c1", OrderIndex = 6 },
            new Category { Id = Guid.Parse("77777777-7777-7777-7777-777777777777"), Name = "Music", Slug = "music", IconUrl = "fas fa-music", ColorCode = "#20c997", OrderIndex = 7 },
            new Category { Id = Guid.Parse("88888888-8888-8888-8888-888888888888"), Name = "Health & Fitness", Slug = "health-fitness", IconUrl = "fas fa-dumbbell", ColorCode = "#dc3545", OrderIndex = 8 }
        };

        builder.Entity<Category>().HasData(categories);

        // Seed Roles
        var roles = new[]
        {
            new IdentityRole<Guid> { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Name = "Administrator", NormalizedName = "ADMINISTRATOR" },
            new IdentityRole<Guid> { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), Name = "Instructor", NormalizedName = "INSTRUCTOR" },
            new IdentityRole<Guid> { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), Name = "Student", NormalizedName = "STUDENT" },
            new IdentityRole<Guid> { Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), Name = "Moderator", NormalizedName = "MODERATOR" }
        };

        builder.Entity<IdentityRole<Guid>>().HasData(roles);
    }

    public override int SaveChanges()
    {
        UpdateAuditableEntities();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditableEntities()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is Course course)
            {
                if (entry.State == EntityState.Modified)
                    course.UpdatedAt = DateTime.UtcNow;
            }
            
            if (entry.Entity is Review review && entry.State == EntityState.Modified)
            {
                review.UpdatedAt = DateTime.UtcNow;
            }
            
            if (entry.Entity is Discussion discussion && entry.State == EntityState.Modified)
            {
                discussion.UpdatedAt = DateTime.UtcNow;
            }
            
            if (entry.Entity is DiscussionComment comment && entry.State == EntityState.Modified)
            {
                comment.UpdatedAt = DateTime.UtcNow;
            }
            
            if (entry.Entity is Note note && entry.State == EntityState.Modified)
            {
                note.UpdatedAt = DateTime.UtcNow;
            }

            // FIX: Add UserSession to auditable entities
            if (entry.Entity is UserSession session)
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                {
                    session.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}