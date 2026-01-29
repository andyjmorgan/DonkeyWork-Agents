using DonkeyWork.Agents.Agents.Contracts.Services;

namespace DonkeyWork.Agents.Agents.Core.Execution;

/// <summary>
/// Extension methods for IExecutionContext.
/// </summary>
public static class ExecutionContextExtensions
{
    /// <summary>
    /// Convert IExecutionContext to a Scriban template context object.
    /// This makes execution state available in expressions like {{steps.step1.result}}.
    /// </summary>
    /// <param name="context">The execution context</param>
    /// <returns>An anonymous object with execution state for Scriban templates</returns>
    public static object ToScribanContext(this IExecutionContext context)
    {
        return new
        {
            // Previous node outputs (accessible as {{Steps.nodeName.property}})
            Steps = context.NodeOutputs,
            steps = context.NodeOutputs, // lowercase alias for compatibility

            // Input provided to the execution (accessible as {{Input.property}})
            Input = context.Input,
            input = context.Input, // lowercase alias for compatibility

            // Variables as an alias for input (accessible as {{Variables.property}})
            // This provides compatibility with common expression patterns
            Variables = context.Input,

            // Execution metadata
            ExecutionId = context.ExecutionId,
            executionId = context.ExecutionId, // camelCase alias
            execution_id = context.ExecutionId, // snake_case alias

            UserId = context.UserId,
            userId = context.UserId, // camelCase alias
            user_id = context.UserId // snake_case alias
        };
    }
}
