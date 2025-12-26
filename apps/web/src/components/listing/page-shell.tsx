import { cn } from "@/lib/utils";
import { ReactNode } from "react";

interface PageShellProps extends React.HTMLAttributes<HTMLDivElement> {
  children: ReactNode;
}

export function PageShell({ children, className, ...props }: PageShellProps) {
  return (
    <div className={cn("container-app space-y-6", className)} {...props}>
      {children}
    </div>
  );
}
