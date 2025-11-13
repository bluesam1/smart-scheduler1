# API Client Setup

This directory contains the API client configuration and utilities for connecting the frontend to the backend API.

## Structure

```
lib/api/
├── generated/
│   └── api-client.ts          # Generated TypeScript client (from NSwag)
├── api-client-config.ts        # Factory function for creating API clients with auth
├── health-check.ts             # Health check utilities
├── error-handling.ts           # Error handling utilities
└── README.md                   # This file
```

## Environment Variables

Create a `.env.local` file in the `frontend/` directory with the following:

```env
# Backend API base URL
NEXT_PUBLIC_API_URL=http://localhost:5004

# Google Places API Key for address autocomplete
# Get your API key from: https://console.cloud.google.com/google/maps-apis/credentials
# Make sure to enable "Places API" and "Maps JavaScript API" in your Google Cloud project
NEXT_PUBLIC_GOOGLE_PLACES_API_KEY=your_google_places_api_key_here

# Cognito Configuration (if using authentication)
# NEXT_PUBLIC_COGNITO_USER_POOL_ID=us-east-2_XXXXXXXXX
# NEXT_PUBLIC_COGNITO_APP_CLIENT_ID=your_app_client_id_here
# NEXT_PUBLIC_COGNITO_REGION=us-east-2
```

**Note:** The `NEXT_PUBLIC_` prefix is required for Next.js to expose the variable to the browser.

**Important:** The `.env.local` file is gitignored and should never be committed to version control.

## API Client Generation

The TypeScript API client is generated from the backend's OpenAPI specification using NSwag.

### Prerequisites

- NSwag CLI tool installed: `dotnet tool install -g NSwag.ConsoleCore`
- Backend API running or built

### Generation Workflow

1. **Build the backend API** to ensure OpenAPI spec is up to date:
   ```bash
   dotnet build src/SmartScheduler.Api/SmartScheduler.Api.csproj
   ```

2. **Generate the TypeScript client**:
   ```bash
   nswag run nswag.json
   ```

   This will:
   - Generate the OpenAPI spec from the backend
   - Generate TypeScript client classes and interfaces
   - Output to `frontend/lib/api/generated/api-client.ts`

3. **Update the factory function** in `api-client-config.ts` to use the generated clients.

### NSwag Configuration

The NSwag configuration is in `nswag.json` at the project root. Key settings:

- **Interface-based models**: `typeStyle: "Interface"` - generates TypeScript interfaces instead of classes
- **Fetch API**: Uses native Fetch API for HTTP requests
- **Authentication**: Supports Bearer token authentication
- **Output**: `frontend/lib/api/generated/api-client.ts`

## Usage

### Creating API Clients

```typescript
import { createApiClients } from '@/lib/api/api-client-config';
import { useAuth } from '@/lib/auth/auth-context';

// In a React component, get token provider from auth context
function MyComponent() {
  const { getTokenProvider } = useAuth();
  
  // Create API clients with token provider (enables automatic token refresh)
  const tokenProvider = getTokenProvider();
  const apiClients = createApiClients(tokenProvider);
  
  // Use the clients - tokens will be automatically refreshed on 401 responses
  const contractors = await apiClients.client.getRoot();
}
```

**Note:** The API client now accepts a `TokenProvider` instead of a static token. This enables:
- Automatic token refresh on 401 responses
- Dynamic token access (always uses current token)
- Seamless token refresh without user intervention

### Health Check

```typescript
import { checkApiHealth, verifyApiAvailability } from '@/lib/api/health-check';

// Check API health
const health = await checkApiHealth();
if (health.isHealthy) {
  console.log('API is healthy');
}

// Verify availability on app load
const isAvailable = await verifyApiAvailability();
```

### Error Handling

```typescript
import { 
  handleApiError, 
  formatErrorForDisplay,
  isAuthenticationError 
} from '@/lib/api/error-handling';

try {
  const response = await fetch('/api/endpoint');
  await handleApiError(response); // Throws ApiException if not ok
  const data = await response.json();
} catch (error) {
  if (isAuthenticationError(error)) {
    // Redirect to login
    redirect('/login');
  } else {
    // Display user-friendly error
    toast.error(formatErrorForDisplay(error));
  }
}
```

## Integration with Authentication

Once Story 0.7 (Cognito Login Frontend Integration) is complete, the API client factory will automatically include JWT tokens from the authentication context.

## CORS Configuration

The backend CORS is already configured (Story 0.1) to allow requests from:
- `http://localhost:3000` (Next.js development server)

CORS is configured with credentials support for authenticated requests.

## Next Steps

1. **Generate API client** once backend endpoints are implemented (Epic 1+)
2. **Update factory function** in `api-client-config.ts` to use generated clients
3. **Integrate with authentication** (Story 0.7) to automatically include JWT tokens
4. **Replace mock data** in UI components with real API calls

