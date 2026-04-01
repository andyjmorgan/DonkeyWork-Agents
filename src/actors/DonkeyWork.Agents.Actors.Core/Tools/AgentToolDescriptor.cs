using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using DonkeyWork.Agents.Actors.Core.Providers;
using DonkeyWork.Agents.Identity.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DonkeyWork.Agents.Actors.Core.Tools;

internal sealed class AgentToolDescriptor
{
    public required string Name { get; init; }

    public string? DisplayName { get; init; }

    public required string Description { get; init; }

    public required ParameterDescriptor[] Parameters { get; init; }

    public required MethodInfo Method { get; init; }

    public required Type DeclaringType { get; init; }

    public InternalToolDefinition ToToolDefinition()
    {
        var properties = new Dictionary<string, object>();
        var required = new List<string>();

        foreach (var param in Parameters)
        {
            var prop = new Dictionary<string, object> { ["type"] = param.JsonType };
            if (param.Description is not null)
            {
                prop["description"] = param.Description;
            }

            if (param.AllowedValues is not null)
            {
                prop["enum"] = param.AllowedValues;
            }

            properties[param.Name] = prop;

            if (param.IsRequired)
            {
                required.Add(param.Name);
            }
        }

        var schema = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = properties,
        };

        if (required.Count > 0)
        {
            schema["required"] = required;
        }

        return new InternalToolDefinition
        {
            Name = Name,
            DisplayName = DisplayName,
            Description = Description,
            InputSchema = schema,
        };
    }

    public async Task<ToolResult> ExecuteAsync(
        JsonElement input,
        GrainContext context,
        IIdentityContext identityContext,
        IServiceProvider serviceProvider,
        CancellationToken ct)
    {
        var methodParams = Method.GetParameters();
        var args = new object?[methodParams.Length];

        for (var i = 0; i < methodParams.Length; i++)
        {
            var mp = methodParams[i];

            if (mp.ParameterType == typeof(GrainContext))
            {
                args[i] = context;
                continue;
            }

            if (mp.ParameterType == typeof(IIdentityContext))
            {
                args[i] = identityContext;
                continue;
            }

            if (mp.ParameterType == typeof(CancellationToken))
            {
                args[i] = ct;
                continue;
            }

            var descriptor = Array.Find(Parameters, p =>
                string.Equals(p.Name, mp.Name, StringComparison.OrdinalIgnoreCase));

            if (descriptor is null)
            {
                args[i] = mp.HasDefaultValue ? mp.DefaultValue : null;
                continue;
            }

            if (input.TryGetProperty(descriptor.Name, out var jsonValue)
                && jsonValue.ValueKind != JsonValueKind.Null)
            {
                args[i] = DeserializeParam(jsonValue, mp.ParameterType);
            }
            else if (mp.HasDefaultValue)
            {
                args[i] = mp.DefaultValue;
            }
            else
            {
                args[i] = null;
            }
        }

        using var scope = serviceProvider.CreateScope();
        var scopedIdentity = scope.ServiceProvider.GetService<IIdentityContext>();
        scopedIdentity?.SetIdentity(identityContext.UserId, identityContext.Email, identityContext.Name, identityContext.Username);
        var instance = ActivatorUtilities.CreateInstance(scope.ServiceProvider, DeclaringType);
        var result = Method.Invoke(instance, args);

        if (result is Task<ToolResult> taskResult)
        {
            return await taskResult;
        }

        if (result is ToolResult syncResult)
        {
            return syncResult;
        }

        return ToolResult.Success(result?.ToString() ?? string.Empty);
    }

    public static IReadOnlyList<AgentToolDescriptor> FromType(Type type)
    {
        var descriptors = new List<AgentToolDescriptor>();

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
        {
            var attr = method.GetCustomAttribute<AgentToolAttribute>();
            if (attr is null)
            {
                continue;
            }

            var toolName = attr.Name ?? method.Name;
            var description = method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? toolName;
            var parameters = BuildParameterDescriptors(method);

            descriptors.Add(new AgentToolDescriptor
            {
                Name = toolName,
                DisplayName = attr.DisplayName,
                Description = description,
                Parameters = parameters,
                Method = method,
                DeclaringType = type,
            });
        }

        return descriptors;
    }

    private static ParameterDescriptor[] BuildParameterDescriptors(MethodInfo method)
    {
        var descriptors = new List<ParameterDescriptor>();

        foreach (var param in method.GetParameters())
        {
            if (param.ParameterType == typeof(GrainContext)
                || param.ParameterType == typeof(IIdentityContext)
                || param.ParameterType == typeof(CancellationToken))
            {
                continue;
            }

            var description = param.GetCustomAttribute<DescriptionAttribute>()?.Description;
            var isRequired = !param.HasDefaultValue
                             && Nullable.GetUnderlyingType(param.ParameterType) is null;

            string[]? allowedValues = null;
            var enumType = Nullable.GetUnderlyingType(param.ParameterType) ?? param.ParameterType;
            if (enumType.IsEnum)
            {
                allowedValues = Enum.GetNames(enumType);
            }

            descriptors.Add(new ParameterDescriptor
            {
                Name = param.Name!,
                ClrType = param.ParameterType,
                JsonType = MapClrTypeToJsonType(param.ParameterType),
                Description = description,
                AllowedValues = allowedValues,
                IsRequired = isRequired,
            });
        }

        return descriptors.ToArray();
    }

    private static string MapClrTypeToJsonType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(string) || underlying == typeof(Guid))
        {
            return "string";
        }

        if (underlying == typeof(bool))
        {
            return "boolean";
        }

        if (underlying == typeof(int)
            || underlying == typeof(long)
            || underlying == typeof(double)
            || underlying == typeof(float)
            || underlying == typeof(decimal))
        {
            return "number";
        }

        if (underlying.IsEnum)
        {
            return "string";
        }

        if (underlying.IsArray || (underlying.IsGenericType
            && underlying.GetGenericTypeDefinition() == typeof(List<>)))
        {
            return "array";
        }

        return "object";
    }

    private static object? DeserializeParam(JsonElement element, Type targetType)
    {
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlying.IsEnum)
        {
            var str = element.GetString();
            return str is not null ? Enum.Parse(underlying, str, ignoreCase: true) : null;
        }

        return element.Deserialize(targetType);
    }
}
