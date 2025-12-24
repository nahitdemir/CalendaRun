"use client";

import { useCallback } from "react";
import { useToast } from "@/components/ui/toast";
import { useTranslation } from "@/contexts/locale-context";
import { ApiError } from "@/lib/api-client";

export function useApiError() {
  const toast = useToast();
  const { t } = useTranslation();

  const handleError = useCallback(
    (error: unknown, fallbackMessage?: string) => {
      if (error instanceof ApiError) {
        const title = error.title || t("toast.error");
        const description = error.detail;

        // Show validation errors if present
        if (error.validationErrors) {
          const errorMessages = Object.entries(error.validationErrors)
            .map(([field, messages]) => `${field}: ${messages.join(", ")}`)
            .join("\n");
          toast.error(title, errorMessages);
        } else {
          toast.error(title, description);
        }

        // Log trace ID for debugging
        if (error.traceId) {
          console.error(`API Error [${error.traceId}]:`, error);
        }

        return error.validationErrors;
      }

      // Generic error
      toast.error(fallbackMessage || t("toast.error"));
      console.error("Unexpected error:", error);
      return undefined;
    },
    [toast, t]
  );

  return { handleError };
}

// Convenience hook for mutation success/error toasts
export function useApiMutation() {
  const toast = useToast();
  const { t } = useTranslation();
  const { handleError } = useApiError();

  const onSuccess = useCallback(
    (messageKey: string = "toast.updated") => {
      toast.success(t(messageKey));
    },
    [toast, t]
  );

  const onError = useCallback(
    (error: unknown) => {
      return handleError(error);
    },
    [handleError]
  );

  return { onSuccess, onError, handleError };
}

