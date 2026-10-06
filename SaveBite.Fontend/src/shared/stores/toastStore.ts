import { create } from "zustand";

export type ToastType = "success" | "error" | "info";

export interface ToastItem {
  id: number;
  type: ToastType;
  message: string;
  duration: number;
}

interface ToastState {
  toasts: ToastItem[];
  addToast: (toast: Omit<ToastItem, "id">) => number;
  removeToast: (id: number) => void;
}

let nextToastId = 1;

export const useToastStore = create<ToastState>((set) => ({
  toasts: [],

  addToast: (toast) => {
    const id = nextToastId++;

    set((state) => ({
      toasts: [...state.toasts, { ...toast, id }],
    }));

    return id;
  },

  removeToast: (id) => {
    set((state) => ({
      toasts: state.toasts.filter((toast) => toast.id !== id),
    }));
  },
}));

function showToast(
  type: ToastType,
  message: string,
  duration = 4000,
): number {
  const {
    addToast,
    removeToast,
    toasts,
  } = useToastStore.getState();

  const duplicate = toasts.find(
    (toast) =>
      toast.type === type &&
      toast.message === message,
  );

  if (duplicate) {
    return duplicate.id;
  }

  const id = addToast({
    type,
    message,
    duration,
  });

  window.setTimeout(() => {
    removeToast(id);
  }, duration);

  return id;
}

export const toast = {
  success(message: string, duration?: number) {
    return showToast("success", message, duration);
  },

  error(message: string, duration = 5000) {
    return showToast("error", message, duration);
  },

  info(message: string, duration?: number) {
    return showToast("info", message, duration);
  },

  dismiss(id: number) {
    useToastStore.getState().removeToast(id);
  },
};