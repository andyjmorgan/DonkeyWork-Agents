using System.Collections.Frozen;
using DonkeyWork.Agents.Actors.Core.Tools.AudioCollections;
using DonkeyWork.Agents.Actors.Core.Tools.ProjectManagement;
using DonkeyWork.Agents.Actors.Core.Tools.Scheduling;
using DonkeyWork.Agents.Actors.Core.Tools.Swarm;

namespace DonkeyWork.Agents.Actors.Core.Tools;

internal static class ToolGroupMap
{
    public static readonly FrozenDictionary<string, Type[]> Groups = new Dictionary<string, Type[]>
    {
        [Actors.Contracts.Models.ToolGroupNames.SwarmDelegate] = [typeof(SwarmDelegateSpawnTools)],
        [Actors.Contracts.Models.ToolGroupNames.SwarmManagement] = [typeof(SwarmAgentManagementTools)],
        [Actors.Contracts.Models.ToolGroupNames.ProjectManagement] =
        [
            typeof(ProjectAgentTools),
            typeof(MilestoneAgentTools),
            typeof(TaskAgentTools),
            typeof(NoteAgentTools),
            typeof(ResearchAgentTools),
        ],
        [Actors.Contracts.Models.ToolGroupNames.SwarmMessaging] = [typeof(SwarmAgentMessagingTools)],
        [Actors.Contracts.Models.ToolGroupNames.SwarmContext] = [typeof(SwarmSharedContextTools)],
        [Actors.Contracts.Models.ToolGroupNames.Scheduling] = [typeof(SchedulingAgentTools)],
        [Actors.Contracts.Models.ToolGroupNames.AudioCollections] =
        [
            typeof(AudioCollectionAgentTools),
            typeof(AudioRecordingAgentTools),
        ],
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static readonly FrozenDictionary<string, string> DisplayNames = new Dictionary<string, string>
    {
        [Actors.Contracts.Models.ToolGroupNames.SwarmDelegate] = "Swarm Delegate",
        [Actors.Contracts.Models.ToolGroupNames.SwarmManagement] = "Swarm Management",
        [Actors.Contracts.Models.ToolGroupNames.ProjectManagement] = "Project Management",
        [Actors.Contracts.Models.ToolGroupNames.Sandbox] = "Sandbox",
        [Actors.Contracts.Models.ToolGroupNames.SwarmMessaging] = "Swarm Messaging",
        [Actors.Contracts.Models.ToolGroupNames.SwarmContext] = "Swarm Context",
        [Actors.Contracts.Models.ToolGroupNames.Scheduling] = "Scheduling",
        [Actors.Contracts.Models.ToolGroupNames.AudioCollections] = "Audio Collections",
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
}
