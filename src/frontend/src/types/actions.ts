/**
 * Auto-generated action schema types
 * These match the C# schema models from the backend
 */

export interface ActionNodeSchema {
  actionType: string
  displayName: string
  category: string
  group?: string
  icon?: string
  description?: string
  maxInputs: number
  maxOutputs: number
  enabled: boolean
  parameters: ParameterSchema[]
}

export interface ParameterSchema {
  name: string
  displayName: string
  description?: string
  type: string
  required: boolean
  defaultValue?: string
  supportsVariables: boolean
  editorType?: string
  controlType?: string
  options?: OptionSchema[]
  validation?: ValidationSchema
  resolvable: boolean
  credentialTypes?: string[]
  dependency?: DependencySchema
}

export interface OptionSchema {
  label: string
  value: string
}

export interface ValidationSchema {
  min?: number
  max?: number
  minLength?: number
  maxLength?: number
  pattern?: string
  step?: number
}

export interface DependencySchema {
  parameterName: string
  showIf?: string
}

/**
 * Runtime parameter value (can be literal or expression)
 */
export interface ParameterValue {
  [key: string]: string | number | boolean | null
}
