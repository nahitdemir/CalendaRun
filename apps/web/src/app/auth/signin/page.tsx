import { Suspense } from "react";
import SignInPage from "./page-client";

export default function Page() {
  return (
    <Suspense fallback={<div />}>
      <SignInPage />
    </Suspense>
  );
}


