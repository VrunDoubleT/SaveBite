import type { Tone } from "./tones";

// Hex values mirror the app theme (index.css) so SVG charts match the Tailwind colours exactly.
export const TONE_HEX: Record<Tone, string> = {
  neutral: "#93a19b",
  primary: "#22a866",
  info: "#2f80ed",
  warning: "#ff9f1c",
  danger: "#e5484d",
};

// Categories with no inherent meaning (payment methods, roles...) are told apart by this ordered set.
export const CATEGORY_COLORS = ["#22a866", "#2f80ed", "#ff9f1c", "#7c5cff", "#0ea5a4", "#e5484d", "#93a19b"];

export const CHART = {
  grid: "#e6ece9",
  tick: "#6b7a74",
  line: "#178a54",
  lineOnDark: "#b7f0d1",
};
