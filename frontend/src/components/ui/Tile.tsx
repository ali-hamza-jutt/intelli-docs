import { cn } from "@/lib/cn";
import { Icon, type IconName } from "./Icon";

const TONES = {
  neutral: "tile-neutral",
  brand: "tile-brand",
  danger: "tile-danger",
};

/** Rounded square holding a glyph — file rows, collection cards, empty states. */
export function Tile({
  icon,
  tone = "neutral",
  className,
}: {
  icon: IconName;
  tone?: keyof typeof TONES;
  className?: string;
}) {
  return (
    <span className={cn("tile", TONES[tone], className)}>
      <Icon name={icon} />
    </span>
  );
}
