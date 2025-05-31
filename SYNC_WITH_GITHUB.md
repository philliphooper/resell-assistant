# Sync Local Project with GitHub Repository

## Method 1: Using VS Code Source Control Panel (Recommended)

### Step 1: Initialize Git Repository
1. Open VS Code Source Control panel (Ctrl+Shift+G)
2. Click "Initialize Repository" button
3. This creates a local Git repository

### Step 2: Add Remote Repository
1. Open VS Code Terminal (Ctrl+`)
2. Run these commands:

```bash
# Add the GitHub repository as remote origin
git remote add origin https://github.com/philliphooper/resell-assistant.git

# Verify the remote was added
git remote -v
```

### Step 3: Stage and Commit Changes
1. In Source Control panel, you'll see all your files under "Changes"
2. Click the "+" button next to "Changes" to stage all files
3. Enter a commit message like "Initial local project setup"
4. Click the ✓ (checkmark) to commit

### Step 4: Pull Latest from GitHub
```bash
# Pull any changes from GitHub first
git pull origin main --allow-unrelated-histories
```

### Step 5: Push to GitHub
1. In Source Control panel, click the "..." menu
2. Select "Push" 
3. Or use terminal: `git push -u origin main`

## Method 2: Using Terminal Commands

```bash
# Initialize git repository
git init

# Add remote repository
git remote add origin https://github.com/philliphooper/resell-assistant.git

# Add all files
git add .

# Commit changes
git commit -m "Initial local project setup"

# Pull and merge with GitHub repo
git pull origin main --allow-unrelated-histories

# Push to GitHub
git push -u origin main
```

## Method 3: Clone and Copy (Alternative)

If you encounter issues:

1. **Backup your current project**:
   ```bash
   # Create backup
   cp -r "c:/Users/phillip/Documents/Resell Assistant" "c:/Users/phillip/Documents/Resell Assistant_backup"
   ```

2. **Clone the repository**:
   ```bash
   cd "c:/Users/phillip/Documents"
   git clone https://github.com/philliphooper/resell-assistant.git
   ```

3. **Copy your local files over**:
   - Copy all your local project files into the cloned repository
   - This ensures you have the Git history and remote connection

## Troubleshooting

### If you get "fatal: refusing to merge unrelated histories":
```bash
git pull origin main --allow-unrelated-histories
```

### If you get authentication errors:
1. Use GitHub Personal Access Token instead of password
2. Or set up SSH keys for GitHub

### To check current status:
```bash
git status
git remote -v
```

## VS Code Source Control Panel Features

Once synced, you can:
- **See file changes** in real-time
- **Stage/unstage files** with click
- **Commit with messages**
- **Push/pull changes**
- **View commit history**
- **Create branches**
- **Resolve merge conflicts** visually

## Next Steps After Sync

1. Your local changes will be merged with the GitHub repository
2. You'll have full version control
3. You can push/pull changes easily
4. Collaborate with others
5. Track project history

The Source Control panel in VS Code makes Git operations visual and easy to use!
