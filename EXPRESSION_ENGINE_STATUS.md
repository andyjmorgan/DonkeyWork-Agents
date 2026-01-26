# Expression Engine Status

## ✅ What's Implemented

### Core Infrastructure

1. **Expression Engine (Scriban-based)** ✅
   - `IExpressionEngine` interface defined
   - `ScribanExpressionEngine` implementation complete
   - Supports template evaluation: `{{Variables.name}}`
   - Type conversion for common types (int, bool, decimal, string, enum)
   - Error handling with clear messages

2. **Parameter Resolver** ✅
   - `IParameterResolver` interface defined
   - `ParameterResolverService` implementation complete
   - Resolves `Resolvable<T>` types (literal or expression)
   - Detects expression syntax with `{{...}}`
   - Parses literal values with type conversion

3. **Resolvable<T> Type** ✅
   - Generic wrapper for parameters that can be literals or expressions
   - Distinguishes between literal values and expressions
   - JSON serialization support
   - Implicit conversions from T and string
   - `IsExpression` and `IsPureExpression` detection

4. **Execution Context** ✅
   - `ExecutionContext` class in Agents module
   - Tracks:
     - `ExecutionId` - unique run identifier
     - `NodeOutputs` - dictionary of step results (accessible as `steps` in Scriban)
     - `Input` - initial execution input
     - `UserId` - execution owner
     - `InputSchema` - JSON schema for validation

### Test Coverage

**44 tests passing** in Actions module:
- ✅ Resolvable<T> construction and conversion
- ✅ Expression detection (IsExpression, IsPureExpression)
- ✅ Type conversion (string, int, bool, decimal)
- ✅ HTTP Action Provider with parameter resolution
- ✅ Schema generation from attributes

## 🔨 What's In Place (Core Components)

### Expression Syntax

```csharp
// Literal values
var timeout = new Resolvable<int>(30);              // "30"
var url = new Resolvable<string>("https://api.com"); // "https://api.com"

// Expression values
var timeout = new Resolvable<int>("{{Variables.timeout}}");
var url = new Resolvable<string>("https://{{Variables.domain}}/api");
var message = new Resolvable<string>("Hello {{steps.step1.name}}!");
```

### Resolution Flow

```csharp
// In action providers
public async Task<object> ExecuteAsync(HttpRequestParameters parameters)
{
    // Resolver unwraps Resolvable<T> using context
    var timeout = _parameterResolver.Resolve(parameters.TimeoutSeconds, context);
    var url = _parameterResolver.ResolveString(parameters.Url, context);

    // Use resolved values
    httpClient.Timeout = TimeSpan.FromSeconds(timeout);
    await httpClient.GetAsync(url);
}
```

### Context Structure

When actions execute, they receive context like:

```csharp
var context = new
{
    steps = new Dictionary<string, object>
    {
        ["step1"] = new { name = "John", age = 30 },
        ["step2"] = new { result = "success" }
    },
    input = executionInput,
    executionId = Guid.NewGuid(),
    userId = currentUserId
};
```

Then in templates:

```
URL: https://api.example.com/users/{{steps.step1.name}}
Message: Previous step returned: {{steps.step2.result}}
Timeout: {{input.timeout}}
```

## ❌ What's NOT Implemented

### 1. Action Executor/Dispatcher ❌

**Missing**: Service to discover and execute action providers

```csharp
// Needs to be created:
public interface IActionExecutor
{
    Task<object> ExecuteAsync(
        string actionType,
        Dictionary<string, object> parameters,
        ExecutionContext context);
}
```

**What it should do**:
- Scan assemblies for `[ActionProvider]` classes
- Maintain registry of action type → provider
- Instantiate providers with DI
- Call `ExecuteAsync` with resolved parameters
- Handle errors and return results

### 2. Action Node Executor ❌

**Missing**: Node executor for action nodes in workflow engine

```csharp
// Needs to be created:
public class ActionNodeExecutor : INodeExecutor
{
    private readonly IActionExecutor _actionExecutor;

    public async Task ExecuteAsync(NodeConfig config, ExecutionContext context)
    {
        var actionConfig = (ActionNodeConfig)config;

        // Execute the action with context
        var result = await _actionExecutor.ExecuteAsync(
            actionConfig.ActionType,
            actionConfig.Parameters,
            context);

        // Store result in context for downstream steps
        context.NodeOutputs[actionConfig.Name] = result;
    }
}
```

### 3. Dynamic Context Building ❌

**Missing**: Logic to build Scriban context from ExecutionContext

```csharp
// Needs to be created:
public static class ExecutionContextExtensions
{
    public static object ToScribanContext(this ExecutionContext context)
    {
        return new
        {
            steps = context.NodeOutputs,
            input = context.Input,
            execution_id = context.ExecutionId,
            user_id = context.UserId
        };
    }
}
```

### 4. Expression Variables UI ❌

**Missing**: Frontend variable picker/helper

- No UI to browse available variables
- No autocomplete for `{{steps.xxx}}`
- No validation of expression syntax
- No preview of resolved values

### 5. Action Provider Registration ❌

**Missing**: DI registration of action providers

```csharp
// Needs to be added to DependencyInjection.cs:
public static IServiceCollection AddActionProviders(this IServiceCollection services)
{
    // Scan for [ActionProvider] classes
    var assembly = typeof(HttpActionProvider).Assembly;
    var providerTypes = assembly.GetTypes()
        .Where(t => t.GetCustomAttribute<ActionProviderAttribute>() != null);

    // Register each provider
    foreach (var providerType in providerTypes)
    {
        services.AddScoped(providerType);
    }

    return services;
}
```

## 🎯 Ready for Use

### What You Can Do NOW

1. **Write Action Providers** ✅
   ```csharp
   [ActionProvider("my_action")]
   public class MyActionProvider
   {
       private readonly IParameterResolver _resolver;

       public async Task<object> ExecuteAsync(
           MyActionParameters parameters,
           object? context = null)
       {
           // Resolve parameters with expressions
           var value = _resolver.Resolve(parameters.MyValue, context);
           var text = _resolver.ResolveString(parameters.MyText, context);

           // Execute action
           return new { processed = true };
       }
   }
   ```

2. **Define Parameters with Resolvable<T>** ✅
   ```csharp
   public class MyActionParameters : BaseActionParameters
   {
       [Parameter("value", "Value", Required = true)]
       public Resolvable<int> MyValue { get; set; } = 0;

       [Parameter("text", "Text", SupportsVariables = true)]
       public Resolvable<string> MyText { get; set; } = string.Empty;
   }
   ```

3. **Test Expression Resolution** ✅
   ```csharp
   var engine = new ScribanExpressionEngine();
   var context = new { name = "John", age = 30 };

   var result = engine.Evaluate("Hello {{name}}, you are {{age}} years old", context);
   // Returns: "Hello John, you are 30 years old"
   ```

### What You CANNOT Do Yet

1. ❌ Execute action nodes in workflows (no ActionNodeExecutor)
2. ❌ Auto-discover action providers (no registration)
3. ❌ Reference previous step outputs in expressions (no context building)
4. ❌ Browse available variables in UI (no variable picker)

## 📋 To Complete Phase 4 (Execution)

### Step 1: Action Provider Discovery & Registration
- Assembly scanning for `[ActionProvider]` classes
- DI registration of providers
- Provider registry (action type → provider instance)

### Step 2: Action Executor Service
- `IActionExecutor` interface
- `ActionExecutorService` implementation
- Execute action by type with parameters and context

### Step 3: Action Node Executor
- `ActionNodeExecutor` class implementing `INodeExecutor`
- Integrate with workflow orchestrator
- Store action results in ExecutionContext.NodeOutputs

### Step 4: Context Building
- Helper to convert ExecutionContext → Scriban context
- Make `steps`, `input`, `execution_id`, `user_id` available in expressions

### Step 5: Frontend Integration (Optional)
- Variable picker component
- Expression autocomplete
- Syntax validation
- Value preview

## 🎨 Example Flow (When Complete)

### Workflow Definition

```
Start Node (input: { url: "api.example.com", timeout: 30 })
   ↓
HTTP Request Node (name: "fetch_user")
   - URL: "https://{{input.url}}/users/123"
   - Timeout: {{input.timeout}}
   ↓
Log Node (name: "log_result")
   - Message: "Fetched user: {{steps.fetch_user.body}}"
   ↓
End Node
```

### Execution Flow

1. **Start Node** executes
   - Context.Input = `{ url: "api.example.com", timeout: 30 }`

2. **HTTP Request Node** executes
   - Parameters: `{ Url: "https://{{input.url}}/users/123", TimeoutSeconds: "{{input.timeout}}" }`
   - ParameterResolver resolves:
     - `Url` → `"https://api.example.com/users/123"`
     - `TimeoutSeconds` → `30`
   - HttpActionProvider makes request
   - Result stored: `context.NodeOutputs["fetch_user"] = { statusCode: 200, body: "{...}" }`

3. **Log Node** executes
   - Parameter: `{ Message: "Fetched user: {{steps.fetch_user.body}}" }`
   - ParameterResolver resolves:
     - `Message` → `"Fetched user: {...}"`
   - LogActionProvider logs message

4. **End Node** executes
   - Workflow completes

## 📚 Key Files

**Expression Engine:**
- `src/actions/DonkeyWork.Agents.Actions.Contracts/Services/IExpressionEngine.cs`
- `src/actions/DonkeyWork.Agents.Actions.Core/Services/ScribanExpressionEngine.cs`

**Parameter Resolution:**
- `src/actions/DonkeyWork.Agents.Actions.Contracts/Services/IParameterResolver.cs`
- `src/actions/DonkeyWork.Agents.Actions.Core/Services/ParameterResolverService.cs`
- `src/actions/DonkeyWork.Agents.Actions.Contracts/Types/Resolvable.cs`

**Execution Context:**
- `src/agents/DonkeyWork.Agents.Agents.Core/Execution/ExecutionContext.cs`

**Tests:**
- `test/actions/DonkeyWork.Agents.Actions.Core.Tests/Types/ResolvableTests.cs` (14 tests)
- `test/actions/DonkeyWork.Agents.Actions.Core.Tests/Providers/HttpActionProviderTests.cs` (10 tests)
- `test/actions/DonkeyWork.Agents.Actions.Core.Tests/Services/ActionSchemaServiceTests.cs` (20 tests)

---

## Summary

**Expression Engine**: ✅ Complete and tested
**Parameter Resolution**: ✅ Complete and tested
**Execution Context**: ✅ Defined
**Action Executor**: ❌ Not implemented
**Integration**: ❌ Not wired up to workflow orchestrator

**To enable expression-based parameters in workflows, you need to implement Phase 4 (Execution Infrastructure).**
