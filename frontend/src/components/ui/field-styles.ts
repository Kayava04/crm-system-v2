// Shared chrome for every text-like control, so inputs, textareas and select
// triggers line up and react to hover/focus/error identically.
export const fieldClasses = [
  'w-full rounded-lg border border-input bg-card px-3 text-sm shadow-xs',
  'transition-[border-color,box-shadow] duration-150',
  'placeholder:text-muted-foreground hover:border-foreground/20',
  'outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/25',
  'disabled:cursor-not-allowed disabled:opacity-50 disabled:hover:border-input',
  'aria-invalid:border-destructive aria-invalid:focus-visible:ring-destructive/20',
]
