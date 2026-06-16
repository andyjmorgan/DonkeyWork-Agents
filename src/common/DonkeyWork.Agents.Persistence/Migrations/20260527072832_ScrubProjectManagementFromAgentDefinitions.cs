using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ScrubProjectManagementFromAgentDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The project_management tool group was removed in #86, but agent definitions
            // saved before that still carry it in their stored JSON. The runtime ignores the
            // unknown group, yet the builder still renders the orphaned node. Strip every trace.
            migrationBuilder.Sql(
                """
                UPDATE agent_definitions.agent_definitions
                SET contract = jsonb_set(
                        contract,
                        '{toolGroups}',
                        COALESCE((
                            SELECT jsonb_agg(elem)
                            FROM jsonb_array_elements(contract->'toolGroups') AS elem
                            WHERE elem <> '"project_management"'::jsonb
                        ), '[]'::jsonb))
                WHERE contract ? 'toolGroups'
                  AND contract->'toolGroups' @> '["project_management"]'::jsonb;

                UPDATE agent_definitions.agent_definitions
                SET react_flow_data = jsonb_set(
                        jsonb_set(
                            react_flow_data,
                            '{nodes}',
                            COALESCE((
                                SELECT jsonb_agg(node)
                                FROM jsonb_array_elements(react_flow_data->'nodes') AS node
                                WHERE COALESCE(node->'data'->>'toolGroupId', '') <> 'project_management'
                            ), '[]'::jsonb)),
                        '{edges}',
                        COALESCE((
                            SELECT jsonb_agg(edge)
                            FROM jsonb_array_elements(react_flow_data->'edges') AS edge
                            WHERE (edge->>'source') NOT IN (
                                    SELECT node->>'id'
                                    FROM jsonb_array_elements(react_flow_data->'nodes') AS node
                                    WHERE COALESCE(node->'data'->>'toolGroupId', '') = 'project_management')
                              AND (edge->>'target') NOT IN (
                                    SELECT node->>'id'
                                    FROM jsonb_array_elements(react_flow_data->'nodes') AS node
                                    WHERE COALESCE(node->'data'->>'toolGroupId', '') = 'project_management')
                        ), '[]'::jsonb))
                WHERE react_flow_data IS NOT NULL
                  AND react_flow_data ? 'nodes'
                  AND EXISTS (
                        SELECT 1 FROM jsonb_array_elements(react_flow_data->'nodes') AS node
                        WHERE COALESCE(node->'data'->>'toolGroupId', '') = 'project_management');

                UPDATE agent_definitions.agent_definitions
                SET node_configurations = COALESCE((
                        SELECT jsonb_object_agg(key, value)
                        FROM jsonb_each(node_configurations) AS entries(key, value)
                        WHERE COALESCE(value->>'toolGroupId', '') <> 'project_management'
                    ), '{}'::jsonb)
                WHERE node_configurations IS NOT NULL
                  AND EXISTS (
                        SELECT 1 FROM jsonb_each(node_configurations) AS entries(key, value)
                        WHERE COALESCE(value->>'toolGroupId', '') = 'project_management');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible: the scrubbed project_management references cannot be reconstructed.
        }
    }
}
