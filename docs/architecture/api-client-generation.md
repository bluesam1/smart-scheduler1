# API Client Generation

### NSwag Configuration

**Purpose:** Generate a type-safe TypeScript API client for the Next.js frontend from the backend's OpenAPI specification.

**Approach:** Interface-based models instead of classes

**Rationale:**
- **Type Safety:** TypeScript interfaces provide compile-time type checking without runtime overhead
- **Simplicity:** Interfaces are lighter weight than classes - no constructor overhead, prototype chains, or class semantics
- **JSON Compatibility:** Plain objects from API responses can be directly typed as interfaces without instantiation
- **Tree Shaking:** Interfaces are erased at compile time, resulting in smaller bundle sizes
- **Immutability Support:** Works naturally with React's immutable state patterns and functional programming
- **Better DX:** Interfaces compose better with utility types (Partial, Pick, Omit, etc.)

**NSwag Configuration (`nswag.json`):**

```json
{
  "runtime": "Net80",
  "defaultVariables": null,
  "documentGenerator": {
    "aspNetCoreToOpenApi": {
      "project": "src/Api/Api.csproj",
      "msBuildProjectExtensionsPath": null,
      "configuration": null,
      "runtime": null,
      "targetFramework": null,
      "noBuild": true,
      "verbose": true,
      "workingDirectory": null,
      "requireParametersWithoutDefault": true,
      "apiGroupNames": null,
      "defaultPropertyNameHandling": "Default",
      "defaultReferenceTypeNullHandling": "NotNull",
      "defaultDictionaryValueReferenceTypeNullHandling": "NotNull",
      "defaultResponseReferenceTypeNullHandling": "NotNull",
      "defaultEnumHandling": "Integer",
      "flattenInheritanceHierarchy": false,
      "generateKnownTypes": true,
      "generateEnumMappingDescription": false,
      "generateXmlObjects": false,
      "generateAbstractProperties": false,
      "generateAbstractSchemas": true,
      "ignoreObsoleteProperties": false,
      "allowReferencesWithProperties": false,
      "excludedTypeNames": [],
      "serviceHost": null,
      "serviceBasePath": null,
      "serviceSchemеs": [],
      "infoTitle": "SmartScheduler API",
      "infoDescription": null,
      "infoVersion": "1.0.0",
      "documentTemplate": null,
      "documentProcessorTypes": [],
      "operationProcessorTypes": [],
      "typeNameGeneratorType": null,
      "schemaNameGeneratorType": null,
      "contractResolverType": null,
      "serializerSettingsType": null,
      "useDocumentProvider": true,
      "documentName": "v1",
      "aspNetCoreEnvironment": null,
      "createWebHostBuilderMethod": null,
      "startupType": null,
      "allowNullableBodyParameters": true,
      "output": "openapi.json",
      "outputType": "OpenApi3",
      "assemblyPaths": [],
      "assemblyConfig": null,
      "referencePaths": [],
      "useNuGetCache": false
    }
  },
  "codeGenerators": {
    "openApiToTypeScriptClient": {
      "className": "{controller}Client",
      "moduleName": "",
      "namespace": "",
      "typeScriptVersion": 5.0,
      "template": "Fetch",
      "promiseType": "Promise",
      "httpClass": "HttpClient",
      "withCredentials": false,
      "useSingletonProvider": false,
      "injectionTokenType": "OpaqueToken",
      "rxJsVersion": 7.0,
      "dateTimeType": "string",
      "nullValue": "Undefined",
      "generateClientClasses": true,
      "generateClientInterfaces": false,
      "generateOptionalParameters": true,
      "exportTypes": true,
      "wrapDtoExceptions": false,
      "exceptionClass": "ApiException",
      "clientBaseClass": null,
      "wrapResponses": false,
      "wrapResponseMethods": [],
      "generateResponseClasses": true,
      "responseClass": "SwaggerResponse",
      "protectedMethods": [],
      "configurationClass": null,
      "useTransformOptionsMethod": false,
      "useTransformResultMethod": false,
      "generateDtoTypes": true,
      "operationGenerationMode": "MultipleClientsFromOperationId",
      "markOptionalProperties": true,
      "generateCloneMethod": false,
      "typeStyle": "Interface",
      "enumStyle": "Enum",
      "useLeafType": false,
      "classTypes": [],
      "extendedClasses": [],
      "extensionCode": null,
      "generateDefaultValues": true,
      "excludedTypeNames": [],
      "excludedParameterNames": [],
      "handleReferences": false,
      "generateConstructorInterface": true,
      "convertConstructorInterfaceData": false,
      "importRequiredTypes": true,
      "useGetBaseUrlMethod": false,
      "baseUrlTokenName": "API_BASE_URL",
      "queryNullValue": "",
      "useAbortSignal": false,
      "inlineNamedDictionaries": false,
      "inlineNamedAny": false,
      "inlineNamedTuples": true,
      "templateDirectory": null,
      "typeNameGeneratorType": null,
      "propertyNameGeneratorType": null,
      "enumNameGeneratorType": null,
      "serviceHost": null,
      "serviceSchemes": null,
      "output": "ui/web/lib/api/generated/api-client.ts",
      "newLineBehavior": "Auto"
    }
  }
}
```

**Key Configuration Settings:**

- **`typeStyle: "Interface"`**: Generate TypeScript interfaces instead of classes for all DTOs
- **`generateCloneMethod: false`**: No class methods needed for interfaces
- **`template: "Fetch"`**: Use native Fetch API for HTTP requests
- **`dateTimeType: "string"`**: Keep dates as ISO8601 strings (parse with date libraries as needed)
- **`markOptionalProperties: true`**: Generate optional properties with `?` syntax
- **`generateDefaultValues: true`**: Include default values in interfaces where applicable
- **`output`**: Generate client to `ui/web/lib/api/generated/api-client.ts`

**Usage in Frontend:**

```typescript
// ui/web/lib/api/api-client-config.ts
import { ContractorsClient, JobsClient, RecommendationsClient } from './generated/api-client';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5004';

// Factory function to create API clients with auth token
export function createApiClients(accessToken: string) {
  const headers = {
    Authorization: `Bearer ${accessToken}`,
  };

  return {
    contractors: new ContractorsClient(API_BASE_URL, {
      headers,
    }),
    jobs: new JobsClient(API_BASE_URL, {
      headers,
    }),
    recommendations: new RecommendationsClient(API_BASE_URL, {
      headers,
    }),
  };
}

// Usage in React components
async function fetchContractors(accessToken: string) {
  const { contractors } = createApiClients(accessToken);
  const contractorList = await contractors.get(undefined, 50); // Returns Contractor[]
  return contractorList;
}
```

**Benefits of This Approach:**

1. **Full Type Safety:** TypeScript interfaces match backend DTOs exactly, catching type mismatches at compile time
2. **Automatic Updates:** Regenerate client when backend API changes - compile errors guide frontend updates
3. **Zero Runtime Overhead:** Interfaces are compile-time only, no class instantiation or prototype chains
4. **Immutable by Default:** Plain objects work naturally with React state and functional programming patterns
5. **Smaller Bundle Size:** No class code in production bundle, just type annotations (erased at compile time)
6. **Better Tree Shaking:** Dead code elimination works better with plain objects
7. **Composition-Friendly:** Interfaces compose naturally with utility types (Partial<T>, Pick<T, K>, etc.)

**Generation Workflow:**

1. **Backend Development:** Update Minimal APIs and add OpenAPI annotations
2. **Generate OpenAPI Spec:** Run `dotnet build` (NSwag generates openapi.json from ASP.NET Core)
3. **Generate TypeScript Client:** Run `nswag run nswag.json` to generate TypeScript client
4. **Frontend Development:** Import interfaces and client classes from generated file
5. **CI/CD Integration:** Automate generation in build pipeline to ensure client stays in sync

**File Organization:**

```
ui/web/
├── lib/
│   └── api/
│       ├── generated/
│       │   └── api-client.ts         # Generated by NSwag (DO NOT EDIT)
│       ├── api-client-config.ts      # Client factory with auth
│       └── hooks/                     # React hooks for API calls
│           ├── useContractors.ts
│           ├── useJobs.ts
│           └── useRecommendations.ts
```

**Example Generated Interface (Reference):**

```typescript
// This is what NSwag generates with typeStyle: "Interface"
export interface Contractor {
  id: string;
  name: string;
  baseLocation: GeoLocation;
  rating: number;
  workingHours: WorkingHours[];
  skills: string[];
  calendar?: ContractorCalendar | undefined;
  createdAt: string;
  updatedAt: string;
}

// Client class for API calls (generated)
export class ContractorsClient {
  constructor(baseUrl?: string, http?: { fetch(url: RequestInfo, init?: RequestInit): Promise<Response> });
  
  get(skills?: string[] | undefined, limit?: number | undefined): Promise<Contractor[]>;
  post(body: CreateContractorRequest): Promise<Contractor>;
  getById(id: string): Promise<Contractor>;
  put(id: string, body: UpdateContractorRequest): Promise<Contractor>;
}
```
