# Branch Protection Setup Checklist

Follow these steps in order to enable branch protection for the `main` branch.

## Prerequisites

- [ ] You have admin/owner permissions on the repository
- [ ] Repository URL: https://github.com/okamarcelo/portable-image-gallery
- [ ] You have pushed at least one feature branch to trigger the workflow

## Step-by-Step Checklist

### 1. Navigate to Repository Settings

- [ ] Go to https://github.com/okamarcelo/portable-image-gallery
- [ ] Click the **Settings** tab (next to "Insights")
- [ ] In left sidebar, click **Branches**
- [ ] Click **Add branch protection rule** button

### 2. Configure Branch Name

- [ ] In "Branch name pattern" field, type: `main`

### 3. Configure Protection Settings

#### Pull Request Requirements
- [ ] ? Check **"Require a pull request before merging"**
- [ ] Set "Require approvals" to: `0` (or `1` if you want self-review)
- [ ] ? Check "Dismiss stale pull request approvals when new commits are pushed" (optional)

#### Status Check Requirements
- [ ] ? Check **"Require status checks to pass before merging"**
- [ ] ? Check "Require branches to be up to date before merging"
- [ ] In the search box, type: `build-and-test`
- [ ] ? Click on `build-and-test / build-and-test` to add it
  - ?? If it doesn't appear, push a feature branch first to trigger the workflow

#### Admin Enforcement
- [ ] ? Check **"Do not allow bypassing the above settings"**
  - This is crucial! It ensures even you can't accidentally push to main

#### Disabled Options (Leave Unchecked)
- [ ] ? "Allow force pushes" - Keep unchecked
- [ ] ? "Allow deletions" - Keep unchecked

### 4. Save Configuration

- [ ] Scroll to bottom
- [ ] Click **"Create"** button
- [ ] Wait for confirmation message

### 5. Verify Branch Protection Works

Open PowerShell/Terminal and run:

```powershell
# Try to push directly to main (this should FAIL)
git checkout main
git pull
echo "# test" >> test-protection.txt
git add test-protection.txt
git commit -m "test: verify branch protection"
git push origin main
```

**Expected Result:**
```
remote: error: GH006: Protected branch update failed
remote: error: Changes must be made through a pull request
```

- [ ] Push to main was blocked ?
- [ ] Error message confirms branch protection is active

### 6. Test the Correct Workflow

```powershell
# Create a feature branch (this should WORK)
git checkout -b feat-branch-protection-test
git push origin feat-branch-protection-test
```

- [ ] Feature branch pushed successfully
- [ ] GitHub Actions workflow started automatically
- [ ] Pull request created automatically (if workflow succeeded)

### 7. Clean Up

```powershell
# Remove test file
git checkout main
git pull
Remove-Item test-protection.txt -ErrorAction SilentlyContinue
```

## Troubleshooting

### Issue: Can't find Settings tab

**Problem:** Settings tab not visible
**Solution:** You need admin/owner permissions on the repository

### Issue: Status check doesn't appear

**Problem:** `build-and-test` doesn't show in search
**Solution:** 
1. Push a feature branch: `git push origin feat-test`
2. Wait for workflow to complete
3. Go back to branch protection and add the status check

### Issue: Still able to push to main

**Problem:** Branch protection not working
**Solution:** 
1. Verify "Do not allow bypassing" is checked
2. Hard refresh browser (Ctrl+F5)
3. Check rule applies to `main` (exact name)

## Summary

Once completed, your development workflow will be:

1. ? Create feature branch (`feat-*` or `fix-*`)
2. ? Push to GitHub
3. ? Automated build & test runs
4. ? PR automatically created on success
5. ? Review and merge PR to main
6. ? Release automatically created
7. ? Direct push to main = BLOCKED

---

**Status After Setup:**
- [ ] Branch protection configured
- [ ] Verified protection works
- [ ] Tested feature branch workflow
- [ ] Ready to develop with confidence!
