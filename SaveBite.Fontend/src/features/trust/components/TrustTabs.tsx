import { Link } from "react-router-dom";
import { tabPath } from "../utils/trustTabs";
import "../styles/trust.css";

export interface TrustTabItem<T extends string> {
  id: T;
  label: string;
}

interface TrustTabsProps<T extends string> {
  tabs: ReadonlyArray<TrustTabItem<T>>;
  active: T;
  /** Each tab links to `${basePath}/${tab.id}`. */
  basePath: string;
  ariaLabel: string;
}

export function TrustTabs<T extends string>({
  tabs,
  active,
  basePath,
  ariaLabel,
}: TrustTabsProps<T>) {
  return (
    <div
      role="tablist"
      aria-label={ariaLabel}
      className="flex gap-1 overflow-x-auto overflow-y-hidden border-b border-border-default pb-px"
    >
      {tabs.map((tab) => {
        const isActive = tab.id === active;
        return (
          <Link
            key={tab.id}
            to={tabPath(basePath, tab.id)}
            role="tab"
            aria-selected={isActive}
            className={`-mb-px whitespace-nowrap border-b-2 px-4 py-2.5 text-sm font-semibold transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-border-focus ${
              isActive
                ? "border-primary-600 text-primary-700"
                : "border-transparent text-text-secondary hover:text-text-primary"
            }`}
          >
            {tab.label}
          </Link>
        );
      })}
    </div>
  );
}
