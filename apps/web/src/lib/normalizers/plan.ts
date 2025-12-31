import { PLAN_STATES } from "@/lib/constants/plan";
import type { PlanState } from "@/lib/constants/plan";

export type PlanStateInput = PlanState | number | null | undefined;

export function normalizePlanState(state: PlanStateInput): PlanState {
  if (typeof state === "number") {
    return PLAN_STATES[state] ?? "Active";
  }
  if (state && PLAN_STATES.includes(state as PlanState)) {
    return state as PlanState;
  }
  return "Active";
}
