# CI/CD Configuration Documentation

This document describes the updated CI/CD pipeline configuration for the Resell Assistant application.

## Overview

The CI/CD pipeline has been completely redesigned to align with the current project structure and best practices. The new configuration includes multiple workflows optimized for different scenarios.

## Workflows

### 1. Build and Test (`ci.yml`)
**Triggers:** Push to `main`/`develop`, Pull Requests

**Key Features:**
- Solution-based build approach using `Resell Assistant.sln`
- Environment variables for maintainable configuration
- Proper test result collection and artifact upload
- Conditional React test execution (only runs if tests exist)
- Streamlined artifact management

**Improvements over old CI:**
- Uses solution file instead of individual project builds
- Better test result collection with proper paths
- More efficient dependency restoration
- Checks for React tests before attempting to run them

### 2. Advanced Build and Test (`build-and-test-advanced.yml`)
**Triggers:** Push to `main`/`develop`, Pull Requests, Scheduled (nightly)

**Key Features:**
- **Code Analysis:** Security vulnerability scanning, ESLint for frontend
- **Multi-Platform Matrix:** Tests across Ubuntu, Windows, macOS with multiple Node.js versions
- **Integration Tests:** Database setup and integration test execution
- **Performance Tests:** Apache Bench load testing (scheduled runs only)
- **Docker Build:** Containerization testing if Dockerfile exists

### 3. Pull Request Checks (`pr-checks.yml`)
**Triggers:** Pull Requests only

**Key Features:**
- Fast validation (15-minute timeout)
- Code formatting verification with `dotnet format`
- Dependency vulnerability scanning
- Quick build verification
- Automated PR comments with results
- Security-focused dependency auditing

### 4. Release Build (`release.yml`)
**Triggers:** Push to `main`, Version tags

**Key Features:**
- Versioned releases with proper semantic versioning
- Production-optimized builds
- Release artifact creation (tar.gz, zip)
- Docker image building and publishing to GitHub Container Registry
- Automated GitHub releases for tagged versions
- ReadyToRun compilation for better performance

## Project Structure Compatibility

The updated CI configuration is designed around the current project structure:

```
Resell Assistant.sln                 # Solution file (now used for builds)
├── Resell Assistant/               # Main project
│   ├── Resell Assistant.csproj     # .NET 9.0 project
│   └── ClientApp/                  # React frontend
│       ├── package.json            # Node.js dependencies
│       └── src/                    # React source
└── Resell Assistant.Tests/         # Test project
    └── Resell Assistant.Tests.csproj # xUnit tests
```

## Technology Stack Support

- **.NET 9.0:** Full support with proper version targeting
- **React/TypeScript:** Frontend build and testing integration
- **Node.js 18/20:** Multi-version compatibility testing
- **SQLite:** Database integration testing
- **Docker:** Containerization support (optional)
- **xUnit/Moq:** .NET testing framework support

## Environment Variables

All workflows use consistent environment variables:

```yaml
env:
  DOTNET_VERSION: '9.0.x'
  NODE_VERSION: '18'
  SOLUTION_PATH: 'Resell Assistant.sln'
  MAIN_PROJECT_PATH: 'Resell Assistant/Resell Assistant.csproj'
  TEST_PROJECT_PATH: 'Resell Assistant.Tests/Resell Assistant.Tests.csproj'
  CLIENT_APP_PATH: 'Resell Assistant/ClientApp'
```

## Security Features

- **Dependency Scanning:** Both .NET and npm packages
- **Vulnerability Assessment:** Automated security checks
- **Container Registry Integration:** Secure Docker image publishing
- **Token Management:** Proper GitHub token usage with minimal permissions

## Artifacts and Outputs

Each workflow produces relevant artifacts:

- **Test Results:** TRX files and coverage reports
- **Build Artifacts:** Compiled applications
- **Security Reports:** Vulnerability scan results
- **Performance Reports:** Load testing results
- **Release Packages:** Versioned distribution files

## Usage Instructions

### For Developers

1. **Pull Requests:** Automatic validation with `pr-checks.yml`
2. **Feature Development:** Full testing with `ci.yml`
3. **Code Quality:** Regular checks with `build-and-test-advanced.yml`

### For Releases

1. **Development Builds:** Automatic on push to `develop`
2. **Production Releases:** Tag with version (e.g., `v1.0.0`)
3. **Docker Images:** Automatic publishing to GitHub Container Registry

### Local Development

The CI configuration mirrors local development commands:

```bash
# Restore dependencies
dotnet restore "Resell Assistant.sln"

# Build frontend
cd "Resell Assistant/ClientApp"
npm ci
npm run build

# Build solution
dotnet build "Resell Assistant.sln" --configuration Release

# Run tests
dotnet test "Resell Assistant.Tests/Resell Assistant.Tests.csproj"
```

## Migration from Old CI

**Key Changes:**
1. **File renamed:** `ci.yml` updated with new logic
2. **Solution-based builds:** Now uses `.sln` file
3. **Environment variables:** Centralized configuration
4. **Conditional React tests:** Only runs if tests exist
5. **Better artifact paths:** Proper test result collection
6. **Additional workflows:** More comprehensive testing scenarios

**Breaking Changes:**
- Test result paths have changed
- Artifact names are different
- Build commands now use solution file

## Troubleshooting

### Common Issues

1. **Missing Test Results:** Check that test projects are properly configured
2. **React Build Failures:** Ensure `package-lock.json` exists and is committed
3. **Docker Build Issues:** Verify `Dockerfile` exists in repository root
4. **Permission Errors:** Ensure GitHub Actions has proper repository permissions

### Debug Steps

1. Check workflow runs in GitHub Actions tab
2. Review artifact uploads for detailed logs
3. Compare environment variables with actual project structure
4. Verify Node.js and .NET versions match project requirements

## Future Enhancements

Potential improvements to consider:

1. **Parallel Test Execution:** Split tests across multiple runners
2. **Cache Optimization:** More aggressive caching of dependencies
3. **Quality Gates:** SonarQube or similar code quality integration
4. **Deployment Automation:** Automatic deployment to staging/production
5. **Notification Integration:** Slack/Teams notifications for build status
