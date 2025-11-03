# Code Signing Setup for ImageGallery

This document explains how to set up trusted code signing for the ImageGallery project to eliminate Windows SmartScreen warnings when users download releases.

## Problem

When users download unsigned executables from GitHub releases, Windows SmartScreen shows security warnings like:
- "Windows protected your PC"
- "This app might harm your device"
- "Unknown publisher"

These warnings can discourage users from installing the software, even though it's completely safe.

## Solution

We implement **automated code signing** in our GitHub Actions workflow using two approaches:

### Option 1: SignPath (Recommended for Production)

[SignPath](https://about.signpath.io/) provides **free code signing for open-source projects** with trusted certificates.

#### Benefits:
- ? **Completely eliminates Windows SmartScreen warnings**
- ? **Free for open-source projects**
- ? **Trusted by Windows (users see green checkmarks)**
- ? **Professional certificate chain**
- ? **Automatic timestamping**

#### Setup Steps:

1. **Apply for free open-source plan:**
   ```
   Go to: https://about.signpath.io/product/open-source
   Submit application with:
   - Project URL: https://github.com/okamarcelo/portable-image-gallery
   - Project description
   - Confirmation that it's open-source
   ```

2. **Configure GitHub repository secrets:**
   ```
   Repository Settings ? Secrets and variables ? Actions ? New repository secret
   
   Add these secrets:
   - SIGNPATH_API_TOKEN: (provided by SignPath)
   - SIGNPATH_ORGANIZATION_ID: (provided by SignPath)  
   - SIGNPATH_PROJECT_SLUG: (your project identifier)
   ```

3. **Test the workflow:**
   - Create a PR with the `feature` label
   - Merge to trigger the release workflow
   - Verify executables are signed and trusted

### Option 2: Self-Signed Certificate (Fallback)

If SignPath is not configured, the workflow automatically falls back to self-signing.

#### Characteristics:
- ?? **Still triggers SmartScreen warnings**
- ? **Better than unsigned (shows intent to sign)**
- ? **No external dependencies**
- ? **Works immediately**

## Workflow Features

The enhanced release workflow (`release-signed.yml`) includes:

### Signing Process
- **Detects** if SignPath is configured via environment variables
- **Uses SignPath** for trusted signing when configured
- **Falls back** to self-signing when SignPath unavailable
- **Verifies** all signatures after signing
- **Logs** detailed signing status

### Enhanced Release Notes
- **Code signing status** clearly displayed
- **Installation instructions** specific to signing method
- **SmartScreen guidance** for self-signed releases
- **SHA256 checksums** for integrity verification

### Security Features
- **Signature verification** in build process
- **Checksum generation** for download integrity
- **Detailed logging** for debugging
- **Clean certificate handling** (no persistence)

## Current Status

### Without SignPath Configuration:
```
?? Self-signed certificate - Windows may show security warnings
Users need to click "More info" ? "Run anyway"
```

### With SignPath Configuration:
```
? Digitally signed with trusted certificate via SignPath
No Windows SmartScreen warnings
Professional installation experience
```

## File Changes

### New Files:
- `.github/workflows/release-signed.yml` - Enhanced release workflow with code signing

### Modified Files:
- (Preserves existing `release.yml` for comparison)

## Testing

### Before SignPath Setup:
1. Create test release using current workflow
2. Download and verify SmartScreen warning appears
3. Note user friction in installation process

### After SignPath Setup:
1. Configure SignPath secrets
2. Create test release using new workflow
3. Download and verify no SmartScreen warnings
4. Confirm professional installation experience

## Benefits for Users

### Immediate Benefits (Self-Signed):
- Shows developer intent to provide secure software
- Slightly reduces user friction
- Maintains full functionality

### Full Benefits (SignPath):
- **No security warnings** - Professional installation experience
- **Increased user trust** - Green checkmarks and verified publisher
- **Wider adoption** - Users more likely to install trusted software
- **Professional appearance** - Matches commercial software standards

## Cost

- **SignPath for Open Source:** FREE
- **Self-signed fallback:** FREE
- **Development time:** Minimal (automated in CI/CD)
- **Maintenance:** None (fully automated)

## Next Steps

1. **Apply for SignPath free open-source plan**
2. **Configure repository secrets** when approved
3. **Test with a release** to verify functionality
4. **Monitor user feedback** on installation experience
5. **Update documentation** with final instructions

---

## Troubleshooting

### Common Issues:

**SignTool not found:**
```powershell
# The workflow automatically locates SignTool from Windows SDK
# If issues persist, check Windows SDK installation
```

**Self-signed certificate warnings:**
```
This is expected behavior for self-signed certificates.
Consider upgrading to SignPath for trusted signing.
```

**SignPath API errors:**
```
Verify that all secrets are correctly configured:
- SIGNPATH_API_TOKEN
- SIGNPATH_ORGANIZATION_ID  
- SIGNPATH_PROJECT_SLUG
```

For more help, check the [SignPath documentation](https://about.signpath.io/documentation) or create an issue in this repository.