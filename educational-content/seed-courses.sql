-- ==============================================================
-- InsightLearn Courses Seed Script - Stress Test Data
-- 30 Courses with Sections and Lessons
-- ==============================================================

USE InsightLearnDb;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

DECLARE @Now DATETIME2 = GETUTCDATE();

-- Category IDs
DECLARE @DevCat UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @BizCat UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
DECLARE @ITCat UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';
DECLARE @DesignCat UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444444444';
DECLARE @MarketCat UNIQUEIDENTIFIER = '55555555-5555-5555-5555-555555555555';
DECLARE @PhotoCat UNIQUEIDENTIFIER = '66666666-6666-6666-6666-666666666666';
DECLARE @MusicCat UNIQUEIDENTIFIER = '77777777-7777-7777-7777-777777777777';
DECLARE @HealthCat UNIQUEIDENTIFIER = '88888888-8888-8888-8888-888888888888';

-- Get Instructor IDs
DECLARE @John UNIQUEIDENTIFIER, @Maria UNIQUEIDENTIFIER, @David UNIQUEIDENTIFIER;
DECLARE @Sarah UNIQUEIDENTIFIER, @Ahmed UNIQUEIDENTIFIER, @Emily UNIQUEIDENTIFIER;
DECLARE @Carlos UNIQUEIDENTIFIER, @Yuki UNIQUEIDENTIFIER, @Sophia UNIQUEIDENTIFIER, @Raj UNIQUEIDENTIFIER;

SELECT @John = Id FROM Users WHERE Email = 'john.smith@instructors.insightlearn.cloud';
SELECT @Maria = Id FROM Users WHERE Email = 'maria.garcia@instructors.insightlearn.cloud';
SELECT @David = Id FROM Users WHERE Email = 'david.chen@instructors.insightlearn.cloud';
SELECT @Sarah = Id FROM Users WHERE Email = 'sarah.johnson@instructors.insightlearn.cloud';
SELECT @Ahmed = Id FROM Users WHERE Email = 'ahmed.hassan@instructors.insightlearn.cloud';
SELECT @Emily = Id FROM Users WHERE Email = 'emily.brown@instructors.insightlearn.cloud';
SELECT @Carlos = Id FROM Users WHERE Email = 'carlos.rodriguez@instructors.insightlearn.cloud';
SELECT @Yuki = Id FROM Users WHERE Email = 'yuki.tanaka@instructors.insightlearn.cloud';
SELECT @Sophia = Id FROM Users WHERE Email = 'sophia.mueller@instructors.insightlearn.cloud';
SELECT @Raj = Id FROM Users WHERE Email = 'raj.patel@instructors.insightlearn.cloud';

PRINT 'Instructor IDs loaded';

-- Course and Section ID storage
DECLARE @CourseId UNIQUEIDENTIFIER;
DECLARE @SectionId UNIQUEIDENTIFIER;

-- ==============================================================
-- DEVELOPMENT COURSES (5 courses)
-- ==============================================================

-- Course 1: Python Programming Complete Bootcamp
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'Python Programming Complete Bootcamp 2024',
    'Master Python from scratch to advanced level. Learn data structures, algorithms, OOP, web scraping, automation, and more. Perfect for beginners and intermediate programmers who want to become proficient in Python.',
    'Complete Python course from beginner to advanced with real-world projects',
    @David, @DevCat, 49.99, 20.00, '/images/courses/python-bootcamp.jpg',
    0, 1, @Now, @Now, 1200,
    'No programming experience required. Basic computer skills. Enthusiasm to learn!',
    'Python fundamentals and syntax. Object-Oriented Programming. Data Structures & Algorithms. Web Scraping with BeautifulSoup. Automation scripts. Database integration.',
    'English', 1, 1, 'python-programming-complete-bootcamp-2024', 15420, 4.7, 342, 1856, 0);

-- Section 1: Python Basics
SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'Python Fundamentals', 'Learn the basics of Python programming', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'Introduction to Python', 'Welcome to Python programming', 0, 1, 15, 1, 1, @Now),
       (NEWID(), @SectionId, 'Installing Python and IDE Setup', 'Set up your development environment', 0, 2, 20, 1, 1, @Now),
       (NEWID(), @SectionId, 'Variables and Data Types', 'Understanding Python data types', 0, 3, 25, 0, 1, @Now),
       (NEWID(), @SectionId, 'Operators and Expressions', 'Working with operators', 0, 4, 20, 0, 1, @Now),
       (NEWID(), @SectionId, 'Input and Output', 'Getting user input and displaying output', 0, 5, 18, 0, 1, @Now);

-- Section 2: Control Flow
SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'Control Flow', 'Master conditional statements and loops', 2, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'If-Else Statements', 'Conditional logic in Python', 0, 1, 22, 0, 1, @Now),
       (NEWID(), @SectionId, 'For Loops', 'Iterating with for loops', 0, 2, 25, 0, 1, @Now),
       (NEWID(), @SectionId, 'While Loops', 'While loop fundamentals', 0, 3, 20, 0, 1, @Now),
       (NEWID(), @SectionId, 'Loop Control Statements', 'Break, continue, and pass', 0, 4, 15, 0, 1, @Now);

-- Section 3: Functions
SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'Functions in Python', 'Learn to create reusable code', 3, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'Defining Functions', 'Create your first function', 0, 1, 20, 0, 1, @Now),
       (NEWID(), @SectionId, 'Parameters and Return Values', 'Function arguments and returns', 0, 2, 25, 0, 1, @Now),
       (NEWID(), @SectionId, 'Lambda Functions', 'Anonymous functions in Python', 0, 3, 18, 0, 1, @Now),
       (NEWID(), @SectionId, 'Decorators', 'Advanced function concepts', 0, 4, 30, 0, 1, @Now);

PRINT 'Course 1 created: Python Programming Complete Bootcamp';

-- Course 2: JavaScript Modern Web Development
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'JavaScript Modern Web Development',
    'Complete JavaScript course covering ES6+, DOM manipulation, async programming, and modern frameworks. Build real-world projects and become a professional JS developer.',
    'Master modern JavaScript from basics to advanced concepts',
    @John, @DevCat, 59.99, 15.00, '/images/courses/javascript-modern.jpg',
    1, 1, @Now, @Now, 900,
    'Basic HTML and CSS knowledge. A computer with internet access.',
    'JavaScript ES6+ features. DOM manipulation. Async/Await and Promises. API integration. Build real projects.',
    'English', 1, 1, 'javascript-modern-web-development', 12350, 4.8, 287, 1523, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'JavaScript Basics', 'Core JavaScript concepts', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'Introduction to JavaScript', 'What is JavaScript and why learn it', 0, 1, 12, 1, 1, @Now),
       (NEWID(), @SectionId, 'Variables: let, const, var', 'Understanding variable declarations', 0, 2, 18, 1, 1, @Now),
       (NEWID(), @SectionId, 'Data Types and Operators', 'JS data types explained', 0, 3, 25, 0, 1, @Now),
       (NEWID(), @SectionId, 'Functions and Scope', 'Function declarations and expressions', 0, 4, 30, 0, 1, @Now);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'ES6+ Features', 'Modern JavaScript syntax', 2, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'Arrow Functions', 'Modern function syntax', 0, 1, 20, 0, 1, @Now),
       (NEWID(), @SectionId, 'Template Literals', 'String interpolation and more', 0, 2, 15, 0, 1, @Now),
       (NEWID(), @SectionId, 'Destructuring', 'Object and array destructuring', 0, 3, 22, 0, 1, @Now),
       (NEWID(), @SectionId, 'Spread and Rest Operators', 'Working with ... operator', 0, 4, 18, 0, 1, @Now);

PRINT 'Course 2 created: JavaScript Modern Web Development';

-- Course 3: React.js Complete Guide
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'React.js Complete Guide with Redux & Hooks',
    'Build modern web applications with React. Learn components, hooks, Redux, routing, and deploy real projects. The most comprehensive React course available.',
    'Build modern web apps with React, Hooks, and Redux',
    @John, @DevCat, 69.99, 25.00, '/images/courses/react-complete.jpg',
    1, 1, @Now, @Now, 1500,
    'JavaScript knowledge required. HTML/CSS basics. Node.js installed.',
    'React fundamentals. Hooks (useState, useEffect, etc). Redux state management. React Router. Testing with Jest. Deployment strategies.',
    'English', 1, 1, 'react-complete-guide-redux-hooks', 18920, 4.9, 456, 2341, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'React Fundamentals', 'Core React concepts', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'What is React?', 'Introduction to React library', 0, 1, 15, 1, 1, @Now),
       (NEWID(), @SectionId, 'Create React App', 'Setting up your first React project', 0, 2, 20, 1, 1, @Now),
       (NEWID(), @SectionId, 'JSX Syntax', 'Understanding JSX', 0, 3, 25, 0, 1, @Now),
       (NEWID(), @SectionId, 'Components and Props', 'Building reusable components', 0, 4, 30, 0, 1, @Now),
       (NEWID(), @SectionId, 'State Management', 'Managing component state', 0, 5, 35, 0, 1, @Now);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'React Hooks Deep Dive', 'Master all React hooks', 2, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'useState Hook', 'State in functional components', 0, 1, 25, 0, 1, @Now),
       (NEWID(), @SectionId, 'useEffect Hook', 'Side effects and lifecycle', 0, 2, 30, 0, 1, @Now),
       (NEWID(), @SectionId, 'useContext Hook', 'Context API with hooks', 0, 3, 25, 0, 1, @Now),
       (NEWID(), @SectionId, 'Custom Hooks', 'Create your own hooks', 0, 4, 35, 0, 1, @Now);

PRINT 'Course 3 created: React.js Complete Guide';

-- Course 4: .NET Core Web API Development
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, '.NET Core Web API Development Masterclass',
    'Build professional REST APIs with ASP.NET Core. Learn Entity Framework, authentication, authorization, testing, and deployment to Azure.',
    'Create professional REST APIs with ASP.NET Core 8',
    @Raj, @DevCat, 79.99, 10.00, '/images/courses/dotnet-api.jpg',
    2, 1, @Now, @Now, 1100,
    'C# programming knowledge. Basic understanding of HTTP. Visual Studio or VS Code.',
    'ASP.NET Core Web API. Entity Framework Core. JWT Authentication. Azure deployment. Unit testing.',
    'English', 1, 1, 'dotnet-core-webapi-masterclass', 8750, 4.6, 198, 987, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'API Fundamentals', 'Building your first API', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'Introduction to Web APIs', 'Understanding REST principles', 0, 1, 18, 1, 1, @Now),
       (NEWID(), @SectionId, 'Project Setup', 'Creating an ASP.NET Core project', 0, 2, 22, 1, 1, @Now),
       (NEWID(), @SectionId, 'Controllers and Routing', 'Handling HTTP requests', 0, 3, 28, 0, 1, @Now),
       (NEWID(), @SectionId, 'Model Binding', 'Working with request data', 0, 4, 25, 0, 1, @Now);

PRINT 'Course 4 created: .NET Core Web API Development';

-- Course 5: Docker and Kubernetes
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'Docker and Kubernetes: The Practical Guide',
    'Master containerization and orchestration. Learn Docker from scratch, then progress to Kubernetes. Deploy real applications to the cloud.',
    'Complete guide to Docker and Kubernetes deployment',
    @David, @DevCat, 89.99, 30.00, '/images/courses/docker-k8s.jpg',
    2, 1, @Now, @Now, 1400,
    'Basic Linux command line. Some programming experience. Cloud account (AWS, Azure, or GCP).',
    'Docker containers and images. Docker Compose. Kubernetes architecture. Deployments and Services. Helm charts. CI/CD integration.',
    'English', 1, 1, 'docker-kubernetes-practical-guide', 11280, 4.8, 312, 1678, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'Docker Fundamentals', 'Getting started with Docker', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'What is Docker?', 'Introduction to containerization', 0, 1, 15, 1, 1, @Now),
       (NEWID(), @SectionId, 'Installing Docker', 'Setup Docker on your machine', 0, 2, 18, 1, 1, @Now),
       (NEWID(), @SectionId, 'Docker Images', 'Understanding and using images', 0, 3, 25, 0, 1, @Now),
       (NEWID(), @SectionId, 'Docker Containers', 'Running and managing containers', 0, 4, 30, 0, 1, @Now),
       (NEWID(), @SectionId, 'Dockerfile Deep Dive', 'Creating custom images', 0, 5, 35, 0, 1, @Now);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'Kubernetes Essentials', 'Container orchestration', 2, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'Kubernetes Architecture', 'Understanding K8s components', 0, 1, 25, 0, 1, @Now),
       (NEWID(), @SectionId, 'Pods and Deployments', 'Running workloads in K8s', 0, 2, 35, 0, 1, @Now),
       (NEWID(), @SectionId, 'Services and Networking', 'Exposing applications', 0, 3, 30, 0, 1, @Now),
       (NEWID(), @SectionId, 'ConfigMaps and Secrets', 'Managing configuration', 0, 4, 25, 0, 1, @Now);

PRINT 'Course 5 created: Docker and Kubernetes';

-- ==============================================================
-- BUSINESS COURSES (4 courses)
-- ==============================================================

-- Course 6: Project Management Professional
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'Project Management Professional (PMP) Prep',
    'Complete PMP exam preparation course. Master all project management knowledge areas and pass the PMP certification exam on your first attempt.',
    'Complete PMP certification exam preparation',
    @Sarah, @BizCat, 99.99, 20.00, '/images/courses/pmp-prep.jpg',
    2, 1, @Now, @Now, 2000,
    'Project management experience recommended. PMI membership for exam discount.',
    'All PMP knowledge areas. Agile and predictive methodologies. Exam strategies. Practice questions. PMBOK Guide overview.',
    'English', 1, 1, 'project-management-professional-pmp-prep', 9870, 4.7, 234, 1234, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'Introduction to PMP', 'Understanding the certification', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'PMP Exam Overview', 'What to expect from the exam', 0, 1, 20, 1, 1, @Now),
       (NEWID(), @SectionId, 'PMI and Exam Registration', 'How to register for the exam', 0, 2, 15, 1, 1, @Now),
       (NEWID(), @SectionId, 'Study Plan', 'Creating an effective study schedule', 0, 3, 18, 0, 1, @Now);

PRINT 'Course 6 created: Project Management Professional';

-- Course 7: Financial Analysis
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'Financial Analysis and Modeling in Excel',
    'Learn financial analysis techniques and build professional financial models in Excel. Perfect for aspiring financial analysts and business professionals.',
    'Master financial analysis and Excel modeling',
    @Emily, @BizCat, 79.99, 15.00, '/images/courses/financial-analysis.jpg',
    1, 1, @Now, @Now, 800,
    'Basic Excel knowledge. Understanding of basic accounting concepts.',
    'Financial statement analysis. Ratio analysis. DCF modeling. Sensitivity analysis. Dashboard creation.',
    'English', 1, 1, 'financial-analysis-modeling-excel', 7650, 4.5, 178, 923, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'Financial Statements', 'Understanding the basics', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'Income Statement Analysis', 'Reading and analyzing P&L', 0, 1, 25, 1, 1, @Now),
       (NEWID(), @SectionId, 'Balance Sheet Analysis', 'Understanding financial position', 0, 2, 28, 0, 1, @Now),
       (NEWID(), @SectionId, 'Cash Flow Statement', 'Tracking cash movements', 0, 3, 25, 0, 1, @Now);

PRINT 'Course 7 created: Financial Analysis';

-- Course 8: Entrepreneurship Masterclass
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'Entrepreneurship: Start Your Business',
    'Complete guide to starting and growing your own business. Learn from successful entrepreneurs about business planning, funding, marketing, and scaling.',
    'Start and grow your own successful business',
    @Carlos, @BizCat, 69.99, 25.00, '/images/courses/entrepreneurship.jpg',
    0, 1, @Now, @Now, 600,
    'Business idea or desire to start a business. Willingness to take action.',
    'Business model canvas. Market research. Financial planning. Funding strategies. Marketing fundamentals.',
    'English', 1, 1, 'entrepreneurship-start-business', 5430, 4.6, 145, 756, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'Business Fundamentals', 'Getting started', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'Finding Your Business Idea', 'Ideation and validation', 0, 1, 22, 1, 1, @Now),
       (NEWID(), @SectionId, 'Business Model Canvas', 'Mapping your business', 0, 2, 28, 0, 1, @Now),
       (NEWID(), @SectionId, 'Market Research', 'Understanding your market', 0, 3, 25, 0, 1, @Now);

PRINT 'Course 8 created: Entrepreneurship';

-- Course 9: Leadership Skills
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'Leadership Skills for Managers',
    'Develop essential leadership skills to inspire and motivate your team. Learn communication, delegation, conflict resolution, and strategic thinking.',
    'Become an effective leader and manager',
    @Sarah, @BizCat, 59.99, 10.00, '/images/courses/leadership.jpg',
    1, 1, @Now, @Now, 500,
    'Some management experience helpful but not required.',
    'Leadership styles. Effective communication. Team motivation. Conflict resolution. Strategic thinking.',
    'English', 1, 1, 'leadership-skills-managers', 4320, 4.4, 123, 634, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'Leadership Foundations', 'Core leadership concepts', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'What Makes a Great Leader', 'Leadership qualities', 0, 1, 18, 1, 1, @Now),
       (NEWID(), @SectionId, 'Leadership Styles', 'Different approaches to leadership', 0, 2, 22, 0, 1, @Now),
       (NEWID(), @SectionId, 'Emotional Intelligence', 'EQ in leadership', 0, 3, 25, 0, 1, @Now);

PRINT 'Course 9 created: Leadership Skills';

-- ==============================================================
-- IT & SOFTWARE COURSES (4 courses)
-- ==============================================================

-- Course 10: AWS Solutions Architect
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'AWS Solutions Architect Associate 2024',
    'Prepare for the AWS Solutions Architect Associate certification. Learn all AWS services, architecture patterns, and best practices.',
    'Complete AWS SAA-C03 certification preparation',
    @Ahmed, @ITCat, 89.99, 20.00, '/images/courses/aws-architect.jpg',
    2, 1, @Now, @Now, 1800,
    'Basic IT knowledge. AWS Free Tier account. Dedication to learn.',
    'AWS core services. Architecture patterns. Security best practices. Cost optimization. Exam preparation.',
    'English', 1, 1, 'aws-solutions-architect-associate-2024', 14560, 4.8, 389, 2156, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'AWS Fundamentals', 'Getting started with AWS', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'Introduction to Cloud Computing', 'Cloud basics', 0, 1, 15, 1, 1, @Now),
       (NEWID(), @SectionId, 'AWS Global Infrastructure', 'Regions and availability zones', 0, 2, 20, 1, 1, @Now),
       (NEWID(), @SectionId, 'IAM Deep Dive', 'Identity and access management', 0, 3, 35, 0, 1, @Now),
       (NEWID(), @SectionId, 'EC2 Fundamentals', 'Virtual servers in the cloud', 0, 4, 40, 0, 1, @Now);

PRINT 'Course 10 created: AWS Solutions Architect';

-- Course 11: Cybersecurity Fundamentals
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'Cybersecurity Fundamentals and Ethical Hacking',
    'Learn cybersecurity concepts and ethical hacking techniques. Understand vulnerabilities, penetration testing, and security best practices.',
    'Master cybersecurity and ethical hacking basics',
    @Ahmed, @ITCat, 79.99, 15.00, '/images/courses/cybersecurity.jpg',
    1, 1, @Now, @Now, 1000,
    'Basic networking knowledge. Linux fundamentals helpful. Curiosity about security.',
    'Security fundamentals. Network security. Vulnerability assessment. Penetration testing basics. Security tools.',
    'English', 1, 1, 'cybersecurity-fundamentals-ethical-hacking', 11230, 4.7, 267, 1432, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'Security Fundamentals', 'Core security concepts', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'Introduction to Cybersecurity', 'The security landscape', 0, 1, 18, 1, 1, @Now),
       (NEWID(), @SectionId, 'CIA Triad', 'Confidentiality, Integrity, Availability', 0, 2, 22, 1, 1, @Now),
       (NEWID(), @SectionId, 'Types of Threats', 'Understanding cyber threats', 0, 3, 28, 0, 1, @Now),
       (NEWID(), @SectionId, 'Security Frameworks', 'NIST, ISO 27001, etc.', 0, 4, 25, 0, 1, @Now);

PRINT 'Course 11 created: Cybersecurity Fundamentals';

-- Course 12: Linux Administration
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'Linux Administration: Complete Course',
    'Master Linux system administration. Learn command line, file systems, user management, networking, and scripting. Perfect for DevOps and sysadmin roles.',
    'Become a Linux system administrator',
    @Raj, @ITCat, 69.99, 10.00, '/images/courses/linux-admin.jpg',
    1, 1, @Now, @Now, 900,
    'Basic computer skills. Access to a Linux system or VM. Willingness to practice.',
    'Linux command line mastery. User and permission management. File system administration. Networking configuration. Shell scripting.',
    'English', 1, 1, 'linux-administration-complete-course', 8940, 4.6, 212, 1087, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'Linux Basics', 'Getting started with Linux', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'Introduction to Linux', 'History and distributions', 0, 1, 15, 1, 1, @Now),
       (NEWID(), @SectionId, 'Installing Linux', 'Setting up your environment', 0, 2, 25, 1, 1, @Now),
       (NEWID(), @SectionId, 'Command Line Basics', 'Essential commands', 0, 3, 30, 0, 1, @Now),
       (NEWID(), @SectionId, 'File System Navigation', 'Navigating the Linux filesystem', 0, 4, 25, 0, 1, @Now);

PRINT 'Course 12 created: Linux Administration';

-- Course 13: SQL and Database Management
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'SQL and Database Management Complete Course',
    'Master SQL from basics to advanced queries. Learn database design, optimization, and administration for MySQL, PostgreSQL, and SQL Server.',
    'Complete SQL and database management training',
    @David, @ITCat, 59.99, 20.00, '/images/courses/sql-database.jpg',
    0, 1, @Now, @Now, 700,
    'No prior experience required. Computer with database software installed.',
    'SQL syntax and queries. Database design principles. Performance optimization. Stored procedures. Database administration basics.',
    'English', 1, 1, 'sql-database-management-complete', 10120, 4.7, 298, 1567, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'SQL Fundamentals', 'Getting started with SQL', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'Introduction to Databases', 'What are databases?', 0, 1, 12, 1, 1, @Now),
       (NEWID(), @SectionId, 'Setting Up Your Database', 'Installing and configuring', 0, 2, 20, 1, 1, @Now),
       (NEWID(), @SectionId, 'SELECT Statement', 'Querying data', 0, 3, 25, 0, 1, @Now),
       (NEWID(), @SectionId, 'Filtering with WHERE', 'Conditional queries', 0, 4, 22, 0, 1, @Now);

PRINT 'Course 13 created: SQL and Database Management';

-- ==============================================================
-- DESIGN COURSES (4 courses)
-- ==============================================================

-- Course 14: UI/UX Design Masterclass
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'UI/UX Design Masterclass: Figma to Real Projects',
    'Complete UI/UX design course using Figma. Learn user research, wireframing, prototyping, and design systems. Build a professional portfolio.',
    'Master UI/UX design with Figma',
    @Sophia, @DesignCat, 79.99, 15.00, '/images/courses/uiux-design.jpg',
    0, 1, @Now, @Now, 1100,
    'No design experience required. Computer with Figma installed (free).',
    'UI/UX design principles. User research methods. Wireframing and prototyping. Design systems. Portfolio building.',
    'English', 1, 1, 'uiux-design-masterclass-figma', 12340, 4.8, 345, 1876, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'Design Fundamentals', 'Core design concepts', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'Introduction to UI/UX', 'What is UI/UX design?', 0, 1, 15, 1, 1, @Now),
       (NEWID(), @SectionId, 'Design Principles', 'Visual hierarchy, contrast, etc.', 0, 2, 25, 1, 1, @Now),
       (NEWID(), @SectionId, 'Color Theory', 'Using color effectively', 0, 3, 22, 0, 1, @Now),
       (NEWID(), @SectionId, 'Typography Basics', 'Choosing and pairing fonts', 0, 4, 20, 0, 1, @Now);

PRINT 'Course 14 created: UI/UX Design Masterclass';

-- Course 15: Adobe Photoshop Masterclass
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'Adobe Photoshop CC: Complete Masterclass',
    'Master Adobe Photoshop from beginner to advanced. Learn photo editing, compositing, digital art, and graphic design techniques.',
    'Complete Photoshop training for all levels',
    @Yuki, @DesignCat, 69.99, 20.00, '/images/courses/photoshop.jpg',
    0, 1, @Now, @Now, 900,
    'Adobe Photoshop CC installed. No prior experience needed.',
    'Photoshop interface and tools. Photo retouching. Compositing techniques. Digital art creation. Graphic design projects.',
    'English', 1, 1, 'adobe-photoshop-cc-masterclass', 9870, 4.6, 278, 1456, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'Getting Started', 'Photoshop basics', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'Photoshop Interface', 'Navigating the workspace', 0, 1, 18, 1, 1, @Now),
       (NEWID(), @SectionId, 'Essential Tools', 'Core tools explained', 0, 2, 28, 1, 1, @Now),
       (NEWID(), @SectionId, 'Layers Fundamentals', 'Working with layers', 0, 3, 30, 0, 1, @Now),
       (NEWID(), @SectionId, 'Selections and Masks', 'Precise editing techniques', 0, 4, 35, 0, 1, @Now);

PRINT 'Course 15 created: Adobe Photoshop Masterclass';

-- Course 16: Graphic Design for Social Media
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'Graphic Design for Social Media Marketing',
    'Create stunning social media graphics using Canva and Adobe tools. Learn best practices for Instagram, Facebook, LinkedIn, and more.',
    'Design professional social media graphics',
    @Sophia, @DesignCat, 49.99, 10.00, '/images/courses/social-media-design.jpg',
    0, 1, @Now, @Now, 500,
    'No design experience required. Free Canva account.',
    'Social media design principles. Platform-specific dimensions. Brand consistency. Content templates. Engagement optimization.',
    'English', 1, 1, 'graphic-design-social-media-marketing', 7650, 4.5, 189, 978, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'Social Media Basics', 'Design for social platforms', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'Social Media Design Principles', 'What works on social media', 0, 1, 15, 1, 1, @Now),
       (NEWID(), @SectionId, 'Platform Dimensions', 'Size guides for all platforms', 0, 2, 18, 1, 1, @Now),
       (NEWID(), @SectionId, 'Creating with Canva', 'Getting started with Canva', 0, 3, 25, 0, 1, @Now);

PRINT 'Course 16 created: Graphic Design for Social Media';

-- Course 17: Motion Graphics with After Effects
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'Motion Graphics with Adobe After Effects',
    'Create stunning motion graphics and visual effects. Learn animation principles, compositing, and professional workflows in After Effects.',
    'Master motion graphics and visual effects',
    @Yuki, @DesignCat, 89.99, 25.00, '/images/courses/after-effects.jpg',
    1, 1, @Now, @Now, 1200,
    'Adobe After Effects CC. Basic design knowledge helpful.',
    'Animation principles. Keyframe animation. Visual effects. Motion graphics templates. Professional workflows.',
    'English', 1, 1, 'motion-graphics-adobe-after-effects', 6540, 4.7, 167, 856, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'After Effects Basics', 'Getting started', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'After Effects Interface', 'Workspace overview', 0, 1, 20, 1, 1, @Now),
       (NEWID(), @SectionId, 'Compositions and Layers', 'Working with compositions', 0, 2, 25, 0, 1, @Now),
       (NEWID(), @SectionId, 'Basic Animation', 'Keyframe fundamentals', 0, 3, 30, 0, 1, @Now);

PRINT 'Course 17 created: Motion Graphics with After Effects';

-- ==============================================================
-- MARKETING COURSES (4 courses)
-- ==============================================================

-- Course 18: Digital Marketing Complete Course
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'Digital Marketing Complete Course 2024',
    'Master all aspects of digital marketing. Learn SEO, social media, email marketing, PPC, content marketing, and analytics in one comprehensive course.',
    'Complete digital marketing training',
    @Maria, @MarketCat, 79.99, 20.00, '/images/courses/digital-marketing.jpg',
    0, 1, @Now, @Now, 1500,
    'No marketing experience required. Access to social media accounts for practice.',
    'SEO fundamentals. Social media marketing. Email marketing. PPC advertising. Content strategy. Analytics and reporting.',
    'English', 1, 1, 'digital-marketing-complete-course-2024', 16780, 4.7, 412, 2234, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'Digital Marketing Overview', 'Introduction to digital marketing', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'What is Digital Marketing?', 'Overview and landscape', 0, 1, 15, 1, 1, @Now),
       (NEWID(), @SectionId, 'Digital Marketing Channels', 'Understanding different channels', 0, 2, 22, 1, 1, @Now),
       (NEWID(), @SectionId, 'Creating a Strategy', 'Building your marketing plan', 0, 3, 28, 0, 1, @Now);

PRINT 'Course 18 created: Digital Marketing Complete Course';

-- Course 19: SEO Masterclass
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'SEO Masterclass: Rank #1 on Google',
    'Learn advanced SEO techniques to rank your website on Google. Master keyword research, on-page optimization, link building, and technical SEO.',
    'Master SEO and rank higher on Google',
    @Maria, @MarketCat, 69.99, 15.00, '/images/courses/seo-masterclass.jpg',
    1, 1, @Now, @Now, 800,
    'Website or blog to practice on. Basic understanding of how websites work.',
    'Keyword research. On-page SEO. Technical SEO. Link building strategies. Local SEO. SEO tools.',
    'English', 1, 1, 'seo-masterclass-rank-google', 11450, 4.8, 334, 1789, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'SEO Fundamentals', 'Understanding SEO', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'How Google Search Works', 'Understanding search engines', 0, 1, 18, 1, 1, @Now),
       (NEWID(), @SectionId, 'Keyword Research', 'Finding the right keywords', 0, 2, 30, 0, 1, @Now),
       (NEWID(), @SectionId, 'On-Page SEO', 'Optimizing your content', 0, 3, 35, 0, 1, @Now);

PRINT 'Course 19 created: SEO Masterclass';

-- Course 20: Social Media Marketing Strategy
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'Social Media Marketing Strategy',
    'Build a winning social media strategy. Learn platform-specific tactics for Instagram, TikTok, LinkedIn, and Twitter. Grow your audience organically.',
    'Create effective social media strategies',
    @Carlos, @MarketCat, 59.99, 10.00, '/images/courses/social-media-strategy.jpg',
    1, 1, @Now, @Now, 600,
    'Active social media accounts. Basic marketing knowledge helpful.',
    'Platform strategies. Content planning. Audience growth. Engagement tactics. Analytics and optimization.',
    'English', 1, 1, 'social-media-marketing-strategy', 8970, 4.5, 234, 1234, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'Social Media Strategy', 'Building your strategy', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'Choosing Your Platforms', 'Where to focus your efforts', 0, 1, 15, 1, 1, @Now),
       (NEWID(), @SectionId, 'Content Strategy', 'Planning your content', 0, 2, 25, 0, 1, @Now),
       (NEWID(), @SectionId, 'Building Your Audience', 'Organic growth tactics', 0, 3, 28, 0, 1, @Now);

PRINT 'Course 20 created: Social Media Marketing Strategy';

-- Course 21: Google Ads PPC Mastery
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'Google Ads PPC Mastery',
    'Master Google Ads and paid advertising. Learn campaign setup, keyword bidding, ad copywriting, and optimization for maximum ROI.',
    'Master Google Ads and PPC advertising',
    @Emily, @MarketCat, 79.99, 25.00, '/images/courses/google-ads.jpg',
    1, 1, @Now, @Now, 700,
    'Google Ads account. Budget for ad testing (small amount). Basic marketing knowledge.',
    'Campaign structure. Keyword strategy. Ad copywriting. Bidding strategies. Conversion tracking. Optimization techniques.',
    'English', 1, 1, 'google-ads-ppc-mastery', 7890, 4.6, 198, 1023, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'Google Ads Fundamentals', 'Getting started with PPC', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'Introduction to PPC', 'How pay-per-click works', 0, 1, 18, 1, 1, @Now),
       (NEWID(), @SectionId, 'Account Structure', 'Organizing your campaigns', 0, 2, 22, 0, 1, @Now),
       (NEWID(), @SectionId, 'Keyword Research for Ads', 'Finding profitable keywords', 0, 3, 28, 0, 1, @Now);

PRINT 'Course 21 created: Google Ads PPC Mastery';

-- ==============================================================
-- PHOTOGRAPHY & VIDEO COURSES (3 courses)
-- ==============================================================

-- Course 22: Photography Masterclass
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'Photography Masterclass: Complete Guide',
    'Master photography from beginner to advanced. Learn camera settings, composition, lighting, and post-processing. Works with any camera including smartphone.',
    'Complete photography training for all levels',
    @Sophia, @PhotoCat, 69.99, 15.00, '/images/courses/photography.jpg',
    0, 1, @Now, @Now, 1000,
    'Any camera (DSLR, mirrorless, or smartphone). Lightroom optional.',
    'Camera fundamentals. Exposure triangle. Composition techniques. Lighting mastery. Post-processing basics.',
    'English', 1, 1, 'photography-masterclass-complete-guide', 13560, 4.8, 387, 2012, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'Photography Basics', 'Getting started', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'Understanding Your Camera', 'Camera types and features', 0, 1, 20, 1, 1, @Now),
       (NEWID(), @SectionId, 'Exposure Triangle', 'Aperture, shutter speed, ISO', 0, 2, 30, 1, 1, @Now),
       (NEWID(), @SectionId, 'Shooting Modes', 'Auto, aperture priority, manual', 0, 3, 25, 0, 1, @Now);

PRINT 'Course 22 created: Photography Masterclass';

-- Course 23: Video Editing with Premiere Pro
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'Video Editing with Adobe Premiere Pro',
    'Master video editing with Premiere Pro. Learn professional editing techniques, color grading, audio mixing, and visual effects.',
    'Professional video editing training',
    @Yuki, @PhotoCat, 79.99, 20.00, '/images/courses/premiere-pro.jpg',
    1, 1, @Now, @Now, 900,
    'Adobe Premiere Pro CC. Video footage to practice with.',
    'Premiere Pro workflow. Editing techniques. Color grading. Audio editing. Effects and transitions. Export settings.',
    'English', 1, 1, 'video-editing-adobe-premiere-pro', 9870, 4.7, 256, 1345, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'Premiere Pro Basics', 'Getting started', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'Premiere Pro Interface', 'Navigating the workspace', 0, 1, 20, 1, 1, @Now),
       (NEWID(), @SectionId, 'Importing Media', 'Getting footage into Premiere', 0, 2, 15, 1, 1, @Now),
       (NEWID(), @SectionId, 'Basic Editing', 'Cutting and arranging clips', 0, 3, 30, 0, 1, @Now);

PRINT 'Course 23 created: Video Editing with Premiere Pro';

-- Course 24: YouTube Content Creation
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'YouTube Content Creation Masterclass',
    'Build a successful YouTube channel. Learn content strategy, video production, SEO, monetization, and audience building techniques.',
    'Build and grow your YouTube channel',
    @Carlos, @PhotoCat, 59.99, 15.00, '/images/courses/youtube-creator.jpg',
    0, 1, @Now, @Now, 700,
    'YouTube account. Basic video recording equipment (smartphone works).',
    'Content strategy. Video production tips. YouTube SEO. Monetization. Audience engagement. Analytics.',
    'English', 1, 1, 'youtube-content-creation-masterclass', 11230, 4.6, 289, 1567, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'YouTube Fundamentals', 'Getting started on YouTube', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'Starting Your Channel', 'Setting up for success', 0, 1, 15, 1, 1, @Now),
       (NEWID(), @SectionId, 'Finding Your Niche', 'Content strategy basics', 0, 2, 22, 1, 1, @Now),
       (NEWID(), @SectionId, 'Video Equipment', 'What you need to start', 0, 3, 20, 0, 1, @Now);

PRINT 'Course 24 created: YouTube Content Creation';

-- ==============================================================
-- MUSIC COURSES (3 courses)
-- ==============================================================

-- Course 25: Music Production with Ableton
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'Music Production with Ableton Live',
    'Create professional music with Ableton Live. Learn beat making, synthesis, mixing, and mastering. Perfect for electronic music producers.',
    'Master electronic music production',
    @Emily, @MusicCat, 89.99, 20.00, '/images/courses/ableton-production.jpg',
    1, 1, @Now, @Now, 1200,
    'Ableton Live (trial version works). MIDI controller helpful but not required.',
    'Ableton Live workflow. Beat making. Synthesis basics. Audio recording. Mixing techniques. Mastering fundamentals.',
    'English', 1, 1, 'music-production-ableton-live', 7890, 4.7, 198, 1023, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'Ableton Live Basics', 'Getting started', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'Ableton Interface', 'Session and arrangement views', 0, 1, 25, 1, 1, @Now),
       (NEWID(), @SectionId, 'Working with Clips', 'Creating and editing clips', 0, 2, 30, 0, 1, @Now),
       (NEWID(), @SectionId, 'MIDI Programming', 'Creating beats and melodies', 0, 3, 35, 0, 1, @Now);

PRINT 'Course 25 created: Music Production with Ableton';

-- Course 26: Guitar for Beginners
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'Guitar for Complete Beginners',
    'Learn guitar from scratch. Master chords, strumming patterns, and play your favorite songs. Step-by-step lessons for acoustic and electric guitar.',
    'Start playing guitar today',
    @Sarah, @MusicCat, 49.99, 10.00, '/images/courses/guitar-beginners.jpg',
    0, 1, @Now, @Now, 600,
    'Any guitar (acoustic or electric). Guitar tuner app. Pick.',
    'Guitar basics and posture. Essential chords. Strumming patterns. Reading tabs. Playing songs.',
    'English', 1, 1, 'guitar-for-complete-beginners', 15670, 4.8, 423, 2345, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'Guitar Basics', 'Getting started', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'Parts of the Guitar', 'Knowing your instrument', 0, 1, 10, 1, 1, @Now),
       (NEWID(), @SectionId, 'How to Hold the Guitar', 'Proper posture and technique', 0, 2, 12, 1, 1, @Now),
       (NEWID(), @SectionId, 'Your First Chord', 'Learning Em chord', 0, 3, 15, 0, 1, @Now);

PRINT 'Course 26 created: Guitar for Beginners';

-- Course 27: Piano Masterclass
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'Piano Masterclass: From Zero to Hero',
    'Complete piano course from beginner to intermediate. Learn proper technique, music theory, and play beautiful pieces. Covers classical and popular music.',
    'Learn piano from scratch',
    @Emily, @MusicCat, 59.99, 15.00, '/images/courses/piano-masterclass.jpg',
    0, 1, @Now, @Now, 800,
    'Piano or keyboard with at least 61 keys. No prior experience needed.',
    'Proper hand technique. Reading sheet music. Music theory basics. Classical pieces. Popular songs.',
    'English', 1, 1, 'piano-masterclass-zero-to-hero', 10890, 4.7, 298, 1567, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'Piano Fundamentals', 'Starting your journey', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'Piano Setup and Posture', 'Proper technique foundation', 0, 1, 12, 1, 1, @Now),
       (NEWID(), @SectionId, 'Finding the Keys', 'Understanding the keyboard layout', 0, 2, 15, 1, 1, @Now),
       (NEWID(), @SectionId, 'Your First Melody', 'Playing simple tunes', 0, 3, 20, 0, 1, @Now);

PRINT 'Course 27 created: Piano Masterclass';

-- ==============================================================
-- HEALTH & FITNESS COURSES (3 courses)
-- ==============================================================

-- Course 28: Complete Fitness Program
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'Complete Home Fitness Program',
    'Transform your body with this complete home workout program. Includes strength training, cardio, flexibility, and nutrition guidance.',
    'Complete home workout transformation',
    @Ahmed, @HealthCat, 69.99, 25.00, '/images/courses/home-fitness.jpg',
    0, 1, @Now, @Now, 900,
    'Exercise mat. Dumbbells or resistance bands helpful. No gym required.',
    'Proper exercise form. Strength training. Cardio workouts. Flexibility routines. Nutrition basics.',
    'English', 1, 1, 'complete-home-fitness-program', 12340, 4.6, 312, 1678, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'Getting Started', 'Foundation for success', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'Fitness Assessment', 'Know your starting point', 0, 1, 15, 1, 1, @Now),
       (NEWID(), @SectionId, 'Setting Goals', 'Creating achievable goals', 0, 2, 12, 1, 1, @Now),
       (NEWID(), @SectionId, 'Warm-up Routines', 'Preparing for exercise', 0, 3, 20, 0, 1, @Now);

PRINT 'Course 28 created: Complete Home Fitness Program';

-- Course 29: Yoga for Everyone
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'Yoga for Everyone: Mind and Body',
    'Discover the benefits of yoga for physical and mental health. Suitable for all fitness levels. Includes various styles from gentle to power yoga.',
    'Transform your mind and body with yoga',
    @Sophia, @HealthCat, 49.99, 10.00, '/images/courses/yoga-everyone.jpg',
    0, 1, @Now, @Now, 600,
    'Yoga mat. Comfortable clothing. Open mind.',
    'Basic yoga poses. Breathing techniques. Flexibility improvement. Stress relief. Different yoga styles.',
    'English', 1, 1, 'yoga-for-everyone-mind-body', 14560, 4.8, 389, 2012, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'Introduction to Yoga', 'Starting your practice', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'What is Yoga?', 'History and benefits', 0, 1, 10, 1, 1, @Now),
       (NEWID(), @SectionId, 'Basic Breathing', 'Pranayama fundamentals', 0, 2, 15, 1, 1, @Now),
       (NEWID(), @SectionId, 'First Poses', 'Beginner-friendly asanas', 0, 3, 25, 0, 1, @Now);

PRINT 'Course 29 created: Yoga for Everyone';

-- Course 30: Nutrition and Healthy Eating
SET @CourseId = NEWID();
INSERT INTO Courses (Id, Title, Description, ShortDescription, InstructorId, CategoryId, Price, DiscountPercentage,
    ThumbnailUrl, Level, Status, CreatedAt, PublishedAt, EstimatedDurationMinutes,
    Requirements, WhatYouWillLearn, Language, HasCertificate, IsActive, Slug, ViewCount, AverageRating, ReviewCount, EnrollmentCount, IsSubscriptionOnly)
VALUES (@CourseId, 'Nutrition and Healthy Eating Masterclass',
    'Understand nutrition science and transform your eating habits. Learn meal planning, macros, and develop a sustainable healthy lifestyle.',
    'Master nutrition for optimal health',
    @Raj, @HealthCat, 59.99, 15.00, '/images/courses/nutrition.jpg',
    0, 1, @Now, @Now, 500,
    'Open mind about nutrition. Kitchen access for cooking.',
    'Nutrition fundamentals. Macronutrients and micronutrients. Meal planning. Reading food labels. Sustainable eating habits.',
    'English', 1, 1, 'nutrition-healthy-eating-masterclass', 8970, 4.5, 234, 1234, 0);

SET @SectionId = NEWID();
INSERT INTO Sections (Id, CourseId, Title, Description, OrderIndex, CreatedAt, IsActive)
VALUES (@SectionId, @CourseId, 'Nutrition Basics', 'Understanding food', 1, @Now, 1);

INSERT INTO Lessons (Id, SectionId, Title, Description, Type, OrderIndex, DurationMinutes, IsFree, IsActive, CreatedAt)
VALUES (NEWID(), @SectionId, 'Introduction to Nutrition', 'Why nutrition matters', 0, 1, 12, 1, 1, @Now),
       (NEWID(), @SectionId, 'Macronutrients', 'Proteins, carbs, and fats', 0, 2, 25, 0, 1, @Now),
       (NEWID(), @SectionId, 'Micronutrients', 'Vitamins and minerals', 0, 3, 22, 0, 1, @Now);

PRINT 'Course 30 created: Nutrition and Healthy Eating';

-- ==============================================================
-- Summary Statistics
-- ==============================================================

SELECT 'Courses created: ' + CAST(COUNT(*) AS VARCHAR(10)) FROM Courses;
SELECT 'Sections created: ' + CAST(COUNT(*) AS VARCHAR(10)) FROM Sections;
SELECT 'Lessons created: ' + CAST(COUNT(*) AS VARCHAR(10)) FROM Lessons;

SET NOCOUNT OFF;
GO
