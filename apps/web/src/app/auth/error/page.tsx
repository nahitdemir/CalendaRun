import { Suspense } from "react";
import AuthErrorPage from "./page-client";

export default function Page() {
  return (
    <Suspense fallback={<div />}>
      <AuthErrorPage />
    </Suspense>
  );
}


