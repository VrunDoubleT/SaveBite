import { useEffect, type ReactNode } from "react";
import { X } from "lucide-react";

interface SharedModalProps {
  isOpen: boolean;
  onClose: () => void;
  closeOnClickOutside?: boolean;
  title?: string;
  children: ReactNode;
  maxWidthClass?: string;
}

// Number of modals currently open. The page scroll is only unlocked
// when this reaches 0, so stacked modals don't unlock it too early.
let openModalCount = 0;

export function SharedModal({
  isOpen,
  onClose,
  closeOnClickOutside = true,
  title,
  children,
  maxWidthClass = "max-w-md",
}: SharedModalProps) {
  // Lock the page scroll while this modal is open
  useEffect(() => {
    if (!isOpen) return;

    openModalCount += 1;
    document.body.style.overflow = "hidden";

    return () => {
      openModalCount -= 1;
      if (openModalCount === 0) {
        document.body.style.overflow = "";
      }
    };
  }, [isOpen]);

  if (!isOpen) return null;

  const handleBackdropClick = (e: React.MouseEvent<HTMLDivElement>) => {
    if (closeOnClickOutside && e.target === e.currentTarget) {
      onClose();
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-neutral-900/40 p-4 backdrop-blur-sm transition-opacity"
      onMouseDown={handleBackdropClick}
    >
      <div
        className={`w-full ${maxWidthClass} rounded-xl bg-white shadow-xl animate-in fade-in zoom-in-95 duration-200 flex flex-col max-h-[90vh]`}
      >
        {title && (
          <div className="flex justify-between items-center p-5 border-b border-border-default shrink-0">
            <h3 className="text-lg font-bold text-text-primary">{title}</h3>
            <button
              type="button"
              onClick={onClose}
              className="text-neutral-400 hover:text-neutral-700 p-1 rounded-md transition-colors"
            >
              <X className="size-5" />
            </button>
          </div>
        )}
        <div className="overflow-y-auto p-6">{children}</div>
      </div>
    </div>
  );
}
