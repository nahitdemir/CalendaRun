"use client";

import { FormEvent, useMemo, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import { Eye, EyeOff, Loader2, ShieldCheck } from "lucide-react";
import { AuthShell } from "@/components/auth/auth-shell";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useToast } from "@/components/ui/toast";

export default function ResetPasswordPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const toast = useToast();

  const email = useMemo(() => searchParams.get("email") || "", [searchParams]);
  const token = useMemo(() => searchParams.get("token") || "", [searchParams]);

  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isComplete, setIsComplete] = useState(false);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!token || !email) {
      toast.error("Invalid link", "Please request a new reset link.");
      return;
    }

    if (password.length < 8) {
      toast.error("Password too short", "Use at least 8 characters.");
      return;
    }

    if (password !== confirmPassword) {
      toast.error("Passwords do not match", "Please re-enter your password.");
      return;
    }

    setIsSubmitting(true);

    try {
      const response = await fetch("/api/auth/reset-password", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({ email, token, password }),
      });

      const data = await response.json().catch(() => ({}));

      if (!response.ok) {
        toast.error("Reset failed", data?.error || "Please request a new link.");
        return;
      }

      setIsComplete(true);
      toast.success("Password updated", "You can sign in with your new password.");
    } catch (error) {
      toast.error("Reset failed", "Please try again in a moment.");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <AuthShell
      title="Set a new password"
      subtitle="Choose a strong password to protect your account."
      sideTitle="Secure access, built in."
      sideDescription="Reset your password without leaving CalendaRUN. Your new session stays protected."
      sideItems={["Encrypted sessions", "Protected by Keycloak", "No redirects"]}
      footer={
        <span>
          Need a fresh link?{" "}
          <Link className="font-semibold text-foreground hover:text-accent" href="/forgot-password">
            Request again
          </Link>
        </span>
      }
    >
      {isComplete ? (
        <div className="rounded-2xl border bg-background/70 p-4 text-sm text-muted-foreground">
          <div className="flex items-center gap-2 text-foreground">
            <ShieldCheck className="h-4 w-4" />
            <span className="font-medium">Password updated</span>
          </div>
          <p className="mt-2">
            Your password has been reset. You can now sign in with your new credentials.
          </p>
          <Button className="mt-4" variant="accent" onClick={() => router.replace("/login")}> 
            Back to sign in
          </Button>
        </div>
      ) : (
        <form className="space-y-4" onSubmit={handleSubmit}>
          <div className="space-y-2">
            <Label htmlFor="password">New password</Label>
            <div className="relative">
              <Input
                id="password"
                type={showPassword ? "text" : "password"}
                autoComplete="new-password"
                placeholder="Create a strong password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                required
              />
              <button
                type="button"
                className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground transition hover:text-foreground"
                onClick={() => setShowPassword((prev) => !prev)}
                aria-label={showPassword ? "Hide password" : "Show password"}
              >
                {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
              </button>
            </div>
          </div>
          <div className="space-y-2">
            <Label htmlFor="confirmPassword">Confirm password</Label>
            <Input
              id="confirmPassword"
              type={showPassword ? "text" : "password"}
              autoComplete="new-password"
              placeholder="Repeat your password"
              value={confirmPassword}
              onChange={(event) => setConfirmPassword(event.target.value)}
              required
            />
          </div>
          <Button type="submit" size="lg" variant="accent" className="w-full" disabled={isSubmitting}>
            {isSubmitting ? (
              <span className="flex items-center gap-2">
                <Loader2 className="h-4 w-4 animate-spin" />
                Updating password...
              </span>
            ) : (
              "Update password"
            )}
          </Button>
        </form>
      )}
    </AuthShell>
  );
}
