using System.Reflection;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Providers;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Schema;
using DonkeyWork.Agents.Orchestrations.Core.Execution;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Executors;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Providers;
using DonkeyWork.Agents.Orchestrations.Core.Services;

namespace DonkeyWork.Agents.Orchestrations.Core.Tests.Services;

public class NodeTypeSchemaServiceTests
{
    private readonly NodeTypeSchemaService _service;

    public NodeTypeSchemaServiceTests()
    {
        var schemaGenerator = new NodeSchemaGenerator();
        var methodRegistry = new NodeMethodRegistry();
        methodRegistry.DiscoverProviders(typeof(HttpNodeProvider).Assembly);
        _service = new NodeTypeSchemaService(schemaGenerator, methodRegistry);
    }

    #region OutputProperties Tests

    [Fact]
    public void GetNodeTypes_TextToSpeech_HasOutputProperties()
    {
        var nodeTypes = _service.GetNodeTypes();
        var tts = nodeTypes.FirstOrDefault(nt => nt.Type == NodeType.TextToSpeech);

        Assert.NotNull(tts);
        Assert.NotNull(tts.OutputProperties);
        Assert.Contains("Clips", tts.OutputProperties);
        Assert.Contains("Voice", tts.OutputProperties);
        Assert.Contains("Model", tts.OutputProperties);
        Assert.Contains("TotalSizeBytes", tts.OutputProperties);
        Assert.Contains("ClipCount", tts.OutputProperties);
    }

    [Fact]
    public void GetNodeTypes_StoreAudio_HasOutputProperties()
    {
        var nodeTypes = _service.GetNodeTypes();
        var storeAudio = nodeTypes.FirstOrDefault(nt => nt.Type == NodeType.StoreAudio);

        Assert.NotNull(storeAudio);
        Assert.NotNull(storeAudio.OutputProperties);
        Assert.Contains("RecordingId", storeAudio.OutputProperties);
        Assert.Contains("Name", storeAudio.OutputProperties);
        Assert.Contains("FilePath", storeAudio.OutputProperties);
    }

    [Fact]
    public void GetNodeTypes_MultimodalChatModel_NotInNodeTypes()
    {
        // MultimodalChatModel uses a separate schema path (MultimodalChatSchemaService)
        // and doesn't have a [Node] attribute, so it's not in the generic node types list
        var nodeTypes = _service.GetNodeTypes();
        var model = nodeTypes.FirstOrDefault(nt => nt.Type == NodeType.MultimodalChatModel);

        Assert.Null(model);
    }

    [Fact]
    public void GetNodeTypes_Model_HasModelOutputProperties()
    {
        var nodeTypes = _service.GetNodeTypes();
        var model = nodeTypes.FirstOrDefault(nt => nt.Type == NodeType.Model);

        Assert.NotNull(model);
        Assert.NotNull(model.OutputProperties);
        Assert.Contains("ResponseText", model.OutputProperties);
    }

    [Fact]
    public void GetNodeTypes_MessageFormatter_HasOutputFromProvider()
    {
        var nodeTypes = _service.GetNodeTypes();
        var formatter = nodeTypes.FirstOrDefault(nt => nt.Type == NodeType.MessageFormatter);

        Assert.NotNull(formatter);
        Assert.NotNull(formatter.OutputProperties);
        Assert.Contains("FormattedMessage", formatter.OutputProperties);
    }

    [Fact]
    public void GetNodeTypes_HttpRequest_HasOutputFromProvider()
    {
        var nodeTypes = _service.GetNodeTypes();
        var http = nodeTypes.FirstOrDefault(nt => nt.Type == NodeType.HttpRequest);

        Assert.NotNull(http);
        Assert.NotNull(http.OutputProperties);
        Assert.Contains("StatusCode", http.OutputProperties);
        Assert.Contains("Body", http.OutputProperties);
    }

    [Fact]
    public void GetNodeTypes_Sleep_HasOutputFromProvider()
    {
        var nodeTypes = _service.GetNodeTypes();
        var sleep = nodeTypes.FirstOrDefault(nt => nt.Type == NodeType.Sleep);

        Assert.NotNull(sleep);
        Assert.NotNull(sleep.OutputProperties);
        Assert.Contains("DurationSeconds", sleep.OutputProperties);
    }

    [Fact]
    public void GetNodeTypes_AllNodeTypes_HaveOutputProperties()
    {
        var nodeTypes = _service.GetNodeTypes();

        foreach (var nt in nodeTypes)
        {
            // Start and End have empty outputs, which is valid (returns null)
            if (nt.Type == NodeType.Start || nt.Type == NodeType.End)
                continue;

            Assert.NotNull(nt.OutputProperties);
            Assert.True(nt.OutputProperties.Count > 0,
                $"NodeType {nt.Type} should have at least one output property");
        }
    }

    [Fact]
    public void GetNodeTypes_OutputProperties_DoNotIncludeToMessageOutput()
    {
        var nodeTypes = _service.GetNodeTypes();

        foreach (var nt in nodeTypes)
        {
            if (nt.OutputProperties == null) continue;
            Assert.DoesNotContain("ToMessageOutput", nt.OutputProperties);
        }
    }

    #endregion

    #region Reflection Verification Tests

    [Theory]
    [InlineData(typeof(TextToSpeechNodeExecutor), typeof(TextToSpeechNodeOutput))]
    [InlineData(typeof(StoreAudioNodeExecutor), typeof(StoreAudioNodeOutput))]
    [InlineData(typeof(MultimodalChatNodeExecutor), typeof(ModelNodeOutput))]
    [InlineData(typeof(ModelNodeExecutor), typeof(ModelNodeOutput))]
    [InlineData(typeof(StartNodeExecutor), typeof(StartNodeOutput))]
    [InlineData(typeof(EndNodeExecutor), typeof(EndNodeOutput))]
    public void DedicatedExecutor_ReflectsCorrectOutputType(Type executorType, Type expectedOutputType)
    {
        var baseType = executorType.BaseType;

        Assert.NotNull(baseType);
        Assert.True(baseType.IsGenericType);
        Assert.Equal(typeof(NodeExecutor<,>), baseType.GetGenericTypeDefinition());

        var actualOutputType = baseType.GetGenericArguments()[1];
        Assert.Equal(expectedOutputType, actualOutputType);
    }

    [Theory]
    [InlineData(NodeType.HttpRequest, "StatusCode")]
    [InlineData(NodeType.HttpRequest, "Body")]
    [InlineData(NodeType.Sleep, "DurationSeconds")]
    [InlineData(NodeType.MessageFormatter, "FormattedMessage")]
    public void ProviderExecutor_MethodRegistry_HasCorrectOutputProperty(NodeType nodeType, string expectedProperty)
    {
        var registry = new NodeMethodRegistry();
        registry.DiscoverProviders(typeof(HttpNodeProvider).Assembly);

        Assert.True(registry.HasMethod(nodeType));

        var methodInfo = registry.GetMethod(nodeType);
        var properties = methodInfo.OutputType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(p => p.Name)
            .ToList();

        Assert.Contains(expectedProperty, properties);
    }

    #endregion
}
