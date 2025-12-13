# Cloudflare Pages Setup Guide for Menlo Project

This guide will walk you through setting up Cloudflare Pages to work with your existing [`cd-frontend.yml`](../../.github/workflows/cd-frontend.yml) GitHub Actions workflow.

## Prerequisites

- A Cloudflare account (free tier is sufficient)
- Access to your custom domain (e.g., `menlo.yourdomain.com`)
- The domain should already be configured in Cloudflare DNS

## Phase 1: Create API Token

### 1.1 Generate Cloudflare API Token

1. Go to [Cloudflare Dashboard > API Tokens](https://dash.cloudflare.com/profile/api-tokens)
2. Click **"Create Token"**
3. Select **"Custom token"** > **"Get started"**
4. Configure the token:
   - **Token name**: `Menlo Pages Deployment`
   - **Permissions**:
     - Account: `Cloudflare Pages:Edit`
     - Zone: `Zone:Read` (for domain verification)
     - User: `UserDetails:Read` (for email retrieval during setup)
   - **Account Resources**: Include: `All accounts` (or select your specific account)
   - **Zone Resources**: Include: `All zones` (or specifically your domain zone)
   - **Client IP Address Filtering**: Leave blank (optional security)
   - **TTL**: Leave default (optional expiry)

5. Click **"Continue to summary"** > **"Create Token"**
6. **IMPORTANT**: Copy the token immediately and store it securely - it won't be shown again

### 1.2 Get Account ID

1. Go to [Cloudflare Dashboard](https://dash.cloudflare.com/)
2. Select any domain (or go to the account overview)
3. In the right sidebar, find **"Account ID"** under the API section
4. Copy the Account ID

## Phase 2: Create Cloudflare Pages Project

### 2.1 Create Project via Wrangler (Recommended)

This creates the project with the exact settings your CD workflow expects:

```powershell
# Install wrangler globally if not already installed
npm install -g wrangler

# Authenticate with your API token
wrangler login

# Navigate to your project's frontend output directory
cd src\ui\web

# Create the Pages project
wrangler pages project create menlo-ui-web

# When prompted:
# Enter the production branch name: main
# Successfully created the 'menlo-ui-web' project. It will be available at ...

cd -
```

### 2.2 Alternative: Create via Dashboard

If you prefer using the web interface:

1. Go to [Cloudflare Dashboard > Workers & Pages](https://dash.cloudflare.com/?to=/:account/workers-and-pages)
2. Click **"Create application"** > **"Pages"** > **"Use direct upload"**
3. **Project name**: `menlo-ui-web`
4. **Drag and drop**: Skip this for now (we'll deploy via GitHub Actions)
5. Click **"Create project"**

## Phase 3: Configure Custom Domain

### 3.1 Add Custom Domain to Pages Project

1. In the Cloudflare dashboard, go to **Workers & Pages**
2. Click on your **"menlo-ui-web"** project
3. Go to **"Custom domains"** tab
4. Click **"Set up a custom domain"**
5. Enter your domain: `menlo.yourdomain.com`
6. Click **"Continue"
7. Cloudflare will verify domain ownership and set up SSL automatically

### 3.2 Verify DNS Configuration

Your DNS should look like this in Cloudflare DNS:

```dns
Type: CNAME
Name: menlo
Target: menlo-ui-web.pages.dev (or similar)
Proxy status: Proxied (orange cloud)
```

**Note**: If you already have an A record for your domain, you may need to:

1. Delete the existing A record
2. Create a CNAME record pointing to your Pages project domain
3. Ensure proxy status is enabled (orange cloud)

## Phase 4: Configure GitHub Repository

### 4.1 Add GitHub Secrets

In your GitHub repository (`DigiBanks99/menlo`):

1. Go to **Settings** > **Secrets and variables** > **Actions**
2. Click **"New repository secret"** for each:

   | Secret Name             | Value                          | Source                     |
   | ----------------------- | ------------------------------ | -------------------------- |
   | `CLOUDFLARE_API_TOKEN`  | Your API token from Phase 1.1  | From token creation        |
   | `CLOUDFLARE_ACCOUNT_ID` | Your account ID from Phase 1.2 | From dashboard API section |

### 4.2 Add GitHub Variables

In the same **"Actions"** section, click **"Variables"** tab:

1. Click **"New repository variable"**:

   | Variable Name                   | Value                          | Purpose                     |
   | ------------------------------- | ------------------------------ | --------------------------- |
   | `CLOUDFLARE_PAGES_PROJECT_NAME` | `menlo-ui-web`                 | Project name for deployment |
   | `FRONTEND_URL`                  | `https://menlo.yourdomain.com` | Production domain           |

### 4.3 Verify Your Workflow Configuration

Your [`cd-frontend.yml`](./.github/workflows/cd-frontend.yml) is already correctly configured! The key parts are:

```yaml
env:
  CLOUDFLARE_API_TOKEN: ${{ secrets.CLOUDFLARE_API_TOKEN }}
  CLOUDFLARE_ACCOUNT_ID: ${{ secrets.CLOUDFLARE_ACCOUNT_ID }}
  CLOUDFLARE_PAGES_PROJECT_NAME: ${{ vars.CLOUDFLARE_PAGES_PROJECT_NAME || 'menlo-ui' }}

# Deploy step uses:
- name: Deploy to Cloudflare Pages
  uses: cloudflare/wrangler-action@v3
  with:
    apiToken: ${{ env.CLOUDFLARE_API_TOKEN }}
    accountId: ${{ env.CLOUDFLARE_ACCOUNT_ID }}
    command: pages deploy src/ui/web/dist/menlo-app --project-name=${{ env.CLOUDFLARE_PAGES_PROJECT_NAME }} --branch=${{ github.ref_name }}
```

## Phase 5: Test Deployment

### 5.1 Trigger First Deployment

1. Push any change to the `main` branch (or manually trigger the workflow)
2. Go to **Actions** tab in your GitHub repository
3. Watch the **"CD - Deploy Frontend to Cloudflare Pages"** workflow run
4. Check for any errors in the deployment logs

### 5.2 Manual Testing (Optional)

You can test deployment locally first:

```powershell
# Build your project
cd src/ui/web
pnpm nx build menlo-app

# Deploy manually using wrangler
$env:CLOUDFLARE_ACCOUNT_ID="your_account_id"
npx wrangler pages deploy dist/menlo-app --project-name=menlo-ui-web --branch=main
```

### 5.3 Verify Deployment

1. **Production URL**: `https://menlo.yourdomain.com`
2. **Pages Dashboard**: Check deployment status in Cloudflare Pages dashboard
3. **Version Check**: `https://menlo.yourdomain.com/version.json` should show build metadata
4. **SPA Routing**: Navigate to any route (e.g., `/budgets`) and refresh - should work

## Phase 6: Configure Branch Deployments (Optional)

For preview deployments on pull requests:

1. In Cloudflare Pages dashboard > **menlo-ui-web** project
2. Go to **"Settings"** > **"Builds & deployments"**
3. **Build configuration**:
   - **Build command**: `pnpm nx build menlo-app`
   - **Build output directory**: `dist/menlo-app`
   - **Root directory**: `src/ui/web`

**Note**: Since you're using Direct Upload, branch deployments are handled by your GitHub Actions workflow automatically.

## Phase 7: Security & Production Settings

### 7.1 Configure Headers (Optional)

Create `src/ui/web/projects/menlo-app/public/_headers` file:

```text
/*
  X-Frame-Options: DENY
  X-Content-Type-Options: nosniff
  X-XSS-Protection: 1; mode=block
  Referrer-Policy: strict-origin-when-cross-origin
```

### 7.2 Configure Redirects

Your `_redirects` file is already configured correctly:

```text
/* /index.html 200
```

This ensures all routes fallback to your Angular app for client-side routing.

## Troubleshooting

### Common Issues

#### 1. "Project not found" error

- Verify `CLOUDFLARE_PAGES_PROJECT_NAME` variable matches exactly: `menlo-ui-web`
- Check the project exists in your Cloudflare Pages dashboard

#### 2. "Permission denied" error

- Regenerate API token with correct permissions: `Account > Cloudflare Pages:Edit`
- Ensure token is active: `curl "https://api.cloudflare.com/client/v4/user/tokens/verify" -H "Authorization: Bearer YOUR_TOKEN"`

#### 3. Custom domain not working

- Check DNS propagation: `nslookup menlo.yourdomain.com`
- Verify CNAME points to your `.pages.dev` domain
- Ensure proxy status is enabled (orange cloud)

#### 4. SPA routes return 404

- Verify `_redirects` file is in the build output
- Check Cloudflare Pages dashboard > project > Functions for redirect rules

### Verification Commands

```powershell
# Test API token
curl "https://api.cloudflare.com/client/v4/user/tokens/verify" -H "Authorization: Bearer YOUR_TOKEN"

# List Pages projects
npx wrangler pages project list

# Check deployment status
curl https://menlo.yourdomain.com/version.json

# Test SPA routing
curl -I https://menlo.yourdomain.com/budgets
# Should return 200, not 404
```

## Success Checklist

- [ ] API token created with correct permissions
- [ ] Account ID obtained
- [ ] Pages project `menlo-ui-web` created
- [ ] Custom domain configured (e.g., `menlo.yourdomain.com`)
- [ ] GitHub secrets `CLOUDFLARE_API_TOKEN` and `CLOUDFLARE_ACCOUNT_ID` set
- [ ] GitHub variable `CLOUDFLARE_PAGES_PROJECT_NAME` set to `menlo-ui-web`
- [ ] GitHub variable `FRONTEND_URL` set to your production domain
- [ ] First deployment successful via GitHub Actions
- [ ] Production site accessible at your custom domain
- [ ] Version metadata accessible at `yourdomain.com/version.json`
- [ ] SPA routing works (refresh on `/budgets` doesn't 404)
- [ ] Pull request preview deployments working

## Next Steps

Once deployment is working:

1. **Monitor Performance**: Check Cloudflare Analytics for page load times
2. **Configure Caching**: Cloudflare automatically caches static assets
3. **Set up Monitoring**: Consider adding uptime monitoring
4. **Security Headers**: Add additional security headers as needed
5. **API Integration**: Ensure your frontend can reach the home server API via Cloudflare Tunnel

---

**Note**: This guide assumes your Angular build process and GitHub Actions workflow are
already correctly configured, which they are based on your current setup. The main missing
piece was the Cloudflare Pages project creation and GitHub secrets configuration.
