import type { Tone } from "./tones";

// CSS variable colors keep SVG charts in sync with the global theme.
export const TONE_HEX: Record<Tone, string> = {
  neutral: "var(--color-neutral-400)",
  primary: "var(--color-primary-500)",
  info: "var(--color-info)",
  warning: "var(--color-warning)",
  danger: "var(--color-danger)",
};
export const CATEGORY_COLORS = [
  "var(--color-primary-500)",
  "var(--color-info)",
  "var(--color-accent-500)",
  "var(--color-secondary-500)",
  "var(--color-primary-800)",
  "var(--color-danger)",
  "var(--color-neutral-400)",
];
export const CHART = {
  grid: "var(--color-border-default)",
  tick: "var(--color-text-muted)",
  line: "var(--color-primary-600)",
  lineOnDark: "var(--color-primary-200)",
};