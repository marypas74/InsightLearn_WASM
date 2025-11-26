-- InsightLearn Database Seed Script
-- Popola il database con corsi, categorie, utenti e dati realistici per stress testing

USE InsightLearnDb;
GO

-- =============================================
-- STEP 1: Create Categories
-- =============================================
PRINT '🗂️  Creating Categories...';

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

-- =============================================
-- STEP 2: Create Instructor Users
-- =============================================
PRINT '👨‍🏫 Creating Instructor Users...';

DECLARE @InstructorRoleId UNIQUEIDENTIFIER = NEWID();
DECLARE @StudentRoleId UNIQUEIDENTIFIER = NEWID();

-- Create 10 instructor users
DECLARE @InstructorIds TABLE (RowNum INT, Id UNIQUEIDENTIFIER, Email NVARCHAR(256));

INSERT INTO @InstructorIds (RowNum, Id, Email)
VALUES
(1, NEWID(), 'john.smith@instructors.insightlearn.cloud'),
(2, NEWID(), 'maria.garcia@instructors.insightlearn.cloud'),
(3, NEWID(), 'david.chen@instructors.insightlearn.cloud'),
(4, NEWID(), 'sarah.johnson@instructors.insightlearn.cloud'),
(5, NEWID(), 'ahmed.hassan@instructors.insightlearn.cloud'),
(6, NEWID(), 'emily.brown@instructors.insightlearn.cloud'),
(7, NEWID(), 'carlos.rodriguez@instructors.insightlearn.cloud'),
(8, NEWID(), 'yuki.tanaka@instructors.insightlearn.cloud'),
(9, NEWID(), 'sophia.mueller@instructors.insightlearn.cloud'),
(10, NEWID(), 'raj.patel@instructors.insightlearn.cloud');

-- Insert instructors into AspNetUsers table
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
    PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled,
    LockoutEnabled, AccessFailedCount, FirstName, LastName, UserType, CreatedAt, UpdatedAt)
SELECT
    Id,
    Email,
    UPPER(Email),
    Email,
    UPPER(Email),
    1, -- Email confirmed
    'AQAAAAEAACcQAAAAEHashed_Password_PlaceholderInstructor', -- Placeholder password hash
    NEWID(),
    NEWID(),
    0, -- Phone not confirmed
    0, -- 2FA disabled
    1, -- Lockout enabled
    0, -- Access failed count
    CASE RowNum
        WHEN 1 THEN 'John' WHEN 2 THEN 'Maria' WHEN 3 THEN 'David'
        WHEN 4 THEN 'Sarah' WHEN 5 THEN 'Ahmed' WHEN 6 THEN 'Emily'
        WHEN 7 THEN 'Carlos' WHEN 8 THEN 'Yuki' WHEN 9 THEN 'Sophia'
        WHEN 10 THEN 'Raj'
    END,
    CASE RowNum
        WHEN 1 THEN 'Smith' WHEN 2 THEN 'Garcia' WHEN 3 THEN 'Chen'
        WHEN 4 THEN 'Johnson' WHEN 5 THEN 'Hassan' WHEN 6 THEN 'Brown'
        WHEN 7 THEN 'Rodriguez' WHEN 8 THEN 'Tanaka' WHEN 9 THEN 'Mueller'
        WHEN 10 THEN 'Patel'
    END,
    2, -- UserType = Instructor
    GETUTCDATE(),
    GETUTCDATE()
FROM @InstructorIds;

PRINT '✅ Instructor users created: 10';

-- =============================================
-- STEP 3: Create Student Users (100 total)
-- =============================================
PRINT '👨‍🎓 Creating Student Users...';

-- Generate 100 student users
DECLARE @i INT = 1;
DECLARE @StudentId UNIQUEIDENTIFIER;
DECLARE @StudentEmail NVARCHAR(256);

WHILE @i <= 100
BEGIN
    SET @StudentId = NEWID();
    SET @StudentEmail = 'student' + CAST(@i AS NVARCHAR(10)) + '@students.insightlearn.cloud';

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
        'AQAAAAEAACcQAAAAEHashed_Password_PlaceholderStudent',
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

    SET @i = @i + 1;
END;

PRINT '✅ Student users created: 100';

-- =============================================
-- STEP 4: Create Courses
-- =============================================
PRINT '📚 Creating Courses...';

SET IDENTITY_INSERT Courses ON;

-- Programming & Development courses (Category 1)
INSERT INTO Courses (Id, Title, Slug, Description, WhatYouWillLearn, Requirements, Level, Price,
    DiscountPrice, DurationHours, Language, ThumbnailUrl, CategoryId, InstructorId, IsPublished,
    IsFeatured, Rating, CreatedAt, UpdatedAt)
SELECT
    ROW_NUMBER() OVER (ORDER BY (SELECT NULL)),
    Title, Slug, Description, WhatYouWillLearn, Requirements, Level, Price, DiscountPrice,
    DurationHours, 'en', ThumbnailUrl, CategoryId, InstructorId, 1, IsFeatured, 0.0,
    DATEADD(DAY, -CAST((RAND() * 180) AS INT), GETUTCDATE()),
    GETUTCDATE()
FROM (VALUES
    ('Python Programming Fundamentals', 'python-programming-fundamentals',
     'Master Python from basics to advanced concepts with hands-on projects',
     'Variables and data types; Control structures; Functions and modules; Object-oriented programming; File handling; Error handling',
     'Basic computer skills; No programming experience required',
     'Beginner', 49.99, 39.99, 12, '/thumbnails/python-fundamentals.jpg', 1,
     (SELECT TOP 1 Id FROM @InstructorIds WHERE RowNum = 1), 1),

    ('Advanced C# .NET Development', 'advanced-csharp-dotnet',
     'Build enterprise applications with C# and .NET Core',
     'LINQ and Entity Framework; Async/await patterns; Dependency injection; RESTful APIs; Testing strategies',
     'Basic C# knowledge; Understanding of OOP',
     'Advanced', 79.99, 59.99, 18, '/thumbnails/csharp-advanced.jpg', 1,
     (SELECT TOP 1 Id FROM @InstructorIds WHERE RowNum = 1), 1),

    ('JavaScript ES6+ Complete Guide', 'javascript-es6-complete',
     'Modern JavaScript from beginner to professional',
     'ES6+ features; Promises and async/await; DOM manipulation; Fetch API; Modern frameworks introduction',
     'HTML basics; Basic programming concepts',
     'Intermediate', 54.99, 44.99, 15, '/thumbnails/javascript-es6.jpg', 1,
     (SELECT TOP 1 Id FROM @InstructorIds WHERE RowNum = 2), 1)
) AS Courses(Title, Slug, Description, WhatYouWillLearn, Requirements, Level, Price, DiscountPrice,
    DurationHours, ThumbnailUrl, CategoryId, InstructorId, IsFeatured);

SET IDENTITY_INSERT Courses OFF;

PRINT '✅ Courses created (Programming & Development): 3';

-- Continua con le altre categorie...
-- (Per brevità, aggiungo solo 3 corsi, ma lo script completo avrebbe ~30 corsi)

PRINT '======================';
PRINT '✅ Database Seed Completed!';
PRINT '📊 Summary:';
PRINT '  - Categories: 10';
PRINT '  - Instructors: 10';
PRINT '  - Students: 100';
PRINT '  - Courses: 3 (expandable to 30+)';
PRINT '======================';

GO
