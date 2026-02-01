# Agent Integration Tests

This document describes integration tests for creating and executing agents, with focus on verifying individual step results.

## Base URL and Authentication

```
Base URL: /api/v1
Authentication: Bearer token (JWT from Keycloak)
Content-Type: application/json
```

---

## Test Flow Overview

1. **Create Agent** - Creates agent with initial draft version
2. **Save Version** - Configure nodes, edges, and node configurations
3. **Publish Version** - Make version executable
4. **Execute Agent** - Run with input data
5. **Verify Results** - Check execution status, node outputs, and final result

---

## API Endpoints

### Create Agent
```http
POST /api/v1/agents
Content-Type: application/json
Authorization: Bearer {token}

{
  "name": "test-agent",
  "description": "Integration test agent"
}
```

**Response (201 Created):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "test-agent",
  "description": "Integration test agent",
  "versionId": "550e8400-e29b-41d4-a716-446655440001",
  "createdAt": "2024-01-15T10:00:00Z"
}
```

### Save Agent Version
```http
POST /api/v1/agents/{agentId}/versions
Content-Type: application/json
Authorization: Bearer {token}

{
  "reactFlowData": { ... },
  "nodeConfigurations": { ... },
  "inputSchema": { ... },
  "outputSchema": null,
  "credentialMappings": []
}
```

### Publish Version
```http
POST /api/v1/agents/{agentId}/versions/publish
Authorization: Bearer {token}
```

### Execute Agent
```http
POST /api/v1/agents/{agentId}/execute
Content-Type: application/json
Authorization: Bearer {token}

{
  "input": { ... }
}
```

**Response (200 OK):**
```json
{
  "executionId": "550e8400-e29b-41d4-a716-446655440099",
  "status": "Completed",
  "output": "...",
  "error": null
}
```

### Get Node Executions (Step Results)
```http
GET /api/v1/agents/executions/{executionId}/nodes
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "nodeExecutions": [
    {
      "id": "...",
      "nodeId": "node-uuid-1",
      "nodeType": "start",
      "nodeName": "start",
      "status": "Completed",
      "input": null,
      "output": "{\"input\":{\"message\":\"hello\"}}",
      "startedAt": "2024-01-15T10:00:01Z",
      "completedAt": "2024-01-15T10:00:01Z",
      "durationMs": 5
    },
    {
      "id": "...",
      "nodeId": "node-uuid-2",
      "nodeType": "messageFormatter",
      "nodeName": "formatter_1",
      "status": "Completed",
      "output": "{\"formattedMessage\":\"Hello, World!\"}",
      "startedAt": "2024-01-15T10:00:01Z",
      "completedAt": "2024-01-15T10:00:01Z",
      "durationMs": 2
    }
  ],
  "totalCount": 3
}
```

---

## Node Types and Configurations

### Start Node
Entry point that passes through input data.

```json
{
  "type": "Start",
  "name": "start",
  "inputSchema": {
    "type": "object",
    "properties": {
      "message": { "type": "string" }
    },
    "required": ["message"]
  }
}
```

**Output Structure:**
```json
{
  "input": { "message": "hello" }
}
```

### End Node
Terminates execution and returns final output.

```json
{
  "type": "End",
  "name": "end",
  "outputSchema": null
}
```

**Output Structure:**
```json
{
  "finalOutput": "..."
}
```

### MessageFormatter Node
Formats text using Scriban templates with variable substitution.

```json
{
  "type": "MessageFormatter",
  "name": "formatter_1",
  "template": "Hello, {{ input.name }}! Your message was: {{ input.message }}"
}
```

**Template Variables:**
- `{{ input.* }}` - Access execution input
- `{{ steps.nodeName.* }}` - Access previous node outputs
- `{{ execution_id }}` - Current execution ID
- `{{ user_id }}` - Current user ID

**Output Structure:**
```json
{
  "formattedMessage": "Hello, World! Your message was: test"
}
```

### HttpRequest Node
Makes HTTP requests to external APIs.

```json
{
  "type": "HttpRequest",
  "name": "http_1",
  "method": "Post",
  "url": "https://httpbin.org/post",
  "headers": {
    "useVariable": false,
    "items": [
      { "key": "Content-Type", "value": "application/json" }
    ]
  },
  "body": "{\"data\": \"{{ input.message }}\"}",
  "timeoutSeconds": 30
}
```

**HTTP Methods:** `Get`, `Post`, `Put`, `Patch`, `Delete`, `Head`, `Options`

**Output Structure:**
```json
{
  "statusCode": 200,
  "body": "{\"json\":{\"data\":\"test\"}}",
  "headers": {
    "Content-Type": "application/json"
  },
  "isSuccess": true
}
```

### Sleep Node
Pauses execution for specified duration.

```json
{
  "type": "Sleep",
  "name": "sleep_1",
  "durationMs": 1000
}
```

**Output Structure:**
```json
{
  "durationMs": 1000
}
```

### Model Node
Calls an LLM provider (requires credential).

```json
{
  "type": "Model",
  "name": "model_1",
  "provider": "Anthropic",
  "modelId": "claude-3-haiku-20240307",
  "credentialId": "credential-uuid",
  "systemPrompts": ["You are a helpful assistant."],
  "userMessages": ["{{ input.question }}"],
  "temperature": 1.0,
  "maxOutputTokens": 1024
}
```

**Providers:** `OpenAI`, `Anthropic`, `Google`

**Output Structure:**
```json
{
  "responseText": "The answer is...",
  "totalTokens": 150,
  "inputTokens": 50,
  "outputTokens": 100
}
```

---

## ReactFlow Data Structure

### Nodes Array
```json
{
  "nodes": [
    {
      "id": "node-1",
      "type": "start",
      "position": { "x": 250, "y": 50 },
      "data": { "label": "start" }
    },
    {
      "id": "node-2",
      "type": "messageFormatter",
      "position": { "x": 250, "y": 150 },
      "data": { "label": "formatter_1" }
    },
    {
      "id": "node-3",
      "type": "end",
      "position": { "x": 250, "y": 250 },
      "data": { "label": "end" }
    }
  ]
}
```

**Node Types (lowercase in ReactFlow):**
- `start` - Start node
- `end` - End node
- `model` - Model/LLM node
- `messageFormatter` - Message formatter node
- `httpRequest` - HTTP request node
- `sleep` - Sleep/delay node

### Edges Array
```json
{
  "edges": [
    {
      "id": "edge-1",
      "source": "node-1",
      "target": "node-2",
      "type": "smoothstep",
      "animated": true
    },
    {
      "id": "edge-2",
      "source": "node-2",
      "target": "node-3",
      "type": "smoothstep",
      "animated": true
    }
  ]
}
```

### Viewport
```json
{
  "viewport": {
    "x": 0,
    "y": 0,
    "zoom": 1
  }
}
```

---

## Integration Test Scenarios

### Test 1: Simple Message Formatter Agent

**Purpose:** Verify basic agent execution with input/output flow.

**Step 1: Create Agent**
```http
POST /api/v1/agents
{
  "name": "simple-formatter-test",
  "description": "Tests message formatting"
}
```

**Step 2: Save Version**
```http
POST /api/v1/agents/{agentId}/versions
{
  "reactFlowData": {
    "nodes": [
      { "id": "n1", "type": "start", "position": {"x":0,"y":0}, "data": {"label":"start"} },
      { "id": "n2", "type": "messageFormatter", "position": {"x":0,"y":100}, "data": {"label":"formatter"} },
      { "id": "n3", "type": "end", "position": {"x":0,"y":200}, "data": {"label":"end"} }
    ],
    "edges": [
      { "id": "e1", "source": "n1", "target": "n2", "type": "smoothstep", "animated": true },
      { "id": "e2", "source": "n2", "target": "n3", "type": "smoothstep", "animated": true }
    ],
    "viewport": { "x": 0, "y": 0, "zoom": 1 }
  },
  "nodeConfigurations": {
    "n1": {
      "type": "Start",
      "name": "start",
      "inputSchema": {
        "type": "object",
        "properties": { "name": { "type": "string" } },
        "required": ["name"]
      }
    },
    "n2": {
      "type": "MessageFormatter",
      "name": "formatter",
      "template": "Hello, {{ input.name }}!"
    },
    "n3": {
      "type": "End",
      "name": "end"
    }
  },
  "inputSchema": {
    "type": "object",
    "properties": { "name": { "type": "string" } },
    "required": ["name"]
  }
}
```

**Step 3: Publish**
```http
POST /api/v1/agents/{agentId}/versions/publish
```

**Step 4: Execute**
```http
POST /api/v1/agents/{agentId}/execute
{
  "input": { "name": "World" }
}
```

**Step 5: Verify Results**
```http
GET /api/v1/agents/executions/{executionId}/nodes
```

**Expected Node Outputs:**

| Node | Expected Output |
|------|----------------|
| start | `{"input":{"name":"World"}}` |
| formatter | `{"formattedMessage":"Hello, World!"}` |
| end | `{"finalOutput":"Hello, World!"}` |

**Assertions:**
- Execution status is `Completed`
- Final output equals `"Hello, World!"`
- 3 node executions exist
- All node statuses are `Completed`
- formatter output contains `formattedMessage` = `"Hello, World!"`

---

### Test 2: HTTP Request Agent

**Purpose:** Verify HTTP request execution and response handling.

**Save Version:**
```json
{
  "reactFlowData": {
    "nodes": [
      { "id": "n1", "type": "start", "position": {"x":0,"y":0}, "data": {} },
      { "id": "n2", "type": "httpRequest", "position": {"x":0,"y":100}, "data": {} },
      { "id": "n3", "type": "end", "position": {"x":0,"y":200}, "data": {} }
    ],
    "edges": [
      { "id": "e1", "source": "n1", "target": "n2", "type": "smoothstep", "animated": true },
      { "id": "e2", "source": "n2", "target": "n3", "type": "smoothstep", "animated": true }
    ],
    "viewport": { "x": 0, "y": 0, "zoom": 1 }
  },
  "nodeConfigurations": {
    "n1": {
      "type": "Start",
      "name": "start",
      "inputSchema": { "type": "object" }
    },
    "n2": {
      "type": "HttpRequest",
      "name": "http_request",
      "method": "Get",
      "url": "https://httpbin.org/get",
      "timeoutSeconds": 30
    },
    "n3": {
      "type": "End",
      "name": "end"
    }
  },
  "inputSchema": { "type": "object" }
}
```

**Execute:**
```json
{
  "input": {}
}
```

**Expected http_request Output:**
```json
{
  "statusCode": 200,
  "body": "...",
  "headers": { ... },
  "isSuccess": true
}
```

**Assertions:**
- `statusCode` equals `200`
- `isSuccess` equals `true`
- `body` contains JSON response from httpbin.org

---

### Test 3: Sleep Node Agent

**Purpose:** Verify sleep execution timing.

**Node Configuration:**
```json
{
  "n2": {
    "type": "Sleep",
    "name": "sleep_node",
    "durationMs": 500
  }
}
```

**Expected sleep_node Output:**
```json
{
  "durationMs": 500
}
```

**Assertions:**
- Node execution `durationMs` >= 500
- Output `durationMs` equals 500

---

### Test 4: Chained Variable Substitution

**Purpose:** Verify variables from previous nodes are correctly substituted.

**Nodes:**
1. `start` - Receives `{"value": 42}`
2. `formatter_1` - Template: `"Value is: {{ input.value }}"`
3. `formatter_2` - Template: `"Previous said: {{ steps.formatter_1.formattedMessage }}"`
4. `end`

**Expected Outputs:**

| Node | Output |
|------|--------|
| formatter_1 | `{"formattedMessage":"Value is: 42"}` |
| formatter_2 | `{"formattedMessage":"Previous said: Value is: 42"}` |

---

### Test 5: HTTP POST with Body

**Purpose:** Verify POST request with JSON body and variable substitution.

**Node Configuration:**
```json
{
  "type": "HttpRequest",
  "name": "post_request",
  "method": "Post",
  "url": "https://httpbin.org/post",
  "headers": {
    "useVariable": false,
    "items": [
      { "key": "Content-Type", "value": "application/json" }
    ]
  },
  "body": "{\"message\": \"{{ input.text }}\"}",
  "timeoutSeconds": 30
}
```

**Input:**
```json
{
  "input": { "text": "Hello API" }
}
```

**Assertions:**
- Response `statusCode` equals `200`
- Response body JSON contains `{"json":{"message":"Hello API"}}`

---

### Test 6: Conditional HTTP Headers

**Purpose:** Verify headers are correctly sent with requests.

**Node Configuration:**
```json
{
  "type": "HttpRequest",
  "name": "headers_test",
  "method": "Get",
  "url": "https://httpbin.org/headers",
  "headers": {
    "useVariable": false,
    "items": [
      { "key": "X-Custom-Header", "value": "test-value" },
      { "key": "Authorization", "value": "Bearer {{ input.token }}" }
    ]
  },
  "timeoutSeconds": 30
}
```

**Input:**
```json
{
  "input": { "token": "my-secret-token" }
}
```

**Assertions:**
- Response body contains `X-Custom-Header: test-value`
- Response body contains `Authorization: Bearer my-secret-token`

---

## Verifying Step Results

### Method 1: Get Node Executions Endpoint

```http
GET /api/v1/agents/executions/{executionId}/nodes
```

Returns ordered list of all node executions with:
- `nodeId` - The node's ID from ReactFlow
- `nodeName` - The configured name
- `nodeType` - The node type
- `status` - `Completed` or `Failed`
- `output` - JSON-serialized output (parse to verify)
- `durationMs` - Execution time

### Method 2: Get Full Execution

```http
GET /api/v1/agents/executions/{executionId}
```

Returns overall execution with:
- `status` - `Completed` or `Failed`
- `output` - Final output from End node
- `errorMessage` - Error details if failed
- `totalTokensUsed` - Sum of all model node tokens

### Method 3: Get Execution Logs

```http
GET /api/v1/agents/executions/{executionId}/logs
```

Returns detailed logs with:
- `nodeId` - Which node logged
- `logLevel` - Debug, Information, Warning, Error
- `message` - Log message
- `details` - JSON details

---

## Output Verification Patterns

### Parse Node Output
```javascript
const nodeExecutions = response.nodeExecutions;
const formatterNode = nodeExecutions.find(n => n.nodeName === 'formatter');
const output = JSON.parse(formatterNode.output);
expect(output.formattedMessage).toBe('Hello, World!');
```

### Verify HTTP Response
```javascript
const httpNode = nodeExecutions.find(n => n.nodeType === 'httpRequest');
const output = JSON.parse(httpNode.output);
expect(output.statusCode).toBe(200);
expect(output.isSuccess).toBe(true);
expect(JSON.parse(output.body).json.message).toBe('test');
```

### Verify Execution Duration
```javascript
const sleepNode = nodeExecutions.find(n => n.nodeName === 'sleep_node');
expect(sleepNode.durationMs).toBeGreaterThanOrEqual(500);
```

### Verify Token Usage (Model Nodes)
```javascript
const modelNode = nodeExecutions.find(n => n.nodeType === 'model');
const output = JSON.parse(modelNode.output);
expect(output.totalTokens).toBeGreaterThan(0);
expect(output.responseText).toBeTruthy();
```

---

## Error Scenarios

### Test: Invalid Input Schema
**Input:** `{ "wrong_field": "value" }` when `name` is required

**Expected:**
- Execution status: `Failed`
- Error message contains schema validation error

### Test: HTTP Request Timeout
**Configuration:** `timeoutSeconds: 1` with slow endpoint

**Expected:**
- Execution status: `Failed`
- Node status: `Failed`
- Error message contains timeout information

### Test: Invalid Template Syntax
**Template:** `"{{ 1 + }}"` (incomplete expression)

**Expected:**
- Execution status: `Failed`
- Error message contains "Template parsing errors"

---

## Complete Test Agent Example

```json
{
  "reactFlowData": {
    "nodes": [
      {
        "id": "start-node",
        "type": "start",
        "position": { "x": 250, "y": 0 },
        "data": { "label": "start" }
      },
      {
        "id": "formatter-node",
        "type": "messageFormatter",
        "position": { "x": 250, "y": 100 },
        "data": { "label": "greeting_formatter" }
      },
      {
        "id": "http-node",
        "type": "httpRequest",
        "position": { "x": 250, "y": 200 },
        "data": { "label": "webhook_call" }
      },
      {
        "id": "sleep-node",
        "type": "sleep",
        "position": { "x": 250, "y": 300 },
        "data": { "label": "pause" }
      },
      {
        "id": "end-node",
        "type": "end",
        "position": { "x": 250, "y": 400 },
        "data": { "label": "end" }
      }
    ],
    "edges": [
      { "id": "e1", "source": "start-node", "target": "formatter-node", "type": "smoothstep", "animated": true },
      { "id": "e2", "source": "formatter-node", "target": "http-node", "type": "smoothstep", "animated": true },
      { "id": "e3", "source": "http-node", "target": "sleep-node", "type": "smoothstep", "animated": true },
      { "id": "e4", "source": "sleep-node", "target": "end-node", "type": "smoothstep", "animated": true }
    ],
    "viewport": { "x": 0, "y": 0, "zoom": 1 }
  },
  "nodeConfigurations": {
    "start-node": {
      "type": "Start",
      "name": "start",
      "inputSchema": {
        "type": "object",
        "properties": {
          "name": { "type": "string" },
          "delay": { "type": "integer" }
        },
        "required": ["name"]
      }
    },
    "formatter-node": {
      "type": "MessageFormatter",
      "name": "greeting_formatter",
      "template": "Hello, {{ input.name }}! Welcome to the test."
    },
    "http-node": {
      "type": "HttpRequest",
      "name": "webhook_call",
      "method": "Post",
      "url": "https://httpbin.org/post",
      "headers": {
        "useVariable": false,
        "items": [
          { "key": "Content-Type", "value": "application/json" }
        ]
      },
      "body": "{\"greeting\": \"{{ steps.greeting_formatter.formattedMessage }}\"}",
      "timeoutSeconds": 30
    },
    "sleep-node": {
      "type": "Sleep",
      "name": "pause",
      "durationMs": 100
    },
    "end-node": {
      "type": "End",
      "name": "end"
    }
  },
  "inputSchema": {
    "type": "object",
    "properties": {
      "name": { "type": "string" },
      "delay": { "type": "integer" }
    },
    "required": ["name"]
  }
}
```

**Execute with:**
```json
{
  "input": {
    "name": "TestUser",
    "delay": 100
  }
}
```

**Expected Results:**

| Node | Expected Output |
|------|----------------|
| start | `{"input":{"name":"TestUser","delay":100}}` |
| greeting_formatter | `{"formattedMessage":"Hello, TestUser! Welcome to the test."}` |
| webhook_call | `{"statusCode":200,"isSuccess":true,"body":"..."}` |
| pause | `{"durationMs":100}` |
| end | Final aggregated output |

**Verification Checklist:**
- [ ] Execution completes with status `Completed`
- [ ] 5 node executions exist
- [ ] All nodes have status `Completed`
- [ ] greeting_formatter output contains "Hello, TestUser!"
- [ ] webhook_call statusCode is 200
- [ ] webhook_call body contains the greeting message
- [ ] pause durationMs is 100
- [ ] Total execution time >= 100ms (due to sleep)
