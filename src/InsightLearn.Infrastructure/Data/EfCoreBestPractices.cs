namespace InsightLearn.Infrastructure.Data;

/// <summary>
/// EF Core Best Practices and Performance Optimizations for ElevateLearning
/// 
/// This file contains documentation and best practices for Entity Framework Core
/// configuration and usage within the ElevateLearning application.
/// </summary>
public static class EfCoreBestPractices
{
    /// <summary>
    /// Global Query Filter Best Practices
    /// 
    /// Issues Fixed:
    /// 1. Navigation property warnings when using global query filters
    /// 2. Cartesian explosion with complex entity graphs
    /// 3. Shadow foreign key warnings
    /// 
    /// Solutions Implemented:
    /// - Proper navigation property configuration in entity type configurations
    /// - Split query behavior for entities with multiple collections
    /// - Explicit foreign key property declarations where needed
    /// </summary>
    public const string GlobalQueryFilterGuidelines = @"
        Global Query Filters Best Practices:
        
        1. Always configure navigation properties explicitly in both directions
        2. Use split queries for entities with multiple collections to avoid Cartesian explosion
        3. Apply filters at the entity level, not in LINQ queries when possible
        4. Use IgnoreQueryFilters() when you need to include soft-deleted entities
        5. Be careful with cascade behaviors when using global filters
        
        Example:
        // In Entity Configuration
        builder.HasOne(e => e.User)
            .WithMany(u => u.Enrollments)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    ";
    
    /// <summary>
    /// Shadow Foreign Key Optimization Guidelines
    /// 
    /// Issues Fixed:
    /// 1. EF Core creating shadow properties for foreign keys
    /// 2. Navigation property configuration conflicts
    /// 3. Duplicate relationship configurations
    /// 
    /// Solutions Implemented:
    /// - Explicit foreign key properties in entity classes
    /// - Centralized relationship configuration in entity type configurations
    /// - Proper cascade behavior configuration
    /// </summary>
    public const string ShadowForeignKeyGuidelines = @"
        Shadow Foreign Key Best Practices:
        
        1. Always declare foreign key properties explicitly in entity classes
        2. Configure relationships in the dependent entity's configuration
        3. Avoid duplicate relationship configurations across multiple configurations
        4. Use appropriate cascade behaviors based on business rules
        5. Consider using composite keys where business logic requires it
        
        Example:
        // In Entity Class
        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;
        
        // In Entity Configuration
        builder.HasOne(e => e.User)
            .WithMany(u => u.Enrollments)
            .HasForeignKey(e => e.UserId);
    ";
    
    /// <summary>
    /// Performance Optimization Guidelines
    /// 
    /// Optimizations Implemented:
    /// 1. Split query behavior for complex entity graphs
    /// 2. Optimal indexing strategies for common query patterns
    /// 3. Query tracking behavior configuration
    /// 4. Connection resiliency and timeout configuration
    /// 5. Filtered indexes for soft-deleted entities
    /// </summary>
    public const string PerformanceOptimizationGuidelines = @"
        Performance Optimization Best Practices:
        
        1. Use split queries for entities with multiple navigation collections
        2. Create composite indexes for common query patterns
        3. Use filtered indexes for soft-delete scenarios
        4. Configure appropriate query tracking behavior
        5. Implement connection resiliency for production environments
        6. Use proper decimal precision configuration
        7. Set appropriate string lengths to optimize storage
        
        Common Query Patterns to Optimize:
        - Course listings by category and status
        - User enrollments and progress tracking
        - Review aggregations by course
        - Discussion threads by course and type
        - Payment history by user and status
    ";
    
    /// <summary>
    /// Migration and Database Maintenance Guidelines
    /// 
    /// Recommendations for database evolution and maintenance
    /// </summary>
    public const string MigrationGuidelines = @"
        Migration Best Practices:
        
        1. Always review generated migrations before applying
        2. Test migrations on a copy of production data
        3. Use data seeding appropriately for reference data
        4. Consider performance impact of index changes
        5. Use proper naming conventions for constraints and indexes
        6. Plan for zero-downtime deployments when possible
        
        Index Naming Convention:
        - IX_{TableName}_{Column1}_{Column2}
        - Use HasDatabaseName() to ensure consistent naming
        
        Example Migration Commands:
        - Add-Migration OptimizeEntityFrameworkConfiguration
        - Update-Database -Verbose
        - Script-Migration -From InitialCreate -To OptimizeEntityFrameworkConfiguration
    ";
    
    /// <summary>
    /// Monitoring and Diagnostics Guidelines
    /// 
    /// Recommendations for monitoring EF Core performance in production
    /// </summary>
    public const string MonitoringGuidelines = @"
        Monitoring and Diagnostics Best Practices:
        
        1. Enable query logging in development environments
        2. Monitor slow queries using SQL Server profiler or Azure SQL Analytics
        3. Track N+1 query problems with appropriate logging
        4. Monitor connection pool usage and timeouts
        5. Use Application Insights or similar tools for production monitoring
        
        Key Metrics to Monitor:
        - Query execution times
        - Connection pool exhaustion
        - Failed retry attempts
        - Memory usage patterns
        - Cache hit ratios
        
        Debugging Queries:
        - Use .ToQueryString() to inspect generated SQL
        - Enable sensitive data logging in development only
        - Use SQL Server Plan Cache to identify expensive queries
    ";
}

/// <summary>
/// Service Registration Extensions for EF Core Configuration
/// </summary>
public static class ServiceRegistrationExtensions
{
    /// <summary>
    /// Example of how to register EF Core with optimized configuration in Program.cs
    /// </summary>
    public const string ServiceRegistrationExample = @"
        // In Program.cs or Startup.cs
        services.AddEfCoreOptimizations(configuration);
        
        // For advanced scenarios, you can also configure:
        services.AddDbContextPool<InsightLearnDbContext>(options =>
        {
            // Use connection pooling for better performance
            options.UseSqlServer(connectionString);
        }, poolSize: 128);
        
        // Register repositories and services
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
    ";
}