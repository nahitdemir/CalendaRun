import { useCallback, useMemo } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useDebouncedValue } from "@/hooks/use-debounced-value";

interface UseListQueryParamsOptions {
  debounceMs?: number;
  pageKey?: string;
  pageSizeKey?: string;
  defaults?: Record<string, string | number>;
  filterKeys?: string[];
}

interface UpdateOptions {
  resetPage?: boolean;
}

export function useListQueryParams(options: UseListQueryParamsOptions = {}) {
  const {
    debounceMs = 400,
    pageKey = "page",
    pageSizeKey = "pageSize",
    defaults = {},
    filterKeys = [],
  } = options;

  const router = useRouter();
  const pathname = usePathname() || "/";
  const searchParams = useSearchParams();
  const debouncedQuery = useDebouncedValue(searchParams.toString(), debounceMs);
  const debouncedSearchParams = useMemo(
    () => new URLSearchParams(debouncedQuery),
    [debouncedQuery]
  );

  const getParam = useCallback(
    (key: string, fallback = "") => searchParams.get(key) ?? fallback,
    [searchParams]
  );

  const getNumberParam = useCallback(
    (key: string, fallback: number) => {
      const raw = searchParams.get(key);
      if (!raw) return fallback;
      const value = Number(raw);
      return Number.isNaN(value) ? fallback : value;
    },
    [searchParams]
  );

  const updateParams = useCallback(
    (updates: Record<string, string | number | null | undefined>, opts: UpdateOptions = {}) => {
      const params = new URLSearchParams(searchParams.toString());
      const updateKeys = Object.keys(updates);
      const shouldResetPage =
        opts.resetPage ?? updateKeys.some((key) => filterKeys.includes(key));

      updateKeys.forEach((key) => {
        const value = updates[key];
        if (value === undefined) return;

        if (key === pageKey) {
          const pageValue = Number(value);
          if (!value || Number.isNaN(pageValue) || pageValue <= 1) {
            params.delete(key);
          } else {
            params.set(key, String(pageValue));
          }
          return;
        }

        if (key === pageSizeKey) {
          const pageSizeValue = Number(value);
          const defaultPageSize =
            typeof defaults[pageSizeKey] === "number" ? defaults[pageSizeKey] : 20;
          if (
            !value ||
            Number.isNaN(pageSizeValue) ||
            pageSizeValue === defaultPageSize
          ) {
            params.delete(key);
          } else {
            params.set(key, String(pageSizeValue));
          }
          return;
        }

        if (value === null || value === "") {
          params.delete(key);
          return;
        }

        const defaultValue = defaults[key];
        if (defaultValue !== undefined && String(defaultValue) === String(value)) {
          params.delete(key);
          return;
        }

        params.set(key, String(value));
      });

      if (shouldResetPage && !updateKeys.includes(pageKey)) {
        params.delete(pageKey);
      }

      const newUrl = params.toString() ? `${pathname}?${params.toString()}` : pathname;
      router.replace(newUrl, { scroll: false });
    },
    [defaults, filterKeys, pageKey, pageSizeKey, pathname, router, searchParams]
  );

  const clearParams = useCallback(
    (keys?: string[]) => {
      const params = new URLSearchParams(searchParams.toString());
      const keysToClear = keys ?? filterKeys;
      keysToClear.forEach((key) => params.delete(key));
      params.delete(pageKey);

      const newUrl = params.toString() ? `${pathname}?${params.toString()}` : pathname;
      router.replace(newUrl, { scroll: false });
    },
    [filterKeys, pageKey, pathname, router, searchParams]
  );

  const hasActiveFilters = useMemo(() => {
    return filterKeys.some((key) => {
      const value = searchParams.get(key);
      if (!value) return false;
      const defaultValue = defaults[key];
      return defaultValue === undefined || String(defaultValue) !== value;
    });
  }, [defaults, filterKeys, searchParams]);

  return {
    searchParams,
    debouncedSearchParams,
    getParam,
    getNumberParam,
    updateParams,
    clearParams,
    hasActiveFilters,
  };
}
