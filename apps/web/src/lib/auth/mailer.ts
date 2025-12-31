import "server-only";
import nodemailer from "nodemailer";

const smtpHost = process.env.SMTP_HOST || "";
const smtpPort = Number(process.env.SMTP_PORT || 587);
const smtpUser = process.env.SMTP_USER || "";
const smtpPass = process.env.SMTP_PASS || "";
const smtpFrom = process.env.SMTP_FROM || "no-reply@calendarun.local";

export async function sendPasswordResetEmail({
  to,
  resetUrl,
}: {
  to: string;
  resetUrl: string;
}) {
  if (!smtpHost) {
    console.warn("SMTP is not configured; skipping password reset email.");
    console.info(`Password reset link: ${resetUrl}`);
    return;
  }

  const transporter = nodemailer.createTransport({
    host: smtpHost,
    port: smtpPort,
    secure: smtpPort === 465,
    auth: smtpUser ? { user: smtpUser, pass: smtpPass } : undefined,
  });

  await transporter.sendMail({
    from: smtpFrom,
    to,
    subject: "Reset your CalendaRUN password",
    text: `We received a request to reset your password.\n\nReset your password: ${resetUrl}\n\nIf you did not request this, you can ignore this email.`,
  });
}
