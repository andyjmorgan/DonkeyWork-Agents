using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.Agents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameTtsInputsToText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rewrites every TTS node config in orchestration_versions:
            //   - rename `inputs` -> `text`
            //   - if the value JSON-parses to a single-element string array, unwrap to that string
            //   - if it's a multi-element string array, join the elements with two newlines
            //   - otherwise leave the rendered text as-is
            // Internal chunking on the executor replaces the array contract; this preserves user intent.
            migrationBuilder.Sql(@"
DO $$
DECLARE
    rec RECORD;
    new_config jsonb;
    node_key text;
    node_val jsonb;
    inputs_val text;
    parsed jsonb;
    new_text text;
BEGIN
    FOR rec IN SELECT id, node_configurations FROM orchestrations.orchestration_versions LOOP
        new_config := rec.node_configurations;
        FOR node_key, node_val IN SELECT * FROM jsonb_each(rec.node_configurations) LOOP
            IF node_val->>'type' IN ('TextToSpeech', 'GeminiTextToSpeech') AND node_val ? 'inputs' THEN
                inputs_val := node_val->>'inputs';
                IF inputs_val IS NULL THEN
                    new_text := '';
                ELSE
                    BEGIN
                        parsed := inputs_val::jsonb;
                        IF jsonb_typeof(parsed) = 'array' THEN
                            IF jsonb_array_length(parsed) = 0 THEN
                                new_text := '';
                            ELSIF jsonb_array_length(parsed) = 1 AND jsonb_typeof(parsed->0) = 'string' THEN
                                new_text := parsed->>0;
                            ELSE
                                SELECT string_agg(
                                    CASE WHEN jsonb_typeof(elem) = 'string'
                                         THEN elem #>> '{}'
                                         ELSE elem::text
                                    END,
                                    E'\n\n'
                                )
                                INTO new_text
                                FROM jsonb_array_elements(parsed) AS elem;
                            END IF;
                        ELSE
                            new_text := inputs_val;
                        END IF;
                    EXCEPTION WHEN others THEN
                        new_text := inputs_val;
                    END;
                END IF;

                node_val := (node_val - 'inputs') || jsonb_build_object('text', new_text);
                new_config := jsonb_set(new_config, ARRAY[node_key], node_val);
            END IF;
        END LOOP;

        IF new_config IS DISTINCT FROM rec.node_configurations THEN
            UPDATE orchestrations.orchestration_versions
            SET node_configurations = new_config
            WHERE id = rec.id;
        END IF;
    END LOOP;
END$$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse: rename `text` back to `inputs`, wrapping the value in a single-element JSON array.
            migrationBuilder.Sql(@"
DO $$
DECLARE
    rec RECORD;
    new_config jsonb;
    node_key text;
    node_val jsonb;
    text_val text;
    wrapped text;
BEGIN
    FOR rec IN SELECT id, node_configurations FROM orchestrations.orchestration_versions LOOP
        new_config := rec.node_configurations;
        FOR node_key, node_val IN SELECT * FROM jsonb_each(rec.node_configurations) LOOP
            IF node_val->>'type' IN ('TextToSpeech', 'GeminiTextToSpeech') AND node_val ? 'text' THEN
                text_val := node_val->>'text';
                wrapped := jsonb_build_array(COALESCE(text_val, ''))::text;
                node_val := (node_val - 'text') || jsonb_build_object('inputs', wrapped);
                new_config := jsonb_set(new_config, ARRAY[node_key], node_val);
            END IF;
        END LOOP;

        IF new_config IS DISTINCT FROM rec.node_configurations THEN
            UPDATE orchestrations.orchestration_versions
            SET node_configurations = new_config
            WHERE id = rec.id;
        END IF;
    END LOOP;
END$$;
");
        }
    }
}
