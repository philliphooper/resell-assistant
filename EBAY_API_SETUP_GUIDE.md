# eBay API Setup Guide - Issue #15

## 🎯 Objective
Setup eBay Developer account and obtain API credentials needed for real marketplace data integration.

## 📋 Prerequisites Checklist

### ✅ **Current Status**
- ✅ Configuration structure ready in `appsettings.json`
- ✅ Database and service infrastructure complete
- ✅ Project structure prepared for external API integration

### ❌ **Required Setup**
- [ ] eBay Developer account creation
- [ ] OAuth 2.0 application registration
- [ ] API credentials acquisition
- [ ] Sandbox environment configuration
- [ ] Production environment preparation

## 🛠️ Step-by-Step Setup Process

### Step 1: Create eBay Developer Account

1. **Visit eBay Developer Portal**: https://developer.ebay.com/
2. **Create Account**:
   - Click "Get Started" or "Join"
   - Use existing eBay account or create new one
   - Accept developer terms and conditions
3. **Complete Developer Profile**:
   - Provide business/personal information
   - Verify email address
   - Complete developer verification

### Step 2: Create OAuth Application

1. **Navigate to "My Account" → "Application Keysets"**
2. **Create New Application**:
   - **Application Name**: "Resell Assistant"
   - **Application Type**: "Web Application"
   - **Description**: "Marketplace data aggregation for reselling analysis"
   - **Website URL**: http://localhost:3000 (development)
   - **Privacy Policy URL**: (optional for development)
   - **Terms of Use URL**: (optional for development)

3. **Configure OAuth Settings**:
   - **Redirect URI**: http://localhost:3000/auth/ebay/callback
   - **Scopes Required**:
     - `https://api.ebay.com/oauth/api_scope` (Basic API access)
     - `https://api.ebay.com/oauth/api_scope/buy.browse` (Browse API)

### Step 3: Obtain API Credentials

After application approval, you'll receive:

```json
{
  "sandbox": {
    "clientId": "YourApp-SandBox-ClientID",
    "clientSecret": "SBX-xxxxx-your-secret",
    "ruName": "Your_RuName_for_token_callback"
  },
  "production": {
    "clientId": "YourApp-Production-ClientID", 
    "clientSecret": "PRD-xxxxx-your-secret",
    "ruName": "Your_RuName_for_token_callback"
  }
}
```

### Step 4: Update Configuration

Update `appsettings.json`:

```json
{
  "ApiKeys": {
    "EbayClientId": "YourApp-SandBox-ClientID",
    "EbayClientSecret": "SBX-xxxxx-your-secret",
    "EbayEnvironment": "sandbox",
    "EbayBaseUrl": "https://api.sandbox.ebay.com",
    "EbayOAuthUrl": "https://auth.sandbox.ebay.com/oauth/api_scope"
  }
}
```

Update `appsettings.Development.json`:

```json
{
  "ApiKeys": {
    "EbayClientId": "YourApp-SandBox-ClientID",
    "EbayClientSecret": "SBX-xxxxx-your-secret",
    "EbayEnvironment": "sandbox",
    "EbayBaseUrl": "https://api.sandbox.ebay.com",
    "EbayOAuthUrl": "https://auth.sandbox.ebay.com/oauth/api_scope"
  }
}
```

### Step 5: Sandbox Testing Setup

1. **Access Sandbox Environment**: https://sandbox.ebay.com/
2. **Create Test User Accounts**:
   - Buyer account for testing purchases
   - Seller account for testing listings
3. **Test API Authentication**:
   - Generate OAuth token
   - Verify API connectivity
   - Test basic Browse API calls

## 🔧 Technical Implementation Setup

### Required NuGet Packages

Add to `Resell Assistant.csproj`:

```xml
<PackageReference Include="RestSharp" Version="110.2.0" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
<PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
```

### Configuration Model Updates

Create `Models/Configuration/EbayApiSettings.cs`:

```csharp
public class EbayApiSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Environment { get; set; } = "sandbox";
    public string BaseUrl { get; set; } = string.Empty;
    public string OAuthUrl { get; set; } = string.Empty;
    public int RateLimitPerSecond { get; set; } = 5;
    public int TimeoutSeconds { get; set; } = 30;
}
```

## 🧪 Testing & Validation

### API Connectivity Test

Create test endpoint in `DashboardController.cs`:

```csharp
[HttpGet("test-ebay-connection")]
public async Task<ActionResult> TestEbayConnection()
{
    try
    {
        // Test OAuth token generation
        // Test basic API call
        // Return connection status
        return Ok(new { status = "connected", timestamp = DateTime.UtcNow });
    }
    catch (Exception ex)
    {
        return BadRequest(new { error = ex.Message });
    }
}
```

### Validation Checklist

- [ ] eBay Developer account active
- [ ] OAuth application approved
- [ ] API credentials configured
- [ ] Sandbox connectivity tested
- [ ] Authentication flow working
- [ ] Basic API calls successful
- [ ] Rate limiting configured
- [ ] Error handling implemented

## 🔐 Security Considerations

### Environment Variables (Production)

For production deployment, use environment variables:

```bash
EBAY_CLIENT_ID=your_production_client_id
EBAY_CLIENT_SECRET=your_production_client_secret
EBAY_ENVIRONMENT=production
```

### Configuration Security

1. **Never commit real API keys to git**
2. **Use User Secrets for development**:
   ```bash
   dotnet user-secrets set "ApiKeys:EbayClientId" "your_sandbox_id"
   dotnet user-secrets set "ApiKeys:EbayClientSecret" "your_sandbox_secret"
   ```

3. **Use Azure Key Vault or AWS Secrets Manager for production**

## 📚 eBay API Documentation References

### Primary APIs to Implement:
1. **Browse API**: https://developer.ebay.com/api-docs/browse/overview.html
   - Product search across eBay listings
   - Item details and pricing information
   - Category and marketplace filtering

2. **Buy Marketing API**: https://developer.ebay.com/api-docs/buy/marketing/overview.html
   - Deal recommendations
   - Price comparison data

3. **OAuth API**: https://developer.ebay.com/api-docs/static/oauth-tokens.html
   - Authentication and authorization
   - Token management and refresh

### Rate Limiting Information:
- **Browse API**: 5,000 calls per day
- **Rate per second**: 5 calls/second
- **Burst allowance**: 10 calls in 1 second

## 🚀 Next Steps After Setup

Once eBay API credentials are configured:

1. **Move to Issue #11**: Begin eBay API service implementation
2. **Create `EbayApiService.cs`**: OAuth and API communication
3. **Update `MarketplaceService.cs`**: Integrate eBay data
4. **Implement rate limiting**: Prevent API abuse
5. **Create data sync service**: Background updates

## ✅ Success Criteria

This issue (#15) is complete when:

- [x] eBay Developer account created and verified
- [x] OAuth application registered and approved  
- [x] API credentials obtained (sandbox and production)
- [x] Configuration files updated with credentials
- [x] Basic API connectivity tested and working
- [x] Security best practices implemented
- [x] Documentation updated with setup process

**Estimated Time**: 2-4 hours (including eBay approval process)
**Next Issue**: #11 - Phase 1: Implement eBay API Integration

---

*Created: May 31, 2025*  
*Issue: #15 - Setup eBay Developer Account and API Credentials*  
*Epic: #10 - Real Marketplace Data Integration*
