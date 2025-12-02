-- ==============================================================
-- InsightLearn Enrollments & Reviews Seed Script
-- Creates realistic data for stress testing
-- ==============================================================

USE InsightLearnDb;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

DECLARE @Now DATETIME2 = GETUTCDATE();

-- ==============================================================
-- ENROLLMENTS (Random students in random courses)
-- Creates ~1500 enrollments
-- ==============================================================

PRINT 'Starting Enrollment Generation...';

DECLARE @StudentCount INT;
DECLARE @CourseCount INT;
DECLARE @i INT = 1;
DECLARE @StudentId UNIQUEIDENTIFIER;
DECLARE @CourseId UNIQUEIDENTIFIER;
DECLARE @CoursePrice DECIMAL(18,2);
DECLARE @EnrollDate DATETIME2;
DECLARE @CompletedLessonsCount INT;

-- Get counts
SELECT @StudentCount = COUNT(*) FROM Users WHERE UserType = 'Student';
SELECT @CourseCount = COUNT(*) FROM Courses;

PRINT 'Students: ' + CAST(@StudentCount AS VARCHAR(10));
PRINT 'Courses: ' + CAST(@CourseCount AS VARCHAR(10));

-- Create enrollments - each student enrolls in 5-20 random courses
DECLARE @StudentNum INT = 1;

WHILE @StudentNum <= @StudentCount
BEGIN
    -- Get student ID
    SELECT @StudentId = Id FROM (
        SELECT Id, ROW_NUMBER() OVER (ORDER BY DateJoined) AS RowNum
        FROM Users WHERE UserType = 'Student'
    ) AS Students WHERE RowNum = @StudentNum;

    -- Enroll in random number of courses (5-20)
    DECLARE @EnrollCount INT = 5 + ABS(CHECKSUM(NEWID())) % 16;
    DECLARE @EnrollNum INT = 1;

    WHILE @EnrollNum <= @EnrollCount
    BEGIN
        -- Get random course
        SELECT TOP 1 @CourseId = Id, @CoursePrice = Price FROM Courses
        ORDER BY NEWID();

        -- Check if not already enrolled
        IF NOT EXISTS (SELECT 1 FROM Enrollments WHERE UserId = @StudentId AND CourseId = @CourseId)
        BEGIN
            -- Random enrollment date (within last year)
            SET @EnrollDate = DATEADD(DAY, -ABS(CHECKSUM(NEWID())) % 365, @Now);
            SET @CompletedLessonsCount = ABS(CHECKSUM(NEWID())) % 15; -- 0-14 lessons completed

            INSERT INTO Enrollments (Id, UserId, CourseId, EnrolledAt, LastAccessedAt,
                AmountPaid, Status, CurrentLessonIndex, HasCertificate, CompletedLessons,
                TotalWatchedMinutes, EnrolledViaSubscription, CompletedAt)
            VALUES (
                NEWID(),
                @StudentId,
                @CourseId,
                @EnrollDate,
                DATEADD(DAY, -ABS(CHECKSUM(NEWID())) % 30, @Now), -- LastAccessedAt
                @CoursePrice * (1 - (ABS(CHECKSUM(NEWID())) % 30) / 100.0), -- Random discount 0-30%
                CASE WHEN @CompletedLessonsCount >= 10 THEN 2 ELSE 1 END, -- Status: 1=Active, 2=Completed
                @CompletedLessonsCount,
                CASE WHEN @CompletedLessonsCount >= 10 THEN 1 ELSE 0 END, -- HasCertificate
                @CompletedLessonsCount,
                @CompletedLessonsCount * (15 + ABS(CHECKSUM(NEWID())) % 20), -- Random watch time
                0, -- EnrolledViaSubscription
                CASE WHEN @CompletedLessonsCount >= 10 THEN DATEADD(DAY, -ABS(CHECKSUM(NEWID())) % 60, @Now) ELSE NULL END
            );
        END

        SET @EnrollNum = @EnrollNum + 1;
    END

    SET @StudentNum = @StudentNum + 1;

    -- Progress indicator every 20 students
    IF @StudentNum % 20 = 0
        PRINT 'Processed ' + CAST(@StudentNum AS VARCHAR(10)) + ' students...';
END

SELECT 'Enrollments created: ' + CAST(COUNT(*) AS VARCHAR(10)) FROM Enrollments;

-- ==============================================================
-- UPDATE COURSE STATISTICS (EnrollmentCount)
-- ==============================================================

PRINT 'Updating course enrollment counts...';

UPDATE c SET
    EnrollmentCount = (SELECT COUNT(*) FROM Enrollments e WHERE e.CourseId = c.Id)
FROM Courses c;

-- ==============================================================
-- REVIEWS (Students who completed courses leave reviews)
-- ==============================================================

PRINT 'Starting Review Generation...';

DECLARE @ReviewTexts TABLE (
    Rating INT,
    ReviewText NVARCHAR(500)
);

-- Positive reviews (4-5 stars)
INSERT INTO @ReviewTexts VALUES
(5, 'Excellent course! The instructor explains everything clearly and the projects are very practical. Highly recommended!'),
(5, 'Best course I have taken on this subject. The content is comprehensive and well-structured.'),
(5, 'Amazing instructor and great content. I learned so much from this course. Worth every penny!'),
(5, 'This course exceeded my expectations. The quality of teaching is outstanding.'),
(5, 'Very thorough course with great examples. I feel confident in applying what I learned.'),
(4, 'Great course overall. Some sections could be more detailed but still very valuable content.'),
(4, 'Solid course with good explanations. A few technical issues with videos but content is excellent.'),
(4, 'Well structured course. The instructor knows the subject matter very well.'),
(4, 'Good course, learned a lot. Would appreciate more advanced topics in future updates.'),
(4, 'Very informative and practical. Some parts were a bit slow but overall excellent.'),
-- Mixed reviews (3 stars)
(3, 'Decent course but could be better organized. The content is good though.'),
(3, 'Average course. Expected more depth in certain topics but basics are covered well.'),
(3, 'The course is okay. Good for beginners but lacks advanced content.'),
-- Negative reviews (1-2 stars) - kept rare
(2, 'Course is outdated in some areas. Needs to be updated with latest information.'),
(1, 'Not what I expected. The content is too basic for the price.');

DECLARE @EnrollmentId UNIQUEIDENTIFIER;
DECLARE @ReviewStudentId UNIQUEIDENTIFIER;
DECLARE @ReviewCourseId UNIQUEIDENTIFIER;
DECLARE @Rating INT;
DECLARE @ReviewText NVARCHAR(500);
DECLARE @ReviewDate DATETIME2;

-- Create reviews for enrollments with CompletedLessons > 5
DECLARE enrollment_cursor CURSOR FOR
SELECT Id, UserId, CourseId FROM Enrollments WHERE CompletedLessons >= 5;

OPEN enrollment_cursor;
FETCH NEXT FROM enrollment_cursor INTO @EnrollmentId, @ReviewStudentId, @ReviewCourseId;

DECLARE @ReviewCount INT = 0;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- 70% chance of leaving a review
    IF ABS(CHECKSUM(NEWID())) % 100 < 70
    BEGIN
        -- Weighted random rating (more positive reviews)
        DECLARE @RatingRandom INT = ABS(CHECKSUM(NEWID())) % 100;
        IF @RatingRandom < 50 SET @Rating = 5
        ELSE IF @RatingRandom < 80 SET @Rating = 4
        ELSE IF @RatingRandom < 90 SET @Rating = 3
        ELSE IF @RatingRandom < 97 SET @Rating = 2
        ELSE SET @Rating = 1;

        -- Get random review text for this rating
        SELECT TOP 1 @ReviewText = ReviewText FROM @ReviewTexts WHERE Rating = @Rating ORDER BY NEWID();

        -- Random review date (after enrollment, within last 6 months)
        SET @ReviewDate = DATEADD(DAY, -ABS(CHECKSUM(NEWID())) % 180, @Now);

        -- Check if review doesn't exist
        IF NOT EXISTS (SELECT 1 FROM Reviews WHERE UserId = @ReviewStudentId AND CourseId = @ReviewCourseId)
        BEGIN
            INSERT INTO Reviews (Id, UserId, CourseId, Rating, Comment, CreatedAt, IsApproved, HelpfulVotes, UnhelpfulVotes)
            VALUES (
                NEWID(),
                @ReviewStudentId,
                @ReviewCourseId,
                @Rating,
                @ReviewText,
                @ReviewDate,
                1, -- IsApproved
                ABS(CHECKSUM(NEWID())) % 50, -- Random helpful votes
                ABS(CHECKSUM(NEWID())) % 5 -- Few unhelpful votes
            );
            SET @ReviewCount = @ReviewCount + 1;
        END
    END

    FETCH NEXT FROM enrollment_cursor INTO @EnrollmentId, @ReviewStudentId, @ReviewCourseId;
END

CLOSE enrollment_cursor;
DEALLOCATE enrollment_cursor;

PRINT 'Reviews created: ' + CAST(@ReviewCount AS VARCHAR(10));

-- ==============================================================
-- UPDATE COURSE STATISTICS (AverageRating, ReviewCount)
-- ==============================================================

PRINT 'Updating course review statistics...';

UPDATE c SET
    ReviewCount = ISNULL((SELECT COUNT(*) FROM Reviews r WHERE r.CourseId = c.Id), 0),
    AverageRating = ISNULL((SELECT AVG(CAST(Rating AS FLOAT)) FROM Reviews r WHERE r.CourseId = c.Id), 0)
FROM Courses c;

-- ==============================================================
-- Final Statistics
-- ==============================================================

PRINT '=================================';
PRINT 'Database Population Complete!';
PRINT '=================================';

SELECT 'Total Users: ' + CAST(COUNT(*) AS VARCHAR(10)) FROM Users;
SELECT 'Total Courses: ' + CAST(COUNT(*) AS VARCHAR(10)) FROM Courses;
SELECT 'Total Sections: ' + CAST(COUNT(*) AS VARCHAR(10)) FROM Sections;
SELECT 'Total Lessons: ' + CAST(COUNT(*) AS VARCHAR(10)) FROM Lessons;
SELECT 'Total Enrollments: ' + CAST(COUNT(*) AS VARCHAR(10)) FROM Enrollments;
SELECT 'Total Reviews: ' + CAST(COUNT(*) AS VARCHAR(10)) FROM Reviews;

-- Show top courses by enrollment
PRINT '';
PRINT 'Top 10 Courses by Enrollment:';
SELECT TOP 10
    Title,
    EnrollmentCount,
    ReviewCount,
    ROUND(AverageRating, 1) AS AvgRating,
    Price
FROM Courses
ORDER BY EnrollmentCount DESC;

SET NOCOUNT OFF;
GO
