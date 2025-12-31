import "server-only";

const API_BASE =
  process.env.INTERNAL_API_URL ||
  process.env.NEXT_PUBLIC_API_URL ||
  "http://localhost:8080";

export async function fetchUserProfile(accessToken: string) {
  const response = await fetch(`${API_BASE.replace(/\/$/, "")}/api/me`, {
    headers: { Authorization: `Bearer ${accessToken}` },
    cache: "no-store",
  });

  if (!response.ok) {
    return null;
  }

  return response.json();
}
