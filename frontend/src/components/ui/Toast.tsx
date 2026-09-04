"use client";

import { createContext, useCallback, useContext, useMemo, useState } from "react";
import { Icon } from "./Icon";
import { cn } from "@/lib/cn";

type Toast = { id: string; text: string; kind: "success" | "warn" };

const ToastContext = createContext<(text: string, kind?: Toast["kind"]) => void>(() => {});

/** Call from anywhere under ToastProvider to raise a transient message. */
export const useToast = () => useContext(ToastContext);

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const push = useCallback((text: string, kind: Toast["kind"] = "success") => {
    const id = Math.random().toString(36).slice(2);
    setToasts((prev) => [...prev, { id, text, kind }]);
    setTimeout(() => setToasts((prev) => prev.filter((t) => t.id !== id)), 3200);
  }, []);

  const value = useMemo(() => push, [push]);

  return (
    <ToastContext.Provider value={value}>
      {children}
      <div
        aria-live="polite"
        className="pointer-events-none fixed right-5 bottom-5 z-90 flex flex-col gap-2.5"
      >
        {toasts.map((toast) => (
          <div
            key={toast.id}
            className="flex items-center gap-2.5 rounded-control bg-inverse px-4 py-3 text-body font-medium text-white shadow-toast animate-sheet-up"
          >
            <Icon
              name={toast.kind === "warn" ? "alert" : "check"}
              className={cn("text-md", toast.kind === "warn" ? "text-warning" : "text-success")}
            />
            {toast.text}
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}
