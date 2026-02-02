Frontend:

why are model properties in the frontend saying supports variables but dont support a field to enter the syntax?

why is max tokens not a slider?

why is an opus model showing a reasoning enum?

the scriban autocomplete in the monaco editor is now longer showing inputs or previous step properties

credential dropdown should show an "Add Credential" option

save errors need to show a toast:

{
"message": "Node configuration for '456c6527-d618-40f5-a058-01c9119e4a66' has no corresponding ReactFlow node"
}

backend and frontend:

nodename can support A-Z a-z 0-9 - _

Backend:

reasoning is dependent on provider so should be a provider property
presence penalty and frequency penalty are too advanced for now
as is top p

AgentOrchestrator.cs
why is this working with strings?
var inputSchemaJson = version.InputSchema.RootElement.GetRawText();                                  
113 +            _executionContext.Hydrate(executionId, userId, input, inputSchemaJson); 

Round 2:

start and end nodes, why are you appending _1 to the first?
 a react node is a guid, not a string.  (ReactFlowNode)

round 3:

Frontend: 

a setting with a depends on, should be rendered in a card, enabling the check should show the required configuraiton together)

Backend:

add a migration for the changes

round 4:

api.ts:21  GET http://localhost:5199/api/v1/agents/c94e9416-02a9-450e-a91e-1c8bbe1f1f62/versions 500 (Internal Server Error)
fetchWithAuth @ api.ts:21
get @ api.ts:49
listVersions @ api.ts:300
loadAgentData @ AgentEditorPage.tsx:58
await in loadAgentData
(anonymous) @ AgentEditorPage.tsx:103
react_stack_bottom_frame @ react-dom_client.js?v=a82d8820:18567
runWithFiberInDEV @ react-dom_client.js?v=a82d8820:997
commitHookEffectListMount @ react-dom_client.js?v=a82d8820:9411
commitHookPassiveMountEffects @ react-dom_client.js?v=a82d8820:9465
commitPassiveMountOnFiber @ react-dom_client.js?v=a82d8820:11040
recursivelyTraversePassiveMountEffects @ react-dom_client.js?v=a82d8820:11010
commitPassiveMountOnFiber @ react-dom_client.js?v=a82d8820:11201
recursivelyTraversePassiveMountEffects @ react-dom_client.js?v=a82d8820:11010
commitPassiveMountOnFiber @ react-dom_client.js?v=a82d8820:11033
recursivelyTraversePassiveMountEffects @ react-dom_client.js?v=a82d8820:11010
commitPassiveMountOnFiber @ react-dom_client.js?v=a82d8820:11033
recursivelyTraversePassiveMountEffects @ react-dom_client.js?v=a82d8820:11010
commitPassiveMountOnFiber @ react-dom_client.js?v=a82d8820:11033
recursivelyTraversePassiveMountEffects @ react-dom_client.js?v=a82d8820:11010
commitPassiveMountOnFiber @ react-dom_client.js?v=a82d8820:11201
recursivelyTraversePassiveMountEffects @ react-dom_client.js?v=a82d8820:11010
commitPassiveMountOnFiber @ react-dom_client.js?v=a82d8820:11201
recursivelyTraversePassiveMountEffects @ react-dom_client.js?v=a82d8820:11010
commitPassiveMountOnFiber @ react-dom_client.js?v=a82d8820:11033
recursivelyTraversePassiveMountEffects @ react-dom_client.js?v=a82d8820:11010
commitPassiveMountOnFiber @ react-dom_client.js?v=a82d8820:11033
recursivelyTraversePassiveMountEffects @ react-dom_client.js?v=a82d8820:11010
commitPassiveMountOnFiber @ react-dom_client.js?v=a82d8820:11033
recursivelyTraversePassiveMountEffects @ react-dom_client.js?v=a82d8820:11010
commitPassiveMountOnFiber @ react-dom_client.js?v=a82d8820:11201
recursivelyTraversePassiveMountEffects @ react-dom_client.js?v=a82d8820:11010
commitPassiveMountOnFiber @ react-dom_client.js?v=a82d8820:11066
flushPassiveEffects @ react-dom_client.js?v=a82d8820:13150
(anonymous) @ react-dom_client.js?v=a82d8820:12776
performWorkUntilDeadline @ react-dom_client.js?v=a82d8820:36
<AgentEditorPage>
exports.jsxDEV @ react_jsx-dev-runtime.js?v=a82d8820:247
App @ App.tsx:30
react_stack_bottom_frame @ react-dom_client.js?v=a82d8820:18509
renderWithHooksAgain @ react-dom_client.js?v=a82d8820:5729
renderWithHooks @ react-dom_client.js?v=a82d8820:5665
updateFunctionComponent @ react-dom_client.js?v=a82d8820:7475
beginWork @ react-dom_client.js?v=a82d8820:8525
runWithFiberInDEV @ react-dom_client.js?v=a82d8820:997
performUnitOfWork @ react-dom_client.js?v=a82d8820:12561
workLoopSync @ react-dom_client.js?v=a82d8820:12424
renderRootSync @ react-dom_client.js?v=a82d8820:12408
performWorkOnRoot @ react-dom_client.js?v=a82d8820:11766
performWorkOnRootViaSchedulerTask @ react-dom_client.js?v=a82d8820:13505
performWorkUntilDeadline @ react-dom_client.js?v=a82d8820:36
<App>
exports.jsxDEV @ react_jsx-dev-runtime.js?v=a82d8820:247
(anonymous) @ main.tsx:11
installHook.js:1 Failed to load agent: SyntaxError: Unexpected token 'S', "System.Not"... is not valid JSON
overrideMethod @ installHook.js:1
loadAgentData @ AgentEditorPage.tsx:96
await in loadAgentData
(anonymous) @ AgentEditorPage.tsx:103
react_stack_bottom_frame @ react-dom_client.js?v=a82d8820:18567
runWithFiberInDEV @ react-dom_client.js?v=a82d8820:997
commitHookEffectListMount @ react-dom_client.js?v=a82d8820:9411
commitHookPassiveMountEffects @ react-dom_client.js?v=a82d8820:9465
commitPassiveMountOnFiber @ react-dom_client.js?v=a82d8820:11040
recursivelyTraversePassiveMountEffects @ react-dom_client.js?v=a82d8820:11010
commitPassiveMountOnFiber @ react-dom_client.js?v=a82d8820:11201
recursivelyTraversePassiveMountEffects @ react-dom_client.js?v=a82d8820:11010
commitPassiveMountOnFiber @ react-dom_client.js?v=a82d8820:11033
recursivelyTraversePassiveMountEffects @ react-dom_client.js?v=a82d8820:11010
commitPassiveMountOnFiber @ react-dom_client.js?v=a82d8820:11033
recursivelyTraversePassiveMountEffects @ react-dom_client.js?v=a82d8820:11010
commitPassiveMountOnFiber @ react-dom_client.js?v=a82d8820:11033
recursivelyTraversePassiveMountEffects @ react-dom_client.js?v=a82d8820:11010
commitPassiveMountOnFiber @ react-dom_client.js?v=a82d8820:11201
recursivelyTraversePassiveMountEffects @ react-dom_client.js?v=a82d8820:11010
commitPassiveMountOnFiber @ react-dom_client.js?v=a82d8820:11201
recursivelyTraversePassiveMountEffects @ react-dom_client.js?v=a82d8820:11010
commitPassiveMountOnFiber @ react-dom_client.js?v=a82d8820:11033
recursivelyTraversePassiveMountEffects @ react-dom_client.js?v=a82d8820:11010
commitPassiveMountOnFiber @ react-dom_client.js?v=a82d8820:11033
recursivelyTraversePassiveMountEffects @ react-dom_client.js?v=a82d8820:11010
commitPassiveMountOnFiber @ react-dom_client.js?v=a82d8820:11033
recursivelyTraversePassiveMountEffects @ react-dom_client.js?v=a82d8820:11010
commitPassiveMountOnFiber @ react-dom_client.js?v=a82d8820:11201
recursivelyTraversePassiveMountEffects @ react-dom_client.js?v=a82d8820:11010
commitPassiveMountOnFiber @ react-dom_client.js?v=a82d8820:11066
flushPassiveEffects @ react-dom_client.js?v=a82d8820:13150
(anonymous) @ react-dom_client.js?v=a82d8820:12776
performWorkUntilDeadline @ react-dom_client.js?v=a82d8820:36
<AgentEditorPage>
exports.jsxDEV @ react_jsx-dev-runtime.js?v=a82d8820:247
App @ App.tsx:30
react_stack_bottom_frame @ react-dom_client.js?v=a82d8820:18509
renderWithHooksAgain @ react-dom_client.js?v=a82d8820:5729
renderWithHooks @ react-dom_client.js?v=a82d8820:5665
updateFunctionComponent @ react-dom_client.js?v=a82d8820:7475
beginWork @ react-dom_client.js?v=a82d8820:8525
runWithFiberInDEV @ react-dom_client.js?v=a82d8820:997
performUnitOfWork @ react-dom_client.js?v=a82d8820:12561
workLoopSync @ react-dom_client.js?v=a82d8820:12424
renderRootSync @ react-dom_client.js?v=a82d8820:12408
performWorkOnRoot @ react-dom_client.js?v=a82d8820:11766
performWorkOnRootViaSchedulerTask @ react-dom_client.js?v=a82d8820:13505
performWorkUntilDeadline @ react-dom_client.js?v=a82d8820:36
<App>
exports.jsxDEV @ react_jsx-dev-runtime.js?v=a82d8820:247
(anonymous) @ main.tsx:11
api.ts:21  GET http://localhost:5199/api/v1/agents/c94e9416-02a9-450e-a91e-1c8bbe1f1f62/versions 500 (Internal Server Error)
fetchWithAuth @ api.ts:21
get @ api.ts:49
listVersions @ api.ts:300
loadAgentData @ AgentEditorPage.tsx:58
await in loadAgentData
(anonymous) @ AgentEditorPage.tsx:103
react_stack_bottom_frame @ react-dom_client.js?v=a82d8820:18567
runWithFiberInDEV @ react-dom_client.js?v=a82d8820:997
commitHookEffectListMount @ react-dom_client.js?v=a82d8820:9411
commitHookPassiveMountEffects @ react-dom_client.js?v=a82d8820:9465
reconnectPassiveEffects @ react-dom_client.js?v=a82d8820:11273
recursivelyTraverseReconnectPassiveEffects @ react-dom_client.js?v=a82d8820:11240
reconnectPassiveEffects @ react-dom_client.js?v=a82d8820:11317
recursivelyTraverseReconnectPassiveEffects @ react-dom_client.js?v=a82d8820:11240
reconnectPassiveEffects @ react-dom_client.js?v=a82d8820:11265
recursivelyTraverseReconnectPassiveEffects @ react-dom_client.js?v=a82d8820:11240
reconnectPassiveEffects @ react-dom_client.js?v=a82d8820:11265
recursivelyTraverseReconnectPassiveEffects @ react-dom_client.js?v=a82d8820:11240
reconnectPassiveEffects @ react-dom_client.js?v=a82d8820:11265
recursivelyTraverseReconnectPassiveEffects @ react-dom_client.js?v=a82d8820:11240
reconnectPassiveEffects @ react-dom_client.js?v=a82d8820:11317
recursivelyTraverseReconnectPassiveEffects @ react-dom_client.js?v=a82d8820:11240
reconnectPassiveEffects @ react-dom_client.js?v=a82d8820:11317
recursivelyTraverseReconnectPassiveEffects @ react-dom_client.js?v=a82d8820:11240
reconnectPassiveEffects @ react-dom_client.js?v=a82d8820:11265
recursivelyTraverseReconnectPassiveEffects @ react-dom_client.js?v=a82d8820:11240
reconnectPassiveEffects @ react-dom_client.js?v=a82d8820:11265
recursivelyTraverseReconnectPassiveEffects @ react-dom_client.js?v=a82d8820:11240
reconnectPassiveEffects @ react-dom_client.js?v=a82d8820:11265
recursivelyTraverseReconnectPassiveEffects @ react-dom_client.js?v=a82d8820:11240
reconnectPassiveEffects @ react-dom_client.js?v=a82d8820:11317
doubleInvokeEffectsOnFiber @ react-dom_client.js?v=a82d8820:13339
runWithFiberInDEV @ react-dom_client.js?v=a82d8820:997
recursivelyTraverseAndDoubleInvokeEffectsInDEV @ react-dom_client.js?v=a82d8820:13312
commitDoubleInvokeEffectsInDEV @ react-dom_client.js?v=a82d8820:13347
flushPassiveEffects @ react-dom_client.js?v=a82d8820:13157
(anonymous) @ react-dom_client.js?v=a82d8820:12776
performWorkUntilDeadline @ react-dom_client.js?v=a82d8820:36
<AgentEditorPage>
exports.jsxDEV @ react_jsx-dev-runtime.js?v=a82d8820:247
App @ App.tsx:30
react_stack_bottom_frame @ react-dom_client.js?v=a82d8820:18509
renderWithHooksAgain @ react-dom_client.js?v=a82d8820:5729
renderWithHooks @ react-dom_client.js?v=a82d8820:5665
updateFunctionComponent @ react-dom_client.js?v=a82d8820:7475
beginWork @ react-dom_client.js?v=a82d8820:8525
runWithFiberInDEV @ react-dom_client.js?v=a82d8820:997
performUnitOfWork @ react-dom_client.js?v=a82d8820:12561
workLoopSync @ react-dom_client.js?v=a82d8820:12424
renderRootSync @ react-dom_client.js?v=a82d8820:12408
performWorkOnRoot @ react-dom_client.js?v=a82d8820:11766
performWorkOnRootViaSchedulerTask @ react-dom_client.js?v=a82d8820:13505
performWorkUntilDeadline @ react-dom_client.js?v=a82d8820:36
<App>
exports.jsxDEV @ react_jsx-dev-runtime.js?v=a82d8820:247
(anonymous) @ main.tsx:11
installHook.js:1 Failed to load agent: SyntaxError: Unexpected token 'S', "System.Not"... is not valid JSON



2026-02-01 21:56:36.040 [ERR] Microsoft.EntityFrameworkCore.Database.Command Failed executing DbCommand (4ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
ALTER TABLE agents.agent_version_credential_mappings ALTER COLUMN node_id TYPE uuid;
