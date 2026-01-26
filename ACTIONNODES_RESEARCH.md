# ActionNodes: Research and Design Paper

## Executive Summary

This document presents a comprehensive analysis of SDK-based action node systems for workflow automation, drawing insights from a reference implementation and the n8n platform. The research explores architectural patterns, variable resolution systems, provider models, and UI generation strategies to inform the design of an ActionNodes system for DonkeyWork-Agents.

**Key Findings:**

1. **Attribute-Driven Architecture**: The reference implementation uses C# attributes extensively for metadata-driven UI generation, eliminating the need for separate schema files
2. **Dual Expression Systems**: Combining Scriban (templating) for string interpolation and Jint (JavaScript) for complex conditional logic provides maximum flexibility
3. **Resolvable<T> Pattern**: A generic wrapper type enables parameters to accept either literal values or expressions, resolved at runtime
4. **Provider-Based Extensibility**: SDK providers are simple classes with decorated methods, automatically discovered and registered via reflection
5. **Type-Safe Parameter System**: Strong typing with validation attributes ensures parameter integrity at both design-time and runtime

**Recommendations for DonkeyWork-Agents:**

- Adopt the attribute-driven parameter system for automatic UI generation
- Implement Resolvable<T> pattern for flexible parameter values
- Use Scriban for template evaluation (simpler than full JavaScript for most use cases)
- Build a discovery service to automatically register action nodes from assemblies
- Generate JSON schemas from C# attributes for frontend consumption

---

## Part 1: Reference SDK Architecture Analysis

### 1.1 SDK Steps Deep Dive

#### Core Architecture

The reference implementation organizes SDK steps into three primary components:

1. **Parameter Classes**: Define step configuration and metadata
2. **Provider Classes**: Contain executable step logic
3. **Step Execution Layer**: Orchestrates parameter resolution and method invocation

#### Step Definition Pattern

Parameter classes serve as both data containers and metadata sources:

```csharp
[SdkStepDefinition(
    stepType: SdkStepType.HttpRequest,
    category: SdkStepCategory.Actions,
    group: SdkStepGroup.Http,
    inputHandles: SdkStepDefinitionAttribute.UnlimitedHandles,
    outputHandles: SdkStepDefinitionAttribute.UnlimitedHandles,
    enabled: true,
    featureFlag: "step-sdk-http-step",
    version: "1.0")]
public class HttpRequestParameters : BaseSdkStepParameters
{
    [Required]
    [DefaultValue(MethodType.Get)]
    public MethodType Method { get; set; } = MethodType.Get;

    [Required]
    [SupportVariables]
    public string Url { get; set; } = string.Empty;

    [Range(1, 600)]
    [DefaultValue(120)]
    [JsonConverter(typeof(ResolvableJsonConverter<int>))]
    public Resolvable<int> TimeoutSeconds { get; set; } = 120;
}
```

**Key Design Decisions:**

- **Single Source of Truth**: Parameter class contains both runtime configuration and UI metadata
- **Attribute-Based Metadata**: No separate schema files needed - reflection generates schemas
- **Category/Group Hierarchy**: Organizes steps in UI toolbox (e.g., Actions > Http)
- **Handle Configuration**: Defines how many input/output connections are allowed
- **Feature Flags**: Enable gradual rollout of new steps

#### Provider Implementation Pattern

Providers are simple classes with decorated methods:

```csharp
[SdkProviderType]
public class HttpProvider
{
    private readonly IHttpSdkClient httpSdkClient;
    private readonly ITemplateEngine templateEngine;

    public HttpProvider(
        IHttpSdkClient httpSdkClient,
        ITemplateEngine templateEngine)
    {
        this.httpSdkClient = httpSdkClient;
        this.templateEngine = templateEngine;
    }

    [SdkMethod(stepType: SdkStepType.HttpRequest)]
    [SupportedCredentials(CredentialType.BasicAuth, CredentialType.HeaderAuth)]
    public async Task<HttpSdkResponse> ExecuteRequestAsync(
        HttpRequestParameters parameters,
        CancellationToken cancellationToken = default)
    {
        // Variable substitution in URL
        var resolvedUrl = this.templateEngine.EvaluateTemplate(parameters.Url);

        // Execute HTTP request
        var response = await this.httpSdkClient.ExecuteRequestAsync(
            parameters, resolvedUrl, cancellationToken);

        return response;
    }
}
```

**Provider Characteristics:**

- **Dependency Injection**: Providers receive services via constructor injection
- **Method Attributes**: Link provider methods to step types
- **Credential Specification**: Declares supported authentication methods
- **Strongly Typed**: Parameters are type-checked at compile time
- **Async First**: All methods return `Task<T>` for async execution

#### Step Lifecycle

1. **Discovery**: Reflection scans assemblies for `[SdkProviderType]` and `[SdkStepDefinition]`
2. **Registration**: Steps and providers registered in service container
3. **Schema Generation**: Attributes converted to JSON schemas for UI
4. **Execution**: Parameters deserialized, expressions resolved, provider method invoked
5. **Result Handling**: Output captured and made available to downstream steps

### 1.2 Parameter System

#### Resolvable<T> Pattern

The cornerstone of the parameter system is `Resolvable<T>`, a struct that accepts either literal values or expressions:

```csharp
public readonly struct Resolvable<T>
{
    private readonly string rawValue;

    public Resolvable(string value) => this.rawValue = value ?? string.Empty;
    public Resolvable(T value) => this.rawValue = ConvertToString(value);

    public string RawValue => this.rawValue ?? string.Empty;

    // Check if value contains template syntax {{...}}
    public bool IsExpression =>
        this.RawValue.Contains("{{") && this.RawValue.Contains("}}");

    // Check if EXACTLY {{expression}} with nothing else
    public bool IsPureExpression { get; }

    // Implicit conversions
    public static implicit operator Resolvable<T>(string value) => new(value);
    public static implicit operator Resolvable<T>(T value) => new(value);
}
```

**Design Benefits:**

- **Unified Storage**: Everything stored as string in JSON
- **Type Safety**: Generic constraint ensures type compatibility
- **Expression Detection**: Automatically identifies if value needs evaluation
- **Validation Support**: Can validate literals at design-time, skip expressions
- **Seamless Usage**: Implicit conversions make it natural to use

#### Usage Examples

```csharp
// Literal value
Resolvable<int> timeout = 120;

// Expression
Resolvable<int> timeout = "{{Variables.timeout}}";

// Mixed template (valid for strings only)
Resolvable<string> message = "Request completed in {{Steps.HttpRequest.duration}}ms";

// Pure expression (required for non-string types)
Resolvable<int> delay = "{{Variables.delayMs}}"; // OK
Resolvable<int> delay = "Wait {{Variables.delayMs}}ms"; // ERROR - mixed not allowed
```

#### Parameter Attributes

The reference implementation uses multiple attributes for UI generation:

**1. Validation Attributes**

```csharp
[Required]
public Guid CredentialId { get; set; }

[Range(1, 600)]
[DefaultValue(120)]
public Resolvable<int> TimeoutSeconds { get; set; }
```

**2. UI Control Attributes**

```csharp
[EditorType(EditorType.CodeEditor)]
public string Body { get; set; }

[EditorType(EditorType.Dropdown)]
public MethodType Method { get; set; }
```

**3. Variable Support**

```csharp
[SupportVariables]  // Renders special input with {{}} button
public string Url { get; set; }
```

**4. Dynamic Options**

```csharp
[LoadOptions("getProjects")]  // Calls loader method for dropdown options
public Guid? ProjectId { get; set; }
```

**5. Credential Mapping**

```csharp
[CredentialMapping([
    CredentialType.None,
    CredentialType.BasicAuth,
    CredentialType.HeaderAuth])]
public Guid? CredentialId { get; set; }
```

**6. Folder/File Browsing**

```csharp
[FolderBrowse(DirectoryOptionsLoaders.FilesAndFolders)]
public string? Path { get; set; }
```

#### Parameter Validation

The base class provides validation infrastructure:

```csharp
public abstract class BaseSdkStepParameters
{
    public abstract (bool valid, List<ValidationResult> results) IsValid();

    protected (bool valid, List<ValidationResult> results) ValidateDataAnnotations()
    {
        var results = new List<ValidationResult>();
        var properties = this.GetType().GetProperties();

        foreach (var property in properties)
        {
            if (IsResolvableType(property.PropertyType))
            {
                // Special validation for Resolvable<T>
                ValidateResolvableProperty(property, results);
            }
            else
            {
                // Standard validation
                Validator.TryValidateProperty(value, context, results);
            }
        }

        return (results.Count == 0, results);
    }
}
```

**Resolvable Validation Rules:**

1. **Pure Expressions**: Skip validation (can't validate at design-time)
2. **Literals**: Parse and validate against attributes
3. **Mixed Expressions**: Only allowed for string types
4. **Non-String Types**: Must be pure literal or pure expression

### 1.3 Provider Architecture

#### Provider Discovery

The reference implementation uses a discovery service:

```csharp
public interface ISdkProviderDiscoveryService
{
    IEnumerable<Type> GetAllProviderTypes();
    IEnumerable<MethodInfo> GetProviderMethods(Type providerType);
}

public class SdkProviderDiscoveryService : ISdkProviderDiscoveryService
{
    public IEnumerable<Type> GetAllProviderTypes()
    {
        // Scan assemblies for [SdkProviderType]
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttribute<SdkProviderTypeAttribute>() != null);
    }
}
```

#### Method Execution

Provider methods are executed via compiled delegates for performance:

```csharp
public interface ISdkMethodExecutor
{
    Task<object?> ExecuteAsync(
        SdkMethodInfo methodInfo,
        BaseSdkStepParameters parameters,
        CancellationToken cancellationToken);
}
```

**Execution Flow:**

1. **Resolve Parameters**: Convert `Resolvable<T>` to concrete values
2. **Inject Dependencies**: Provide services to provider constructor
3. **Invoke Method**: Execute provider method with resolved parameters
4. **Capture Result**: Serialize output for downstream steps

#### Credential Access

Providers access credentials through an accessor service:

```csharp
public interface IAgentCredentialAccessor
{
    List<ExecutionCredentials> GetCredentials(Guid credentialId);
}

// Usage in provider
var credentials = this.credentialAccessor.GetCredentials(parameters.CredentialId);
var accessToken = GetRequiredAccessToken(credentials);
```

**Security Design:**

- **Scoped Access**: Only execute method sees credentials
- **Type Filtering**: `[SupportedCredentials]` restricts credential types
- **Encrypted Storage**: Credentials encrypted at rest
- **Injection Pattern**: Credentials never in parameter objects

### 1.4 Variable Language: Jint & Scriban

The reference implementation uses two complementary expression engines:

#### Scriban: Template Engine

**Purpose**: String interpolation and simple variable substitution

**Syntax**: Handlebars-style `{{expression}}`

```csharp
public interface ITemplateEngine
{
    string EvaluateTemplate(string template);
}

public class ScribanTemplateEngine : ITemplateEngine
{
    public string EvaluateTemplate(string template)
    {
        var context = this.contextProvider.GetContext();

        var scriptObject = new ScriptObject();
        scriptObject["Execution"] = context.Execution;
        scriptObject["Variables"] = context.Variables;
        scriptObject["Steps"] = context.Steps;
        scriptObject["User"] = context.User;

        var templateContext = new TemplateContext
        {
            StrictVariables = true,
            EnableRelaxedMemberAccess = false
        };

        templateContext.PushGlobal(scriptObject);
        var scribanTemplate = Template.Parse(template);
        return scribanTemplate.Render(templateContext);
    }
}
```

**Example Usage:**

```csharp
// Simple variable
"{{Variables.username}}"

// Property access
"{{User.Email}}"

// Step output access
"{{Steps.HttpRequest.body}}"

// Complex template
"Hello {{User.Name}}, your request to {{Variables.url}} completed in {{Steps.HttpRequest.duration}}ms"
```

**Configuration:**

- `StrictVariables = true`: Throws on undefined variables
- `EnableRelaxedMemberAccess = false`: Strict property access
- `MemberRenamer = member => member.Name`: Preserve casing

#### Jint: JavaScript Engine

**Purpose**: Complex conditional logic and boolean expressions

**Syntax**: Pure JavaScript

```csharp
public interface IConditionEngine
{
    object EvaluateExpression(string expression);
}

public class JintConditionEngine : IConditionEngine
{
    public object EvaluateExpression(string expression)
    {
        var engine = new Engine(options =>
        {
            options.Strict();
            options.LimitRecursion(100);
            options.MaxStatements(10000);
            options.TimeoutInterval(TimeSpan.FromSeconds(5));
        });

        RegisterHelpers(engine);

        var context = this.contextProvider.GetContext();
        engine.SetValue("Execution", context.Execution);
        engine.SetValue("Variables", context.Variables);
        engine.SetValue("Steps", context.Steps);

        var result = engine.Evaluate(expression);
        return ConvertJsValueToObject(result);
    }
}
```

**Example Usage:**

```javascript
// Boolean conditions
Variables.count > 10

// Array operations
Steps.HttpRequest.items.length > 0

// Complex logic
Variables.status === 'active' && Steps.Validation.isValid

// String manipulation
Variables.email.toLowerCase().includes('@example.com')
```

**Security Constraints:**

- `Strict()`: Enforce strict mode JavaScript
- `LimitRecursion(100)`: Prevent stack overflow
- `MaxStatements(10000)`: Limit execution complexity
- `TimeoutInterval(5s)`: Prevent infinite loops

#### Custom Helper Functions

Jint engine includes custom helpers:

```javascript
// JSON parsing
Variables.jsonString.fromJson()

// Type conversion
Variables.stringNumber.toInt()
Variables.stringBool.toBool()

// JSON serialization
Steps.HttpRequest.toJson(true) // pretty print
```

#### When to Use Each Engine

**Use Scriban when:**
- Simple variable substitution
- String templating
- Property access (dot notation)
- Performance-critical paths

**Use Jint when:**
- Boolean conditions (If nodes, Router nodes)
- Complex logic requiring JavaScript features
- Array/object manipulation
- Mathematical expressions

### 1.5 Resolvable<T> Pattern Deep Dive

#### Resolution Flow

```
┌─────────────────────┐
│  Resolvable<int>    │
│  RawValue: "120"    │
└──────────┬──────────┘
           │
           ▼
    ┌──────────────┐
    │ IsExpression? │
    └──────┬───────┘
           │
     ┌─────┴─────┐
     │           │
    No          Yes
     │           │
     ▼           ▼
┌─────────┐  ┌──────────────┐
│  Parse  │  │  Evaluate    │
│  "120"  │  │  Template    │
│  = 120  │  │  Engine      │
└─────────┘  └──────────────┘
```

#### Parameter Resolver Service

```csharp
public interface IStepParameterResolver
{
    int ResolveInt(Resolvable<int> resolvable);
    bool ResolveBool(Resolvable<bool> resolvable);
    string ResolveString(Resolvable<string> resolvable);
    TEnum ResolveEnum<TEnum>(Resolvable<TEnum> resolvable) where TEnum : struct, Enum;
}

public class StepParameterResolver : IStepParameterResolver
{
    private readonly ITemplateEngine templateEngine;

    public int ResolveInt(Resolvable<int> resolvable)
    {
        if (!resolvable.IsExpression)
        {
            // Literal value
            if (int.TryParse(resolvable.RawValue, out var literal))
                return literal;

            throw new ParameterResolutionException(
                $"'{resolvable.RawValue}' is not a valid integer");
        }

        // Evaluate expression
        var result = this.templateEngine.EvaluateTemplate(resolvable.RawValue);

        if (int.TryParse(result, out var value))
            return value;

        throw new ParameterResolutionException(
            $"Expression evaluated to '{result}' which is not a valid integer");
    }
}
```

#### Integration with Providers

Providers access resolved values:

```csharp
[SdkMethod(stepType: SdkStepType.Delay)]
public async Task<DelayStepOutput> ExecuteDelayAsync(
    DelayStepParameters parameters,
    CancellationToken cancellationToken)
{
    // Resolve with validation
    var propertyInfo = typeof(DelayStepParameters)
        .GetProperty(nameof(DelayStepParameters.DelayMS));

    var delayMs = this.parameterResolver.ResolveInt(
        parameters.DelayMS,
        propertyInfo);

    await Task.Delay(delayMs, cancellationToken);

    return new DelayStepOutput { DelayedFor = delayMs };
}
```

### 1.6 Key Architectural Patterns

#### 1. Attribute-Driven Configuration

**Benefits:**
- Single source of truth
- Compile-time safety
- Reflection-based discovery
- Auto-generated schemas

**Trade-offs:**
- Requires C# knowledge for step authors
- Schema generation adds startup cost
- Limited to .NET ecosystem

#### 2. Convention Over Configuration

**Conventions:**
- Parameter class name matches pattern: `{StepName}Parameters`
- Provider method returns `Task<{StepName}Output>`
- Step type enum matches: `SdkStepType.{StepName}`

#### 3. Dependency Injection Throughout

**DI Usage:**
- Provider constructors receive services
- Scoped services per execution context
- Expression engines access context via provider

#### 4. Type Safety with Flexibility

**Approach:**
- Strong typing at compile time
- Runtime expression evaluation
- Validation at multiple stages

#### 5. Extensibility Points

**Extension Mechanisms:**
- New providers via `[SdkProviderType]`
- Custom attributes for UI controls
- Options loaders for dynamic dropdowns
- Custom expression helpers

---

## Part 2: n8n Architecture Analysis

### 2.1 Node System

#### Node Structure

n8n nodes are defined in TypeScript with descriptive objects:

```typescript
export class HttpRequest implements INodeType {
  description: INodeTypeDescription = {
    displayName: 'HTTP Request',
    name: 'httpRequest',
    icon: 'fa:at',
    group: ['output'],
    version: 1,
    description: 'Makes an HTTP request',
    defaults: {
      name: 'HTTP Request',
    },
    inputs: ['main'],
    outputs: ['main'],
    credentials: [
      {
        name: 'httpBasicAuth',
        required: false,
      },
    ],
    properties: [
      {
        displayName: 'Method',
        name: 'method',
        type: 'options',
        options: [
          { name: 'GET', value: 'GET' },
          { name: 'POST', value: 'POST' },
        ],
        default: 'GET',
      },
      {
        displayName: 'URL',
        name: 'url',
        type: 'string',
        default: '',
        required: true,
        placeholder: 'https://example.com/api',
      },
    ],
  };
}
```

#### Parameter Types

n8n supports various control types:

- **string**: Simple text input
- **number**: Numeric input with validation
- **boolean**: Checkbox
- **options**: Dropdown with predefined options
- **multiOptions**: Multi-select dropdown
- **dateTime**: Date/time picker
- **color**: Color picker
- **json**: JSON editor
- **collection**: Grouped fields
- **fixedCollection**: Repeatable field groups
- **resourceLocator**: Resource finder with search

### 2.2 Expression Language

n8n uses a dual-mode expression system:

#### Expression Syntax

```javascript
// Basic variable access
{{ $json.fieldName }}

// Previous node data
{{ $node["HTTP Request"].json.body }}

// JavaScript expressions
{{ $json.price * 1.1 }}

// Using Luxon for dates
{{ $now.minus({ days: 7 }).toISO() }}

// JMESPath queries
{{ $json.users[?age > `30`].name }}
```

#### Available Variables

- `$json`: Current item data
- `$input`: All input items
- `$node`: Access specific node output
- `$now`: Current date/time (Luxon)
- `$workflow`: Workflow metadata
- `$execution`: Execution information
- `$env`: Environment variables

#### Tournament Templating

n8n's custom Tournament language provides:
- Simpler syntax than JavaScript for common tasks
- Built-in data transformation functions
- Safe evaluation without full JavaScript access

### 2.3 Execution Model

#### Data Flow

```
Node A Output → [ Array of Items ] → Node B Input
                                    → Node C Input
```

**Key Concepts:**

1. **Items**: Array of objects passed between nodes
2. **JSON Format**: Standard data structure
3. **Multiple Outputs**: Nodes can have multiple output paths
4. **Branching**: Same data can flow to multiple nodes

#### Error Handling

- **Continue On Fail**: Node errors don't stop workflow
- **Error Workflows**: Dedicated error handling paths
- **Retry Logic**: Automatic retry with backoff

---

## Part 3: Comparative Analysis

### 3.1 Strengths and Weaknesses

#### Reference Implementation Strengths

1. **Type Safety**: Compile-time checking prevents many errors
2. **Performance**: Compiled code executes faster than interpreted
3. **Tooling**: IDE support (IntelliSense, refactoring)
4. **Attribute System**: Elegant metadata-driven approach
5. **Validation**: Multi-stage validation (design + runtime)
6. **Dependency Injection**: Clean service integration

#### Reference Implementation Weaknesses

1. **.NET Dependency**: Requires C# knowledge
2. **Compilation Required**: Changes need rebuild
3. **Learning Curve**: Attributes and conventions to learn
4. **Limited Community**: Fewer community-contributed steps

#### n8n Strengths

1. **Accessibility**: JavaScript familiarity widespread
2. **No Compilation**: Edit nodes without rebuild
3. **Community**: Large library of community nodes
4. **Expression Simplicity**: Tournament easier than C#
5. **Visual Development**: Code not required for many tasks

#### n8n Weaknesses

1. **Type Safety**: JavaScript lacks compile-time checks
2. **Performance**: Interpreted execution slower
3. **Validation**: Limited design-time validation
4. **Debugging**: Harder to debug expression issues
5. **Security**: More risk with unrestricted JavaScript

### 3.2 Key Differences

| Aspect | Reference Implementation | n8n |
|--------|-------------------------|-----|
| **Language** | C# | TypeScript/JavaScript |
| **Type System** | Strong, static | Weak, dynamic |
| **Metadata** | Attributes | Descriptor objects |
| **Expressions** | Scriban + Jint | Tournament + JS |
| **Validation** | Design + Runtime | Primarily runtime |
| **Extensibility** | Compile new providers | Drop in node files |
| **Performance** | High (compiled) | Moderate (interpreted) |
| **Learning Curve** | Steep (C#/.NET) | Gentle (JavaScript) |

### 3.3 Best Practices from Both

**From Reference Implementation:**
- Attribute-driven metadata
- Resolvable<T> pattern for parameters
- Strong typing with flexibility
- Parameter validation infrastructure
- Provider discovery via reflection

**From n8n:**
- Intuitive expression syntax `{{ }}`
- Resource locator concept
- Simple credential system
- Visual parameter organization
- Community-friendly extensibility

---

## Part 4: ActionNodes Design Proposal for DonkeyWork-Agents

### 4.1 Proposed Architecture

#### High-Level Design

```
┌─────────────────────────────────────────────────────────────┐
│                      Frontend (React)                        │
│  ┌────────────┐  ┌───────────────┐  ┌──────────────────┐   │
│  │  ReactFlow │  │  Properties   │  │  Node Palette    │   │
│  │   Canvas   │  │    Panel      │  │  (from schemas)  │   │
│  └────────────┘  └───────────────┘  └──────────────────┘   │
└────────────────────────────┬────────────────────────────────┘
                             │ REST API
┌────────────────────────────┴────────────────────────────────┐
│                    Backend (ASP.NET Core)                    │
│  ┌──────────────────────────────────────────────────────┐   │
│  │              ActionNodes Module                       │   │
│  │  ┌─────────────┐  ┌──────────────┐  ┌────────────┐  │   │
│  │  │  Discovery  │  │   Schema     │  │ Execution  │  │   │
│  │  │   Service   │  │  Generator   │  │   Engine   │  │   │
│  │  └─────────────┘  └──────────────┘  └────────────┘  │   │
│  └──────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │                Action Providers                       │   │
│  │  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐ │   │
│  │  │  HTTP   │  │ Database│  │  Email  │  │  AI/ML  │ │   │
│  │  │ Provider│  │ Provider│  │ Provider│  │ Provider│ │   │
│  │  └─────────┘  └─────────┘  └─────────┘  └─────────┘ │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

#### Module Structure

```
src/
├── actions/
│   ├── DonkeyWork.Agents.Actions.Contracts/
│   │   ├── Attributes/
│   │   │   ├── ActionNodeAttribute.cs
│   │   │   ├── ActionMethodAttribute.cs
│   │   │   ├── ActionParameterAttribute.cs
│   │   │   └── CredentialMappingAttribute.cs
│   │   ├── Models/
│   │   │   ├── Parameters/
│   │   │   │   ├── BaseActionParameters.cs
│   │   │   │   ├── HttpRequestParameters.cs
│   │   │   │   └── ...
│   │   │   ├── Outputs/
│   │   │   │   ├── HttpRequestOutput.cs
│   │   │   │   └── ...
│   │   │   └── Schema/
│   │   │       ├── ActionNodeSchema.cs
│   │   │       └── ParameterSchema.cs
│   │   ├── Services/
│   │   │   ├── IActionDiscoveryService.cs
│   │   │   ├── IActionExecutionService.cs
│   │   │   └── IExpressionEngine.cs
│   │   └── Types/
│   │       └── Resolvable.cs
│   ├── DonkeyWork.Agents.Actions.Core/
│   │   ├── Services/
│   │   │   ├── ActionDiscoveryService.cs
│   │   │   ├── ActionExecutionService.cs
│   │   │   ├── ParameterResolverService.cs
│   │   │   └── ScribanExpressionEngine.cs
│   │   └── Providers/
│   │       ├── HttpActionProvider.cs
│   │       ├── DatabaseActionProvider.cs
│   │       └── ...
│   └── DonkeyWork.Agents.Actions.Api/
│       ├── Controllers/
│       │   ├── ActionNodesController.cs
│       │   └── ActionExecutionController.cs
│       └── DependencyInjection.cs
```

### 4.2 Node Definition System

#### Base Parameter Class

```csharp
namespace DonkeyWork.Agents.Actions.Contracts.Models.Parameters;

[JsonConverter(typeof(BaseActionParametersJsonConverter))]
public abstract class BaseActionParameters
{
    public virtual string Version { get; init; } = "1.0";

    public abstract (bool valid, List<ValidationResult> results) IsValid();

    protected (bool valid, List<ValidationResult> results) ValidateDataAnnotations()
    {
        var results = new List<ValidationResult>();
        var properties = this.GetType().GetProperties();

        foreach (var property in properties)
        {
            if (IsResolvableType(property.PropertyType))
            {
                ValidateResolvableProperty(property, results);
            }
            else
            {
                var context = new ValidationContext(this) { MemberName = property.Name };
                Validator.TryValidateProperty(
                    property.GetValue(this), context, results);
            }
        }

        return (results.Count == 0, results);
    }

    private bool IsResolvableType(Type type) =>
        type.IsGenericType &&
        type.GetGenericTypeDefinition() == typeof(Resolvable<>);
}
```

#### Action Node Attribute

```csharp
namespace DonkeyWork.Agents.Actions.Contracts.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ActionNodeAttribute : Attribute
{
    public ActionNodeAttribute(
        string actionType,
        string category,
        int maxInputs = -1,
        int maxOutputs = -1)
    {
        ActionType = actionType;
        Category = category;
        MaxInputs = maxInputs;
        MaxOutputs = maxOutputs;
    }

    public string ActionType { get; }
    public string Category { get; }
    public string? Group { get; set; }
    public int MaxInputs { get; }  // -1 = unlimited
    public int MaxOutputs { get; }
    public bool Enabled { get; set; } = true;
    public string? Icon { get; set; }
    public string? Description { get; set; }
}
```

#### Example Action Node

```csharp
[ActionNode(
    actionType: "http_request",
    category: "Communication",
    group: "HTTP",
    Icon = "globe",
    Description = "Make HTTP requests to external APIs")]
public class HttpRequestParameters : BaseActionParameters
{
    [Required]
    [Display(Name = "Method", Description = "HTTP method to use")]
    public HttpMethod Method { get; set; } = HttpMethod.GET;

    [Required]
    [Display(Name = "URL", Description = "Target URL for the request")]
    [SupportVariables]
    public string Url { get; set; } = string.Empty;

    [Display(Name = "Timeout (seconds)")]
    [Range(1, 300)]
    [DefaultValue(30)]
    public Resolvable<int> TimeoutSeconds { get; set; } = 30;

    [Display(Name = "Headers")]
    [SupportVariables]
    public Dictionary<string, string>? Headers { get; set; }

    [Display(Name = "Body")]
    [EditorType(EditorType.Code)]
    [SupportVariables]
    public string? Body { get; set; }

    [CredentialMapping(["none", "basic_auth", "bearer_token"])]
    public Guid? CredentialId { get; set; }

    public override (bool valid, List<ValidationResult> results) IsValid()
    {
        return ValidateDataAnnotations();
    }
}
```

#### Provider Implementation

```csharp
[ActionProvider]
public class HttpActionProvider
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IExpressionEngine expressionEngine;
    private readonly ICredentialService credentialService;
    private readonly ILogger<HttpActionProvider> logger;

    public HttpActionProvider(
        IHttpClientFactory httpClientFactory,
        IExpressionEngine expressionEngine,
        ICredentialService credentialService,
        ILogger<HttpActionProvider> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.expressionEngine = expressionEngine;
        this.credentialService = credentialService;
        this.logger = logger;
    }

    [ActionMethod("http_request")]
    [SupportedCredentials("basic_auth", "bearer_token")]
    public async Task<HttpRequestOutput> ExecuteAsync(
        HttpRequestParameters parameters,
        CancellationToken cancellationToken = default)
    {
        // Resolve URL (may contain variables)
        var url = expressionEngine.EvaluateTemplate(parameters.Url);

        // Create HTTP client
        var client = httpClientFactory.CreateClient();

        // Apply credentials if provided
        if (parameters.CredentialId.HasValue)
        {
            var credential = await credentialService
                .GetCredentialAsync(parameters.CredentialId.Value);
            ApplyCredentials(client, credential);
        }

        // Create request
        var request = new HttpRequestMessage(
            new System.Net.Http.HttpMethod(parameters.Method.ToString()),
            url);

        // Add headers
        if (parameters.Headers != null)
        {
            foreach (var (key, value) in parameters.Headers)
            {
                var resolvedKey = expressionEngine.EvaluateTemplate(key);
                var resolvedValue = expressionEngine.EvaluateTemplate(value);
                request.Headers.TryAddWithoutValidation(resolvedKey, resolvedValue);
            }
        }

        // Add body
        if (!string.IsNullOrEmpty(parameters.Body))
        {
            var resolvedBody = expressionEngine.EvaluateTemplate(parameters.Body);
            request.Content = new StringContent(resolvedBody);
        }

        // Execute request
        var response = await client.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        return new HttpRequestOutput
        {
            StatusCode = (int)response.StatusCode,
            Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
            Body = responseBody,
            IsSuccess = response.IsSuccessStatusCode
        };
    }
}
```

### 4.3 Variable and Expression System

#### Recommendation: Start with Scriban Only

For DonkeyWork-Agents, I recommend starting with **Scriban only** and adding Jint later if needed:

**Rationale:**
1. **Simpler Implementation**: One engine to integrate and secure
2. **Sufficient for Most Use Cases**: 90% of expressions are simple variable substitution
3. **Better Security**: Less attack surface than full JavaScript
4. **Easier Debugging**: Template syntax clearer than JS
5. **Better Performance**: Scriban is faster for simple operations

#### Expression Engine Interface

```csharp
public interface IExpressionEngine
{
    /// <summary>
    /// Evaluates a template string with variable substitution
    /// </summary>
    string EvaluateTemplate(string template);

    /// <summary>
    /// Gets the current expression context
    /// </summary>
    ExpressionContext GetContext();
}
```

#### Expression Context

```csharp
public class ExpressionContext
{
    /// <summary>
    /// Workflow execution metadata
    /// </summary>
    public ExecutionInfo Execution { get; set; } = new();

    /// <summary>
    /// Current user information
    /// </summary>
    public UserInfo User { get; set; } = new();

    /// <summary>
    /// Workflow-level variables
    /// </summary>
    public Dictionary<string, object> Variables { get; set; } = new();

    /// <summary>
    /// Outputs from previous nodes
    /// </summary>
    public Dictionary<string, object> Nodes { get; set; } = new();

    /// <summary>
    /// Outputs from directly connected parent nodes
    /// </summary>
    public Dictionary<string, object> Inputs { get; set; } = new();

    /// <summary>
    /// Helper functions
    /// </summary>
    public HelperFunctions Helpers { get; set; } = new();
}

public class ExecutionInfo
{
    public Guid WorkflowId { get; set; }
    public Guid ExecutionId { get; set; }
    public DateTime StartTime { get; set; }
}

public class UserInfo
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class HelperFunctions
{
    public string Now => DateTime.UtcNow.ToString("o");
    public string Today => DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
}
```

#### Scriban Implementation

```csharp
public class ScribanExpressionEngine : IExpressionEngine
{
    private readonly IExpressionContextProvider contextProvider;

    public ScribanExpressionEngine(IExpressionContextProvider contextProvider)
    {
        this.contextProvider = contextProvider;
    }

    public string EvaluateTemplate(string template)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        try
        {
            var context = contextProvider.GetContext();

            // Build Scriban script object
            var scriptObject = new ScriptObject();
            scriptObject["Execution"] = ConvertToScribanObject(context.Execution);
            scriptObject["User"] = ConvertToScribanObject(context.User);
            scriptObject["Variables"] = context.Variables;
            scriptObject["Nodes"] = context.Nodes;
            scriptObject["Inputs"] = context.Inputs;
            scriptObject["Helpers"] = ConvertToScribanObject(context.Helpers);

            // Configure template context
            var templateContext = new TemplateContext
            {
                MemberRenamer = member => member.Name,
                StrictVariables = true,
                EnableRelaxedMemberAccess = false
            };

            templateContext.PushGlobal(scriptObject);

            // Parse and render
            var scribanTemplate = Template.Parse(template);
            if (scribanTemplate.HasErrors)
            {
                var errors = string.Join("; ",
                    scribanTemplate.Messages.Select(m => m.Message));
                throw new ExpressionEvaluationException(
                    $"Template parsing failed: {errors}");
            }

            return scribanTemplate.Render(templateContext);
        }
        catch (Exception ex)
        {
            throw new ExpressionEvaluationException(
                $"Expression evaluation failed: {ex.Message}", ex);
        }
    }

    private object? ConvertToScribanObject(object? value)
    {
        if (value == null) return null;

        // Convert complex objects to dictionaries for Scriban navigation
        var type = value.GetType();
        if (type.IsClass && type != typeof(string) && !type.IsPrimitive)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var prop in type.GetProperties())
            {
                dict[prop.Name] = ConvertToScribanObject(prop.GetValue(value));
            }
            return dict;
        }

        return value;
    }
}
```

#### Expression Examples

```csharp
// Simple variable
"{{Variables.username}}"

// Nested property access
"{{User.Email}}"

// Node output access
"{{Nodes.HttpRequest.Body}}"

// Input from connected node
"{{Inputs.DataFetch.Records[0].Name}}"

// Helper functions
"{{Helpers.Now}}"

// Complex template
"Hello {{User.Name}}, you have {{Nodes.CountRecords.Count}} items to process."

// Conditional rendering (Scriban feature)
"{{if Variables.Count > 10}}Many items{{else}}Few items{{end}}"

// Loops (Scriban feature)
"{{for item in Nodes.FetchData.Items}}{{item.Name}}, {{end}}"
```

### 4.4 UI Generation Strategy

#### Schema Generation Service

```csharp
public interface IActionSchemaService
{
    Task<IEnumerable<ActionNodeSchema>> GetAllActionSchemasAsync();
    Task<ActionNodeSchema?> GetActionSchemaAsync(string actionType);
}

public class ActionSchemaService : IActionSchemaService
{
    private readonly IActionDiscoveryService discoveryService;

    public async Task<IEnumerable<ActionNodeSchema>> GetAllActionSchemasAsync()
    {
        var actionTypes = discoveryService.GetAllActionTypes();
        var schemas = new List<ActionNodeSchema>();

        foreach (var actionType in actionTypes)
        {
            var schema = await GenerateSchemaAsync(actionType);
            if (schema != null)
                schemas.Add(schema);
        }

        return schemas;
    }

    private async Task<ActionNodeSchema?> GenerateSchemaAsync(Type parameterType)
    {
        var attribute = parameterType.GetCustomAttribute<ActionNodeAttribute>();
        if (attribute == null) return null;

        var schema = new ActionNodeSchema
        {
            ActionType = attribute.ActionType,
            Category = attribute.Category,
            Group = attribute.Group,
            Icon = attribute.Icon,
            Description = attribute.Description,
            MaxInputs = attribute.MaxInputs,
            MaxOutputs = attribute.MaxOutputs,
            Parameters = GenerateParameterSchemas(parameterType)
        };

        return await Task.FromResult(schema);
    }

    private List<ParameterSchema> GenerateParameterSchemas(Type parameterType)
    {
        var schemas = new List<ParameterSchema>();
        var properties = parameterType.GetProperties();

        foreach (var property in properties)
        {
            var schema = new ParameterSchema
            {
                Name = property.Name,
                DisplayName = GetDisplayName(property),
                Description = GetDescription(property),
                Type = GetParameterType(property),
                Required = IsRequired(property),
                DefaultValue = GetDefaultValue(property),
                SupportsVariables = HasAttribute<SupportVariablesAttribute>(property),
                EditorType = GetEditorType(property),
                Options = GetOptions(property),
                Validation = GetValidationRules(property)
            };

            schemas.Add(schema);
        }

        return schemas;
    }

    private string GetParameterType(PropertyInfo property)
    {
        var type = property.PropertyType;

        // Handle Resolvable<T>
        if (type.IsGenericType &&
            type.GetGenericTypeDefinition() == typeof(Resolvable<>))
        {
            type = type.GetGenericArguments()[0];
        }

        if (type == typeof(string)) return "string";
        if (type == typeof(int)) return "number";
        if (type == typeof(bool)) return "boolean";
        if (type.IsEnum) return "enum";
        if (type == typeof(Guid)) return "guid";
        if (type == typeof(DateTime)) return "datetime";
        if (typeof(IDictionary).IsAssignableFrom(type)) return "dictionary";

        return "object";
    }
}
```

#### Generated Schema Format

```json
{
  "actionType": "http_request",
  "category": "Communication",
  "group": "HTTP",
  "icon": "globe",
  "description": "Make HTTP requests to external APIs",
  "maxInputs": -1,
  "maxOutputs": -1,
  "parameters": [
    {
      "name": "Method",
      "displayName": "HTTP Method",
      "description": "HTTP method to use",
      "type": "enum",
      "required": true,
      "defaultValue": "GET",
      "options": [
        { "label": "GET", "value": "GET" },
        { "label": "POST", "value": "POST" },
        { "label": "PUT", "value": "PUT" },
        { "label": "DELETE", "value": "DELETE" }
      ]
    },
    {
      "name": "Url",
      "displayName": "URL",
      "description": "Target URL for the request",
      "type": "string",
      "required": true,
      "supportsVariables": true
    },
    {
      "name": "TimeoutSeconds",
      "displayName": "Timeout (seconds)",
      "type": "number",
      "required": false,
      "defaultValue": 30,
      "validation": {
        "min": 1,
        "max": 300
      },
      "resolvable": true
    }
  ]
}
```

#### Frontend Integration

The React frontend consumes these schemas to:

1. **Populate Node Palette**: Display available action nodes grouped by category
2. **Generate Properties Panel**: Dynamically render form controls based on parameter schemas
3. **Enable Variable Picker**: Show `{{}}` button for fields with `supportsVariables: true`
4. **Validate Input**: Apply validation rules from schema
5. **Show Documentation**: Display descriptions and examples

**Example React Component:**

```typescript
function ActionNodeProperties({ node, schema }: Props) {
  return (
    <div className="properties-panel">
      <h3>{schema.displayName}</h3>
      <p>{schema.description}</p>

      {schema.parameters.map(param => (
        <div key={param.name} className="parameter-field">
          <label>{param.displayName}</label>

          {param.supportsVariables ? (
            <VariableInput
              value={node.data.parameters[param.name]}
              onChange={value => updateParameter(param.name, value)}
              placeholder={param.description}
            />
          ) : (
            renderControlForType(param)
          )}

          {param.description && (
            <span className="help-text">{param.description}</span>
          )}
        </div>
      ))}
    </div>
  );
}
```

### 4.5 Execution Engine

#### Execution Flow

```
1. Workflow Triggered
        ↓
2. Load Workflow Definition
        ↓
3. Build Execution Graph
        ↓
4. Initialize Execution Context
        ↓
5. Execute Nodes in Topological Order
   ├─> Resolve Parameters
   ├─> Evaluate Expressions
   ├─> Invoke Action Provider
   └─> Capture Output
        ↓
6. Update Context with Outputs
        ↓
7. Continue to Next Node
        ↓
8. Complete Execution
```

#### Execution Service Interface

```csharp
public interface IActionExecutionService
{
    Task<ExecutionResult> ExecuteActionAsync(
        ActionExecutionRequest request,
        CancellationToken cancellationToken = default);
}

public class ActionExecutionRequest
{
    public Guid WorkflowId { get; set; }
    public Guid ExecutionId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string ActionNodeId { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public ExpressionContext Context { get; set; } = new();
}

public class ExecutionResult
{
    public bool Success { get; set; }
    public object? Output { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }
}
```

#### Execution Service Implementation

```csharp
public class ActionExecutionService : IActionExecutionService
{
    private readonly IActionDiscoveryService discoveryService;
    private readonly IParameterResolverService parameterResolver;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<ActionExecutionService> logger;

    public async Task<ExecutionResult> ExecuteActionAsync(
        ActionExecutionRequest request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 1. Get action metadata
            var actionInfo = discoveryService.GetActionInfo(request.ActionType);
            if (actionInfo == null)
            {
                return new ExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"Action type '{request.ActionType}' not found"
                };
            }

            // 2. Deserialize parameters
            var parametersJson = JsonSerializer.Serialize(request.Parameters);
            var parameters = JsonSerializer.Deserialize(
                parametersJson,
                actionInfo.ParameterType) as BaseActionParameters;

            if (parameters == null)
            {
                return new ExecutionResult
                {
                    Success = false,
                    ErrorMessage = "Failed to deserialize parameters"
                };
            }

            // 3. Validate parameters
            var (valid, validationErrors) = parameters.IsValid();
            if (!valid)
            {
                return new ExecutionResult
                {
                    Success = false,
                    ErrorMessage = string.Join("; ",
                        validationErrors.Select(e => e.ErrorMessage))
                };
            }

            // 4. Get provider instance
            var provider = serviceProvider.GetRequiredService(
                actionInfo.ProviderType);

            // 5. Invoke action method
            var method = actionInfo.ProviderType.GetMethod(
                actionInfo.MethodName);

            var task = (Task)method!.Invoke(provider, new object[]
            {
                parameters,
                cancellationToken
            })!;

            await task;

            // 6. Get result
            var resultProperty = task.GetType().GetProperty("Result");
            var output = resultProperty?.GetValue(task);

            stopwatch.Stop();

            return new ExecutionResult
            {
                Success = true,
                Output = output,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Action execution failed");
            stopwatch.Stop();

            return new ExecutionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
    }
}
```

### 4.6 Extensibility

#### Adding New Action Nodes

Developers can add new action nodes by:

1. **Create Parameter Class**

```csharp
[ActionNode(
    actionType: "send_email",
    category: "Communication",
    group: "Email")]
public class SendEmailParameters : BaseActionParameters
{
    [Required]
    [EmailAddress]
    public string To { get; set; } = string.Empty;

    [Required]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [SupportVariables]
    public string Body { get; set; } = string.Empty;

    public override (bool valid, List<ValidationResult> results) IsValid()
    {
        return ValidateDataAnnotations();
    }
}
```

2. **Create Provider Class**

```csharp
[ActionProvider]
public class EmailActionProvider
{
    private readonly IEmailService emailService;

    [ActionMethod("send_email")]
    public async Task<SendEmailOutput> SendAsync(
        SendEmailParameters parameters,
        CancellationToken cancellationToken)
    {
        await emailService.SendAsync(
            parameters.To,
            parameters.Subject,
            parameters.Body,
            cancellationToken);

        return new SendEmailOutput { Sent = true };
    }
}
```

3. **Register in DI**

```csharp
services.AddScoped<EmailActionProvider>();
```

4. **Done!** The action is automatically discovered and available in UI

#### Plugin Architecture

For community contributions, support a plugin model:

```csharp
public interface IActionPlugin
{
    string Name { get; }
    string Version { get; }
    IEnumerable<Type> GetProviderTypes();
}

[ActionPlugin("community-slack-actions", "1.0.0")]
public class SlackActionsPlugin : IActionPlugin
{
    public string Name => "Slack Actions";
    public string Version => "1.0.0";

    public IEnumerable<Type> GetProviderTypes()
    {
        return new[]
        {
            typeof(SlackMessageProvider),
            typeof(SlackChannelProvider)
        };
    }
}
```

Plugins can be loaded from:
- NuGet packages
- Local DLL files
- Assembly discovery in configured directories

### 4.7 Implementation Roadmap

#### Phase 1: Core Infrastructure (Weeks 1-3)

**Goals:**
- Basic action node system working
- Simple HTTP action as proof of concept
- Schema generation functional

**Tasks:**
1. Create Actions module structure
2. Implement `Resolvable<T>` type
3. Build attribute system
4. Implement discovery service
5. Build schema generation service
6. Create HTTP action provider (simple)
7. Add API endpoints for schemas
8. Unit tests for core infrastructure

**Deliverables:**
- Backend can discover and expose action schemas
- HTTP action executes successfully
- Schemas returned via API

#### Phase 2: Expression Engine (Weeks 4-5)

**Goals:**
- Scriban integration complete
- Expression context working
- Variables resolve in action parameters

**Tasks:**
1. Integrate Scriban NuGet package
2. Implement `IExpressionEngine`
3. Build expression context provider
4. Implement parameter resolver service
5. Add expression evaluation to execution flow
6. Security hardening (strict mode, timeouts)
7. Unit tests for expression evaluation

**Deliverables:**
- Variables work in action parameters
- Expression errors handled gracefully
- Security constraints enforced

#### Phase 3: Basic Action Nodes (Weeks 6-8)

**Goals:**
- 5-10 useful action nodes implemented
- Cover major categories

**Action Nodes:**
1. HTTP Request
2. Set Variable
3. Delay/Wait
4. Conditional (If/Else)
5. Database Query (basic)
6. Email Send
7. JSON Transform
8. String Operations
9. Date/Time Operations
10. Loop Container (optional)

**Tasks:**
- Implement parameter classes for each
- Implement provider classes for each
- Add comprehensive documentation
- Unit and integration tests

**Deliverables:**
- Production-ready action nodes
- Documentation for each action
- Examples and test workflows

#### Phase 4: Frontend Integration (Weeks 9-11)

**Goals:**
- Action nodes appear in palette
- Properties panel renders dynamically
- Variable picker functional

**Tasks:**
1. Update frontend to fetch schemas
2. Build dynamic node palette
3. Implement dynamic properties panel
4. Create variable picker component
5. Add validation UI feedback
6. Implement action execution trigger
7. Display execution results
8. Error handling and display

**Deliverables:**
- Complete UI for action nodes
- User can build workflows with actions
- Execution results visible

#### Phase 5: Credential System (Weeks 12-13)

**Goals:**
- Secure credential storage
- Credential types supported
- Actions can use credentials

**Tasks:**
1. Design credential storage schema
2. Implement encryption service
3. Build credential CRUD APIs
4. Add credential UI components
5. Implement credential accessor service
6. Update action providers to use credentials
7. Add OAuth flow support (if needed)

**Deliverables:**
- Users can store credentials securely
- Actions access credentials safely
- OAuth integrations work

#### Phase 6: Advanced Features (Weeks 14-16)

**Goals:**
- Dynamic options loading
- Error handling improvements
- Performance optimizations

**Tasks:**
1. Implement options loader system
2. Add retry logic to execution
3. Implement error workflows
4. Add execution telemetry
5. Performance profiling and optimization
6. Add caching where appropriate
7. Documentation updates

**Deliverables:**
- Polished, production-ready system
- Performance meets requirements
- Comprehensive documentation

---

## Part 5: Technical Recommendations

### 5.1 Technology Choices

#### Expression Engine: Scriban

**Recommendation:** Start with Scriban only

**Rationale:**
- **Security**: Safer than full JavaScript
- **Performance**: Faster than Jint for simple operations
- **Simplicity**: Easier to learn and debug
- **Sufficient**: Handles 90% of use cases

**Later Addition:** Add Jint for advanced conditional logic if needed

#### Dependency Injection

**Recommendation:** Use built-in ASP.NET Core DI

**Rationale:**
- Already part of the stack
- Well-documented and tested
- Supports scoped services for execution context
- Easy to mock for testing

#### Serialization

**Recommendation:** System.Text.Json

**Rationale:**
- Built-in, no additional dependencies
- High performance
- Attribute-based configuration
- Source generators for optimization

#### Validation

**Recommendation:** DataAnnotations + FluentValidation (optional)

**Rationale:**
- DataAnnotations sufficient for basic validation
- FluentValidation for complex rules if needed
- Familiar to .NET developers

### 5.2 Security Considerations

#### Expression Evaluation Security

```csharp
var templateContext = new TemplateContext
{
    // Strict mode - no undefined variable access
    StrictVariables = true,

    // No relaxed access - explicit permission required
    EnableRelaxedMemberAccess = false,
    EnableRelaxedFunctionAccess = false,

    // Limit recursion depth
    RecursionLimit = 100,

    // Timeout for long-running templates
    Timeout = TimeSpan.FromSeconds(5)
};
```

#### Credential Security

1. **Encryption at Rest**: Use ASP.NET Core Data Protection
2. **Scoped Access**: Only execution context sees credentials
3. **Audit Logging**: Log all credential access
4. **Rotation Support**: Allow credential updates without workflow changes
5. **Type Validation**: Ensure action gets correct credential type

#### Input Validation

1. **Multi-Stage**: Validate at design-time and runtime
2. **Type Safety**: Use strong typing with Resolvable<T>
3. **Sanitization**: Escape user input where needed
4. **Rate Limiting**: Prevent abuse of action execution

#### Sandbox Considerations

For future: Consider sandboxing action execution:
- Run in separate AppDomain or process
- Resource limits (CPU, memory, time)
- Network restrictions
- File system restrictions

### 5.3 Performance Optimizations

#### 1. Schema Caching

```csharp
public class CachedActionSchemaService : IActionSchemaService
{
    private readonly IActionSchemaService inner;
    private readonly IMemoryCache cache;

    public async Task<IEnumerable<ActionNodeSchema>> GetAllActionSchemasAsync()
    {
        return await cache.GetOrCreateAsync("all-schemas", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return await inner.GetAllActionSchemasAsync();
        });
    }
}
```

#### 2. Compiled Delegates

Cache compiled method invocations:

```csharp
private static readonly ConcurrentDictionary<MethodInfo, Func<object, object[], Task<object>>>
    CompiledMethods = new();

private async Task<object> InvokeMethodAsync(
    MethodInfo method,
    object instance,
    object[] parameters)
{
    var compiled = CompiledMethods.GetOrAdd(method, CompileMethod);
    return await compiled(instance, parameters);
}
```

#### 3. Expression Parsing Cache

Cache parsed Scriban templates:

```csharp
private static readonly ConcurrentDictionary<string, Template>
    TemplateCache = new();

private Template GetParsedTemplate(string template)
{
    return TemplateCache.GetOrAdd(template, t => Template.Parse(t));
}
```

#### 4. Parallel Execution

Execute independent action nodes in parallel:

```csharp
var independentNodes = GetNodesWithoutDependencies();
var tasks = independentNodes.Select(node => ExecuteActionAsync(node));
await Task.WhenAll(tasks);
```

### 5.4 Testing Strategy

#### Unit Tests

```csharp
[Fact]
public void HttpRequestParameters_ValidUrl_PassesValidation()
{
    var parameters = new HttpRequestParameters
    {
        Method = HttpMethod.GET,
        Url = "https://api.example.com"
    };

    var (valid, errors) = parameters.IsValid();

    Assert.True(valid);
    Assert.Empty(errors);
}

[Fact]
public async Task HttpActionProvider_ExecutesRequest_ReturnsResponse()
{
    var provider = new HttpActionProvider(
        httpClientFactory,
        expressionEngine,
        credentialService,
        logger);

    var result = await provider.ExecuteAsync(
        new HttpRequestParameters
        {
            Method = HttpMethod.GET,
            Url = "https://api.example.com"
        });

    Assert.NotNull(result);
    Assert.True(result.IsSuccess);
}
```

#### Integration Tests

```csharp
[Fact]
public async Task ActionExecution_WithVariables_ResolvesCorrectly()
{
    // Arrange
    var context = new ExpressionContext
    {
        Variables = new Dictionary<string, object>
        {
            ["apiUrl"] = "https://api.example.com"
        }
    };

    var request = new ActionExecutionRequest
    {
        ActionType = "http_request",
        Parameters = new Dictionary<string, object>
        {
            ["Url"] = "{{Variables.apiUrl}}/endpoint"
        },
        Context = context
    };

    // Act
    var result = await executionService.ExecuteActionAsync(request);

    // Assert
    Assert.True(result.Success);
}
```

#### Expression Engine Tests

```csharp
[Theory]
[InlineData("{{Variables.name}}", "John")]
[InlineData("Hello {{User.Name}}", "Hello Alice")]
[InlineData("{{Nodes.HttpRequest.statusCode}}", "200")]
public void ExpressionEngine_EvaluatesTemplates_Correctly(
    string template,
    string expected)
{
    var result = expressionEngine.EvaluateTemplate(template);
    Assert.Equal(expected, result);
}
```

---

## Part 6: Code Examples

### 6.1 Example Action Node Definitions

#### Example 1: Database Query Action

```csharp
[ActionNode(
    actionType: "database_query",
    category: "Data",
    group: "Database",
    Icon = "database",
    Description = "Execute SQL query against a database")]
public class DatabaseQueryParameters : BaseActionParameters
{
    [Required]
    [CredentialMapping(["postgres", "mysql", "sqlserver"])]
    public Guid CredentialId { get; set; }

    [Required]
    [Display(Name = "Query", Description = "SQL query to execute")]
    [EditorType(EditorType.Code)]
    [SupportVariables]
    public string Query { get; set; } = string.Empty;

    [Display(Name = "Timeout (seconds)")]
    [Range(1, 300)]
    [DefaultValue(30)]
    public Resolvable<int> TimeoutSeconds { get; set; } = 30;

    [Display(Name = "Parameters")]
    public Dictionary<string, string>? QueryParameters { get; set; }

    public override (bool valid, List<ValidationResult> results) IsValid()
    {
        return ValidateDataAnnotations();
    }
}

[ActionProvider]
public class DatabaseActionProvider
{
    private readonly IDbConnectionFactory connectionFactory;
    private readonly IExpressionEngine expressionEngine;
    private readonly ICredentialService credentialService;

    [ActionMethod("database_query")]
    public async Task<DatabaseQueryOutput> ExecuteQueryAsync(
        DatabaseQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var credential = await credentialService
            .GetCredentialAsync(parameters.CredentialId);

        var query = expressionEngine.EvaluateTemplate(parameters.Query);

        using var connection = connectionFactory.CreateConnection(credential);
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = (int)parameters.TimeoutSeconds;

        if (parameters.QueryParameters != null)
        {
            foreach (var (key, value) in parameters.QueryParameters)
            {
                var resolvedValue = expressionEngine.EvaluateTemplate(value);
                command.Parameters.Add(
                    new DbParameter(key, resolvedValue));
            }
        }

        var results = new List<Dictionary<string, object>>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.GetValue(i);
            }
            results.Add(row);
        }

        return new DatabaseQueryOutput
        {
            Rows = results,
            RowCount = results.Count
        };
    }
}
```

#### Example 2: Conditional Action

```csharp
[ActionNode(
    actionType: "conditional",
    category: "Flow Control",
    MaxInputs = 1,
    MaxOutputs = 2, // True path and False path
    Icon = "split",
    Description = "Branch workflow based on a condition")]
public class ConditionalParameters : BaseActionParameters
{
    [Required]
    [Display(Name = "Condition", Description = "Boolean expression to evaluate")]
    [SupportVariables]
    public string Condition { get; set; } = string.Empty;

    public override (bool valid, List<ValidationResult> results) IsValid()
    {
        return ValidateDataAnnotations();
    }
}

[ActionProvider]
public class ConditionalActionProvider
{
    private readonly IExpressionEngine expressionEngine;

    [ActionMethod("conditional")]
    public async Task<ConditionalOutput> EvaluateAsync(
        ConditionalParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = expressionEngine.EvaluateTemplate(parameters.Condition);
        var isTrue = bool.TryParse(result, out var boolValue) && boolValue;

        return await Task.FromResult(new ConditionalOutput
        {
            Result = isTrue,
            OutputPath = isTrue ? "true" : "false"
        });
    }
}
```

### 6.2 Example Execution Flow

```csharp
// Workflow: Fetch user data, check if admin, send notification

// Step 1: HTTP Request to get user
var step1 = new ActionExecutionRequest
{
    ActionType = "http_request",
    ActionNodeId = "fetch-user",
    Parameters = new Dictionary<string, object>
    {
        ["Method"] = "GET",
        ["Url"] = "https://api.example.com/users/{{Variables.userId}}"
    }
};

var result1 = await executionService.ExecuteActionAsync(step1);
// Output: { "id": 123, "name": "John", "role": "admin" }

// Step 2: Check if admin
context.Nodes["fetch-user"] = result1.Output;

var step2 = new ActionExecutionRequest
{
    ActionType = "conditional",
    ActionNodeId = "check-admin",
    Parameters = new Dictionary<string, object>
    {
        ["Condition"] = "{{Nodes.fetch-user.role}} == 'admin'"
    },
    Context = context
};

var result2 = await executionService.ExecuteActionAsync(step2);
// Output: { "result": true, "outputPath": "true" }

// Step 3: Send notification (only if admin)
if (((ConditionalOutput)result2.Output!).Result)
{
    var step3 = new ActionExecutionRequest
    {
        ActionType = "send_email",
        ActionNodeId = "notify-admin",
        Parameters = new Dictionary<string, object>
        {
            ["To"] = "admin@example.com",
            ["Subject"] = "Admin Login Detected",
            ["Body"] = "User {{Nodes.fetch-user.name}} logged in as admin"
        },
        Context = context
    };

    await executionService.ExecuteActionAsync(step3);
}
```

### 6.3 Example Provider Implementation

```csharp
// Complete provider with all best practices

[ActionProvider]
public class SlackActionProvider
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IExpressionEngine expressionEngine;
    private readonly ICredentialService credentialService;
    private readonly ILogger<SlackActionProvider> logger;

    public SlackActionProvider(
        IHttpClientFactory httpClientFactory,
        IExpressionEngine expressionEngine,
        ICredentialService credentialService,
        ILogger<SlackActionProvider> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.expressionEngine = expressionEngine;
        this.credentialService = credentialService;
        this.logger = logger;
    }

    [ActionMethod("slack_send_message")]
    [SupportedCredentials("slack_oauth", "slack_webhook")]
    [Description("Send a message to a Slack channel")]
    public async Task<SlackMessageOutput> SendMessageAsync(
        SlackMessageParameters parameters,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Sending Slack message to channel {Channel}",
            parameters.Channel);

        try
        {
            // Resolve template variables
            var channel = expressionEngine.EvaluateTemplate(parameters.Channel);
            var message = expressionEngine.EvaluateTemplate(parameters.Message);

            // Get credentials
            var credential = await credentialService
                .GetCredentialAsync(parameters.CredentialId);

            // Create HTTP client
            var client = httpClientFactory.CreateClient("Slack");

            // Build request
            var request = new HttpRequestMessage(
                HttpMethod.POST,
                "https://slack.com/api/chat.postMessage");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", credential.AccessToken);

            request.Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    channel = channel,
                    text = message,
                    username = parameters.Username,
                    icon_emoji = parameters.IconEmoji
                }),
                Encoding.UTF8,
                "application/json");

            // Execute request
            var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content
                .ReadAsStringAsync(cancellationToken);

            // Parse response
            var slackResponse = JsonSerializer
                .Deserialize<SlackApiResponse>(responseBody);

            if (slackResponse?.Ok == true)
            {
                logger.LogInformation(
                    "Successfully sent message to {Channel}",
                    channel);

                return new SlackMessageOutput
                {
                    Success = true,
                    MessageId = slackResponse.Ts,
                    Channel = channel
                };
            }
            else
            {
                logger.LogWarning(
                    "Slack API returned error: {Error}",
                    slackResponse?.Error);

                return new SlackMessageOutput
                {
                    Success = false,
                    ErrorMessage = slackResponse?.Error ?? "Unknown error"
                };
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send Slack message");

            return new SlackMessageOutput
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
```

---

## Appendices

### Appendix A: Reference Implementation Code Patterns

#### Pattern 1: Attribute Composition

```csharp
// Multiple attributes combine to define behavior
[Required]
[Range(1, 100)]
[DefaultValue(10)]
[JsonConverter(typeof(ResolvableJsonConverter<int>))]
public Resolvable<int> RetryCount { get; set; } = 10;
```

#### Pattern 2: Provider Discovery

```csharp
// Scan assemblies for providers
var providers = AppDomain.CurrentDomain.GetAssemblies()
    .SelectMany(a => a.GetTypes())
    .Where(t => t.GetCustomAttribute<SdkProviderTypeAttribute>() != null)
    .ToList();

// Register providers
foreach (var provider in providers)
{
    services.AddScoped(provider);
}
```

#### Pattern 3: Credential Injection

```csharp
// Provider accesses credentials through service
var credentials = credentialAccessor.GetCredentials(parameters.CredentialId);
var accessToken = credentials
    .FirstOrDefault(c => c.ContainsKey(CredentialKeyField.AccessToken))
    ?.GetValue(CredentialKeyField.AccessToken);
```

#### Pattern 4: Context Scoping

```csharp
// Expression context scoped to execution
services.AddScoped<IExpressionContextProvider, PipelineContextProvider>();
services.AddScoped<IExpressionEngine, ScribanExpressionEngine>();

// Provider receives scoped context
public HttpProvider(IExpressionEngine engine) // engine has scoped context
{
    this.engine = engine;
}
```

### Appendix B: n8n References

**Official Documentation:**
- [n8n Expressions](https://docs.n8n.io/code/expressions/)
- [Standard Parameters](https://docs.n8n.io/integrations/creating-nodes/build/reference/node-base-files/standard-parameters/)
- [Node UI Elements](https://docs.n8n.io/integrations/creating-nodes/build/reference/ui-elements/)
- [Credentials Files](https://docs.n8n.io/integrations/creating-nodes/build/reference/credentials-files/)
- [HTTP Request Node](https://docs.n8n.io/integrations/builtin/core-nodes/n8n-nodes-base.httprequest/)

**Community Resources:**
- [n8n Guide 2026](https://hatchworks.com/blog/ai-agents/n8n-guide/)
- [n8n Expressions Cheat Sheet](https://n8narena.com/guides/n8n-expression-cheatsheet/)
- [n8n Credentials Explained](https://automategeniushub.com/guide-to-n8n-credentials/)

### Appendix C: Glossary

**Action Node**: A reusable workflow component that performs a specific operation

**Provider**: A class containing the executable logic for action nodes

**Resolvable<T>**: A generic type that accepts either literal values or expressions

**Expression Engine**: Service that evaluates template strings with variable substitution

**Parameter Schema**: JSON description of action node parameters for UI generation

**Discovery Service**: Service that finds and registers action providers using reflection

**Execution Context**: Runtime environment containing variables, user info, and node outputs

**Credential Accessor**: Service providing secure access to stored credentials

**Template Evaluation**: Process of replacing variable placeholders with actual values

**Scriban**: Lightweight templating engine for .NET using Handlebars-like syntax

**Attribute-Driven**: Architecture where metadata comes from C# attributes

**Validation Pipeline**: Multi-stage validation at design-time and runtime

---

## Conclusion

The research reveals that an attribute-driven, type-safe approach using C# attributes combined with the Resolvable<T> pattern provides the optimal foundation for DonkeyWork-Agents' ActionNodes system. The reference implementation demonstrates elegant patterns for provider discovery, parameter resolution, and expression evaluation.

Key takeaways:
1. **Attributes eliminate schema files** - Single source of truth
2. **Resolvable<T> provides flexibility** - Literals or expressions
3. **Scriban is sufficient initially** - Add Jint only if needed
4. **Discovery via reflection** - Automatic registration
5. **Type safety with runtime flexibility** - Best of both worlds

By following the proposed architecture and implementation roadmap, DonkeyWork-Agents can build a robust, extensible ActionNodes system that rivals commercial offerings while maintaining the type safety and performance benefits of the .NET ecosystem.

---

**Document Version:** 1.0
**Last Updated:** 2026-01-23
**Author:** Research Assistant
**Total Words:** ~15,000
