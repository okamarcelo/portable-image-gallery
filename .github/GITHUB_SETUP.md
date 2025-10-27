# GitHub Repository Configuration

This document describes the required GitHub repository settings for the automated workflows.

## Branch Protection Rules

To block direct commits to `main` and enforce the PR workflow, configure the following branch protection rules:

### Main Branch Protection

1. Go to: **Settings** ? **Branches** ? **Branch protection rules** ? **Add rule**

2. Configure the following settings:

   **Branch name pattern:** `main`

   ? **Require a pull request before merging**
   - ? Require approvals: `0` (or `1` if you want reviews)
   - ? Dismiss stale pull request approvals when new commits are pushed
   - ? Require review from Code Owners (optional)

   ? **Require status checks to pass before merging**
   - ? Require branches to be up to date before merging
   - Required status checks:
     - `build-and-test / build-and-test`

   ? **Require conversation resolution before merging** (optional)

   ? **Require linear history** (optional, prevents merge commits)

   ? **Do not allow bypassing the above settings**
   - This ensures admins also follow the rules

   ? **Allow force pushes** (keep disabled)

   ? **Allow deletions** (keep disabled)

3. Click **Create** or **Save changes**

## Workflow Files

The repository includes two GitHub Actions workflows:

### 1. Build and Test (`build-and-test.yml`)

**Triggers:**
- Push to branches starting with `feat*` or `fix*`

**Actions:**
- Restores .NET dependencies
- Builds the solution in Release mode
- Runs all tests
- If successful, automatically creates a Pull Request to `main`

**Required Permissions:**
- `contents: write`
- `pull-requests: write`

### 2. Release (`release.yml`)

**Triggers:**
- When a Pull Request to `main` is merged (closed with merge)

**Actions:**
- Builds the solution in Release mode
- Runs all tests
- Publishes the application
- Creates a versioned release with the binary
- Generates a changelog from the PR

**Required Permissions:**
- `contents: write`

## Development Workflow

### Creating a New Feature or Fix

1. **Create a new branch** from `main`:
   ```bash
   git checkout main
   git pull
   git checkout -b feat-my-new-feature
   # or
   git checkout -b fix-bug-description
   ```

2. **Make your changes** and commit:
   ```bash
   git add .
   git commit -m "Add new feature"
   git push origin feat-my-new-feature
   ```

3. **Automatic workflow**:
   - GitHub Actions builds and tests your branch
   - If successful, a PR to `main` is automatically created
   - Review the PR (or have it reviewed)
   - Merge the PR

4. **Release creation**:
   - When the PR is merged, a new release is automatically created
   - The release includes the compiled binary
   - Version format: `v{date}-pr{number}` (e.g., `v2025-10-27-pr5`)

### Branch Naming Convention

Use these prefixes for automatic PR creation:
- `feat*` - New features (e.g., `feat-add-cli-support`)
- `fix*` - Bug fixes (e.g., `fix-resize-issue`)

Examples:
- `feat-fullscreen-mode`
- `feat-cli-parameters`
- `fix-window-resize`
- `fix-pattern-search`

## Manual Operations

### Creating a Release Manually (if needed)

If you need to create a release manually:

1. Go to **Releases** ? **Draft a new release**
2. Click **Choose a tag** ? Enter version (e.g., `v1.0.0`)
3. Set **Release title** (e.g., `Release v1.0.0`)
4. Add **Description**
5. Upload the binary from `src/ImageGallery/bin/Release/net8.0-windows/win-x64/publish/`
6. Click **Publish release**

### Managing Pull Requests

- PRs are automatically created by the workflow
- You can still create PRs manually if needed
- Review and merge PRs through the GitHub UI
- Delete the source branch after merging (GitHub can do this automatically)

## Troubleshooting

### Workflow Fails to Create PR

**Possible causes:**
- Branch protection rules prevent the action
- Missing permissions
- PR already exists

**Solution:**
Check the Actions log for detailed error messages.

### Release Not Created After PR Merge

**Possible causes:**
- PR was closed without merging
- Workflow failed (check Actions tab)
- Missing write permissions

**Solution:**
1. Check the Actions tab for workflow runs
2. Review the workflow logs
3. Ensure permissions are set correctly

### Build or Test Failures

**Solution:**
1. Review the failed workflow in the Actions tab
2. Fix the issues in your branch
3. Push new commits
4. The workflow will run again automatically

## Security Considerations

- The workflows use `GITHUB_TOKEN` which is automatically provided
- No additional secrets are required
- Branch protection ensures code review
- All builds and tests must pass before merging

## Maintenance

### Updating Workflows

To update the workflows:
1. Edit the `.yml` files in `.github/workflows/`
2. Commit and push changes
3. Workflows are automatically updated

### .NET Version Updates

To update the .NET version:
1. Edit `dotnet-version` in both workflow files
2. Update the project files if needed
3. Test locally before pushing

---

**Note:** After setting up branch protection, you won't be able to push directly to `main`. All changes must go through the PR process.
