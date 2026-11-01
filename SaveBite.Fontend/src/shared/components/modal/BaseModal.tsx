import type { ReactNode } from "react";
import { Modal } from "@/shared/components/ui/Overlays";

interface SharedModalProps {
  isOpen: boolean;
  onClose: () => void;
  closeOnClickOutside?: boolean;
  title?: string;
  children: ReactNode;
  maxWidthClass?: string;
}
// Retain the existing API; the shared primitive supplies dialog focus and Escape handling.
export function SharedModal({ title, maxWidthClass = "max-w-md", ...props }: SharedModalProps) {
  return (
    <Modal {...props} title={title ?? "Dialog"} hideHeader={!title} maxWidthClass={maxWidthClass} />
  );
}
