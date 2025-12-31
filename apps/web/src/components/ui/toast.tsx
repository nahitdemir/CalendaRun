"use client";

import { createContext, useContext, useState, useCallback, ReactNode } from "react";
import { X, CheckCircle2, AlertCircle, Info, AlertTriangle } from "lucide-react";
import { cn } from "@/lib/utils";

type ToastType = "success" | "error" | "info" | "warning";

interface ToastAction {
  label: string;
  onClick: () => void;
}

interface Toast {
  id: string;
  type: ToastType;
  title: string;
  description?: string;
  action?: ToastAction;
}

interface ToastContextType {
  toasts: Toast[];
  addToast: (
    type: ToastType,
    title: string,
    description?: string,
    action?: ToastAction
  ) => void;
  removeToast: (id: string) => void;
  success: (title: string, description?: string, action?: ToastAction) => void;
  error: (title: string, description?: string, action?: ToastAction) => void;
  info: (title: string, description?: string, action?: ToastAction) => void;
  warning: (title: string, description?: string, action?: ToastAction) => void;
}

const ToastContext = createContext<ToastContextType | undefined>(undefined);

const toastIcons: Record<ToastType, typeof CheckCircle2> = {
  success: CheckCircle2,
  error: AlertCircle,
  info: Info,
  warning: AlertTriangle,
};

const toastStyles: Record<ToastType, string> = {
  success: "border-success/50 bg-success/10 text-success",
  error: "border-destructive/50 bg-destructive/10 text-destructive",
  info: "border-primary/50 bg-primary/10 text-primary",
  warning: "border-warning/50 bg-warning/10 text-warning",
};

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const addToast = useCallback(
    (type: ToastType, title: string, description?: string, action?: ToastAction) => {
      const id = Math.random().toString(36).substring(2, 9);
      setToasts((prev) => [...prev, { id, type, title, description, action }]);

      // Auto remove after 5 seconds
      setTimeout(() => {
        setToasts((prev) => prev.filter((t) => t.id !== id));
      }, 5000);
    },
    []
  );

  const removeToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const success = useCallback(
    (title: string, description?: string, action?: ToastAction) =>
      addToast("success", title, description, action),
    [addToast]
  );

  const error = useCallback(
    (title: string, description?: string, action?: ToastAction) =>
      addToast("error", title, description, action),
    [addToast]
  );

  const info = useCallback(
    (title: string, description?: string, action?: ToastAction) =>
      addToast("info", title, description, action),
    [addToast]
  );

  const warning = useCallback(
    (title: string, description?: string, action?: ToastAction) =>
      addToast("warning", title, description, action),
    [addToast]
  );

  return (
    <ToastContext.Provider
      value={{ toasts, addToast, removeToast, success, error, info, warning }}
    >
      {children}
      <ToastContainer toasts={toasts} onRemove={removeToast} />
    </ToastContext.Provider>
  );
}

function ToastContainer({
  toasts,
  onRemove,
}: {
  toasts: Toast[];
  onRemove: (id: string) => void;
}) {
  if (toasts.length === 0) return null;

  return (
    <div className="fixed bottom-4 right-4 z-[100] flex flex-col gap-2 md:bottom-6 md:right-6">
      {toasts.map((toast) => {
        const Icon = toastIcons[toast.type];
        return (
          <div
            key={toast.id}
            className={cn(
              "flex w-80 items-start gap-3 rounded-xl border p-4 shadow-lg backdrop-blur animate-slide-up",
              toastStyles[toast.type]
            )}
          >
            <Icon className="mt-0.5 h-5 w-5 shrink-0" />
            <div className="flex-1 space-y-1">
              <p className="font-medium">{toast.title}</p>
              {toast.description && (
                <p className="text-sm opacity-80">{toast.description}</p>
              )}
              {toast.action && (
                <button
                  onClick={() => {
                    onRemove(toast.id);
                    toast.action?.onClick();
                  }}
                  className="mt-1 inline-flex items-center rounded-md border border-current/20 px-2 py-1 text-xs font-medium opacity-90 transition-opacity hover:opacity-100"
                >
                  {toast.action.label}
                </button>
              )}
            </div>
            <button
              onClick={() => onRemove(toast.id)}
              className="shrink-0 rounded p-1 opacity-60 transition-opacity hover:opacity-100"
            >
              <X className="h-4 w-4" />
            </button>
          </div>
        );
      })}
    </div>
  );
}

export function useToast() {
  const context = useContext(ToastContext);
  if (context === undefined) {
    throw new Error("useToast must be used within a ToastProvider");
  }
  return context;
}
