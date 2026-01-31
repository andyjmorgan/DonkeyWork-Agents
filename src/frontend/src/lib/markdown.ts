import { marked } from 'marked'
import TurndownService from 'turndown'

// Configure marked for GitHub Flavored Markdown
marked.setOptions({
  gfm: true,
  breaks: true,
})

/**
 * Convert markdown to HTML for Tiptap editor
 */
export function parseMarkdown(markdown: string): string {
  if (!markdown) return ''

  // Convert markdown to HTML
  let html = marked.parse(markdown, { async: false }) as string

  // Convert GFM task list syntax to Tiptap-compatible format
  html = html.replace(
    /<li><input type="checkbox" disabled(?:\s+checked)?>\s*/g,
    (match) => {
      const isChecked = match.includes('checked')
      return `<li data-type="taskItem" data-checked="${isChecked}">`
    }
  )

  return html
}

// Configure turndown for HTML to markdown conversion
const turndownService = new TurndownService({
  headingStyle: 'atx',
  codeBlockStyle: 'fenced',
  bulletListMarker: '-',
})

// Custom rule for task list items
turndownService.addRule('taskListItem', {
  filter: (node) => {
    return (
      node.nodeName === 'LI' &&
      node.getAttribute('data-type') === 'taskItem'
    )
  },
  replacement: (content, node) => {
    const element = node as HTMLElement
    const isChecked = element.getAttribute('data-checked') === 'true'
    const checkbox = isChecked ? '[x]' : '[ ]'
    const trimmedContent = content.trim().replace(/^\n+/, '')
    return `- ${checkbox} ${trimmedContent}\n`
  },
})

// Custom rule for strikethrough
turndownService.addRule('strikethrough', {
  filter: ['s', 'del'],
  replacement: (content) => `~~${content}~~`,
})

// Custom rule for underline (convert to emphasis since markdown doesn't support underline)
turndownService.addRule('underline', {
  filter: 'u',
  replacement: (content) => `_${content}_`,
})

/**
 * Convert HTML from Tiptap editor to markdown
 */
export function serializeMarkdown(html: string): string {
  if (!html) return ''
  return turndownService.turndown(html)
}
