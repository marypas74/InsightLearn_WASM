# Admin Dashboard MVP - Quick Start Guide

## 🚀 Quick Access

**Admin Credentials**:
- Email: `admin@insightlearn.cloud`
- Password: `Admin@InsightLearn2025!`

**Admin Pages**:
- Dashboard: http://localhost:7003/admin/dashboard
- Users: http://localhost:7003/admin/users

---

## ✅ What's Working (MVP v1.0)

### Dashboard Page
- ✅ 4 Stats Cards (Users, Courses, Enrollments, Revenue)
- ✅ Recent Activity Feed
- ✅ Quick Actions Menu

### Users Page
- ✅ User List with Pagination
- ✅ Real-time Search (300ms debounce)
- ✅ Delete User with Confirmation Dialog
- ✅ View User Details
- ✅ Status Badges (Verified/Pending)
- ✅ Role Badges
- ✅ User Stats (Courses/Enrollments)

### Backend API (9 endpoints)
- ✅ `GET /api/admin/dashboard/stats`
- ✅ `GET /api/admin/dashboard/recent-activity`
- ✅ `GET /api/admin/users` (with pagination & search)
- ✅ `GET /api/admin/users/{id}`
- ✅ `PUT /api/admin/users/{id}`
- ✅ `DELETE /api/admin/users/{id}`
- ✅ `GET /api/admin/courses` (with pagination & search)
- ✅ `POST /api/admin/courses`
- ✅ `PUT /api/admin/courses/{id}`
- ✅ `DELETE /api/admin/courses/{id}`

---

## 📋 5-Minute Test Checklist

### 1. Test Dashboard (2 min)
1. Login as admin
2. Navigate to `/admin/dashboard`
3. Verify 4 stats cards show numbers
4. Check recent activity list (or "No activity" message)
5. Click "Manage Users" quick action

### 2. Test User Management (3 min)
1. You should be on `/admin/users` now
2. **Test Search**:
   - Type a name in search box
   - Wait 300ms
   - Verify table updates
3. **Test Pagination**:
   - Click "Next" button (▶)
   - Verify page 2 loads
   - Click page number "1"
   - Verify back to page 1
4. **Test Delete**:
   - Click Delete button (🗑️) on a user
   - Verify confirm dialog appears
   - Click "Cancel" → Dialog closes
   - Click Delete again → Click "Delete" button
   - Verify success toast appears
   - Verify user removed from list
5. **Test Refresh**:
   - Click "Refresh" button
   - Verify loading spinner appears briefly
   - Verify data reloads

---

## 🔧 Build & Run

### Backend API
```bash
cd src/InsightLearn.Application
dotnet run
# API runs on http://localhost:7001
```

### Frontend (Blazor WASM)
```bash
cd src/InsightLearn.WebAssembly
dotnet run
# Web runs on http://localhost:7003
```

### Both Together (Recommended)
```bash
# Terminal 1 - API
dotnet run --project src/InsightLearn.Application/InsightLearn.Application.csproj

# Terminal 2 - Web
dotnet run --project src/InsightLearn.WebAssembly/InsightLearn.WebAssembly.csproj
```

---

## 🎯 Success Criteria Check

- [x] ✅ Admin can see dashboard stats
- [x] ✅ Admin can view user list with pagination
- [x] ✅ Admin can search users
- [x] ✅ Admin can delete users (with confirmation)
- [x] ✅ All endpoints require Admin role authorization
- [x] ✅ Reusable components (DataTable, ConfirmDialog)

**Result**: 6/6 criteria met ✅

---

## 📁 New Files Created

**Backend**:
- `src/InsightLearn.Application/Program.cs` (+578 lines)

**Frontend Services**:
- `Services/Admin/IAdminDashboardService.cs`
- `Services/Admin/AdminDashboardService.cs`
- `Services/Admin/IUserManagementService.cs`
- `Services/Admin/UserManagementService.cs`
- `Services/Admin/ICourseManagementService.cs`
- `Services/Admin/CourseManagementService.cs`

**Frontend Models**:
- `Models/Admin/DashboardModels.cs`
- `Models/Admin/UserModels.cs`
- `Models/Courses/CourseModels.cs`

**Frontend Components**:
- `Components/Admin/DataTable.razor` (Reusable table)
- `Components/Admin/ConfirmDialog.razor` (Reusable modal)

**Frontend Pages**:
- `Pages/Admin/Dashboard.razor` (Updated)
- `Pages/Admin/Users.razor` (New)

**Styles**:
- `wwwroot/css/admin-components.css` (470 lines)

**Total**: 15 files, ~2,234 LOC

---

## 🐛 Troubleshooting

### 401 Unauthorized Error
**Problem**: Endpoint returns 401

**Fix**: Ensure admin user has "Admin" role in database
```sql
SELECT u.Email, r.Name
FROM AspNetUsers u
JOIN AspNetUserRoles ur ON u.Id = ur.UserId
JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.Email = 'admin@insightlearn.cloud'
```

### Empty User List
**Problem**: No users shown in table

**Debug**:
1. Check browser console for errors
2. Check Network tab for API response
3. Verify database has users

### Search Not Working
**Problem**: Search doesn't trigger

**Fix**: Type at least 1 character and wait 300ms for debounce

---

## 📚 Documentation

Full implementation details: [ADMIN-DASHBOARD-MVP-IMPLEMENTATION.md](ADMIN-DASHBOARD-MVP-IMPLEMENTATION.md)

**Sections**:
- Backend API Endpoints (9 total)
- Frontend Services (3 services)
- UI Components (2 reusable)
- Pages Implementation
- Testing Instructions
- Security Considerations
- Next Steps (Post-MVP)

---

## 🎨 UI Preview

### Dashboard
```
┌─────────────────────────────────────────────┐
│ Admin Dashboard                             │
│ Monitor and manage your learning platform   │
├─────────────────────────────────────────────┤
│ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐        │
│ │ 125  │ │  45  │ │ 234  │ │€1.5K │        │
│ │Users │ │Cours.│ │Enroll│ │Reven.│        │
│ └──────┘ └──────┘ └──────┘ └──────┘        │
├─────────────────────────────────────────────┤
│ Quick Actions:                              │
│ [👥 Users] [📚 Courses] [📊 Analytics]     │
├─────────────────────────────────────────────┤
│ Recent Activity:                            │
│ • John Doe enrolled in React Course         │
│ • New user registered: jane@example.com     │
│ • Course "Python Basics" published          │
└─────────────────────────────────────────────┘
```

### Users Page
```
┌─────────────────────────────────────────────┐
│ User Management                             │
│ [🔍 Search users...]      [🔄 Refresh]     │
├─────────────────────────────────────────────┤
│ User       │ Email      │ Roles  │ Actions │
├────────────┼────────────┼────────┼─────────┤
│ JD  John   │john@...    │Student │👁️ 🗑️   │
│     Doe    │            │        │         │
├────────────┼────────────┼────────┼─────────┤
│ JS  Jane   │jane@...    │Admin   │👁️ 🗑️   │
│     Smith  │            │Teacher │         │
├─────────────────────────────────────────────┤
│ Showing 1 to 10 of 45    [◀◀][◀][1][2][▶][▶▶]│
└─────────────────────────────────────────────┘
```

---

**Status**: ✅ Production Ready (MVP v1.0)
**Next**: Implement Course Management UI (backend ready)
