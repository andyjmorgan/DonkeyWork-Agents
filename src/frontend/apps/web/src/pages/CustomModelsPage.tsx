import { useCallback, useEffect, useState } from 'react'
import { Cpu, KeyRound, Loader2, Pencil, Plus, TestTube2, Trash2 } from 'lucide-react'
import {
  customModels,
  type CustomModel,
  type CustomModelWireFormat,
  type SaveCustomModelRequest,
  type TestCustomModelResponse,
} from '@donkeywork/api-client'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  Label,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@donkeywork/ui'

interface FormState {
  name: string
  endpoint: string
  wireFormat: CustomModelWireFormat
  modelName: string
  apiKey: string
  clearApiKey: boolean
  maxInputTokens: number
  maxOutputTokens: number
  supportsTools: boolean
}

const emptyForm: FormState = {
  name: '',
  endpoint: '',
  wireFormat: 'AnthropicMessages',
  modelName: '',
  apiKey: '',
  clearApiKey: false,
  maxInputTokens: 131072,
  maxOutputTokens: 16384,
  supportsTools: true,
}

export function CustomModelsPage() {
  const [items, setItems] = useState<CustomModel[]>([])
  const [loading, setLoading] = useState(true)
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editing, setEditing] = useState<CustomModel | null>(null)
  const [form, setForm] = useState<FormState>(emptyForm)
  const [saving, setSaving] = useState(false)
  const [testing, setTesting] = useState(false)
  const [testResult, setTestResult] = useState<TestCustomModelResponse | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<CustomModel | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    try { setItems(await customModels.list()) }
    catch (err) { setError(errorMessage(err)) }
    finally { setLoading(false) }
  }, [])

  useEffect(() => { void load() }, [load])

  const openCreate = () => {
    setEditing(null)
    setForm(emptyForm)
    setTestResult(null)
    setError(null)
    setDialogOpen(true)
  }

  const openEdit = (model: CustomModel) => {
    setEditing(model)
    setForm({
      name: model.name,
      endpoint: model.endpoint,
      wireFormat: model.wireFormat,
      modelName: model.modelName,
      apiKey: '',
      clearApiKey: false,
      maxInputTokens: model.maxInputTokens,
      maxOutputTokens: model.maxOutputTokens,
      supportsTools: model.supportsTools,
    })
    setTestResult(null)
    setError(null)
    setDialogOpen(true)
  }

  const request = (): SaveCustomModelRequest => ({
    name: form.name.trim(),
    endpoint: form.endpoint.trim(),
    wireFormat: form.wireFormat,
    modelName: form.modelName.trim(),
    apiKey: form.apiKey.trim() || undefined,
    clearApiKey: form.clearApiKey,
    maxInputTokens: Number(form.maxInputTokens),
    maxOutputTokens: Number(form.maxOutputTokens),
    supportsTools: form.supportsTools,
  })

  const handleTest = async () => {
    setTesting(true)
    setTestResult(null)
    setError(null)
    try {
      setTestResult(await customModels.test({
        id: editing?.id,
        endpoint: form.endpoint.trim(),
        wireFormat: form.wireFormat,
        modelName: form.modelName.trim(),
        apiKey: form.apiKey.trim() || undefined,
        clearApiKey: form.clearApiKey,
      }))
    } catch (err) { setError(errorMessage(err)) }
    finally { setTesting(false) }
  }

  const handleSave = async () => {
    setSaving(true)
    setError(null)
    try {
      if (editing) await customModels.update(editing.id, request())
      else await customModels.create(request())
      setDialogOpen(false)
      await load()
    } catch (err) { setError(errorMessage(err)) }
    finally { setSaving(false) }
  }

  const handleDelete = async () => {
    if (!deleteTarget) return
    try {
      await customModels.delete(deleteTarget.id)
      setDeleteTarget(null)
      await load()
    } catch (err) { setError(errorMessage(err)) }
  }

  if (loading) return <div className="flex justify-center py-16"><Loader2 className="h-8 w-8 animate-spin text-muted-foreground" /></div>

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold">Custom Models</h1>
          <p className="text-muted-foreground">Connect compatible models without adding a new provider integration.</p>
        </div>
        <Button onClick={openCreate}><Plus className="h-4 w-4" /> Add model</Button>
      </div>

      {error && <div className="rounded-xl border border-destructive/40 bg-destructive/10 p-3 text-sm text-destructive">{error}</div>}

      {items.length === 0 ? (
        <div className="flex flex-col items-center rounded-2xl border border-dashed p-12 text-center">
          <Cpu className="h-9 w-9 text-muted-foreground" />
          <h2 className="mt-4 font-semibold">No custom models yet</h2>
          <p className="mt-1 max-w-md text-sm text-muted-foreground">Add any endpoint that speaks Anthropic Messages or OpenAI Responses.</p>
          <Button className="mt-5" onClick={openCreate}><Plus className="h-4 w-4" /> Add your first model</Button>
        </div>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {items.map((model) => (
            <Card key={model.id} className="overflow-hidden">
              <CardHeader className="pb-3">
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0"><CardTitle className="truncate text-lg">{model.name}</CardTitle><p className="mt-1 truncate font-mono text-xs text-muted-foreground">{model.modelName}</p></div>
                  <Badge variant="secondary">{wireLabel(model.wireFormat)}</Badge>
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                <p className="truncate text-sm text-muted-foreground" title={model.endpoint}>{model.endpoint}</p>
                <div className="flex items-center justify-between text-xs text-muted-foreground">
                  <span>{model.maxInputTokens.toLocaleString()} context</span>
                  <span className="flex items-center gap-1"><KeyRound className="h-3.5 w-3.5" />{model.hasApiKey ? 'Key saved' : 'No key'}</span>
                </div>
                <div className="flex justify-end gap-2">
                  <Button variant="outline" size="sm" onClick={() => openEdit(model)}><Pencil className="h-3.5 w-3.5" /> Edit</Button>
                  <Button variant="ghost" size="sm" className="text-destructive hover:text-destructive" onClick={() => setDeleteTarget(model)}><Trash2 className="h-3.5 w-3.5" /></Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
          <DialogHeader><DialogTitle>{editing ? 'Edit custom model' : 'Add custom model'}</DialogTitle><DialogDescription>The endpoint is the full callable URL, including <span className="font-mono">/v1/messages</span> or <span className="font-mono">/v1/responses</span>.</DialogDescription></DialogHeader>
          <div className="grid gap-5 py-2 sm:grid-cols-2">
            <Field label="Display name"><Input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} placeholder="My custom model" /></Field>
            <Field label="Wire format"><Select value={form.wireFormat} onValueChange={(value: CustomModelWireFormat) => setForm({ ...form, wireFormat: value })}><SelectTrigger><SelectValue /></SelectTrigger><SelectContent><SelectItem value="AnthropicMessages">Anthropic Messages</SelectItem><SelectItem value="OpenAIResponses">OpenAI Responses</SelectItem></SelectContent></Select></Field>
            <div className="sm:col-span-2"><Field label="Endpoint"><Input value={form.endpoint} onChange={(e) => setForm({ ...form, endpoint: e.target.value })} placeholder={form.wireFormat === 'AnthropicMessages' ? 'https://host.example/v1/messages' : 'https://host.example/v1/responses'} /></Field></div>
            <Field label="Upstream model name"><Input value={form.modelName} onChange={(e) => setForm({ ...form, modelName: e.target.value })} placeholder="upstream-model-name" /></Field>
            <Field label={`API key (optional${editing?.hasApiKey ? ', leave blank to keep' : ''})`}><Input type="password" autoComplete="new-password" value={form.apiKey} onChange={(e) => setForm({ ...form, apiKey: e.target.value, clearApiKey: false })} placeholder={editing?.hasApiKey ? 'Saved key' : 'No key required'} /></Field>
            {editing?.hasApiKey && <label className="-mt-3 flex items-center gap-2 text-xs text-muted-foreground sm:col-start-2"><input type="checkbox" checked={form.clearApiKey} onChange={(e) => setForm({ ...form, clearApiKey: e.target.checked, apiKey: '' })} /> Remove saved key</label>}
            <Field label="Max input tokens"><Input type="number" min={1} value={form.maxInputTokens} onChange={(e) => setForm({ ...form, maxInputTokens: Number(e.target.value) })} /></Field>
            <Field label="Max output tokens"><Input type="number" min={1} value={form.maxOutputTokens} onChange={(e) => setForm({ ...form, maxOutputTokens: Number(e.target.value) })} /></Field>
            <label className="flex items-center gap-2 text-sm sm:col-span-2"><input type="checkbox" checked={form.supportsTools} onChange={(e) => setForm({ ...form, supportsTools: e.target.checked })} /> This endpoint supports tool/function calling</label>
          </div>
          {testResult && <div className={`rounded-xl border p-3 text-sm ${testResult.success ? 'border-emerald-500/40 bg-emerald-500/10 text-emerald-600' : 'border-destructive/40 bg-destructive/10 text-destructive'}`}>{testResult.message} <span className="opacity-70">({testResult.durationMs} ms)</span></div>}
          {error && <div className="rounded-xl border border-destructive/40 bg-destructive/10 p-3 text-sm text-destructive">{error}</div>}
          <DialogFooter className="gap-2 sm:justify-between">
            <Button variant="outline" onClick={handleTest} disabled={testing || !form.endpoint || !form.modelName}>{testing ? <Loader2 className="h-4 w-4 animate-spin" /> : <TestTube2 className="h-4 w-4" />} Test</Button>
            <div className="flex gap-2"><Button variant="ghost" onClick={() => setDialogOpen(false)}>Cancel</Button><Button onClick={handleSave} disabled={saving || !form.name || !form.endpoint || !form.modelName}>{saving && <Loader2 className="h-4 w-4 animate-spin" />} Save model</Button></div>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={!!deleteTarget} onOpenChange={(open) => !open && setDeleteTarget(null)}>
        <DialogContent><DialogHeader><DialogTitle>Delete {deleteTarget?.name}?</DialogTitle><DialogDescription>Existing agent definitions selecting this model will need another model before they can run.</DialogDescription></DialogHeader><DialogFooter><Button variant="ghost" onClick={() => setDeleteTarget(null)}>Cancel</Button><Button variant="destructive" onClick={handleDelete}>Delete model</Button></DialogFooter></DialogContent>
      </Dialog>
    </div>
  )
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return <div className="space-y-2"><Label>{label}</Label>{children}</div>
}

function wireLabel(wireFormat: CustomModelWireFormat) {
  return wireFormat === 'AnthropicMessages' ? 'Messages' : 'Responses'
}

function errorMessage(error: unknown) {
  return error instanceof Error ? error.message : 'Something went wrong.'
}
