# SQL Server Samples Version Management

This repository includes an automated version management system that ensures all libraries and projects maintain consistent version numbers across builds.

## 🚀 Features

- **Centralized Version Configuration**: Single source of truth for all version numbers
- **Automatic Version Incrementing**: Build-triggered version updates based on branch and event type
- **Multi-Platform Support**: Handles C#, Node.js, JavaScript libraries, and more
- **CI/CD Integration**: Works with GitHub Actions, Azure DevOps, Jenkins, and other CI systems
- **Flexible Versioning Strategies**: Support for major, minor, and patch increments
- **Build History Tracking**: Maintains history of version changes and build numbers

## 📁 File Structure

```
sql-server-samples/
├── version.json                          # Central version configuration
├── update-versions.js                    # Universal version update script
├── build-integration.js                  # CI/CD build integration script
├── .github/workflows/version-management.yml  # GitHub Actions workflow
└── VERSION_MANAGEMENT.md                 # This documentation
```

## 🔧 Core Components

### 1. Version Configuration (`version.json`)

Central configuration file that defines:

```json
{
  "version": "1.0.0",                    // Main project version
  "buildNumber": 0,                      // Incremental build number
  "projects": {
    "csharp": {
      "assemblyVersion": "1.0.0.0",      // C# assembly version
      "fileVersion": "1.0.0.0"           // C# file version
    },
    "nodejs": {
      "version": "1.0.0"                 // Node.js package version
    },
    "javascript": {
      "bootstrap": "3.3.7",              // Bootstrap library version
      "q": "1.4.1"                       // Q library version
    }
  },
  "versioningStrategy": {
    "major": 1,
    "minor": 0,
    "patch": 0,
    "buildIncrement": "patch"
  },
  "lastUpdated": "2024-01-01T00:00:00Z",
  "buildHistory": []                      // Track last 10 builds
}
```

### 2. Update Script (`update-versions.js`)

Universal script that updates version numbers across all project types:

- **C# Projects**: Updates `AssemblyInfo.cs` and `.csproj` files
- **Node.js Projects**: Updates `package.json` files
- **JavaScript Libraries**: Uses existing scripts like Bootstrap's `change-version.js`
- **Composer**: Updates `composer.json` version

#### Usage

```bash
# Manual version updates
node update-versions.js patch          # Increment patch version (1.0.0 → 1.0.1)
node update-versions.js minor          # Increment minor version (1.0.0 → 1.1.0)
node update-versions.js major          # Increment major version (1.0.0 → 2.0.0)

# Options
node update-versions.js patch --dry-run    # Preview changes without applying
node update-versions.js patch --verbose    # Show detailed output
```

### 3. Build Integration (`build-integration.js`)

Automatically detects build environment and applies appropriate versioning strategy:

```bash
node build-integration.js
```

**Auto-detection rules:**
- **Major increment**: Release events, version tags
- **Minor increment**: Pushes to main/master branches
- **Patch increment**: All other pushes and pull requests

**Supported CI/CD platforms:**
- GitHub Actions
- Azure DevOps
- Jenkins
- Generic CI environments

## 🤖 CI/CD Integration

### GitHub Actions

The included workflow (`.github/workflows/version-management.yml`) automatically:

1. **Triggers on:**
   - Pushes to main/master/develop branches
   - Pull requests to main/master
   - Release events
   - Manual workflow dispatch

2. **Updates versions** based on trigger type and branch
3. **Commits changes** back to the repository
4. **Creates release tags** for main branch updates
5. **Validates consistency** across all project files

#### Manual Trigger

You can manually trigger version updates through the GitHub Actions UI:

1. Go to Actions → Version Management
2. Click "Run workflow"
3. Select version strategy (patch/minor/major)
4. Click "Run workflow"

### Other CI/CD Systems

#### Azure DevOps

```yaml
- task: NodeTool@0
  inputs:
    versionSpec: '18'
- script: |
    chmod +x ./build-integration.js
    node ./build-integration.js
  displayName: 'Update Versions'
```

#### Jenkins

```groovy
node {
    stage('Update Versions') {
        sh 'chmod +x ./build-integration.js'
        sh 'node ./build-integration.js'
    }
}
```

## 📋 Project Type Support

### C# Projects

**AssemblyInfo.cs files:**
```csharp
[assembly: AssemblyVersion("1.0.0.0")]      // Updated automatically
[assembly: AssemblyFileVersion("1.0.0.0")]  // Updated automatically
```

**Modern .csproj files:**
```xml
<PropertyGroup>
  <Version>1.0.0</Version>                   <!-- Updated automatically -->
  <PackageVersion>1.0.0</PackageVersion>     <!-- Updated automatically -->
</PropertyGroup>
```

### Node.js Projects

**package.json files:**
```json
{
  "name": "my-project",
  "version": "1.0.0"                         // Updated automatically
}
```

### JavaScript Libraries

**Bootstrap**: Uses existing `grunt/change-version.js` script
**Other libraries**: Can be extended with custom update logic

### PHP Projects

**composer.json:**
```json
{
  "name": "microsoft/sql-server-samples",
  "version": "1.0.0"                         // Added/updated automatically
}
```

## 🛠️ Configuration

### Excluded Directories

The following directories are automatically excluded from version updates:
- `.git`
- `node_modules`
- `bin`, `obj` (C# build outputs)
- `dist`, `build` (Build artifacts)
- `vendor` (Composer dependencies)
- `.vs` (Visual Studio files)

### File Type Support

Currently supported file extensions:
- `.cs` (AssemblyInfo.cs)
- `.csproj` (C# projects)
- `.json` (package.json, composer.json)
- `.js` (JavaScript files with version references)

### Smart Change Detection

The system automatically skips version updates when only documentation files change:
- `*.md` files (README, etc.)
- `LICENSE` files
- `*.txt` documentation
- Files in `docs/` directories

## 🚨 Best Practices

### 1. Version Strategy Guidelines

- **Patch (x.x.X)**: Bug fixes, minor updates, documentation
- **Minor (x.X.0)**: New features, backwards-compatible changes
- **Major (X.0.0)**: Breaking changes, major releases

### 2. Branch Strategy

- **main/master**: Minor increments (feature releases)
- **develop**: Patch increments (development builds)
- **release branches**: Major increments (release candidates)

### 3. CI/CD Setup

1. Ensure your CI system has write access to the repository
2. Configure proper Git credentials for automated commits
3. Set up branch protection rules if needed
4. Test the workflow with manual triggers first

### 4. Rollback Strategy

If you need to rollback a version:

```bash
# Revert to previous version manually
node update-versions.js --dry-run  # Check current state
# Edit version.json manually to previous values
node update-versions.js patch      # Apply the rollback
```

## 🔍 Troubleshooting

### Common Issues

**1. Permission denied errors:**
```bash
chmod +x ./update-versions.js
chmod +x ./build-integration.js
```

**2. Git commit failures:**
- Ensure Git is configured with user name and email
- Check repository write permissions
- Verify branch protection rules

**3. Version mismatch errors:**
- Run validation: `node update-versions.js --dry-run`
- Check for manual edits in project files
- Ensure all project files are included in Git

**4. CI/CD integration issues:**
- Check environment variable detection in `build-integration.js`
- Verify Node.js is available in CI environment
- Ensure proper Git configuration in CI

### Debug Mode

Run with verbose output to see detailed information:

```bash
node update-versions.js patch --verbose --dry-run
```

## 🤝 Contributing

To extend the version management system:

1. **Add new file type support** in `update-versions.js`
2. **Add new CI/CD platform detection** in `build-integration.js`
3. **Update documentation** with new features
4. **Test thoroughly** with `--dry-run` flag

### Adding New Project Types

```javascript
// In update-versions.js, add a new function like:
function updateMyProjectType(newVersion) {
  const files = findFiles(process.cwd(), [/\.myext$/]);
  // Implementation here
}

// Then call it in the main updateVersions function
updateMyProjectType(newVersion);
```

## 📊 Monitoring

The system provides several monitoring capabilities:

- **Build History**: Track in `version.json`
- **GitHub Actions Summary**: Detailed reports in workflow runs
- **Validation**: Automatic consistency checking
- **Verbose Logging**: Detailed output for debugging

## 📄 License

This version management system is part of the SQL Server Samples repository and follows the same MIT license terms.