"use client";

import { signIn, getProviders } from "next-auth/react";
import { useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import { Button } from "@/components/ui/button";
import { BrandLogo } from "@/components/brand/BrandLogo";
import { PageHeader } from "@/components/page-header";
import { useTranslation } from "@/contexts/locale-context";

export default function SignInPage() {
  const searchParams = useSearchParams();
  const { t, locale } = useTranslation();
  const [providers, setProviders] = useState<Record<string, any> | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const callbackUrl = searchParams.get("callbackUrl") || "/";
  const error = searchParams.get("error");

  useEffect(() => {
    const loadProviders = async () => {
      const res = await getProviders();
      setProviders(res);
      setIsLoading(false);
    };
    loadProviders();
  }, []);

  if (isLoading) {
    return (
      <div className="container-app flex min-h-[calc(100vh-4rem)] items-center justify-center">
        <div className="text-center">
          <div className="mb-4 h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent mx-auto" />
          <p className="text-muted-foreground">{locale === "tr" ? "Yükleniyor..." : "Loading..."}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="container-app flex min-h-[calc(100vh-4rem)] items-center justify-center">
      <div className="w-full max-w-md space-y-6">
        <div className="flex justify-center">
          <BrandLogo variant="wordmark" size="md" priority />
        </div>
        <PageHeader
          title={t("nav.signIn")}
          subtitle={locale === "tr" ? "Hesabınıza giriş yapın" : "Sign in to your account"}
        />

        {error && (
          <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-4 text-sm text-destructive">
            {error === "OAuthSignin" && (
              <p>
                {locale === "tr"
                  ? "Giriş yapılırken bir hata oluştu. Lütfen tekrar deneyin."
                  : "An error occurred during sign in. Please try again."}
              </p>
            )}
            {error === "OAuthCallback" && (
              <p>
                {locale === "tr"
                  ? "Giriş işlemi tamamlanamadı. Lütfen tekrar deneyin."
                  : "Sign in process could not be completed. Please try again."}
              </p>
            )}
            {error === "OAuthCreateAccount" && (
              <p>
                {locale === "tr"
                  ? "Hesap oluşturulamadı. Lütfen tekrar deneyin."
                  : "Account could not be created. Please try again."}
              </p>
            )}
            {error === "EmailCreateAccount" && (
              <p>
                {locale === "tr"
                  ? "E-posta ile hesap oluşturulamadı."
                  : "Account could not be created with email."}
              </p>
            )}
            {error === "Callback" && (
              <p>
                {locale === "tr"
                  ? "Giriş işlemi sırasında bir hata oluştu."
                  : "An error occurred during the sign in process."}
              </p>
            )}
            {error === "OAuthAccountNotLinked" && (
              <p>
                {locale === "tr"
                  ? "Bu e-posta adresi başka bir hesap ile ilişkilendirilmiş."
                  : "This email address is already associated with another account."}
              </p>
            )}
            {error === "EmailSignin" && (
              <p>
                {locale === "tr"
                  ? "E-posta gönderilemedi. Lütfen tekrar deneyin."
                  : "Email could not be sent. Please try again."}
              </p>
            )}
            {error === "CredentialsSignin" && (
              <p>
                {locale === "tr"
                  ? "E-posta veya şifre hatalı."
                  : "Invalid email or password."}
              </p>
            )}
            {error === "SessionRequired" && (
              <p>
                {locale === "tr"
                  ? "Bu sayfaya erişmek için giriş yapmanız gerekiyor."
                  : "You need to sign in to access this page."}
              </p>
            )}
          </div>
        )}

        <div className="space-y-4">
          {providers &&
            Object.values(providers).map((provider) => (
              <Button
                key={provider.name}
                variant="accent"
                size="lg"
                className="w-full"
                onClick={() => signIn(provider.id, { callbackUrl, prompt: "login" })}
              >
                {locale === "tr" ? "Keycloak ile Giriş Yap" : "Sign in with Keycloak"}
              </Button>
            ))}
        </div>

        {!providers && (
          <div className="rounded-lg border border-warning/50 bg-warning/10 p-4 text-sm text-warning">
            <p>
              {locale === "tr"
                ? "Giriş sağlayıcıları yüklenemedi. Lütfen sayfayı yenileyin."
                : "Sign in providers could not be loaded. Please refresh the page."}
            </p>
          </div>
        )}
      </div>
    </div>
  );
}
