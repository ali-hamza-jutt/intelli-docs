import { Icon, type IconName } from "./Icon";

/** Shared "nothing here yet" panel for documents and collections. */
export function EmptyState({
  icon,
  title,
  body,
  action,
}: {
  icon: IconName;
  title: string;
  body: string;
  action?: React.ReactNode;
}) {
  return (
    <div className="card px-6 py-16 text-center">
      <span className="tile tile-neutral mx-auto size-12 rounded-[13px] text-[22px]">
        <Icon name={icon} />
      </span>
      <p className="mt-[18px] mb-1.5 text-lg font-semibold">{title}</p>
      <p className="mb-5 text-base text-muted">{body}</p>
      {action}
    </div>
  );
}
