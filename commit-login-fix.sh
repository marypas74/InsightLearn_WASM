#!/bin/bash

# Script to commit the login navigation loop fix

echo "==================================="
echo "Login Navigation Loop Fix"
echo "==================================="

# Show the changes made
echo ""
echo "Changes made:"
echo "1. Added SanitizeReturnUrl method to Login.razor"
echo "2. Modified HandleLogin to use sanitized URL with forceLoad: true"
echo "3. Added delay for auth state propagation"
echo "4. Modified LoginWithGoogle to use sanitized URL"
echo ""

# Show the git diff for the specific file
echo "Git diff for Login.razor:"
echo "------------------------"
git diff src/InsightLearn.WebAssembly/Pages/Login.razor

echo ""
echo "==================================="
echo "Summary of the fix:"
echo "==================================="
echo "PROBLEM: Login page was receiving absolute URLs in returnUrl parameter"
echo "         (e.g., 'https://www.insightlearn.cloud/admin/dashboard')"
echo "         causing navigation loop after successful authentication."
echo ""
echo "SOLUTION:"
echo "1. Created SanitizeReturnUrl method to extract relative path from absolute URLs"
echo "2. Added 100ms delay after login to allow auth state to propagate"
echo "3. Used NavigateTo with forceLoad: true for proper navigation after login"
echo "4. Added console logging for debugging navigation flow"
echo ""
echo "EXPECTED BEHAVIOR:"
echo "- User logs in successfully"
echo "- returnUrl is sanitized from absolute to relative path"
echo "- Navigation properly redirects to /admin/dashboard"
echo "- No more login/dashboard loop"
echo ""

# Ask for confirmation to commit
read -p "Do you want to commit these changes? (y/n): " -n 1 -r
echo ""

if [[ $REPLY =~ ^[Yy]$ ]]; then
    git add src/InsightLearn.WebAssembly/Pages/Login.razor
    git commit -m "fix: Resolve login/dashboard navigation loop by sanitizing returnUrl

PROBLEM: Login page was receiving absolute URLs in returnUrl parameter
(e.g., 'https://www.insightlearn.cloud/admin/dashboard') causing a
navigation loop after successful authentication.

SOLUTION:
- Add SanitizeReturnUrl method to extract relative paths from absolute URLs
- Add 100ms delay after login for auth state propagation
- Use NavigateTo with forceLoad: true for proper post-login navigation
- Update both email and Google OAuth login flows

IMPACT:
- Users can now successfully navigate to dashboard after login
- No more infinite redirect loops
- Proper handling of both relative and absolute return URLs"

    echo ""
    echo "✅ Changes committed successfully!"
    echo ""
    echo "To push: git push origin main"
else
    echo "Commit cancelled."
fi