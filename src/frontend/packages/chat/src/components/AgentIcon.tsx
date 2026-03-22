import { icons } from "lucide-react";

function isUrl(value: string): boolean {
  return value.startsWith("http://") || value.startsWith("https://") || value.startsWith("/") || value.startsWith("data:");
}

function toLucidePascalCase(name: string): string {
  return name
    .split(/[-_\s]+/)
    .map((s) => s.charAt(0).toUpperCase() + s.slice(1))
    .join("");
}

export function AgentIcon({
  icon,
  className = "w-4 h-4",
  fallbackClassName,
}: {
  icon?: string;
  className?: string;
  fallbackClassName?: string;
}) {
  if (!icon) return null;

  if (isUrl(icon)) {
    return (
      <img
        src={icon}
        alt=""
        className={`${className} rounded-sm object-contain`}
      />
    );
  }

  const pascalName = toLucidePascalCase(icon);
  const LucideIcon = icons[pascalName as keyof typeof icons];
  if (LucideIcon) {
    return <LucideIcon className={`${className} ${fallbackClassName ?? ""}`} />;
  }

  return null;
}
