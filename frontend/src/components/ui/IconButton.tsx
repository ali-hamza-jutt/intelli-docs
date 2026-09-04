import Link from "next/link";
import { cn } from "@/lib/cn";
import { Icon, type IconName } from "./Icon";

type Props = {
  icon: IconName;
  /** Required — these buttons have no visible text. */
  label: string;
  size?: "sm" | "md";
  tone?: "plain" | "bordered" | "danger";
  className?: string;
};

const TONES = {
  plain: "icon-btn-plain",
  bordered: "icon-btn-bordered",
  danger: "icon-btn-plain icon-btn-danger",
};

function iconButtonClass({ size = "md", tone = "plain", className }: Props) {
  return cn("icon-btn", size === "sm" ? "icon-btn-sm" : "icon-btn-md", TONES[tone], className);
}

export function IconButton({
  icon,
  label,
  size,
  tone,
  className,
  ...rest
}: Props & React.ButtonHTMLAttributes<HTMLButtonElement>) {
  return (
    <button
      aria-label={label}
      title={label}
      className={iconButtonClass({ icon, label, size, tone, className })}
      {...rest}
    >
      <Icon name={icon} />
    </button>
  );
}

export function IconButtonLink(props: Props & { href: string }) {
  return (
    <Link href={props.href} aria-label={props.label} title={props.label} className={iconButtonClass(props)}>
      <Icon name={props.icon} />
    </Link>
  );
}
