export const MILESTONE_TYPES = {
  regOpen: "REG_OPEN",
  regClose: "REG_CLOSE",
  eventStart: "EVENT_START",
  eventEnd: "EVENT_END",
  custom: "CUSTOM",
} as const;

export type MilestoneType = (typeof MILESTONE_TYPES)[keyof typeof MILESTONE_TYPES];

export const REGISTRATION_MILESTONE_TYPES = [
  MILESTONE_TYPES.regOpen,
  MILESTONE_TYPES.regClose,
] as const;

export type RegistrationMilestoneType =
  (typeof REGISTRATION_MILESTONE_TYPES)[number];

export const isRegistrationMilestone = (
  type: string
): type is RegistrationMilestoneType =>
  (REGISTRATION_MILESTONE_TYPES as readonly string[]).includes(type);
