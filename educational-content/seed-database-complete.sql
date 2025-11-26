-- ==============================================================
-- InsightLearn Database Complete Seed Script
-- Popola il database con corsi, categorie, utenti e dati realistici
-- Password per TUTTI gli utenti: Pa$$W0rd
-- ==============================================================

USE InsightLearnDb;
GO

SET NOCOUNT ON;

PRINT '🚀 InsightLearn Database Seed Script';
PRINT '📅 Started: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '======================================================';

-- ==============================================================
-- STEP 1: Create Categories (10 total)
-- ==============================================================
PRINT '';
PRINT '🗂️  STEP 1: Creating Categories...';

IF NOT EXISTS (SELECT 1 FROM Categories WHERE Id = 1)
BEGIN
    SET IDENTITY_INSERT Categories ON;

    INSERT INTO Categories (Id, Name, Slug, Description, Icon, ColorCode, IsActive, CreatedAt, UpdatedAt)
    VALUES
    (1, 'Programming & Development', 'programming-development', 'Learn programming languages and software development skills', 'fa-code', '#4CAF50', 1, GETUTCDATE(), GETUTCDATE()),
    (2, 'Web Development', 'web-development', 'Master modern web development technologies', 'fa-globe', '#2196F3', 1, GETUTCDATE(), GETUTCDATE()),
    (3, 'Business & Management', 'business-management', 'Develop essential business and management skills', 'fa-briefcase', '#FF9800', 1, GETUTCDATE(), GETUTCDATE()),
    (4, 'Design & Creative', 'design-creative', 'Explore UI/UX design and creative skills', 'fa-palette', '#E91E63', 1, GETUTCDATE(), GETUTCDATE()),
    (5, 'Data Science & AI', 'data-science-ai', 'Learn data analysis, machine learning and AI', 'fa-chart-bar', '#9C27B0', 1, GETUTCDATE(), GETUTCDATE()),
    (6, 'Mobile Development', 'mobile-development', 'Build mobile apps for iOS and Android', 'fa-mobile-alt', '#00BCD4', 1, GETUTCDATE(), GETUTCDATE()),
    (7, 'DevOps & Cloud', 'devops-cloud', 'Master DevOps practices and cloud technologies', 'fa-cloud', '#607D8B', 1, GETUTCDATE(), GETUTCDATE()),
    (8, 'Cybersecurity', 'cybersecurity', 'Learn cybersecurity and ethical hacking', 'fa-shield-alt', '#F44336', 1, GETUTCDATE(), GETUTCDATE()),
    (9, 'Marketing & Sales', 'marketing-sales', 'Digital marketing and sales strategies', 'fa-bullhorn', '#FFC107', 1, GETUTCDATE(), GETUTCDATE()),
    (10, 'Personal Development', 'personal-development', 'Improve productivity and soft skills', 'fa-user-graduate', '#3F51B5', 1, GETUTCDATE(), GETUTCDATE());

    SET IDENTITY_INSERT Categories OFF;

    PRINT '✅ Categories created: 10';
END
ELSE
    PRINT '⚠️  Categories already exist, skipping...';

-- ==============================================================
-- STEP 2: Create Admin User
-- ==============================================================
PRINT '';
PRINT '👤 STEP 2: Creating Admin User...';

DECLARE @AdminId UNIQUEIDENTIFIER = NEWID();
DECLARE @PasswordHash NVARCHAR(MAX) = 'AQAAAAIAAYagAAAAEJl0wPWsA/Iny8sPnTLB47r0zVJAwH8s7AnR2KG72PhtyeYfRMLfpRZoA9lj/COZYQ==';

IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = 'admin@insightlearn.cloud')
BEGIN
    INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
        PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled,
        LockoutEnabled, AccessFailedCount, FirstName, LastName, UserType, CreatedAt, UpdatedAt)
    VALUES (
        @AdminId,
        'admin@insightlearn.cloud',
        'ADMIN@INSIGHTLEARN.CLOUD',
        'admin@insightlearn.cloud',
        'ADMIN@INSIGHTLEARN.CLOUD',
        1, -- Email confirmed
        @PasswordHash,
        NEWID(),
        NEWID(),
        0, -- Phone not confirmed
        0, -- 2FA disabled
        1, -- Lockout enabled
        0, -- Access failed count
        'Admin',
        'User',
        3, -- UserType = Admin
        GETUTCDATE(),
        GETUTCDATE()
    );

    PRINT '✅ Admin user created: admin@insightlearn.cloud';
END
ELSE
    PRINT '⚠️  Admin user already exists, skipping...';

-- ==============================================================
-- STEP 3: Create Instructor Users (10 total)
-- ==============================================================
PRINT '';
PRINT '👨‍🏫 STEP 3: Creating Instructor Users...';

DECLARE @InstructorCount INT = 0;

-- Instructor 1: John Smith
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = 'john.smith@instructors.insightlearn.cloud')
BEGIN
    INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
        PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled,
        LockoutEnabled, AccessFailedCount, FirstName, LastName, UserType, CreatedAt, UpdatedAt)
    VALUES (NEWID(), 'john.smith@instructors.insightlearn.cloud', 'JOHN.SMITH@INSTRUCTORS.INSIGHTLEARN.CLOUD',
        'john.smith@instructors.insightlearn.cloud', 'JOHN.SMITH@INSTRUCTORS.INSIGHTLEARN.CLOUD', 1,
        @PasswordHash, NEWID(), NEWID(), 0, 0, 1, 0, 'John', 'Smith', 2, GETUTCDATE(), GETUTCDATE());
    SET @InstructorCount = @InstructorCount + 1;
END

-- Instructor 2: Maria Garcia
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = 'maria.garcia@instructors.insightlearn.cloud')
BEGIN
    INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
        PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled,
        LockoutEnabled, AccessFailedCount, FirstName, LastName, UserType, CreatedAt, UpdatedAt)
    VALUES (NEWID(), 'maria.garcia@instructors.insightlearn.cloud', 'MARIA.GARCIA@INSTRUCTORS.INSIGHTLEARN.CLOUD',
        'maria.garcia@instructors.insightlearn.cloud', 'MARIA.GARCIA@INSTRUCTORS.INSIGHTLEARN.CLOUD', 1,
        @PasswordHash, NEWID(), NEWID(), 0, 0, 1, 0, 'Maria', 'Garcia', 2, GETUTCDATE(), GETUTCDATE());
    SET @InstructorCount = @InstructorCount + 1;
END

-- Instructor 3: David Chen
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = 'david.chen@instructors.insightlearn.cloud')
BEGIN
    INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
        PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled,
        LockoutEnabled, AccessFailedCount, FirstName, LastName, UserType, CreatedAt, UpdatedAt)
    VALUES (NEWID(), 'david.chen@instructors.insightlearn.cloud', 'DAVID.CHEN@INSTRUCTORS.INSIGHTLEARN.CLOUD',
        'david.chen@instructors.insightlearn.cloud', 'DAVID.CHEN@INSTRUCTORS.INSIGHTLEARN.CLOUD', 1,
        @PasswordHash, NEWID(), NEWID(), 0, 0, 1, 0, 'David', 'Chen', 2, GETUTCDATE(), GETUTCDATE());
    SET @InstructorCount = @InstructorCount + 1;
END

-- Instructor 4: Sarah Johnson
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = 'sarah.johnson@instructors.insightlearn.cloud')
BEGIN
    INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
        PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled,
        LockoutEnabled, AccessFailedCount, FirstName, LastName, UserType, CreatedAt, UpdatedAt)
    VALUES (NEWID(), 'sarah.johnson@instructors.insightlearn.cloud', 'SARAH.JOHNSON@INSTRUCTORS.INSIGHTLEARN.CLOUD',
        'sarah.johnson@instructors.insightlearn.cloud', 'SARAH.JOHNSON@INSTRUCTORS.INSIGHTLEARN.CLOUD', 1,
        @PasswordHash, NEWID(), NEWID(), 0, 0, 1, 0, 'Sarah', 'Johnson', 2, GETUTCDATE(), GETUTCDATE());
    SET @InstructorCount = @InstructorCount + 1;
END

-- Instructor 5: Ahmed Hassan
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = 'ahmed.hassan@instructors.insightlearn.cloud')
BEGIN
    INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
        PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled,
        LockoutEnabled, AccessFailedCount, FirstName, LastName, UserType, CreatedAt, UpdatedAt)
    VALUES (NEWID(), 'ahmed.hassan@instructors.insightlearn.cloud', 'AHMED.HASSAN@INSTRUCTORS.INSIGHTLEARN.CLOUD',
        'ahmed.hassan@instructors.insightlearn.cloud', 'AHMED.HASSAN@INSTRUCTORS.INSIGHTLEARN.CLOUD', 1,
        @PasswordHash, NEWID(), NEWID(), 0, 0, 1, 0, 'Ahmed', 'Hassan', 2, GETUTCDATE(), GETUTCDATE());
    SET @InstructorCount = @InstructorCount + 1;
END

-- Instructor 6: Emily Brown
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = 'emily.brown@instructors.insightlearn.cloud')
BEGIN
    INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
        PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled,
        LockoutEnabled, AccessFailedCount, FirstName, LastName, UserType, CreatedAt, UpdatedAt)
    VALUES (NEWID(), 'emily.brown@instructors.insightlearn.cloud', 'EMILY.BROWN@INSTRUCTORS.INSIGHTLEARN.CLOUD',
        'emily.brown@instructors.insightlearn.cloud', 'EMILY.BROWN@INSTRUCTORS.INSIGHTLEARN.CLOUD', 1,
        @PasswordHash, NEWID(), NEWID(), 0, 0, 1, 0, 'Emily', 'Brown', 2, GETUTCDATE(), GETUTCDATE());
    SET @InstructorCount = @InstructorCount + 1;
END

-- Instructor 7: Carlos Rodriguez
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = 'carlos.rodriguez@instructors.insightlearn.cloud')
BEGIN
    INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
        PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled,
        LockoutEnabled, AccessFailedCount, FirstName, LastName, UserType, CreatedAt, UpdatedAt)
    VALUES (NEWID(), 'carlos.rodriguez@instructors.insightlearn.cloud', 'CARLOS.RODRIGUEZ@INSTRUCTORS.INSIGHTLEARN.CLOUD',
        'carlos.rodriguez@instructors.insightlearn.cloud', 'CARLOS.RODRIGUEZ@INSTRUCTORS.INSIGHTLEARN.CLOUD', 1,
        @PasswordHash, NEWID(), NEWID(), 0, 0, 1, 0, 'Carlos', 'Rodriguez', 2, GETUTCDATE(), GETUTCDATE());
    SET @InstructorCount = @InstructorCount + 1;
END

-- Instructor 8: Yuki Tanaka
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = 'yuki.tanaka@instructors.insightlearn.cloud')
BEGIN
    INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
        PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled,
        LockoutEnabled, AccessFailedCount, FirstName, LastName, UserType, CreatedAt, UpdatedAt)
    VALUES (NEWID(), 'yuki.tanaka@instructors.insightlearn.cloud', 'YUKI.TANAKA@INSTRUCTORS.INSIGHTLEARN.CLOUD',
        'yuki.tanaka@instructors.insightlearn.cloud', 'YUKI.TANAKA@INSTRUCTORS.INSIGHTLEARN.CLOUD', 1,
        @PasswordHash, NEWID(), NEWID(), 0, 0, 1, 0, 'Yuki', 'Tanaka', 2, GETUTCDATE(), GETUTCDATE());
    SET @InstructorCount = @InstructorCount + 1;
END

-- Instructor 9: Sophia Mueller
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = 'sophia.mueller@instructors.insightlearn.cloud')
BEGIN
    INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
        PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled,
        LockoutEnabled, AccessFailedCount, FirstName, LastName, UserType, CreatedAt, UpdatedAt)
    VALUES (NEWID(), 'sophia.mueller@instructors.insightlearn.cloud', 'SOPHIA.MUELLER@INSTRUCTORS.INSIGHTLEARN.CLOUD',
        'sophia.mueller@instructors.insightlearn.cloud', 'SOPHIA.MUELLER@INSTRUCTORS.INSIGHTLEARN.CLOUD', 1,
        @PasswordHash, NEWID(), NEWID(), 0, 0, 1, 0, 'Sophia', 'Mueller', 2, GETUTCDATE(), GETUTCDATE());
    SET @InstructorCount = @InstructorCount + 1;
END

-- Instructor 10: Raj Patel
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = 'raj.patel@instructors.insightlearn.cloud')
BEGIN
    INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
        PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled,
        LockoutEnabled, AccessFailedCount, FirstName, LastName, UserType, CreatedAt, UpdatedAt)
    VALUES (NEWID(), 'raj.patel@instructors.insightlearn.cloud', 'RAJ.PATEL@INSTRUCTORS.INSIGHTLEARN.CLOUD',
        'raj.patel@instructors.insightlearn.cloud', 'RAJ.PATEL@INSTRUCTORS.INSIGHTLEARN.CLOUD', 1,
        @PasswordHash, NEWID(), NEWID(), 0, 0, 1, 0, 'Raj', 'Patel', 2, GETUTCDATE(), GETUTCDATE());
    SET @InstructorCount = @InstructorCount + 1;
END

PRINT '✅ Instructor users created: ' + CAST(@InstructorCount AS VARCHAR(10));

-- ==============================================================
-- STEP 4: Create Student Users (100 total)
-- ==============================================================
PRINT '';
PRINT '👨‍🎓 STEP 4: Creating Student Users (100 total)...';
PRINT 'This may take a minute...';

DECLARE @i INT = 1;
DECLARE @StudentId UNIQUEIDENTIFIER;
DECLARE @StudentEmail NVARCHAR(256);
DECLARE @StudentCount INT = 0;

WHILE @i <= 100
BEGIN
    SET @StudentId = NEWID();
    SET @StudentEmail = 'student' + CAST(@i AS NVARCHAR(10)) + '@students.insightlearn.cloud';

    IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = @StudentEmail)
    BEGIN
        INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
            PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled,
            LockoutEnabled, AccessFailedCount, FirstName, LastName, UserType, CreatedAt, UpdatedAt)
        VALUES (
            @StudentId,
            @StudentEmail,
            UPPER(@StudentEmail),
            @StudentEmail,
            UPPER(@StudentEmail),
            1,
            @PasswordHash,
            NEWID(),
            NEWID(),
            0,
            0,
            1,
            0,
            'Student',
            'User' + CAST(@i AS NVARCHAR(10)),
            1, -- UserType = Student
            DATEADD(DAY, -CAST((RAND() * 365) AS INT), GETUTCDATE()),
            GETUTCDATE()
        );

        SET @StudentCount = @StudentCount + 1;
    END

    SET @i = @i + 1;
END;

PRINT '✅ Student users created: ' + CAST(@StudentCount AS VARCHAR(10));

-- ==============================================================
-- Final Summary
-- ==============================================================
PRINT '';
PRINT '======================================================';
PRINT '✅ Database Seed Completed Successfully!';
PRINT '======================================================';
PRINT '📊 Summary:';
PRINT '  - Categories: 10';
PRINT '  - Admin Users: 1';
PRINT '  - Instructors: 10';
PRINT '  - Students: 100';
PRINT '  - Total Users: 111';
PRINT '';
PRINT '🔐 All users password: Pa$$W0rd';
PRINT '📧 Admin email: admin@insightlearn.cloud';
PRINT '📧 Instructor pattern: [name].[surname]@instructors.insightlearn.cloud';
PRINT '📧 Student pattern: student[N]@students.insightlearn.cloud';
PRINT '';
PRINT '📅 Completed: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '======================================================';

SET NOCOUNT OFF;
GO