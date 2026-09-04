import { cn } from "@/lib/cn";
import { Icon, type IconName } from "@/components/ui/Icon";

/** Full-bleed section with a centred, width-capped inner column. */
export function Section({
  id,
  children,
  className,
  tone = "surface",
}: {
  id?: string;
  children: React.ReactNode;
  className?: string;
  tone?: "surface" | "canvas" | "inverse";
}) {
  const TONES = {
    surface: "bg-surface",
    canvas: "bg-canvas",
    inverse: "bg-inverse text-white",
  };

  return (
    <section id={id} className={cn(TONES[tone], className)}>
      <div className="mx-auto w-full max-w-[1200px] px-6 py-20">{children}</div>
    </section>
  );
}

export function SectionHeading({
  eyebrow,
  title,
  body,
  centered = true,
}: {
  eyebrow?: string;
  title: string;
  body?: string;
  centered?: boolean;
}) {
  return (
    <div className={cn("mb-12", centered && "mx-auto max-w-[620px] text-center")}>
      {eyebrow && <p className="mb-3 eyebrow text-brand">{eyebrow}</p>}
      <h2 className="m-0 text-[clamp(28px,4vw,40px)] font-bold leading-tight tracking-[-0.03em]">
        {title}
      </h2>
      {body && <p className="mt-4 mb-0 text-lg leading-relaxed text-muted">{body}</p>}
    </div>
  );
}

/** Icon + title + copy — used by the problem, feature and step grids. */
export function FeatureCard({
  icon,
  title,
  body,
  className,
}: {
  icon: IconName;
  title: string;
  body: string;
  className?: string;
}) {
  return (
    <div className={cn("card card-pad card-hoverable", className)}>
      <span className="tile tile-brand size-10 text-xl">
        <Icon name={icon} />
      </span>
      <h3 className="mt-4 mb-1.5 text-md font-semibold">{title}</h3>
      <p className="m-0 text-body leading-relaxed text-muted">{body}</p>
    </div>
  );
}
