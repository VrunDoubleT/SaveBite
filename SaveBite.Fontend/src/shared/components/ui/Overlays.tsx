import { useEffect, useId, useRef, type ReactNode } from "react";
import { X } from "lucide-react";
import { IconButton } from "./Controls";

let openDialogs = 0;
let previousBodyOverflow = "";

/** Native dialog supplies focus trapping, Escape handling and focus restoration. */
export function Modal({
  isOpen,
  onClose,
  title,
  headerContent,
  children,
  drawer = false,
  hideHeader = false,
  closeOnClickOutside = true,
  maxWidthClass = "max-w-lg",
}: {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  headerContent?: ReactNode;
  children: ReactNode;
  drawer?: boolean;
  hideHeader?: boolean;
  closeOnClickOutside?: boolean;
  maxWidthClass?: string;
}) {
  const ref = useRef<HTMLDialogElement>(null);
  const titleId = useId();
  useEffect(() => {
    const dialog = ref.current;
    if (!isOpen || !dialog) return;
    const previous = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    if (openDialogs === 0) previousBodyOverflow = document.body.style.overflow;
    openDialogs += 1;
    dialog.showModal();
    document.body.style.overflow = "hidden";
    return () => {
      dialog.close();
      openDialogs -= 1;
      if (openDialogs === 0) document.body.style.overflow = previousBodyOverflow;
      previous?.focus();
    };
  }, [isOpen]);
  if (!isOpen) return null;
  return (
    <dialog
      ref={ref}
      aria-labelledby={titleId}
      onCancel={(event) => {
        event.preventDefault();
        onClose();
      }}
      onKeyDown={(event) => {
        if (event.key !== "Tab") return;
        const controls = Array.from(
          event.currentTarget.querySelectorAll<HTMLElement>(
            "button:not([disabled]), a[href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]",
          ),
        ).filter((element) => element.tabIndex >= 0 && element.getClientRects().length > 0);
        const first = controls[0];
        const last = controls[controls.length - 1];
        if (!first) {
          event.preventDefault();
          return;
        }
        if (event.shiftKey && document.activeElement === first) {
          event.preventDefault();
          last.focus();
        } else if (!event.shiftKey && document.activeElement === last) {
          event.preventDefault();
          first.focus();
        }
      }}
      onClick={(event) => {
        if (closeOnClickOutside && event.target === event.currentTarget) {
          const rect = event.currentTarget.getBoundingClientRect();
          if (
            event.clientX < rect.left ||
            event.clientX > rect.right ||
            event.clientY < rect.top ||
            event.clientY > rect.bottom
          )
            onClose();
        }
      }}
      className={`fixed max-h-[calc(100dvh-2rem)] w-[calc(100%-2rem)] overflow-auto border border-border-default bg-bg-surface p-0 text-text-primary shadow-lg ${drawer ? "inset-y-0 right-auto left-0 m-0 h-dvh max-h-dvh max-w-sm rounded-none" : `inset-0 m-auto rounded-lg ${maxWidthClass}`}`}
    >
      {hideHeader ? (
        <h2 id={titleId} className="sr-only">
          {title}
        </h2>
      ) : (
        <div className="flex items-start justify-between gap-4 border-b border-border-default p-4 sm:px-6">
          <div className="flex min-w-0 flex-col items-start gap-1">
            <h2 id={titleId} className="text-section font-semibold">
              {title}
            </h2>
            {headerContent}
          </div>
          <IconButton aria-label="Close dialog" onClick={onClose}>
            <X className="size-5" aria-hidden="true" />
          </IconButton>
        </div>
      )}
      <div className="p-4 sm:p-6">{children}</div>
    </dialog>
  );
}
export function Drawer(props: Omit<Parameters<typeof Modal>[0], "drawer">) {
  return <Modal {...props} drawer />;
}
