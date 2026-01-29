import ReactJson from '@microlink/react-json-view'
import { useThemeStore } from '@/store/theme'

interface JsonViewerProps {
  data: unknown
  collapsed?: number
  name?: string | false
  className?: string
}

export function JsonViewer({ data, collapsed = 2, name = false, className }: JsonViewerProps) {
  const { theme } = useThemeStore()
  const isDark = theme === 'dark'

  // Parse string data if needed
  let parsedData = data
  if (typeof data === 'string') {
    try {
      parsedData = JSON.parse(data)
    } catch {
      // If parsing fails, wrap in an object to display as string
      parsedData = { value: data }
    }
  }

  // Handle null/undefined
  if (parsedData === null || parsedData === undefined) {
    return (
      <div className={`text-sm text-muted-foreground ${className}`}>
        No data
      </div>
    )
  }

  return (
    <div className={`rounded-md overflow-auto ${className}`}>
      <ReactJson
        src={parsedData as object}
        theme={isDark ? 'monokai' : 'rjv-default'}
        collapsed={collapsed}
        name={name}
        displayDataTypes={false}
        displayObjectSize={false}
        enableClipboard={true}
        style={{
          padding: '12px',
          borderRadius: '6px',
          fontSize: '12px',
          backgroundColor: isDark ? 'hsl(var(--muted))' : 'hsl(var(--muted))',
        }}
      />
    </div>
  )
}
