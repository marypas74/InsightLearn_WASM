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
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Certificate> Certificates { get; set; }
    public DbSet<Note> Notes { get; set; }
    
    // Logging entities for Admin Console
    public DbSet<LogEntry> LogEntries { get; set; }
    public DbSet<AccessLog> AccessLogs { get; set; }
    public DbSet<ErrorLog> ErrorLogs { get; set; }
    public DbSet<AdminAuditLog> AdminAuditLogs { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    
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

    // System Configuration
    public DbSet<SystemEndpoint> SystemEndpoints { get; set; }
    
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

    // Student Learning Space entities (v2.1.0)
    public DbSet<StudentNote> StudentNotes { get; set; }
    public DbSet<VideoBookmark> VideoBookmarks { get; set; }
    public DbSet<VideoTranscriptMetadata> VideoTranscriptMetadata { get; set; }
    public DbSet<AIKeyTakeawaysMetadata> AIKeyTakeawaysMetadata { get; set; }
    public DbSet<AIConversation> AIConversations { get; set; }

    // Multi-language subtitle support (v2.2.0-dev)
    public DbSet<SubtitleTrack> SubtitleTracks { get; set; }

    // Phase 8: Multi-Language Subtitle Translation (v2.3.24-dev)
    public DbSet<VideoTranscriptTranslation> VideoTranscriptTranslations { get; set; }

    // AI Service Configuration (v2.3.63-dev) - Admin AI provider switching
    public DbSet<AIServiceConfiguration> AIServiceConfigurations { get; set; }

    // Transcript Job Status (v2.3.97-dev) - Real-time chunked transcription monitoring
    public DbSet<TranscriptJobStatus> TranscriptJobStatuses { get; set; }

    // SaaS Subscription Model entities
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public DbSet<UserSubscription> UserSubscriptions { get; set; }
    public DbSet<CourseEngagement> CourseEngagements { get; set; }
    public DbSet<InstructorPayout> InstructorPayouts { get; set; }
    public DbSet<SubscriptionRevenue> SubscriptionRevenues { get; set; }
    public DbSet<InstructorConnectAccount> InstructorConnectAccounts { get; set; }

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

        // Configure SaaS Subscription Model relationships
        ConfigureSubscriptionEntities(builder);

        // Configure Student Learning Space entities (v2.1.0)
        ConfigureStudentLearningSpaceEntities(builder);

        // Configure AI Service Configuration (v2.3.63-dev)
        ConfigureAIServiceConfiguration(builder);

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

        // Seed System Endpoints for database-driven endpoint configuration
        var endpoints = new List<SystemEndpoint>
        {
            // Auth endpoints
            new SystemEndpoint { Id = 1, Category = "Auth", EndpointKey = "Login", EndpointPath = "api/auth/login", HttpMethod = "POST", Description = "User login", IsActive = true },
            new SystemEndpoint { Id = 2, Category = "Auth", EndpointKey = "Register", EndpointPath = "api/auth/register", HttpMethod = "POST", Description = "User registration", IsActive = true },
            new SystemEndpoint { Id = 3, Category = "Auth", EndpointKey = "CompleteRegistration", EndpointPath = "api/auth/complete-registration", HttpMethod = "POST", Description = "Complete user registration", IsActive = true },
            new SystemEndpoint { Id = 4, Category = "Auth", EndpointKey = "Refresh", EndpointPath = "api/auth/refresh", HttpMethod = "POST", Description = "Refresh JWT token", IsActive = true },
            new SystemEndpoint { Id = 5, Category = "Auth", EndpointKey = "Me", EndpointPath = "api/auth/me", HttpMethod = "GET", Description = "Get current user", IsActive = true },
            new SystemEndpoint { Id = 6, Category = "Auth", EndpointKey = "OAuthCallback", EndpointPath = "api/auth/oauth-callback", HttpMethod = "POST", Description = "OAuth callback", IsActive = true },

            // Courses endpoints
            new SystemEndpoint { Id = 10, Category = "Courses", EndpointKey = "GetAll", EndpointPath = "api/courses", HttpMethod = "GET", Description = "Get all courses", IsActive = true },
            new SystemEndpoint { Id = 11, Category = "Courses", EndpointKey = "GetById", EndpointPath = "api/courses/{0}", HttpMethod = "GET", Description = "Get course by ID", IsActive = true },
            new SystemEndpoint { Id = 12, Category = "Courses", EndpointKey = "Create", EndpointPath = "api/courses", HttpMethod = "POST", Description = "Create new course", IsActive = true },
            new SystemEndpoint { Id = 13, Category = "Courses", EndpointKey = "Update", EndpointPath = "api/courses/{0}", HttpMethod = "PUT", Description = "Update course", IsActive = true },
            new SystemEndpoint { Id = 14, Category = "Courses", EndpointKey = "Delete", EndpointPath = "api/courses/{0}", HttpMethod = "DELETE", Description = "Delete course", IsActive = true },
            new SystemEndpoint { Id = 15, Category = "Courses", EndpointKey = "Search", EndpointPath = "api/courses/search", HttpMethod = "GET", Description = "Search courses", IsActive = true },
            new SystemEndpoint { Id = 16, Category = "Courses", EndpointKey = "GetByCategory", EndpointPath = "api/courses/category/{0}", HttpMethod = "GET", Description = "Get courses by category", IsActive = true },

            // Categories endpoints
            new SystemEndpoint { Id = 20, Category = "Categories", EndpointKey = "GetAll", EndpointPath = "api/categories", HttpMethod = "GET", Description = "Get all categories", IsActive = true },
            new SystemEndpoint { Id = 21, Category = "Categories", EndpointKey = "GetById", EndpointPath = "api/categories/{0}", HttpMethod = "GET", Description = "Get category by ID", IsActive = true },
            new SystemEndpoint { Id = 22, Category = "Categories", EndpointKey = "Create", EndpointPath = "api/categories", HttpMethod = "POST", Description = "Create new category", IsActive = true },
            new SystemEndpoint { Id = 23, Category = "Categories", EndpointKey = "Update", EndpointPath = "api/categories/{0}", HttpMethod = "PUT", Description = "Update category", IsActive = true },
            new SystemEndpoint { Id = 24, Category = "Categories", EndpointKey = "Delete", EndpointPath = "api/categories/{0}", HttpMethod = "DELETE", Description = "Delete category", IsActive = true },

            // Enrollments endpoints
            new SystemEndpoint { Id = 30, Category = "Enrollments", EndpointKey = "GetAll", EndpointPath = "api/enrollments", HttpMethod = "GET", Description = "Get all enrollments", IsActive = true },
            new SystemEndpoint { Id = 31, Category = "Enrollments", EndpointKey = "GetById", EndpointPath = "api/enrollments/{0}", HttpMethod = "GET", Description = "Get enrollment by ID", IsActive = true },
            new SystemEndpoint { Id = 32, Category = "Enrollments", EndpointKey = "Create", EndpointPath = "api/enrollments", HttpMethod = "POST", Description = "Create enrollment", IsActive = true },
            new SystemEndpoint { Id = 33, Category = "Enrollments", EndpointKey = "GetByCourse", EndpointPath = "api/enrollments/course/{0}", HttpMethod = "GET", Description = "Get enrollments by course", IsActive = true },
            new SystemEndpoint { Id = 34, Category = "Enrollments", EndpointKey = "GetByUser", EndpointPath = "api/enrollments/user/{0}", HttpMethod = "GET", Description = "Get enrollments by user", IsActive = true },

            // Users endpoints
            new SystemEndpoint { Id = 40, Category = "Users", EndpointKey = "GetAll", EndpointPath = "api/users", HttpMethod = "GET", Description = "Get all users", IsActive = true },
            new SystemEndpoint { Id = 41, Category = "Users", EndpointKey = "GetById", EndpointPath = "api/users/{0}", HttpMethod = "GET", Description = "Get user by ID", IsActive = true },
            new SystemEndpoint { Id = 42, Category = "Users", EndpointKey = "Update", EndpointPath = "api/users/{0}", HttpMethod = "PUT", Description = "Update user", IsActive = true },
            new SystemEndpoint { Id = 43, Category = "Users", EndpointKey = "Delete", EndpointPath = "api/users/{0}", HttpMethod = "DELETE", Description = "Delete user", IsActive = true },
            new SystemEndpoint { Id = 44, Category = "Users", EndpointKey = "GetProfile", EndpointPath = "api/users/profile", HttpMethod = "GET", Description = "Get user profile", IsActive = true },

            // Dashboard endpoints
            new SystemEndpoint { Id = 50, Category = "Dashboard", EndpointKey = "GetStats", EndpointPath = "api/dashboard/stats", HttpMethod = "GET", Description = "Get dashboard statistics", IsActive = true },
            new SystemEndpoint { Id = 51, Category = "Dashboard", EndpointKey = "GetRecentActivity", EndpointPath = "api/dashboard/recent-activity", HttpMethod = "GET", Description = "Get recent activity", IsActive = true },

            // Reviews endpoints
            new SystemEndpoint { Id = 60, Category = "Reviews", EndpointKey = "GetAll", EndpointPath = "api/reviews", HttpMethod = "GET", Description = "Get all reviews", IsActive = true },
            new SystemEndpoint { Id = 61, Category = "Reviews", EndpointKey = "GetById", EndpointPath = "api/reviews/{0}", HttpMethod = "GET", Description = "Get review by ID", IsActive = true },
            new SystemEndpoint { Id = 62, Category = "Reviews", EndpointKey = "Create", EndpointPath = "api/reviews", HttpMethod = "POST", Description = "Create review", IsActive = true },
            new SystemEndpoint { Id = 63, Category = "Reviews", EndpointKey = "GetByCourse", EndpointPath = "api/reviews/course/{0}", HttpMethod = "GET", Description = "Get reviews by course", IsActive = true },

            // Payments endpoints
            new SystemEndpoint { Id = 70, Category = "Payments", EndpointKey = "CreateCheckout", EndpointPath = "api/payments/create-checkout", HttpMethod = "POST", Description = "Create payment checkout", IsActive = true },
            new SystemEndpoint { Id = 71, Category = "Payments", EndpointKey = "GetTransactions", EndpointPath = "api/payments/transactions", HttpMethod = "GET", Description = "Get payment transactions", IsActive = true },
            new SystemEndpoint { Id = 72, Category = "Payments", EndpointKey = "GetTransactionById", EndpointPath = "api/payments/transactions/{0}", HttpMethod = "GET", Description = "Get transaction by ID", IsActive = true },

            // Chat endpoints
            new SystemEndpoint { Id = 80, Category = "Chat", EndpointKey = "SendMessage", EndpointPath = "api/chat/message", HttpMethod = "POST", Description = "Send chat message", IsActive = true },
            new SystemEndpoint { Id = 81, Category = "Chat", EndpointKey = "GetHistory", EndpointPath = "api/chat/history", HttpMethod = "GET", Description = "Get chat history", IsActive = true }
        };

        builder.Entity<SystemEndpoint>().HasData(endpoints);
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

            // Student Learning Space entities (v2.1.0)
            if (entry.Entity is StudentNote studentNote && entry.State == EntityState.Modified)
            {
                studentNote.UpdatedAt = DateTime.UtcNow;
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

    private void ConfigureSubscriptionEntities(ModelBuilder builder)
    {
        // SubscriptionPlan configuration
        builder.Entity<SubscriptionPlan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PriceMonthly).HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(e => e.PriceYearly).HasColumnType("decimal(10,2)");
            entity.Property(e => e.StripeProductId).HasMaxLength(255);
            entity.Property(e => e.StripePriceMonthlyId).HasMaxLength(255);
            entity.Property(e => e.StripePriceYearlyId).HasMaxLength(255);

            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.StripeProductId);
            entity.HasIndex(e => new { e.IsActive, e.PriceMonthly });
        });

        // UserSubscription configuration
        builder.Entity<UserSubscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.BillingInterval).IsRequired().HasMaxLength(20);
            entity.Property(e => e.StripeSubscriptionId).HasMaxLength(255);
            entity.Property(e => e.StripeCustomerId).HasMaxLength(255);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Subscriptions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Plan)
                .WithMany(p => p.UserSubscriptions)
                .HasForeignKey(e => e.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.StripeSubscriptionId).IsUnique();
            entity.HasIndex(e => new { e.Status, e.CurrentPeriodEnd });
        });

        // CourseEngagement configuration
        builder.Entity<CourseEngagement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EngagementType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ValidationScore).HasColumnType("decimal(3,2)");
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.DeviceFingerprint).HasMaxLength(255);

            entity.HasOne(e => e.User)
                .WithMany(u => u.CourseEngagements)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Course)
                .WithMany(c => c.CourseEngagements)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.UserId, e.CourseId, e.StartedAt });
            entity.HasIndex(e => new { e.CountsForPayout, e.StartedAt });
            entity.HasIndex(e => e.CourseId);
        });

        // InstructorPayout configuration
        builder.Entity<InstructorPayout>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalPlatformRevenue).HasColumnType("decimal(18,2)");
            entity.Property(e => e.EngagementPercentage).HasColumnType("decimal(8,6)");
            entity.Property(e => e.PlatformCommissionRate).HasColumnType("decimal(3,2)");
            entity.Property(e => e.PayoutAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.StripeTransferId).HasMaxLength(255);

            entity.HasOne(e => e.Instructor)
                .WithMany(u => u.InstructorPayouts)
                .HasForeignKey(e => e.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.InstructorId, e.Month, e.Year }).IsUnique();
            entity.HasIndex(e => new { e.Status, e.Month, e.Year });
            entity.HasIndex(e => e.StripeTransferId);
        });

        // SubscriptionRevenue configuration
        builder.Entity<SubscriptionRevenue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("EUR");
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.StripeInvoiceId).HasMaxLength(255);
            entity.Property(e => e.StripePaymentIntentId).HasMaxLength(255);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.CardLast4).HasMaxLength(4);
            entity.Property(e => e.CardBrand).HasMaxLength(50);
            entity.Property(e => e.RefundAmount).HasColumnType("decimal(10,2)");

            entity.HasOne(e => e.Subscription)
                .WithMany(s => s.SubscriptionRevenues)
                .HasForeignKey(e => e.SubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.StripeInvoiceId).IsUnique();
            entity.HasIndex(e => new { e.Status, e.PaidAt });
            entity.HasIndex(e => new { e.BillingPeriodStart, e.BillingPeriodEnd });
        });

        // InstructorConnectAccount configuration
        builder.Entity<InstructorConnectAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StripeAccountId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.OnboardingStatus).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Country).HasMaxLength(2);
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.Property(e => e.TotalPaidOut).HasColumnType("decimal(18,2)");
            entity.Property(e => e.VerificationStatus).IsRequired().HasMaxLength(50);

            entity.HasOne(e => e.Instructor)
                .WithOne(u => u.ConnectAccount)
                .HasForeignKey<InstructorConnectAccount>(e => e.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.StripeAccountId).IsUnique();
            entity.HasIndex(e => e.InstructorId).IsUnique();
            entity.HasIndex(e => new { e.OnboardingStatus, e.PayoutsEnabled });
        });

        // CartItem configuration (Shopping Cart - v2.2.0)
        builder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PriceAtAddition).HasColumnType("decimal(18,2)");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CouponCode).HasMaxLength(50);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Course)
                .WithMany()
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint: user can only have one cart entry per course
            entity.HasIndex(e => new { e.UserId, e.CourseId }).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.AddedAt);
        });

        // Update Enrollment to support subscriptions
        builder.Entity<Enrollment>(entity =>
        {
            entity.HasOne(e => e.Subscription)
                .WithMany()
                .HasForeignKey(e => e.SubscriptionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.SubscriptionId);
        });

        // AuditLog indexes for performance
        builder.Entity<AuditLog>(entity =>
        {
            // Index 1: UserId (for user-specific queries)
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_AuditLogs_UserId")
                .IsDescending(false);

            // Index 2: Action (for action-type filtering)
            entity.HasIndex(e => e.Action)
                .HasDatabaseName("IX_AuditLogs_Action")
                .IsDescending(false);

            // Index 3: Timestamp (for sorting and range queries)
            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("IX_AuditLogs_Timestamp")
                .IsDescending(true);  // DESC for recent-first queries

            // Index 4: UserId + Timestamp (composite for user history)
            // Most important index - covers common query pattern
            entity.HasIndex(e => new { e.UserId, e.Timestamp })
                .HasDatabaseName("IX_AuditLogs_UserId_Timestamp")
                .IsDescending(false, true);  // UserId ASC, Timestamp DESC

            // Index 5: Action + Timestamp (composite for action reports)
            entity.HasIndex(e => new { e.Action, e.Timestamp })
                .HasDatabaseName("IX_AuditLogs_Action_Timestamp")
                .IsDescending(false, true);  // Action ASC, Timestamp DESC

            // Index 6: EntityId (for entity-specific audit trail)
            entity.HasIndex(e => e.EntityId)
                .HasDatabaseName("IX_AuditLogs_EntityId")
                .IsDescending(false)
                .HasFilter("[EntityId] IS NOT NULL");  // Partial index (exclude nulls)

            // Index 7: RequestId (for request correlation)
            entity.HasIndex(e => e.RequestId)
                .HasDatabaseName("IX_AuditLogs_RequestId")
                .IsDescending(false)
                .HasFilter("[RequestId] IS NOT NULL");  // Partial index (exclude nulls)
        });
    }

    /// <summary>
    /// Configures Student Learning Space entities (v2.1.0).
    /// Includes indexes, unique constraints, and relationships.
    /// </summary>
    private void ConfigureStudentLearningSpaceEntities(ModelBuilder builder)
    {
        // StudentNote configuration
        builder.Entity<StudentNote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NoteText).IsRequired().HasMaxLength(4000);
            entity.Property(e => e.VideoTimestamp).IsRequired();
            entity.Property(e => e.IsShared).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsBookmarked).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");

            // Foreign keys
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Lesson)
                .WithMany()
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for performance
            entity.HasIndex(e => new { e.UserId, e.LessonId })
                .HasDatabaseName("IX_StudentNotes_UserId_LessonId");

            entity.HasIndex(e => e.VideoTimestamp)
                .HasDatabaseName("IX_StudentNotes_VideoTimestamp");

            entity.HasIndex(e => e.IsBookmarked)
                .HasDatabaseName("IX_StudentNotes_IsBookmarked")
                .HasFilter("[IsBookmarked] = 1");  // Partial index for bookmarked notes only
        });

        // VideoBookmark configuration
        builder.Entity<VideoBookmark>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.VideoTimestamp).IsRequired();
            entity.Property(e => e.Label).HasMaxLength(200);
            entity.Property(e => e.BookmarkType).IsRequired().HasMaxLength(50).HasDefaultValue("Manual");
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");

            // Foreign keys
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Lesson)
                .WithMany()
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for performance
            entity.HasIndex(e => new { e.UserId, e.LessonId })
                .HasDatabaseName("IX_VideoBookmarks_UserId_LessonId");
        });

        // VideoTranscriptMetadata configuration
        builder.Entity<VideoTranscriptMetadata>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LessonId).IsRequired();
            entity.Property(e => e.MongoDocumentId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Language).IsRequired().HasMaxLength(10).HasDefaultValue("en-US");
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("Pending");
            entity.Property(e => e.SegmentCount);
            entity.Property(e => e.DurationSeconds);
            entity.Property(e => e.GeneratedAt);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");

            // Foreign key
            entity.HasOne(e => e.Lesson)
                .WithMany()
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint: one transcript per lesson
            entity.HasIndex(e => e.LessonId)
                .IsUnique()
                .HasDatabaseName("IX_VideoTranscriptMetadata_LessonId_Unique");

            // Index for status queries
            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_VideoTranscriptMetadata_Status");
        });

        // AIKeyTakeawaysMetadata configuration
        builder.Entity<AIKeyTakeawaysMetadata>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LessonId).IsRequired();
            entity.Property(e => e.MongoDocumentId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.TakeawayCount).IsRequired().HasDefaultValue(0);
            entity.Property(e => e.ProcessingStatus).IsRequired().HasMaxLength(50).HasDefaultValue("Pending");
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");

            // Foreign key
            entity.HasOne(e => e.Lesson)
                .WithMany()
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint: one set of takeaways per lesson
            entity.HasIndex(e => e.LessonId)
                .IsUnique()
                .HasDatabaseName("IX_AIKeyTakeawaysMetadata_LessonId_Unique");
        });

        // AIConversation configuration
        builder.Entity<AIConversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd(); // Generate GUID automatically
            entity.Property(e => e.SessionId).IsRequired();
            // UserId is nullable - anonymous users (free lessons) use SessionId for tracking
            entity.Property(e => e.UserId).IsRequired(false);
            entity.Property(e => e.MongoDocumentId).HasMaxLength(100);
            entity.Property(e => e.MessageCount).IsRequired().HasDefaultValue(0);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");

            // Foreign keys - User is optional (null for anonymous users on free lessons)
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Lesson is optional - null for general conversations without video context
            entity.HasOne(e => e.Lesson)
                .WithMany()
                .HasForeignKey(e => e.LessonId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Unique constraint: SessionId must be unique
            entity.HasIndex(e => e.SessionId)
                .IsUnique()
                .HasDatabaseName("IX_AIConversations_SessionId_Unique");

            // Indexes for performance
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_AIConversations_UserId");
        });

        // SubtitleTrack configuration (v2.2.0-dev) - Multi-language subtitle support
        builder.Entity<SubtitleTrack>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LessonId).IsRequired();
            entity.Property(e => e.Language).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Label).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FileUrl).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Kind).IsRequired().HasMaxLength(20).HasDefaultValue("subtitles");
            entity.Property(e => e.IsDefault).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");

            // Foreign key to Lesson
            entity.HasOne(e => e.Lesson)
                .WithMany(l => l.SubtitleTracks)
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            // Foreign key to User (CreatedBy) - IMPORTANT: explicitly use CreatedByUserId
            entity.HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes for performance
            entity.HasIndex(e => e.LessonId)
                .HasDatabaseName("IX_SubtitleTracks_LessonId");

            entity.HasIndex(e => new { e.LessonId, e.Language })
                .IsUnique()
                .HasDatabaseName("IX_SubtitleTracks_LessonId_Language_Unique");
        });

        // VideoTranscriptTranslation configuration (v2.3.24-dev) - Phase 8: Multi-Language Subtitle Translation
        builder.Entity<VideoTranscriptTranslation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LessonId).IsRequired();
            entity.Property(e => e.SourceLanguage).IsRequired().HasMaxLength(10).HasDefaultValue("en");
            entity.Property(e => e.TargetLanguage).IsRequired().HasMaxLength(10);
            entity.Property(e => e.MongoDocumentId).HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("Pending");
            entity.Property(e => e.QualityTier).IsRequired().HasMaxLength(50).HasDefaultValue("Auto/Ollama");
            entity.Property(e => e.SegmentCount);
            entity.Property(e => e.TotalCharacters);
            entity.Property(e => e.EstimatedCost).HasColumnType("decimal(10,4)").HasDefaultValue(0.0m);
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.CompletedAt);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");

            // Foreign key to Lesson
            entity.HasOne(e => e.Lesson)
                .WithMany()
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint: one translation per lesson per target language
            entity.HasIndex(e => new { e.LessonId, e.TargetLanguage })
                .IsUnique()
                .HasDatabaseName("IX_VideoTranscriptTranslations_LessonId_TargetLanguage_Unique");

            // Indexes for performance
            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_VideoTranscriptTranslations_Status");

            entity.HasIndex(e => e.QualityTier)
                .HasDatabaseName("IX_VideoTranscriptTranslations_QualityTier");

            entity.HasIndex(e => new { e.LessonId, e.Status })
                .HasDatabaseName("IX_VideoTranscriptTranslations_LessonId_Status");
        });
    }

    /// <summary>
    /// Configures AI Service Configuration entity (v2.3.63-dev).
    /// Allows admin to switch between OpenAI and Ollama providers.
    /// </summary>
    private void ConfigureAIServiceConfiguration(ModelBuilder builder)
    {
        builder.Entity<AIServiceConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.ServiceType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ActiveProvider).IsRequired().HasMaxLength(50).HasDefaultValue("OpenAI");
            entity.Property(e => e.OpenAIApiKey).HasMaxLength(500);
            entity.Property(e => e.OpenAIModel).HasMaxLength(100);
            entity.Property(e => e.OllamaBaseUrl).HasMaxLength(255);
            entity.Property(e => e.OllamaModel).HasMaxLength(100);
            entity.Property(e => e.FasterWhisperUrl).HasMaxLength(255);
            entity.Property(e => e.FasterWhisperModel).HasMaxLength(50);
            entity.Property(e => e.Temperature).HasDefaultValue(0.7);
            entity.Property(e => e.TimeoutSeconds).HasDefaultValue(120);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.EnableFallback).HasDefaultValue(true);
            entity.Property(e => e.FallbackProvider).HasMaxLength(50);
            entity.Property(e => e.LastErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");

            // Unique constraint: one configuration per service type
            entity.HasIndex(e => e.ServiceType)
                .IsUnique()
                .HasDatabaseName("IX_AIServiceConfigurations_ServiceType_Unique");

            // Index for active provider queries
            entity.HasIndex(e => new { e.ServiceType, e.ActiveProvider })
                .HasDatabaseName("IX_AIServiceConfigurations_ServiceType_ActiveProvider");
        });
    }
}