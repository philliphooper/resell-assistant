# Copilot Instructions

## Project Overview
This project is a tool to find deals in marketplaces (currently only eBay API is configured). The basic workflow is:
1. Look for listings for trending products in the marketplace that would be good resell opportunities.
2. Find prices for listings for those products.
3. Analyze the prices to determine if they are below a certain threshold.
4. Present the findings to the user.

## Code Structure
The frontend is built with React and the backend is a .NET Web API. To start each:
- Backend
1. Navigate to `Resell Assistant` and run the backend with `dotnet run`.
- Frontend
1. Navigate to `Resell Assistant/ClientApp` and run the frontend with `npm start`.

## Preferred Practices
- When running functions like `taskkill`, use double \\ to escape backslashes in paths.
- When querying, use https port 5001, not http port 5000.
- Use gh cli for GitHub operations, e.g., `gh pr create`, `gh issue create`.
- Avoid creating new .md files; instead, use the existing `README.md` and `SETUP.md` files, and write to GitHub (e.g. issues) for instructions and documentation.
- When starting a server (frontend or backend), ensure it is not already running to avoid port conflicts.
- When setting labels for GitHub issues, first verify the labels exist in the repository. If not, create them using the GitHub CLI.

## Testing
- A `tests/` directory should be used for any test scripts. Stay organized and cleanup after yourself.
- Directory `.gihub/workflows/` contains CI/CD workflows.
