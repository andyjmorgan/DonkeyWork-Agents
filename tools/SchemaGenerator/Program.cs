using System.Reflection;
using DonkeyWork.Agents.Actions.Core.Services;

namespace DonkeyWork.Agents.Tools.SchemaGenerator;

/// <summary>
/// Build-time tool to generate action node schemas from C# attributes
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        try
        {
            Console.WriteLine("DonkeyWork Actions Schema Generator");
            Console.WriteLine("===================================");
            Console.WriteLine();

            // Parse arguments
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: SchemaGenerator <assembly-path> <output-path>");
                Console.WriteLine();
                Console.WriteLine("Example:");
                Console.WriteLine("  SchemaGenerator Actions.Core.dll src/frontend/src/schemas/actions.json");
                return 1;
            }

            var assemblyPath = args[0];
            var outputPath = args[1];

            // Validate assembly exists
            if (!File.Exists(assemblyPath))
            {
                Console.WriteLine($"ERROR: Assembly not found: {assemblyPath}");
                return 1;
            }

            Console.WriteLine($"Assembly: {assemblyPath}");
            Console.WriteLine($"Output:   {outputPath}");
            Console.WriteLine();

            // Load assembly
            Console.WriteLine("Loading assembly...");
            var assembly = Assembly.LoadFrom(assemblyPath);
            Console.WriteLine($"Loaded: {assembly.GetName().Name}");

            // Generate schemas
            Console.WriteLine("Scanning for [ActionNode] types...");
            var schemaService = new ActionSchemaService();
            var schemas = schemaService.GenerateSchemas(assembly);
            Console.WriteLine($"Found {schemas.Count} action node(s)");

            if (schemas.Count > 0)
            {
                foreach (var schema in schemas)
                {
                    Console.WriteLine($"  - {schema.ActionType} ({schema.DisplayName})");
                }
            }

            // Export to JSON
            Console.WriteLine();
            Console.WriteLine("Generating JSON schema...");
            var json = schemaService.ExportAsJson(schemas);

            // Ensure output directory exists
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Write to file
            File.WriteAllText(outputPath, json);
            Console.WriteLine($"Schema written to: {outputPath}");
            Console.WriteLine($"Size: {json.Length} bytes");

            Console.WriteLine();
            Console.WriteLine("✓ Schema generation complete");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Stack trace:");
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }
}
