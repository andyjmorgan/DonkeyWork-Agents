import { describe, it, expect } from "vitest";
import type { ContentBox, ChatMessage, AgentGroupBox } from "@donkeywork/api-client";
import {
  updateNestedGroup,
  tryUpdateNested,
  attachToInner,
  attachChildAgent,
  attachRootAgent,
  makeAgentGroup,
} from "./agentBoxHelpers";

function msg(id: string, boxes: ContentBox[]): ChatMessage {
  return { id, role: "assistant", content: "", boxes };
}

function toolUse(toolName: string, toolUseId: string, subAgent?: AgentGroupBox): ContentBox {
  return { type: "tool_use", toolName, toolUseId, subAgent };
}

function thinking(text: string): ContentBox {
  return { type: "thinking", text };
}

function text(t: string): ContentBox {
  return { type: "text", text: t };
}

function usage(): ContentBox {
  return { type: "usage", inputTokens: 100, outputTokens: 50, webSearchRequests: 0, contextWindowLimit: 200000, maxOutputTokens: 64000 };
}

describe("updateNestedGroup", () => {
  it("finds subAgent on tool_use box by agentKey", () => {
    const group = makeAgentGroup("agent-1", "agent", "Test Agent");
    const box = toolUse("spawn_agent", "tool-1", group);

    const result = updateNestedGroup(box, "agent-1", (inner) => [...inner, text("added")]);
    expect(result).not.toBeNull();
    expect((result as any).subAgent.boxes).toHaveLength(1);
    expect((result as any).subAgent.boxes[0].text).toBe("added");
  });

  it("finds agent_group box by agentKey", () => {
    const box: ContentBox = { ...makeAgentGroup("agent-1", "agent"), boxes: [] };

    const result = updateNestedGroup(box, "agent-1", (inner) => [...inner, text("added")]);
    expect(result).not.toBeNull();
    expect((result as any).boxes).toHaveLength(1);
  });

  it("returns null for non-matching agentKey", () => {
    const box = toolUse("spawn_agent", "tool-1", makeAgentGroup("agent-1", "agent"));
    const result = updateNestedGroup(box, "agent-WRONG", (inner) => inner);
    expect(result).toBeNull();
  });

  it("returns null for boxes without agent groups", () => {
    expect(updateNestedGroup(thinking("hi"), "agent-1", (i) => i)).toBeNull();
    expect(updateNestedGroup(text("hi"), "agent-1", (i) => i)).toBeNull();
    expect(updateNestedGroup(usage(), "agent-1", (i) => i)).toBeNull();
  });

  it("recursively finds deeply nested agent groups", () => {
    const innerGroup = makeAgentGroup("deep-agent", "agent");
    const outerGroup: AgentGroupBox = {
      ...makeAgentGroup("outer-agent", "agent"),
      boxes: [toolUse("spawn_agent", "tool-inner", innerGroup)],
    };
    const rootBox = toolUse("spawn_agent", "tool-outer", outerGroup);

    const result = updateNestedGroup(rootBox, "deep-agent", (inner) => [...inner, text("deep-add")]);
    expect(result).not.toBeNull();

    const outerSub = (result as any).subAgent;
    const innerTool = outerSub.boxes[0];
    expect(innerTool.subAgent.boxes).toHaveLength(1);
    expect(innerTool.subAgent.boxes[0].text).toBe("deep-add");
  });
});

describe("tryUpdateNested", () => {
  it("uses fast path when hintIndex is correct", () => {
    const group = makeAgentGroup("agent-1", "agent");
    const boxes: ContentBox[] = [usage(), thinking("hi"), toolUse("spawn_agent", "t1", group)];

    const result = tryUpdateNested(boxes, "agent-1", 2, (inner) => [...inner, text("added")]);
    expect(result).not.toBeNull();
    expect(result!.boxIndex).toBe(2);
  });

  it("falls back to slow path when hintIndex is stale", () => {
    const group = makeAgentGroup("agent-1", "agent");
    const boxes: ContentBox[] = [usage(), thinking("hi"), toolUse("spawn_agent", "t1", group), thinking("more")];

    const result = tryUpdateNested(boxes, "agent-1", 3, (inner) => [...inner, text("added")]);
    expect(result).not.toBeNull();
    expect(result!.boxIndex).toBe(2);
  });

  it("handles provisional hintIndex of -1 by scanning all boxes", () => {
    const group = makeAgentGroup("agent-1", "agent");
    const boxes: ContentBox[] = [usage(), toolUse("spawn_agent", "t1", group)];

    const result = tryUpdateNested(boxes, "agent-1", -1, (inner) => [...inner, text("added")]);
    expect(result).not.toBeNull();
    expect(result!.boxIndex).toBe(1);
  });

  it("returns null when agent not found in any box", () => {
    const boxes: ContentBox[] = [usage(), thinking("hi"), text("hello")];
    const result = tryUpdateNested(boxes, "agent-1", 0, (inner) => inner);
    expect(result).toBeNull();
  });
});

describe("attachToInner", () => {
  it("attaches to last unattached tool_use", () => {
    const inner: ContentBox[] = [thinking("plan"), toolUse("spawn_agent", "t1")];
    const group = makeAgentGroup("child-1", "agent");
    const { boxes, attached } = attachToInner(inner, group);

    expect(attached).toBe(true);
    expect(boxes).toHaveLength(2);
    expect((boxes[1] as any).subAgent?.agentKey).toBe("child-1");
  });

  it("skips tool_use that already has a subAgent", () => {
    const existing = makeAgentGroup("existing", "agent");
    const inner: ContentBox[] = [
      toolUse("spawn_agent", "t1", existing),
      toolUse("spawn_agent", "t2"),
    ];
    const group = makeAgentGroup("child-1", "agent");
    const { boxes, attached } = attachToInner(inner, group);

    expect(attached).toBe(true);
    expect((boxes[0] as any).subAgent?.agentKey).toBe("existing");
    expect((boxes[1] as any).subAgent?.agentKey).toBe("child-1");
  });

  it("appends standalone when no unattached tool_use exists", () => {
    const inner: ContentBox[] = [thinking("plan"), text("hello")];
    const group = makeAgentGroup("child-1", "agent");
    const { boxes, attached } = attachToInner(inner, group);

    expect(attached).toBe(false);
    expect(boxes).toHaveLength(3);
    expect(boxes[2]).toEqual(group);
  });
});

describe("attachChildAgent", () => {
  it("nests child inside parent via indexed fast path", () => {
    const parentGroup = makeAgentGroup("parent-agent", "agent");
    parentGroup.boxes = [toolUse("spawn_agent", "spawn-child")];

    const messages = [
      msg("msg-1", [
        usage(),
        thinking("plan"),
        toolUse("spawn_agent", "spawn-parent", parentGroup),
      ]),
    ];

    const childGroup = makeAgentGroup("child-agent", "agent", "Research Agent");
    const parentEntry = { messageId: "msg-1", boxIndex: 2 };

    const result = attachChildAgent(messages, parentEntry, "parent-agent", childGroup, "msg-1");

    const rootBox = result.messages[0].boxes[2] as any;
    expect(rootBox.subAgent.agentKey).toBe("parent-agent");
    expect(rootBox.subAgent.boxes[0].subAgent.agentKey).toBe("child-agent");
  });

  it("nests child when boxIndex is stale (slow path within same message)", () => {
    const parentGroup = makeAgentGroup("parent-agent", "agent");
    parentGroup.boxes = [toolUse("spawn_agent", "spawn-child")];

    const messages = [
      msg("msg-1", [
        usage(),
        thinking("plan"),
        toolUse("spawn_agent", "spawn-parent", parentGroup),
        thinking("more thinking"),
        text("response text"),
      ]),
    ];

    const childGroup = makeAgentGroup("child-agent", "agent", "Research Agent");
    const parentEntry = { messageId: "msg-1", boxIndex: 3 };

    const result = attachChildAgent(messages, parentEntry, "parent-agent", childGroup, "msg-1");

    const rootBox = result.messages[0].boxes[2] as any;
    expect(rootBox.subAgent.boxes[0].subAgent?.agentKey).toBe("child-agent");
    expect(result.entry.boxIndex).toBe(2);
  });

  it("finds parent in a different message via global scan", () => {
    const parentGroup = makeAgentGroup("parent-agent", "agent");
    parentGroup.boxes = [toolUse("spawn_agent", "spawn-child")];

    const messages = [
      msg("msg-1", [
        usage(),
        thinking("old turn"),
        toolUse("spawn_agent", "spawn-parent", parentGroup),
      ]),
      msg("msg-2", [usage(), thinking("new turn")]),
    ];

    const childGroup = makeAgentGroup("child-agent", "agent", "Research Agent");
    const parentEntry = { messageId: "msg-2", boxIndex: 0 };

    const result = attachChildAgent(messages, parentEntry, "parent-agent", childGroup, "msg-2");

    // Child should be nested inside parent in msg-1, NOT standalone in msg-2
    const parentBox = result.messages[0].boxes[2] as any;
    expect(parentBox.subAgent.boxes[0].subAgent?.agentKey).toBe("child-agent");
    expect(result.entry.messageId).toBe("msg-1");
    expect(result.messages[1].boxes).toHaveLength(2); // msg-2 untouched
  });

  it("handles multiple research agents spawned inside deep research", () => {
    const deepResearchGroup = makeAgentGroup("deep-research", "agent", "Deep Research", "database-search");
    deepResearchGroup.boxes = [
      thinking("planning sub-questions"),
      text("I'll research these topics..."),
      toolUse("research_create", "rc-1"),
      toolUse("spawn_agent", "spawn-r1"),
    ];

    let messages = [
      msg("msg-1", [
        usage(),
        thinking("let me spawn deep research"),
        toolUse("spawn_agent", "spawn-dr", deepResearchGroup),
      ]),
    ];

    const parentEntry = { messageId: "msg-1", boxIndex: 2 };

    const r1 = makeAgentGroup("research-1", "agent", "Survey of space missions", "text-select", "Research Agent");
    let result = attachChildAgent(messages, parentEntry, "deep-research", r1, "msg-1");
    messages = result.messages;

    let dr = (messages[0].boxes[2] as any).subAgent;
    expect(dr.boxes[3].subAgent?.agentKey).toBe("research-1");

    dr.boxes.push(toolUse("spawn_agent", "spawn-r2"));
    const r2 = makeAgentGroup("research-2", "agent", "NASA missions", "text-select", "Research Agent");
    result = attachChildAgent(messages, parentEntry, "deep-research", r2, "msg-1");
    messages = result.messages;

    dr = (messages[0].boxes[2] as any).subAgent;
    expect(dr.boxes[4].subAgent?.agentKey).toBe("research-2");

    dr.boxes.push(toolUse("spawn_agent", "spawn-r3"));
    const r3 = makeAgentGroup("research-3", "agent", "Commercial missions", "text-select", "Research Agent");
    result = attachChildAgent(messages, parentEntry, "deep-research", r3, "msg-1");
    messages = result.messages;

    dr = (messages[0].boxes[2] as any).subAgent;
    expect(dr.boxes[5].subAgent?.agentKey).toBe("research-3");

    dr.boxes.push(toolUse("spawn_agent", "spawn-r4"));
    const r4 = makeAgentGroup("research-4", "agent", "Scientific highlights", "text-select", "Research Agent");
    result = attachChildAgent(messages, parentEntry, "deep-research", r4, "msg-1");
    messages = result.messages;

    dr = (messages[0].boxes[2] as any).subAgent;
    expect(dr.boxes[6].subAgent?.agentKey).toBe("research-4");

    // ALL 4 research agents must be INSIDE deep research
    expect(messages[0].boxes).toHaveLength(3);
  });

  it("handles provisional boxIndex of -1", () => {
    const parentGroup = makeAgentGroup("parent-agent", "agent");
    parentGroup.boxes = [toolUse("spawn_agent", "spawn-child")];

    const messages = [
      msg("msg-1", [usage(), toolUse("spawn_agent", "spawn-parent", parentGroup)]),
    ];

    const childGroup = makeAgentGroup("child-agent", "agent");
    const parentEntry = { messageId: "msg-1", boxIndex: -1 };

    const result = attachChildAgent(messages, parentEntry, "parent-agent", childGroup, "msg-1");

    const rootBox = result.messages[0].boxes[1] as any;
    expect(rootBox.subAgent.boxes[0].subAgent?.agentKey).toBe("child-agent");
  });
});

describe("attachRootAgent", () => {
  it("attaches to last unattached spawn tool_use", () => {
    const messages = [
      msg("msg-1", [usage(), thinking("spawning agent"), toolUse("spawn_agent", "spawn-1")]),
    ];

    const group = makeAgentGroup("agent-1", "agent", "Deep Research");
    const result = attachRootAgent(messages, "msg-1", group);

    const spawnBox = result.messages[0].boxes[2] as any;
    expect(spawnBox.subAgent?.agentKey).toBe("agent-1");
    expect(result.entry.boxIndex).toBe(2);
  });

  it("skips non-spawn tool_use boxes", () => {
    const messages = [
      msg("msg-1", [usage(), toolUse("sandbox_exec", "t1"), toolUse("spawn_agent", "t2")]),
    ];

    const group = makeAgentGroup("agent-1", "agent");
    const result = attachRootAgent(messages, "msg-1", group);

    expect((result.messages[0].boxes[1] as any).subAgent).toBeUndefined();
    expect((result.messages[0].boxes[2] as any).subAgent?.agentKey).toBe("agent-1");
  });

  it("appends standalone when no spawn tool_use exists", () => {
    const messages = [msg("msg-1", [usage(), thinking("hi"), text("hello")])];

    const group = makeAgentGroup("agent-1", "agent");
    const result = attachRootAgent(messages, "msg-1", group);

    expect(result.messages[0].boxes).toHaveLength(4);
    expect(result.messages[0].boxes[3]).toEqual(group);
    expect(result.entry.boxIndex).toBe(3);
  });
});
