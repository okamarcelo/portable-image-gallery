# Semantic Versioning

This project uses [Semantic Versioning 2.0.0](https://semver.org/) for releases.

## Version Format

Versions follow the format: `vMAJOR.MINOR.PATCH`

- **MAJOR**: Incremented for breaking changes (incompatible API changes)
- **MINOR**: Incremented for new features (backward-compatible functionality)
- **PATCH**: Incremented for bug fixes (backward-compatible fixes)

## Automatic Version Bumping

The release workflow automatically determines the version bump based on **PR labels**:

### Label-Based Versioning

| PR Label | Version Bump | Example |
|----------|--------------|---------|
| `major` or `breaking` | MAJOR version | v1.2.3 ? v2.0.0 |
| `minor` or `feature` | MINOR version | v1.2.3 ? v1.3.0 |
| `patch`, `fix`, or no label | PATCH version | v1.2.3 ? v1.2.4 |

### Examples

**Breaking Change (MAJOR bump):**
```
PR Title: "refactor!: change CLI argument format"
Labels: breaking
Result: v1.2.3 ? v2.0.0
```

**New Feature (MINOR bump):**
```
PR Title: "feat: add slideshow pause on mouse hover"
Labels: feature
Result: v1.2.3 ? v1.3.0
```

**Bug Fix (PATCH bump):**
```
PR Title: "fix: resolve memory leak in image loading"
Labels: fix
Result: v1.2.3 ? v1.2.4
```

**No Label (PATCH bump - default):**
```
PR Title: "chore: update dependencies"
Labels: (none)
Result: v1.2.3 ? v1.2.4
```

## How to Use

### 1. Create a Feature Branch
```bash
git checkout -b feature/my-new-feature
```

### 2. Make Your Changes
Commit your changes following conventional commit format:
```bash
git commit -m "feat: add new image filter"
git commit -m "fix: correct rotation angle"
```

### 3. Push and Create PR
```bash
git push origin feature/my-new-feature
```

The build-and-test workflow will automatically create a PR.

### 4. Add Version Label
Add one of these labels to your PR:
- `major` or `breaking` - for breaking changes
- `minor` or `feature` - for new features
- `fix` or `patch` - for bug fixes
- (or leave unlabeled for default PATCH bump)

### 5. Merge the PR
When you merge the PR to `main`, the release workflow:
1. Reads the latest version from releases
2. Determines the version bump based on PR labels
3. Increments the appropriate version component
4. Creates a new release with the semantic version tag

## First Release

If no releases exist yet, the first version will be `v1.0.0`.

## Version History

You can view all releases and their versions:
```bash
gh release list
```

Or check the latest version:
```bash
gh release view --json tagName -q .tagName
```

## Manual Override

If you need to manually set a version, you can create a release manually:
```bash
gh release create v2.0.0 --title "Release v2.0.0" --notes "Manual release"
```

The next automated release will increment from this version.

## Conventional Commits

While not required, it's recommended to use [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` - New feature (suggests `minor` label)
- `fix:` - Bug fix (suggests `fix` label)
- `docs:` - Documentation only (suggests `patch` label)
- `refactor:` - Code refactoring (suggests `patch` label)
- `perf:` - Performance improvement (suggests `patch` label)
- `test:` - Adding tests (suggests `patch` label)
- `chore:` - Maintenance tasks (suggests `patch` label)
- `feat!:` or `fix!:` - Breaking change (suggests `breaking` label)

The `!` suffix indicates a breaking change and should use the `breaking` or `major` label.
