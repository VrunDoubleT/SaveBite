export function inputClass(hasError: boolean): string {
  return `w-full rounded-md border px-3 py-2.5 text-sm transition-all focus:outline-none focus:ring-1 ${
    hasError
      ? "border-danger focus:border-danger focus:ring-danger"
      : "border-border-default focus:border-border-focus focus:ring-border-focus"
  }`;
}
