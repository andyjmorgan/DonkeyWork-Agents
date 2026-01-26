namespace DonkeyWork.Agents.Agents.Core.Execution;

/// <summary>
/// Extension methods for ExecutionContext.
/// </summary>
public static class ExecutionContextExtensions
{
    /// <summary>
    /// Convert ExecutionContext to a Scriban template context object.
    /// This makes execution state available in expressions like {{steps.step1.result}}.
    /// </summary>
    /// <param name="context">The execution context</param>
    /// <returns>An anonymous object with execution state for Scriban templates</returns>
    public static object ToScribanContext(this ExecutionContext context)
    {
        return new
        {
            // Previous node outputs (accessible as {{steps.nodeName.property}})
            steps = context.NodeOutputs,

            // Input provided to the execution (accessible as {{input.property}})
            input = context.Input,

            // Variables as an alias for input (accessible as {{Variables.property}})
            // This provides compatibility with common expression patterns
            Variables = context.Input,

            // Execution metadata
            execution_id = context.ExecutionId,
            executionId = context.ExecutionId, // CamelCase alias

            user_id = context.UserId,
            userId = context.UserId // CamelCase alias
        };
    }
}
