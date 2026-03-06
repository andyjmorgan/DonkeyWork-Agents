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

  // Convert task lists UL to have data-type="taskList"
  const parser = new DOMParser()
  const doc = parser.parseFromString(html, 'text/html')

  // Find all list items that contain task list checkboxes
  const listItems = doc.querySelectorAll('li')
  listItems.forEach((li) => {
    const checkbox = li.querySelector('input[type="checkbox"]')
    if (checkbox) {
      const ul = li.parentElement
      if (ul && ul.tagName === 'UL') {
        ul.setAttribute('data-type', 'taskList')
        li.setAttribute('data-type', 'taskItem')
        li.setAttribute('data-checked', checkbox.hasAttribute('checked') ? 'true' : 'false')
      }
    }
  })

  // Remove trailing newlines from code blocks
  const codeBlocks = doc.querySelectorAll('pre code')
  codeBlocks.forEach((code) => {
    if (code.textContent) {
      code.textContent = code.textContent.replace(/\n+$/, '')
    }
  })

  return doc.body.innerHTML
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

// Custom rule for code blocks to remove trailing newlines
turndownService.addRule('codeBlock', {
  filter: (node) => {
    return node.nodeName === 'PRE' && node.firstChild?.nodeName === 'CODE'
  },
  replacement: (content, node) => {
    const preElement = node as HTMLElement
    const codeElement = node.firstChild as HTMLElement

    // Check for language in data-language attribute or class
    const dataLanguage = preElement.getAttribute('data-language')
    const classLanguage = codeElement?.className.replace('language-', '')
    const language = dataLanguage || classLanguage || ''

    const trimmedContent = content.replace(/\n+$/, '') // Remove trailing newlines
    return `\n\`\`\`${language}\n${trimmedContent}\n\`\`\`\n`
  },
})

// Custom rule for GFM-style tables
turndownService.addRule('table', {
  filter: 'table',
  replacement: (_content, node) => {
    const table = node as HTMLTableElement
    const rows = Array.from(table.querySelectorAll('tr'))

    if (rows.length === 0) return ''

    let markdown = '\n'

    rows.forEach((row, rowIndex) => {
      const cells = Array.from(row.querySelectorAll('th, td'))
      const cellContents = cells.map(cell => {
        return cell.textContent?.trim().replace(/\n/g, ' ') || ''
      })

      markdown += '| ' + cellContents.join(' | ') + ' |\n'

      // Add separator row after header
      if (rowIndex === 0 && row.querySelector('th')) {
        const separator = cells.map(() => '---').join(' | ')
        markdown += '| ' + separator + ' |\n'
      }
    })

    markdown += '\n'
    return markdown
  },
})

/**
 * Convert HTML from Tiptap editor to markdown
 */
export function serializeMarkdown(html: string): string {
  if (!html) return ''
  return turndownService.turndown(html)
}
