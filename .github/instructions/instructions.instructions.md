---
applyTo: '**'
---
# Copilot Instructions

## Project Overview
This project is a tool to find deals in marketplaces (currently only eBay API is configured). The basic workflow is:
1. Look for listings for trending products in the marketplace that would be good resell opportunities.
2. Find prices for listings for those products.
3. Analyze the prices to determine if they are below a certain threshold.
4. Present the findings to the user.
The logic of finding deals should be:
1. Find a certain number of specific products (trending?)
2. Find multiple listings for each product
3. Show deal cards for the best (cheapest) listing for that product, along with details of the listings it was compared to.
E.g.:
1. I set Discover Deal filters to 10 results, with $50 target buy price, for 5 unique products, 5 listings per product. No search terms are added for this discovery.
2. I click 'Discover Deals' button on the dashboard.
3. The app looks for 5 unique products that would be good resell opportunities (if a search term had been included in the filter, that would be used in finding products).
4. The app finds the best 5 listings for each product. The loading/finding deals animation on the dashboard should get live feedback on it's search process and what its finding.
5. The app creates 5 deal cards for the best listing for each of the 5 products. Each deal card shows helpful information about the 4 other listings each of the 5 deal listings were compared to.

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
- Never create synthetic, test, or fake data. Always use real live data from marketplace APIs when discovering deals or doing price analysis.
- Price, listing, and deal data should always come from live marketplace queries. Never use the database. The database should be used for storing credentials, settings, preferences, etc...

## Testing
- A `tests/` directory should be used for any test scripts. Stay organized and cleanup after yourself.
- Directory `.gihub/workflows/` contains CI/CD workflows.
