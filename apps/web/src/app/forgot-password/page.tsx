"use client";

import { FormEvent, useState } from "react";
import Link from "next/link";
import { Loader2, Mail } from "lucide-react";
import { AuthShell } from "@/components/auth/auth-shell";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useToast } from "@/components/ui/toast";

export default function ForgotPasswordPage() {
  const toast = useToast();
  const [email, setEmail] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isSubmitted, setIsSubmitted] = useState(false);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSubmitting(true);

    try {
      await fetch("/api/auth/forgot-password", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({ email }),
      });
      setIsSubmitted(true);
      toast.success("Check your inbox", "If the email exists, we sent a reset link.");
    } catch (error) {
      toast.error("Request failed", "Please try again in a moment.");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <AuthShell
      title="Reset your password"
      subtitle="We will send you a secure link to set a new password."
      sideTitle="Get back on track."
      sideDescription="No Keycloak screens, no detours. Reset your password right here and keep planning."
      sideItems={["Private reset links", "Short-lived tokens", "Instant access"]}
      footer={
        <span>
          Remember your password?{" "}
          <Link className="font-semibold text-foreground hover:text-accent" href="/login">
            Sign in
          </Link>
        </span>
      }
    >
      {isSubmitted ? (
        <div className="rounded-2xl border bg-background/70 p-4 text-sm text-muted-foreground">
          <div className="flex items-center gap-2 text-foreground">
            <Mail className="h-4 w-4" />
            <span className="font-medium">Email sent</span>
          </div>
          <p className="mt-2">
            If the address exists, you will receive a reset link shortly. Please check your inbox.
          </p>
        </div>
      ) : (
        <form className="space-y-4" onSubmit={handleSubmit}>
          <div className="space-y-2">
            <Label htmlFor="email">Email</Label>
            <Input
              id="email"
              type="email"
              autoComplete="email"
              placeholder="you@calendarun.com"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              required
            />
          </div>
          <Button type="submit" size="lg" variant="accent" className="w-full" disabled={isSubmitting}>
            {isSubmitting ? (
              <span className="flex items-center gap-2">
                <Loader2 className="h-4 w-4 animate-spin" />
                Sending link...
              </span>
            ) : (
              "Send reset link"
            )}
          </Button>
        </form>
      )}
    </AuthShell>
  );
}
