export const PLAN_STATES = ["Active", "Registered", "Completed", "Cancelled"] as const;

export type PlanState = (typeof PLAN_STATES)[number];
