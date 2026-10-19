import "../styles/trust.css";

export interface TrustTabItem<T extends string> {
  id: T;
  label: string;
}

interface TrustTabsProps<T extends string> {
  tabs: ReadonlyArray<TrustTabItem<T>>;
  active: T;
  onChange: (id: T) => void;
  ariaLabel: string;
}

export function TrustTabs<T extends string>({
  tabs,
  active,
  onChange,
  ariaLabel,
}: TrustTabsProps<T>) {
  return (
    <div
      role="tablist"
      aria-label={ariaLabel}
      className="flex gap-1 overflow-x-auto border-b border-border-default"
    >
      {tabs.map((tab) => {
        const isActive = tab.id === active;
        return (
          <button
            key={tab.id}
            type="button"
            role="tab"
            aria-selected={isActive}
            onClick={() => onChange(tab.id)}
            className={`-mb-px whitespace-nowrap border-b-2 px-4 py-2.5 text-sm font-semibold transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-border-focus ${
              isActive
                ? "border-primary-600 text-primary-700"
                : "border-transparent text-text-secondary hover:text-text-primary"
            }`}
          >
            {tab.label}
          </button>
        );
      })}
    </div>
  );
}
