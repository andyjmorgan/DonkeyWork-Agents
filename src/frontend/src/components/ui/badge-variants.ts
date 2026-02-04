import { cva } from "class-variance-authority"

export const badgeVariants = cva(
  "inline-flex items-center gap-1.5 rounded-full border px-3 py-1.5 text-xs font-medium transition-colors focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2",
  {
    variants: {
      variant: {
        default:
          "bg-accent/20 text-accent border-accent/30",
        secondary:
          "bg-secondary text-secondary-foreground border-border",
        destructive:
          "bg-destructive/20 text-destructive border-destructive/30",
        outline: "text-foreground border-border",
        // Status variants matching design system
        success:
          "bg-emerald-500/20 text-emerald-500 border-emerald-500/30 dark:text-emerald-400 dark:border-emerald-400/30",
        warning:
          "bg-amber-500/20 text-amber-600 border-amber-500/30 dark:text-amber-400 dark:border-amber-400/30",
        pending:
          "bg-cyan-500/20 text-cyan-600 border-cyan-500/30 dark:text-cyan-400 dark:border-cyan-400/30",
        muted:
          "bg-muted text-muted-foreground border-border",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  }
)
