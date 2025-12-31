"use client";

import { useSearchParams } from "next/navigation";
import { Button } from "@/components/ui/button";
import { BrandLogo } from "@/components/Brand/BrandLogo";
import { PageHeader } from "@/components/page-header";
import { useTranslation } from "@/contexts/locale-context";
import { useRouter } from "next/navigation";

export default function AuthErrorPage() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const { t, locale } = useTranslation();
  const error = searchParams.get("error");

  const getErrorMessage = () => {
    switch (error) {
      case "Configuration":
        return locale === "tr"
          ? "Sunucu yapılandırma hatası. Lütfen yöneticiye başvurun."
          : "Server configuration error. Please contact the administrator.";
      case "AccessDenied":
        return locale === "tr"
          ? "Erişim reddedildi. Bu hesap ile giriş yapma izniniz yok."
          : "Access denied. You do not have permission to sign in with this account.";
      case "Verification":
        return locale === "tr"
          ? "Doğrulama hatası. Lütfen tekrar deneyin."
          : "Verification error. Please try again.";
      default:
        return locale === "tr"
          ? "Giriş yapılırken bir hata oluştu."
          : "An error occurred during sign in.";
    }
  };

  return (
    <div className="container-app flex min-h-[calc(100vh-4rem)] items-center justify-center">
      <div className="w-full max-w-md space-y-6">
        <div className="flex justify-center">
          <BrandLogo variant="wordmark" size="md" priority />
        </div>
        <PageHeader
          title={locale === "tr" ? "Giriş Hatası" : "Sign In Error"}
          subtitle={getErrorMessage()}
        />

        <div className="space-y-4">
          <Button
            variant="accent"
            size="lg"
            className="w-full"
            onClick={() => router.push("/login")}
          >
            {locale === "tr" ? "Tekrar Dene" : "Try Again"}
          </Button>
          <Button
            variant="outline"
            size="lg"
            className="w-full"
            onClick={() => router.push("/")}
          >
            {locale === "tr" ? "Ana Sayfaya Dön" : "Back to Home"}
          </Button>
        </div>
      </div>
    </div>
  );
}
