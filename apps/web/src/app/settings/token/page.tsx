"use client";

import { useState } from "react";
import { useAuth } from "@/contexts/auth-context";
import { useToast } from "@/components/ui/toast";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { PageHeader } from "@/components/page-header";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Key, Copy, Trash2, RefreshCw, AlertTriangle } from "lucide-react";

export default function TokenSettingsPage() {
  const { devToken, setDevToken, refreshProfile } = useAuth();
  const toast = useToast();
  const [tokenInput, setTokenInput] = useState("");
  const [isRefreshing, setIsRefreshing] = useState(false);

  const handleSaveToken = async () => {
    if (!tokenInput.trim()) {
      toast.error("Token boş olamaz");
      return;
    }

    setDevToken(tokenInput.trim());
    setIsRefreshing(true);

    try {
      await refreshProfile();
      toast.success("Token kaydedildi ve profil yenilendi");
      setTokenInput("");
    } catch {
      toast.error("Token geçersiz veya API'ye bağlanılamadı");
    } finally {
      setIsRefreshing(false);
    }
  };

  const handleClearToken = () => {
    setDevToken(null);
    toast.info("Token temizlendi");
  };

  const handleCopyToken = () => {
    if (devToken) {
      navigator.clipboard.writeText(devToken);
      toast.success("Token panoya kopyalandı");
    }
  };

  return (
    <div className="container-app max-w-2xl">
      <PageHeader
        title="Dev Token"
        subtitle="Geliştirme için manuel token yönetimi"
      />

      <div className="mt-6 space-y-6">
        {/* Warning */}
        <Card className="border-warning/50 bg-warning/5">
          <CardHeader className="pb-3">
            <div className="flex items-center gap-2 text-warning">
              <AlertTriangle className="h-5 w-5" />
              <CardTitle className="text-base">Geliştirme Modu</CardTitle>
            </div>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground">
              Bu sayfa sadece geliştirme amaçlıdır. Keycloak'tan aldığınız JWT
              token'ı buraya yapıştırarak API'leri test edebilirsiniz.
              Production'da Keycloak OAuth flow kullanılacaktır.
            </p>
          </CardContent>
        </Card>

        {/* Token Input */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Key className="h-5 w-5" />
              Access Token
            </CardTitle>
            <CardDescription>
              Keycloak'tan aldığınız JWT access token'ı yapıştırın
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="token">Token</Label>
              <Input
                id="token"
                type="password"
                value={tokenInput}
                onChange={(e) => setTokenInput(e.target.value)}
                placeholder="eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..."
                className="font-mono text-sm"
              />
            </div>

            <Button
              onClick={handleSaveToken}
              disabled={!tokenInput.trim() || isRefreshing}
              className="w-full"
            >
              {isRefreshing ? (
                <>
                  <RefreshCw className="mr-2 h-4 w-4 animate-spin" />
                  Doğrulanıyor...
                </>
              ) : (
                "Token Kaydet"
              )}
            </Button>
          </CardContent>
        </Card>

        {/* Current Token */}
        {devToken && (
          <Card>
            <CardHeader>
              <CardTitle>Mevcut Token</CardTitle>
              <CardDescription>
                Şu anda kullanılan token (ilk 50 karakter)
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <code className="block rounded-lg bg-muted p-3 text-xs break-all">
                {devToken.substring(0, 50)}...
              </code>

              <div className="flex gap-2">
                <Button variant="outline" size="sm" onClick={handleCopyToken}>
                  <Copy className="mr-2 h-4 w-4" />
                  Kopyala
                </Button>
                <Button
                  variant="destructive"
                  size="sm"
                  onClick={handleClearToken}
                >
                  <Trash2 className="mr-2 h-4 w-4" />
                  Temizle
                </Button>
              </div>
            </CardContent>
          </Card>
        )}

        {/* How to get token */}
        <Card>
          <CardHeader>
            <CardTitle>Token Nasıl Alınır?</CardTitle>
          </CardHeader>
          <CardContent>
            <pre className="rounded-lg bg-muted p-4 text-xs overflow-x-auto">
{`# Super admin token al
curl -s -X POST "http://localhost:8180/realms/calendarun/protocol/openid-connect/token" \\
  -H "Content-Type: application/x-www-form-urlencoded" \\
  -d "grant_type=password" \\
  -d "client_id=calendarun-web" \\
  -d "username=super@calendarun.local" \\
  -d "password=super123" | jq -r '.access_token'`}
            </pre>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

