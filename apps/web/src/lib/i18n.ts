import trMessages from "../../messages/tr.json";
import enMessages from "../../messages/en.json";

export type Locale = "tr" | "en";

type Messages = typeof trMessages;

const messages: Record<Locale, Messages> = {
  tr: trMessages,
  en: enMessages,
};

// Get nested value from object using dot notation
function getNestedValue(obj: unknown, path: string): string {
  const keys = path.split(".");
  let current: unknown = obj;

  for (const key of keys) {
    if (current && typeof current === "object" && key in current) {
      current = (current as Record<string, unknown>)[key];
    } else {
      return path; // Return the key if not found
    }
  }

  return typeof current === "string" ? current : path;
}

export function createTranslator(locale: Locale) {
  const messageSet = messages[locale];

  return function t(key: string, params?: Record<string, string | number>): string {
    let value = getNestedValue(messageSet, key);

    if (params) {
      Object.entries(params).forEach(([paramKey, paramValue]) => {
        value = value.replace(`{${paramKey}}`, String(paramValue));
      });
    }

    return value;
  };
}

export function getMessages(locale: Locale): Messages {
  return messages[locale];
}

export const defaultLocale: Locale = "tr";

export const locales: Locale[] = ["tr", "en"];

export const localeNames: Record<Locale, string> = {
  tr: "Türkçe",
  en: "English",
};

