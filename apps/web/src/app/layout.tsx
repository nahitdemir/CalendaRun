import type { Metadata } from "next";
import Script from "next/script";
import { Inter, Barlow_Condensed } from "next/font/google";
import "./globals.css";
import { Providers } from "@/components/providers";
import { AppShell } from "@/components/layout/app-shell";

const inter = Inter({
  variable: "--font-sans",
  subsets: ["latin"],
});

const barlowCondensed = Barlow_Condensed({
  variable: "--font-display",
  subsets: ["latin"],
  weight: ["400", "500", "600", "700"],
});

export const metadata: Metadata = {
  title: "CalendaRUN - Plan Your Races",
  description: "Never miss race registrations. Add to your calendar, get reminders.",
};

const themeScript = `
(() => {
  try {
    const storageKey = "calendarun-theme";
    const stored = localStorage.getItem(storageKey);
    const theme =
      stored === "light" || stored === "dark" || stored === "system"
        ? stored
        : "system";
    const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
    const resolved = theme === "system" ? (prefersDark ? "dark" : "light") : theme;
    document.documentElement.classList.toggle("dark", resolved === "dark");
    document.documentElement.style.colorScheme = resolved;
  } catch {
    // noop
  }
})();
`;

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" suppressHydrationWarning>
      <head>
        <Script
          id="theme-init"
          strategy="beforeInteractive"
          dangerouslySetInnerHTML={{ __html: themeScript }}
        />
      </head>
      <body
        className={`${inter.variable} ${barlowCondensed.variable} min-h-screen bg-background font-sans antialiased`}
      >
        <Providers>
          <AppShell>{children}</AppShell>
        </Providers>
      </body>
    </html>
  );
}
