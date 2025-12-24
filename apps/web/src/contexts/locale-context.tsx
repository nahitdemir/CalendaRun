"use client";

import {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
  ReactNode,
} from "react";

// Types
type Locale = "tr" | "en";
type Dictionary = Record<string, unknown>;

interface LocaleContextType {
  locale: Locale;
  setLocale: (locale: Locale) => void;
  t: (key: string, params?: Record<string, string | number>) => string;
}

// Context
const LocaleContext = createContext<LocaleContextType | undefined>(undefined);

// Storage key
const LOCALE_STORAGE_KEY = "calendarun-locale";

// Get browser locale
function getBrowserLocale(): Locale {
  if (typeof window === "undefined") return "tr";
  const lang = navigator.language.split("-")[0];
  return lang === "en" ? "en" : "tr";
}

// Get nested value from object
function getNestedValue(obj: Record<string, unknown>, path: string): string | undefined {
  const parts = path.split(".");
  let current: unknown = obj;

  for (const part of parts) {
    if (current && typeof current === "object" && part in current) {
      current = (current as Record<string, unknown>)[part];
    } else {
      return undefined;
    }
  }

  return typeof current === "string" ? current : undefined;
}

// Provider
export function LocaleProvider({ children }: { children: ReactNode }) {
  const [locale, setLocaleState] = useState<Locale>("tr");
  const [dictionary, setDictionary] = useState<Dictionary>({});

  // Load dictionary
  const loadDictionary = useCallback(async (loc: Locale) => {
    try {
      const dict = await import(`../../messages/${loc}.json`);
      setDictionary(dict.default || dict);
    } catch (error) {
      console.error(`Failed to load dictionary for ${loc}:`, error);
      // Fallback to TR
      if (loc !== "tr") {
        const fallback = await import(`../../messages/tr.json`);
        setDictionary(fallback.default || fallback);
        setLocaleState("tr");
      }
    }
  }, []);

  // Initialize on mount
  useEffect(() => {
    const stored = localStorage.getItem(LOCALE_STORAGE_KEY) as Locale | null;
    const initial = stored || getBrowserLocale();
    setLocaleState(initial);
    loadDictionary(initial);
  }, [loadDictionary]);

  // Set locale
  const setLocale = useCallback(
    (newLocale: Locale) => {
      setLocaleState(newLocale);
      localStorage.setItem(LOCALE_STORAGE_KEY, newLocale);
      loadDictionary(newLocale);
    },
    [loadDictionary]
  );

  // Translation function
  const t = useCallback(
    (key: string, params?: Record<string, string | number>): string => {
      let text = getNestedValue(dictionary, key);

      if (!text) {
        // Return key if not found (for development)
        return key;
      }

      // Replace params
      if (params) {
        Object.entries(params).forEach(([k, v]) => {
          text = text!.replace(`{${k}}`, String(v));
        });
      }

      return text;
    },
    [dictionary]
  );

  return (
    <LocaleContext.Provider value={{ locale, setLocale, t }}>
      {children}
    </LocaleContext.Provider>
  );
}

// Hook
export function useTranslation() {
  const context = useContext(LocaleContext);
  if (context === undefined) {
    throw new Error("useTranslation must be used within a LocaleProvider");
  }
  return context;
}

// Alias for compatibility
export const useLocale = useTranslation;
