# Model Configuration Schema System

## Overview

This system provides dynamic form schemas for configuring LLM models. Backend C# classes with attributes define configuration fields, which are automatically converted to JSON schemas for frontend consumption.

## Quick Start for Frontend

### 1. Fetch Available Models

```typescript
GET /api/v1/models

Response:
{
  "models": [
    {
      "id": "claude-opus-4-5",
      "name": "Claude Opus 4.5",
      "provider": "Anthropic",
      "mode": "Chat",
      "supports": {
        "reasoning": true,
        "vision": true,
        // ...
      }
    }
  ]
}
```

### 2. Get Configuration Schema for a Model

```typescript
GET /api/v1/models/{modelId}/config-schema

Response:
{
  "model_id": "claude-opus-4-5",
  "model_name": "Claude Opus 4.5",
  "provider": "Anthropic",
  "mode": "Chat",
  "fields": [
    {
      "name": "temperature",
      "label": "Temperature",
      "description": "Controls randomness (0=deterministic, higher=creative)",
      "control_type": "Slider",
      "property_type": "double",
      "order": 10,
      "min": 0,
      "max": 1.0,
      "step": 0.1,
      "default": 1.0
    },
    {
      "name": "reasoningEffort",
      "label": "Reasoning Effort",
      "control_type": "Select",
      "property_type": "string",
      "order": 40,
      "options": ["Low", "Medium", "High"],
      "default": "Medium"
    }
  ]
}
```

### 3. Get All Schemas at Once

```typescript
GET /api/v1/models/config-schemas

Response:
{
  "schemas": {
    "claude-opus-4-5": { /* ModelConfigSchema */ },
    "gpt-5": { /* ModelConfigSchema */ },
    // ...
  }
}
```

---

## API Reference

### Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/models` | List all available models |
| GET | `/api/v1/models/{modelId}` | Get specific model definition |
| GET | `/api/v1/models/{modelId}/config-schema` | Get config schema for model |
| GET | `/api/v1/models/config-schemas` | Get all config schemas |

### Response Types

#### ModelConfigSchema
```typescript
interface ModelConfigSchema {
  model_id: string;
  model_name: string;
  provider: "OpenAI" | "Anthropic" | "Google";
  mode: "Chat" | "ImageGeneration" | "AudioGeneration" | "Embedding" | "AudioTranscription" | "VideoGeneration";
  fields: ConfigFieldSchema[];
}
```

#### ConfigFieldSchema
```typescript
interface ConfigFieldSchema {
  name: string;                          // Camel case field name
  label: string;                         // Display label
  description?: string;                  // Help text
  control_type: FieldControlType;        // UI control to render
  property_type: string;                 // Data type (double, int32, string, boolean)
  order: number;                         // Display order (ascending)
  group?: string;                        // Optional group name (e.g., "Advanced")
  min?: number;                          // Min value (for sliders/number inputs)
  max?: number;                          // Max value (for sliders/number inputs)
  step?: number;                         // Step increment (for sliders)
  default?: any;                         // Default value
  options?: string[];                    // Options array (for selects)
  depends_on?: FieldDependency[];        // Conditional visibility rules
}
```

#### FieldControlType
```typescript
type FieldControlType =
  | "Slider"       // Range slider (has min, max, step)
  | "NumberInput"  // Number input box (has min, max)
  | "TextInput"    // Text input box
  | "Select"       // Dropdown (has options array)
  | "Toggle";      // Boolean checkbox/toggle
```

#### FieldDependency
```typescript
interface FieldDependency {
  field: string;   // Name of field this depends on
  value: any;      // Value that field must equal for this field to be visible
}
```

---

## Control Types Guide

### Slider
Used for continuous numeric values with visual range.

```typescript
{
  "name": "temperature",
  "control_type": "Slider",
  "property_type": "double",
  "min": 0,
  "max": 2.0,
  "step": 0.1,
  "default": 1.0
}
```

**Render as:** Range slider with value display

### NumberInput
Used for discrete integer values.

```typescript
{
  "name": "maxOutputTokens",
  "control_type": "NumberInput",
  "property_type": "int32",
  "min": 1,
  "max": 128000,
  "default": 4096
}
```

**Render as:** Number input with min/max validation

### Select
Used for enumerated options.

```typescript
{
  "name": "reasoningEffort",
  "control_type": "Select",
  "property_type": "string",
  "options": ["Low", "Medium", "High"],
  "default": "Medium"
}
```

**Render as:** Dropdown select

### Toggle
Used for boolean on/off options.

```typescript
{
  "name": "enableCaching",
  "control_type": "Toggle",
  "property_type": "boolean",
  "default": true
}
```

**Render as:** Checkbox or toggle switch

### TextInput
Used for string values.

```typescript
{
  "name": "systemPrompt",
  "control_type": "TextInput",
  "property_type": "string"
}
```

**Render as:** Text input or textarea

---

## Field Ordering and Grouping

### Ordering
Fields are returned sorted by the `order` property (ascending).

```typescript
fields.sort((a, b) => a.order - b.order);
```

### Grouping
Fields with a `group` property can be visually grouped:

```typescript
const fieldsByGroup = fields.reduce((acc, field) => {
  const group = field.group || "General";
  acc[group] = acc[group] || [];
  acc[group].push(field);
  return acc;
}, {});

// Example output:
// {
//   "General": [temperature, maxOutputTokens],
//   "Advanced": [topP, frequencyPenalty],
//   "Reasoning": [reasoningEffort, thinkingBudget]
// }
```

---

## Conditional Field Visibility

Some fields should only appear when certain conditions are met.

### Example: Conditional Fields
```typescript
{
  "name": "cacheSize",
  "label": "Cache Size",
  "control_type": "NumberInput",
  "depends_on": [
    {
      "field": "enableCaching",
      "value": true
    }
  ]
}
```

**Logic:** Show `cacheSize` only when `enableCaching` is `true`.

### Implementation

```typescript
function isFieldVisible(
  field: ConfigFieldSchema,
  currentValues: Record<string, any>
): boolean {
  if (!field.depends_on || field.depends_on.length === 0) {
    return true;
  }

  // All dependencies must be satisfied
  return field.depends_on.every(dep =>
    currentValues[dep.field] === dep.value
  );
}

// Usage in React:
const visibleFields = schema.fields.filter(field =>
  isFieldVisible(field, formValues)
);
```

### Multiple Dependencies
A field can depend on multiple conditions (all must be true):

```typescript
{
  "name": "advancedOption",
  "depends_on": [
    { "field": "enableAdvanced", "value": true },
    { "field": "mode", "value": "Expert" }
  ]
}
```

---

## Provider-Specific Fields

Different providers have different configuration options:

### Common Fields (All Chat Models)
- `temperature` - Randomness control
- `maxOutputTokens` - Max generation length
- `topP` - Nucleus sampling threshold

### Anthropic-Specific
- `thinkingBudget` - Max tokens for extended thinking (requires `reasoning: true`)

### OpenAI-Specific
- `frequencyPenalty` - Reduce token repetition
- `presencePenalty` - Encourage new topics

### Google-Specific
- `topK` - Top-K sampling parameter

### Checking Provider
```typescript
if (schema.provider === "Anthropic") {
  // Show Anthropic-specific UI hints
}
```

---

## Model Capabilities

Fields may only appear if the model supports certain capabilities.

### Example: Reasoning Fields
```typescript
// Only appears if model.supports.reasoning === true
{
  "name": "reasoningEffort",
  "control_type": "Select",
  "options": ["Low", "Medium", "High"]
}
```

### Capability Filtering
The backend automatically filters fields based on `model.supports`. Frontend doesn't need to implement this logic - if a field is in the schema, the model supports it.

```typescript
// ✅ Already filtered by backend
// No need to check model.supports.reasoning
schema.fields.forEach(field => renderField(field));
```

---

## Model Overrides

Some models have specific constraints that override base config:

### Example: Anthropic Temperature
```json
// Base default: max = 2.0
// Anthropic override: max = 1.0

{
  "name": "temperature",
  "max": 1.0  // ← Override applied
}
```

### Example: Model-Specific Defaults
```json
{
  "name": "maxOutputTokens",
  "max": 64000,    // ← Model-specific max
  "default": 8192  // ← Model-specific default
}
```

Overrides are already applied in the schema - no frontend logic needed.

---

## Complete React Example

```typescript
import { useState, useEffect } from 'react';

interface ModelConfigFormProps {
  modelId: string;
}

function ModelConfigForm({ modelId }: ModelConfigFormProps) {
  const [schema, setSchema] = useState<ModelConfigSchema | null>(null);
  const [values, setValues] = useState<Record<string, any>>({});

  useEffect(() => {
    // Fetch schema
    fetch(`/api/v1/models/${modelId}/config-schema`)
      .then(r => r.json())
      .then(schema => {
        setSchema(schema);
        // Initialize with defaults
        const defaults = schema.fields.reduce((acc, field) => {
          if (field.default !== undefined) {
            acc[field.name] = field.default;
          }
          return acc;
        }, {});
        setValues(defaults);
      });
  }, [modelId]);

  const updateValue = (name: string, value: any) => {
    setValues(prev => ({ ...prev, [name]: value }));
  };

  const isFieldVisible = (field: ConfigFieldSchema): boolean => {
    if (!field.depends_on) return true;
    return field.depends_on.every(dep =>
      values[dep.field] === dep.value
    );
  };

  const renderField = (field: ConfigFieldSchema) => {
    if (!isFieldVisible(field)) return null;

    switch (field.control_type) {
      case 'Slider':
        return (
          <div key={field.name}>
            <label>{field.label}</label>
            {field.description && <p>{field.description}</p>}
            <input
              type="range"
              min={field.min}
              max={field.max}
              step={field.step}
              value={values[field.name] ?? field.default}
              onChange={e => updateValue(field.name, parseFloat(e.target.value))}
            />
            <span>{values[field.name]}</span>
          </div>
        );

      case 'NumberInput':
        return (
          <div key={field.name}>
            <label>{field.label}</label>
            <input
              type="number"
              min={field.min}
              max={field.max}
              value={values[field.name] ?? field.default}
              onChange={e => updateValue(field.name, parseInt(e.target.value))}
            />
          </div>
        );

      case 'Select':
        return (
          <div key={field.name}>
            <label>{field.label}</label>
            <select
              value={values[field.name] ?? field.default}
              onChange={e => updateValue(field.name, e.target.value)}
            >
              {field.options?.map(opt => (
                <option key={opt} value={opt}>{opt}</option>
              ))}
            </select>
          </div>
        );

      case 'Toggle':
        return (
          <div key={field.name}>
            <label>
              <input
                type="checkbox"
                checked={values[field.name] ?? field.default}
                onChange={e => updateValue(field.name, e.target.checked)}
              />
              {field.label}
            </label>
          </div>
        );

      default:
        return null;
    }
  };

  if (!schema) return <div>Loading...</div>;

  // Group fields
  const fieldsByGroup = schema.fields.reduce((acc, field) => {
    const group = field.group || 'General';
    acc[group] = acc[group] || [];
    acc[group].push(field);
    return acc;
  }, {} as Record<string, ConfigFieldSchema[]>);

  return (
    <form>
      <h2>{schema.model_name} Configuration</h2>
      {Object.entries(fieldsByGroup).map(([group, fields]) => (
        <fieldset key={group}>
          <legend>{group}</legend>
          {fields.map(renderField)}
        </fieldset>
      ))}
      <button onClick={() => console.log(values)}>
        Save Configuration
      </button>
    </form>
  );
}
```

---

## Validation

### Client-Side
Validate against schema constraints:

```typescript
function validateField(field: ConfigFieldSchema, value: any): string | null {
  if (field.min !== undefined && value < field.min) {
    return `${field.label} must be at least ${field.min}`;
  }
  if (field.max !== undefined && value > field.max) {
    return `${field.label} must be at most ${field.max}`;
  }
  if (field.options && !field.options.includes(value)) {
    return `${field.label} must be one of: ${field.options.join(', ')}`;
  }
  return null;
}
```

### Server-Side
The backend validates all configurations before use. Client-side validation is for UX only.

---

## Real-World Examples

### Example 1: Claude Opus 4.5
```json
{
  "model_id": "claude-opus-4-5",
  "provider": "Anthropic",
  "fields": [
    {
      "name": "temperature",
      "label": "Temperature",
      "control_type": "Slider",
      "min": 0,
      "max": 1.0,
      "step": 0.1,
      "default": 1.0,
      "order": 10
    },
    {
      "name": "maxOutputTokens",
      "label": "Max Output Tokens",
      "control_type": "NumberInput",
      "min": 1,
      "max": 64000,
      "default": 8192,
      "order": 20
    },
    {
      "name": "topP",
      "label": "Top P",
      "control_type": "Slider",
      "min": 0,
      "max": 1,
      "step": 0.05,
      "default": 1.0,
      "order": 30,
      "group": "Advanced"
    },
    {
      "name": "reasoningEffort",
      "label": "Reasoning Effort",
      "control_type": "Select",
      "options": ["Low", "Medium", "High"],
      "default": "Medium",
      "order": 40
    },
    {
      "name": "thinkingBudget",
      "label": "Thinking Budget",
      "description": "Max tokens for extended thinking",
      "control_type": "NumberInput",
      "min": 1024,
      "max": 128000,
      "default": 10000,
      "order": 41,
      "group": "Reasoning"
    }
  ]
}
```

### Example 2: GPT-5
```json
{
  "model_id": "gpt-5",
  "provider": "OpenAI",
  "fields": [
    {
      "name": "temperature",
      "max": 2.0,
      "order": 10
    },
    {
      "name": "maxOutputTokens",
      "max": 32768,
      "order": 20
    },
    {
      "name": "frequencyPenalty",
      "label": "Frequency Penalty",
      "control_type": "Slider",
      "min": -2.0,
      "max": 2.0,
      "step": 0.1,
      "default": 0,
      "order": 50,
      "group": "Advanced"
    },
    {
      "name": "presencePenalty",
      "label": "Presence Penalty",
      "control_type": "Slider",
      "min": -2.0,
      "max": 2.0,
      "step": 0.1,
      "default": 0,
      "order": 51,
      "group": "Advanced"
    }
  ]
}
```

### Example 3: Claude Haiku 4.5 (No Reasoning)
```json
{
  "model_id": "claude-haiku-4-5",
  "provider": "Anthropic",
  "fields": [
    {
      "name": "temperature",
      "order": 10
    },
    {
      "name": "maxOutputTokens",
      "order": 20
    },
    {
      "name": "topP",
      "order": 30,
      "group": "Advanced"
    }
    // Note: No reasoningEffort or thinkingBudget (model doesn't support reasoning)
  ]
}
```

---

## Schema Stability

### Backward Compatibility
- Fields may be added in future versions
- Existing field properties won't be removed
- New field properties are additive (with defaults)

### Caching Strategy
- Schemas are stable per model version
- Safe to cache aggressively on client
- Cache invalidation: model version change or API version upgrade

```typescript
// Example caching
const CACHE_KEY = `model-schema-${modelId}`;
const CACHE_TTL = 24 * 60 * 60 * 1000; // 24 hours

function getCachedSchema(modelId: string): ModelConfigSchema | null {
  const cached = localStorage.getItem(CACHE_KEY);
  if (!cached) return null;

  const { schema, timestamp } = JSON.parse(cached);
  if (Date.now() - timestamp > CACHE_TTL) {
    return null;
  }

  return schema;
}
```

---

## Common Patterns

### Pattern 1: Accordion Groups
```typescript
const groups = ["General", "Advanced", "Reasoning"];
groups.map(group => (
  <Accordion key={group}>
    <AccordionSummary>{group}</AccordionSummary>
    <AccordionDetails>
      {fieldsByGroup[group]?.map(renderField)}
    </AccordionDetails>
  </Accordion>
));
```

### Pattern 2: Inline Help Icons
```typescript
{field.description && (
  <Tooltip title={field.description}>
    <InfoIcon />
  </Tooltip>
)}
```

### Pattern 3: Live Validation
```typescript
const [errors, setErrors] = useState<Record<string, string>>({});

const validateAndUpdate = (name: string, value: any) => {
  const field = schema.fields.find(f => f.name === name);
  const error = validateField(field, value);

  setErrors(prev => ({ ...prev, [name]: error }));
  setValues(prev => ({ ...prev, [name]: value }));
};
```

### Pattern 4: Reset to Defaults
```typescript
const resetToDefaults = () => {
  const defaults = schema.fields.reduce((acc, field) => {
    if (field.default !== undefined) {
      acc[field.name] = field.default;
    }
    return acc;
  }, {});
  setValues(defaults);
};
```

---

## Testing

### Example Test Cases
```typescript
describe('Model Config Form', () => {
  it('should render all visible fields', () => {
    const schema = { fields: [...] };
    render(<ModelConfigForm schema={schema} />);
    expect(screen.getByLabelText('Temperature')).toBeInTheDocument();
  });

  it('should hide dependent fields when condition not met', () => {
    const schema = {
      fields: [
        { name: 'enableCaching', control_type: 'Toggle' },
        {
          name: 'cacheSize',
          control_type: 'NumberInput',
          depends_on: [{ field: 'enableCaching', value: true }]
        }
      ]
    };

    render(<ModelConfigForm schema={schema} />);
    expect(screen.queryByLabelText('Cache Size')).not.toBeInTheDocument();

    fireEvent.click(screen.getByLabelText('Enable Caching'));
    expect(screen.getByLabelText('Cache Size')).toBeInTheDocument();
  });

  it('should validate min/max constraints', () => {
    const field = { name: 'temp', min: 0, max: 2 };
    expect(validateField(field, 3)).toBeTruthy();
    expect(validateField(field, 1)).toBeNull();
  });
});
```

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                         Frontend                             │
│  ┌────────────┐  ┌────────────┐  ┌──────────────┐          │
│  │ Model List │→│ Fetch Schema│→│ Render Form   │          │
│  └────────────┘  └────────────┘  └──────────────┘          │
└────────────────────────────┬────────────────────────────────┘
                             │ GET /api/v1/models/{id}/config-schema
                             ↓
┌─────────────────────────────────────────────────────────────┐
│                     ModelsController                         │
│                   (Providers.Api)                            │
└────────────────────────────┬────────────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────────────┐
│              ModelConfigSchemaService                        │
│                  (Providers.Core)                            │
│  • Reflection on config classes                              │
│  • Read attributes (ConfigField, Slider, etc.)               │
│  • Filter by capabilities (RequiresCapability)               │
│  • Merge overrides from models.json                          │
│  • Process dependencies (DependsOn)                          │
│  • Cache results                                             │
└────────────────────────────┬────────────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────────────┐
│              Configuration Classes                           │
│         (Providers.Contracts/Configuration)                  │
│  • ChatModelConfig (base)                                    │
│  • AnthropicChatConfig (extends base)                        │
│  • OpenAIChatConfig (extends base)                           │
│  • GoogleChatConfig (extends base)                           │
└─────────────────────────────────────────────────────────────┘
```

---

## Support

### Questions?
- Check models.json for available models and their capabilities
- Review configuration class files for field definitions
- Test with `/api/v1/models/config-schemas` to see all schemas

### Adding New Fields?
Backend developers can add new fields by:
1. Adding properties to config classes with `[ConfigField]` attribute
2. Specifying control type via `[Slider]`, `[RangeConstraint]`, or `[Select]`
3. Adding capability requirements with `[RequiresCapability]`
4. Adding conditional visibility with `[DependsOn]`

Frontend will automatically pick up new fields on next schema fetch - no frontend code changes needed!
