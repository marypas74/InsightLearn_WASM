-- ==============================================================
-- InsightLearn User Seed Script - Production Ready
-- Password per TUTTI: Pa$$W0rd
-- Hash: AQAAAAIAAYagAAAAEJl0wPWsA/Iny8sPnTLB47r0zVJAwH8s7AnR2KG72PhtyeYfRMLfpRZoA9lj/COZYQ==
-- FIX: Added IsGoogleUser column (NOT NULL constraint)
-- ==============================================================

USE InsightLearnDb;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

DECLARE @PasswordHash NVARCHAR(MAX) = 'AQAAAAIAAYagAAAAEJl0wPWsA/Iny8sPnTLB47r0zVJAwH8s7AnR2KG72PhtyeYfRMLfpRZoA9lj/COZYQ==';
DECLARE @Now DATETIME2 = GETUTCDATE();

PRINT 'Starting User Seed...';

-- Update Admin user to correct type
UPDATE Users SET UserType = 'Admin' WHERE Email = 'admin@insightlearn.cloud';
PRINT 'Admin user updated to Admin type';

-- Create Instructors (10)
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'john.smith@instructors.insightlearn.cloud')
INSERT INTO Users (Id, Email, NormalizedEmail, UserName, NormalizedUserName, FirstName, LastName,
    PasswordHash, SecurityStamp, ConcurrencyStamp, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled,
    LockoutEnabled, AccessFailedCount, UserType, IsInstructor, IsVerified, DateJoined, UpdatedAt,
    HasAgreedToTerms, TermsAgreedDate, HasAgreedToPrivacyPolicy, PrivacyPolicyAgreedDate, RegistrationCompleted, WalletBalance, IsGoogleUser)
VALUES (NEWID(), 'john.smith@instructors.insightlearn.cloud', 'JOHN.SMITH@INSTRUCTORS.INSIGHTLEARN.CLOUD',
    'john.smith@instructors.insightlearn.cloud', 'JOHN.SMITH@INSTRUCTORS.INSIGHTLEARN.CLOUD', 'John', 'Smith',
    @PasswordHash, NEWID(), NEWID(), 1, 0, 0, 1, 0, 'Instructor', 1, 1, @Now, @Now, 1, @Now, 1, @Now, 1, 0, 0);

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'maria.garcia@instructors.insightlearn.cloud')
INSERT INTO Users (Id, Email, NormalizedEmail, UserName, NormalizedUserName, FirstName, LastName,
    PasswordHash, SecurityStamp, ConcurrencyStamp, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled,
    LockoutEnabled, AccessFailedCount, UserType, IsInstructor, IsVerified, DateJoined, UpdatedAt,
    HasAgreedToTerms, TermsAgreedDate, HasAgreedToPrivacyPolicy, PrivacyPolicyAgreedDate, RegistrationCompleted, WalletBalance, IsGoogleUser)
VALUES (NEWID(), 'maria.garcia@instructors.insightlearn.cloud', 'MARIA.GARCIA@INSTRUCTORS.INSIGHTLEARN.CLOUD',
    'maria.garcia@instructors.insightlearn.cloud', 'MARIA.GARCIA@INSTRUCTORS.INSIGHTLEARN.CLOUD', 'Maria', 'Garcia',
    @PasswordHash, NEWID(), NEWID(), 1, 0, 0, 1, 0, 'Instructor', 1, 1, @Now, @Now, 1, @Now, 1, @Now, 1, 0, 0);

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'david.chen@instructors.insightlearn.cloud')
INSERT INTO Users (Id, Email, NormalizedEmail, UserName, NormalizedUserName, FirstName, LastName,
    PasswordHash, SecurityStamp, ConcurrencyStamp, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled,
    LockoutEnabled, AccessFailedCount, UserType, IsInstructor, IsVerified, DateJoined, UpdatedAt,
    HasAgreedToTerms, TermsAgreedDate, HasAgreedToPrivacyPolicy, PrivacyPolicyAgreedDate, RegistrationCompleted, WalletBalance, IsGoogleUser)
VALUES (NEWID(), 'david.chen@instructors.insightlearn.cloud', 'DAVID.CHEN@INSTRUCTORS.INSIGHTLEARN.CLOUD',
    'david.chen@instructors.insightlearn.cloud', 'DAVID.CHEN@INSTRUCTORS.INSIGHTLEARN.CLOUD', 'David', 'Chen',
    @PasswordHash, NEWID(), NEWID(), 1, 0, 0, 1, 0, 'Instructor', 1, 1, @Now, @Now, 1, @Now, 1, @Now, 1, 0, 0);

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'sarah.johnson@instructors.insightlearn.cloud')
INSERT INTO Users (Id, Email, NormalizedEmail, UserName, NormalizedUserName, FirstName, LastName,
    PasswordHash, SecurityStamp, ConcurrencyStamp, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled,
    LockoutEnabled, AccessFailedCount, UserType, IsInstructor, IsVerified, DateJoined, UpdatedAt,
    HasAgreedToTerms, TermsAgreedDate, HasAgreedToPrivacyPolicy, PrivacyPolicyAgreedDate, RegistrationCompleted, WalletBalance, IsGoogleUser)
VALUES (NEWID(), 'sarah.johnson@instructors.insightlearn.cloud', 'SARAH.JOHNSON@INSTRUCTORS.INSIGHTLEARN.CLOUD',
    'sarah.johnson@instructors.insightlearn.cloud', 'SARAH.JOHNSON@INSTRUCTORS.INSIGHTLEARN.CLOUD', 'Sarah', 'Johnson',
    @PasswordHash, NEWID(), NEWID(), 1, 0, 0, 1, 0, 'Instructor', 1, 1, @Now, @Now, 1, @Now, 1, @Now, 1, 0, 0);

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'ahmed.hassan@instructors.insightlearn.cloud')
INSERT INTO Users (Id, Email, NormalizedEmail, UserName, NormalizedUserName, FirstName, LastName,
    PasswordHash, SecurityStamp, ConcurrencyStamp, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled,
    LockoutEnabled, AccessFailedCount, UserType, IsInstructor, IsVerified, DateJoined, UpdatedAt,
    HasAgreedToTerms, TermsAgreedDate, HasAgreedToPrivacyPolicy, PrivacyPolicyAgreedDate, RegistrationCompleted, WalletBalance, IsGoogleUser)
VALUES (NEWID(), 'ahmed.hassan@instructors.insightlearn.cloud', 'AHMED.HASSAN@INSTRUCTORS.INSIGHTLEARN.CLOUD',
    'ahmed.hassan@instructors.insightlearn.cloud', 'AHMED.HASSAN@INSTRUCTORS.INSIGHTLEARN.CLOUD', 'Ahmed', 'Hassan',
    @PasswordHash, NEWID(), NEWID(), 1, 0, 0, 1, 0, 'Instructor', 1, 1, @Now, @Now, 1, @Now, 1, @Now, 1, 0, 0);

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'emily.brown@instructors.insightlearn.cloud')
INSERT INTO Users (Id, Email, NormalizedEmail, UserName, NormalizedUserName, FirstName, LastName,
    PasswordHash, SecurityStamp, ConcurrencyStamp, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled,
    LockoutEnabled, AccessFailedCount, UserType, IsInstructor, IsVerified, DateJoined, UpdatedAt,
    HasAgreedToTerms, TermsAgreedDate, HasAgreedToPrivacyPolicy, PrivacyPolicyAgreedDate, RegistrationCompleted, WalletBalance, IsGoogleUser)
VALUES (NEWID(), 'emily.brown@instructors.insightlearn.cloud', 'EMILY.BROWN@INSTRUCTORS.INSIGHTLEARN.CLOUD',
    'emily.brown@instructors.insightlearn.cloud', 'EMILY.BROWN@INSTRUCTORS.INSIGHTLEARN.CLOUD', 'Emily', 'Brown',
    @PasswordHash, NEWID(), NEWID(), 1, 0, 0, 1, 0, 'Instructor', 1, 1, @Now, @Now, 1, @Now, 1, @Now, 1, 0, 0);

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'carlos.rodriguez@instructors.insightlearn.cloud')
INSERT INTO Users (Id, Email, NormalizedEmail, UserName, NormalizedUserName, FirstName, LastName,
    PasswordHash, SecurityStamp, ConcurrencyStamp, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled,
    LockoutEnabled, AccessFailedCount, UserType, IsInstructor, IsVerified, DateJoined, UpdatedAt,
    HasAgreedToTerms, TermsAgreedDate, HasAgreedToPrivacyPolicy, PrivacyPolicyAgreedDate, RegistrationCompleted, WalletBalance, IsGoogleUser)
VALUES (NEWID(), 'carlos.rodriguez@instructors.insightlearn.cloud', 'CARLOS.RODRIGUEZ@INSTRUCTORS.INSIGHTLEARN.CLOUD',
    'carlos.rodriguez@instructors.insightlearn.cloud', 'CARLOS.RODRIGUEZ@INSTRUCTORS.INSIGHTLEARN.CLOUD', 'Carlos', 'Rodriguez',
    @PasswordHash, NEWID(), NEWID(), 1, 0, 0, 1, 0, 'Instructor', 1, 1, @Now, @Now, 1, @Now, 1, @Now, 1, 0, 0);

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'yuki.tanaka@instructors.insightlearn.cloud')
INSERT INTO Users (Id, Email, NormalizedEmail, UserName, NormalizedUserName, FirstName, LastName,
    PasswordHash, SecurityStamp, ConcurrencyStamp, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled,
    LockoutEnabled, AccessFailedCount, UserType, IsInstructor, IsVerified, DateJoined, UpdatedAt,
    HasAgreedToTerms, TermsAgreedDate, HasAgreedToPrivacyPolicy, PrivacyPolicyAgreedDate, RegistrationCompleted, WalletBalance, IsGoogleUser)
VALUES (NEWID(), 'yuki.tanaka@instructors.insightlearn.cloud', 'YUKI.TANAKA@INSTRUCTORS.INSIGHTLEARN.CLOUD',
    'yuki.tanaka@instructors.insightlearn.cloud', 'YUKI.TANAKA@INSTRUCTORS.INSIGHTLEARN.CLOUD', 'Yuki', 'Tanaka',
    @PasswordHash, NEWID(), NEWID(), 1, 0, 0, 1, 0, 'Instructor', 1, 1, @Now, @Now, 1, @Now, 1, @Now, 1, 0, 0);

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'sophia.mueller@instructors.insightlearn.cloud')
INSERT INTO Users (Id, Email, NormalizedEmail, UserName, NormalizedUserName, FirstName, LastName,
    PasswordHash, SecurityStamp, ConcurrencyStamp, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled,
    LockoutEnabled, AccessFailedCount, UserType, IsInstructor, IsVerified, DateJoined, UpdatedAt,
    HasAgreedToTerms, TermsAgreedDate, HasAgreedToPrivacyPolicy, PrivacyPolicyAgreedDate, RegistrationCompleted, WalletBalance, IsGoogleUser)
VALUES (NEWID(), 'sophia.mueller@instructors.insightlearn.cloud', 'SOPHIA.MUELLER@INSTRUCTORS.INSIGHTLEARN.CLOUD',
    'sophia.mueller@instructors.insightlearn.cloud', 'SOPHIA.MUELLER@INSTRUCTORS.INSIGHTLEARN.CLOUD', 'Sophia', 'Mueller',
    @PasswordHash, NEWID(), NEWID(), 1, 0, 0, 1, 0, 'Instructor', 1, 1, @Now, @Now, 1, @Now, 1, @Now, 1, 0, 0);

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'raj.patel@instructors.insightlearn.cloud')
INSERT INTO Users (Id, Email, NormalizedEmail, UserName, NormalizedUserName, FirstName, LastName,
    PasswordHash, SecurityStamp, ConcurrencyStamp, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled,
    LockoutEnabled, AccessFailedCount, UserType, IsInstructor, IsVerified, DateJoined, UpdatedAt,
    HasAgreedToTerms, TermsAgreedDate, HasAgreedToPrivacyPolicy, PrivacyPolicyAgreedDate, RegistrationCompleted, WalletBalance, IsGoogleUser)
VALUES (NEWID(), 'raj.patel@instructors.insightlearn.cloud', 'RAJ.PATEL@INSTRUCTORS.INSIGHTLEARN.CLOUD',
    'raj.patel@instructors.insightlearn.cloud', 'RAJ.PATEL@INSTRUCTORS.INSIGHTLEARN.CLOUD', 'Raj', 'Patel',
    @PasswordHash, NEWID(), NEWID(), 1, 0, 0, 1, 0, 'Instructor', 1, 1, @Now, @Now, 1, @Now, 1, @Now, 1, 0, 0);

PRINT 'Instructors created: 10';

-- Create Students (100)
DECLARE @i INT = 1;
DECLARE @StudentEmail NVARCHAR(256);

WHILE @i <= 100
BEGIN
    SET @StudentEmail = 'student' + CAST(@i AS NVARCHAR(10)) + '@students.insightlearn.cloud';

    IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = @StudentEmail)
    BEGIN
        INSERT INTO Users (Id, Email, NormalizedEmail, UserName, NormalizedUserName, FirstName, LastName,
            PasswordHash, SecurityStamp, ConcurrencyStamp, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled,
            LockoutEnabled, AccessFailedCount, UserType, IsInstructor, IsVerified, DateJoined, UpdatedAt,
            HasAgreedToTerms, TermsAgreedDate, HasAgreedToPrivacyPolicy, PrivacyPolicyAgreedDate, RegistrationCompleted, WalletBalance, IsGoogleUser)
        VALUES (NEWID(), @StudentEmail, UPPER(@StudentEmail), @StudentEmail, UPPER(@StudentEmail),
            'Student', 'User' + CAST(@i AS NVARCHAR(10)),
            @PasswordHash, NEWID(), NEWID(), 1, 0, 0, 1, 0, 'Student', 0, 1,
            DATEADD(DAY, -ABS(CHECKSUM(NEWID())) % 365, @Now), @Now, 1, @Now, 1, @Now, 1, 0, 0);
    END

    SET @i = @i + 1;
END;

PRINT 'Students created: 100';

SELECT 'Final user count: ' + CAST(COUNT(*) AS VARCHAR(10)) FROM Users;

SET NOCOUNT OFF;
GO
