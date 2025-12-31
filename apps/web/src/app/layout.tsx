import type { Metadata } from "next";
import Script from "next/script";
import { Inter, Barlow_Condensed, Manrope } from "next/font/google";
import "./globals.css";
import { Providers } from "@/components/providers";
import { AppShell } from "@/components/layout/app-shell";
import { BRAND } from "@/constants/brand";

const inter = Inter({
  variable: "--font-sans",
  subsets: ["latin"],
});

const barlowCondensed = Barlow_Condensed({
  variable: "--font-accent",
  subsets: ["latin"],
  weight: ["400", "500", "600", "700"],
});

const manrope = Manrope({
  variable: "--font-brand",
  subsets: ["latin"],
  weight: ["600", "700", "800"],
});

export const metadata: Metadata = {
  title: "CalendaRUN",
  icons: {
    icon: "/favicon.ico",
    shortcut: BRAND.favicon,
    apple: BRAND.assets.apple180,
  },
  manifest: "/manifest.webmanifest",
  openGraph: {
    title: BRAND.name,
    images: [BRAND.logo],
  },
  twitter: {
    card: "summary_large_image",
    title: BRAND.name,
    images: [BRAND.logo],
  },
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
        className={`${inter.variable} ${barlowCondensed.variable} ${manrope.variable} min-h-screen bg-background font-sans antialiased`}
      >
        <Providers>
          <AppShell>{children}</AppShell>
        </Providers>
      </body>
    </html>
  );
}
