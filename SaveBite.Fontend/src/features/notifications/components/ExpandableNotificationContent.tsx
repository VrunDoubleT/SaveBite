import { useEffect, useId, useRef, useState, type ReactNode } from "react";
import { ChevronDown, ChevronUp } from "lucide-react";

export function ExpandableNotificationContent({
  title,
  body,
  onSelect,
  children,
  unread = false,
}: {
  title: string;
  body?: string | null;
  onSelect: () => void;
  children?: ReactNode;
  unread?: boolean;
}) {
  const [expanded, setExpanded] = useState(false);
  const [overflowing, setOverflowing] = useState(false);
  const bodyRef = useRef<HTMLParagraphElement>(null);
  const titleRef = useRef<HTMLParagraphElement>(null);
  const id = useId();
  useEffect(() => {
    const measure = () => {
      if (expanded) return;
      const text = bodyRef.current;
      const heading = titleRef.current;
      setOverflowing(
        Boolean(
          (text && text.scrollHeight > text.clientHeight + 1) ||
          (heading && heading.scrollWidth > heading.clientWidth + 1),
        ),
      );
    };
    const observer = new ResizeObserver(measure);
    if (bodyRef.current) observer.observe(bodyRef.current);
    if (titleRef.current) observer.observe(titleRef.current);
    measure();
    return () => observer.disconnect();
  }, [title, body, expanded]);
  return (
    <div className="min-w-0 flex-1">
      <button
        type="button"
        onClick={onSelect}
        className="block w-full min-w-0 text-left focus-visible:outline-2 focus-visible:outline-primary-600"
      >
        <div id={id}>
          <p
            ref={titleRef}
            className={`text-xs leading-4 ${expanded ? "whitespace-pre-wrap break-words [overflow-wrap:anywhere]" : "truncate"} ${unread ? "font-bold text-neutral-900" : "font-semibold text-neutral-700"}`}
          >
            {title}
            {unread && (
              <span
                className="ml-1.5 inline-block size-1.5 rounded-full bg-emerald-600"
                aria-label="Unread"
              />
            )}
          </p>
          <p
            ref={bodyRef}
            className={`mt-1 text-xs leading-4.5 text-neutral-600 ${expanded ? "whitespace-pre-wrap break-words [overflow-wrap:anywhere]" : "line-clamp-2"}`}
          >
            {body || "—"}
          </p>
        </div>
        {children}
      </button>
      {(overflowing || expanded) && (
        <button
          type="button"
          aria-expanded={expanded}
          aria-controls={id}
          onClick={() => setExpanded((value) => !value)}
          className="mt-1 inline-flex min-h-9 items-center gap-1 rounded-md px-1 text-xs font-semibold text-primary-700 hover:bg-primary-50 focus-visible:outline-2 focus-visible:outline-primary-600"
        >
          {expanded ? "Show less" : "Show more"}
          {expanded ? (
            <ChevronUp size={14} aria-hidden="true" />
          ) : (
            <ChevronDown size={14} aria-hidden="true" />
          )}
        </button>
      )}
    </div>
  );
}
