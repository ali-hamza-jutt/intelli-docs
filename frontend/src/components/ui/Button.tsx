import Link from "next/link";
import { cn } from "@/lib/cn";
import { Icon, type IconName } from "./Icon";

type Variant = "primary" | "secondary" | "ghost" | "danger" | "disabled";
type Size = "sm" | "md" | "lg";

const VARIANTS: Record<Variant, string> = {
  primary: "btn-primary",
  secondary: "btn-secondary",
  ghost: "btn-ghost",
  danger: "btn-danger",
  disabled: "btn-disabled",
};

const SIZES: Record<Size, string> = {
  sm: "btn-sm",
  md: "btn-md",
  lg: "btn-lg",
};

type BaseProps = {
  variant?: Variant;
  size?: Size;
  /** Glyph rendered before the label. */
  icon?: IconName;
  /** Glyph rendered after the label — use for forward actions. */
  trailingIcon?: IconName;
  fullWidth?: boolean;
  className?: string;
  children?: React.ReactNode;
};

function buttonClass({ variant = "primary", size = "md", fullWidth, className }: BaseProps) {
  return cn("btn", VARIANTS[variant], SIZES[size], fullWidth && "w-full", className);
}

export function Button({
  variant,
  size,
  icon,
  trailingIcon,
  fullWidth,
  className,
  children,
  ...rest
}: BaseProps & React.ButtonHTMLAttributes<HTMLButtonElement>) {
  return (
    <button className={buttonClass({ variant, size, fullWidth, className })} {...rest}>
      {icon && <Icon name={icon} className="text-lg" />}
      {children}
      {trailingIcon && <Icon name={trailingIcon} className="text-md" />}
    </button>
  );
}

export function ButtonLink({
  href,
  variant,
  size,
  icon,
  trailingIcon,
  fullWidth,
  className,
  children,
}: BaseProps & { href: string }) {
  return (
    <Link href={href} className={buttonClass({ variant, size, fullWidth, className })}>
      {icon && <Icon name={icon} className="text-lg" />}
      {children}
      {trailingIcon && <Icon name={trailingIcon} className="text-md" />}
    </Link>
  );
}
