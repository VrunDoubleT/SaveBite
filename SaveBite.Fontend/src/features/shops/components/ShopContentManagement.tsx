import type { ReactNode } from "react";
import type { LucideIcon } from "lucide-react";
import {
  CalendarClock,
  CalendarPlus,
  Clock,
  ExternalLink,
  FileText,
  Files,
  Mail,
  MapPin,
  Navigation,
  Phone,
  ScrollText,
  Store,
} from "lucide-react";
import { OpenStateBadge, ShopStatusBadge } from "./ShopProfileHeader";
import type { ShopContentManagement as ShopContentManagementModel } from "../types/shopManagement.types";

type OwnerInfo = { id: string; fullName: string; email: string | null; phoneNumber: string | null };

const formatDate = (value?: string | null) =>
  value ? new Date(value).toLocaleString("vi-VN") : "—";

const CARD = "rounded-3xl bg-white shadow-sm ring-1 ring-gray-200/70";
const ACTION_BUTTON =
  "inline-flex shrink-0 items-center gap-2 rounded-full bg-primary-50 px-4 py-2 text-xs font-semibold text-primary-700 transition-colors hover:bg-primary-600 hover:text-white";

function Section({
  icon: Icon,
  title,
  aside,
  children,
}: {
  icon: LucideIcon;
  title: string;
  aside?: ReactNode;
  children: ReactNode;
}) {
  return (
    <section className={`${CARD} p-6`}>
      <div className="mb-5 flex items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <span className="flex size-10 items-center justify-center rounded-2xl bg-gradient-to-br from-primary-50 to-primary-100 text-primary-700">
            <Icon className="size-5" />
          </span>
          <h2 className="text-lg font-semibold text-text-primary">{title}</h2>
        </div>
        {aside}
      </div>
      <div className="space-y-5">{children}</div>
    </section>
  );
}

function InfoTile({
  icon: Icon,
  label,
  value,
}: {
  icon: LucideIcon;
  label: string;
  value: ReactNode;
}) {
  return (
    <div className="flex items-start gap-3 rounded-2xl bg-neutral-50 p-4 ring-1 ring-gray-100">
      <span className="flex size-9 shrink-0 items-center justify-center rounded-xl bg-white text-primary-600 shadow-sm">
        <Icon className="size-4" />
      </span>
      <div className="min-w-0">
        <p className="text-[11px] font-medium uppercase tracking-wide text-text-secondary">
          {label}
        </p>
        <p className="mt-0.5 break-words text-sm font-semibold text-text-primary">{value}</p>
      </div>
    </div>
  );
}

function ContactRow({ icon: Icon, value }: { icon: LucideIcon; value?: string | null }) {
  return (
    <li className="flex items-center gap-3 rounded-xl bg-neutral-50 px-3 py-2.5 text-sm">
      <Icon className="size-4 shrink-0 text-primary-600" />
      <span className={`min-w-0 break-all ${value ? "text-text-primary" : "text-text-muted"}`}>
        {value || "—"}
      </span>
    </li>
  );
}

export function ShopContentManagement({ shop: s }: { shop: ShopContentManagementModel }) {
  const owner = (s as ShopContentManagementModel & { owner?: OwnerInfo | null }).owner;
  const documents = s.documents ?? [];
  const address = [s.addressLine, s.ward, s.district, s.city].filter(Boolean).join(", ");
  const openingHours =
    s.openingTime && s.closingTime ? `${s.openingTime} – ${s.closingTime}` : null;
  const hasCoordinates = s.latitude != null && s.longitude != null;

  return (
    <div className="space-y-6">
      {/* Section 1: cover, logo, name, status, open state */}
      <section className={`${CARD} overflow-hidden`}>
        <div className="relative h-48 w-full sm:h-64">
          {s.coverImageUrl ? (
            <>
              <img src={s.coverImageUrl} alt="Shop cover" className="size-full object-cover" />
              <div className="absolute inset-0 bg-gradient-to-t from-black/40 via-black/5 to-transparent" />
            </>
          ) : (
            <div className="flex size-full items-center justify-center bg-gradient-to-br from-primary-600 via-primary-500 to-primary-300 text-white/40">
              <Store className="size-16" />
            </div>
          )}
        </div>

        <div className="flex flex-col gap-4 px-6 pb-5 pt-4 sm:flex-row sm:items-end sm:gap-5 sm:px-8">
          {s.logoUrl ? (
            <img
              src={s.logoUrl}
              alt="Shop logo"
              className="relative z-10 -mt-14 size-24 shrink-0 rounded-3xl border-4 border-white bg-white object-cover shadow-lg ring-1 ring-gray-200/70 sm:-mt-16 sm:size-28"
            />
          ) : (
            <div
              aria-hidden="true"
              className="relative z-10 -mt-14 flex size-24 shrink-0 items-center justify-center rounded-3xl border-4 border-white bg-gradient-to-br from-primary-100 to-primary-50 text-4xl font-bold text-primary-700 shadow-lg ring-1 ring-gray-200/70 sm:-mt-16 sm:size-28"
            >
              {s.name.charAt(0).toUpperCase()}
            </div>
          )}

          <div className="flex min-w-0 flex-1 flex-col gap-3 sm:flex-row sm:items-center sm:justify-between sm:gap-4">
            <div className="min-w-0 space-y-2">
              <h2 className="truncate text-xl font-bold tracking-tight text-text-primary sm:text-2xl">
                {s.name}
              </h2>
              <ShopStatusBadge status={s.status} />
            </div>

            <div className="shrink-0">
              <OpenStateBadge open={s.openingTime} close={s.closingTime} />
            </div>
          </div>
        </div>
      </section>

      <div className="grid items-start gap-6 lg:grid-cols-3">
        {/* Main column */}
        <div className="space-y-6 lg:col-span-2">
          <Section icon={Store} title="About the shop">
            <p className="whitespace-pre-wrap text-sm leading-relaxed text-text-secondary">
              {s.description || <span className="text-text-muted">No description</span>}
            </p>
            <div className="grid gap-3 sm:grid-cols-3">
              <InfoTile icon={Clock} label="Opening hours" value={openingHours ?? "—"} />
              <InfoTile icon={CalendarPlus} label="Created" value={formatDate(s.createdAt)} />
              <InfoTile icon={CalendarClock} label="Updated" value={formatDate(s.updatedAt)} />
            </div>
          </Section>

          <Section
            icon={MapPin}
            title="Location"
            aside={
              hasCoordinates && (
                <a
                  className={ACTION_BUTTON}
                  href={`https://www.google.com/maps?q=${s.latitude},${s.longitude}`}
                  target="_blank"
                  rel="noopener noreferrer"
                >
                  <Navigation className="size-3.5" />
                  View on map
                </a>
              )
            }
          >
            <InfoTile icon={MapPin} label="Address" value={address || "—"} />
            <div className="grid gap-3 sm:grid-cols-2">
              <InfoTile icon={Navigation} label="Latitude" value={s.latitude ?? "—"} />
              <InfoTile icon={Navigation} label="Longitude" value={s.longitude ?? "—"} />
            </div>
          </Section>

          <Section icon={ScrollText} title="Business registration">
            <InfoTile
              icon={FileText}
              label="Business license number"
              value={s.businessLicenseNo || "—"}
            />

            <div className="space-y-3">
              <h3 className="flex items-center gap-2 text-sm font-semibold text-text-primary">
                Documents
                <span className="rounded-full bg-neutral-100 px-2 py-0.5 text-xs font-medium text-text-secondary">
                  {documents.length}
                </span>
              </h3>

              {documents.length ? (
                <ul className="space-y-3">
                  {documents.map((d) => (
                    <li
                      key={d.id}
                      className="group flex flex-wrap items-center justify-between gap-3 rounded-2xl p-3 ring-1 ring-gray-200 transition-all hover:bg-neutral-50 hover:shadow-sm hover:ring-primary-200"
                    >
                      <div className="flex min-w-0 items-center gap-3">
                        <span className="flex size-11 shrink-0 items-center justify-center rounded-xl bg-primary-50 text-primary-700 transition-colors group-hover:bg-primary-100">
                          <FileText className="size-5" />
                        </span>
                        <div className="min-w-0">
                          <p className="text-sm font-semibold text-text-primary">
                            {d.documentType}
                          </p>
                          <p className="truncate text-xs text-text-secondary">
                            {d.originalFileName} · Uploaded {formatDate(d.uploadedAt)}
                          </p>
                        </div>
                      </div>
                      <a
                        className={ACTION_BUTTON}
                        href={d.fileUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                      >
                        <ExternalLink className="size-3.5" />
                        Open document
                      </a>
                    </li>
                  ))}
                </ul>
              ) : (
                <div className="flex flex-col items-center gap-2 rounded-2xl border border-dashed border-gray-300 bg-neutral-50/60 p-8 text-text-muted">
                  <Files className="size-7" />
                  <p className="text-sm">No current documents</p>
                </div>
              )}
            </div>
          </Section>
        </div>

        {/* Sidebar */}
        <aside className="lg:sticky lg:top-6">
          <section className={`${CARD} overflow-hidden`}>
            <div className="h-20 bg-gradient-to-r from-primary-600 to-primary-400" />
            <div className="-mt-10 px-6 pb-6">
              <div
                aria-hidden="true"
                className="relative z-10 flex size-20 items-center justify-center rounded-full border-4 border-white bg-primary-50 text-2xl font-bold text-primary-700 shadow-md"
              >
                {(owner?.fullName || "?").charAt(0).toUpperCase()}
              </div>

              <div className="mt-3 min-w-0">
                <p className="truncate text-base font-semibold text-text-primary">
                  {owner?.fullName || "Unknown owner"}
                </p>
                <p className="text-xs font-medium uppercase tracking-wide text-text-secondary">
                  Shop owner
                </p>
              </div>

              <ul className="mt-5 space-y-2">
                <ContactRow icon={Mail} value={owner?.email} />
                <ContactRow icon={Phone} value={owner?.phoneNumber} />
              </ul>
            </div>
          </section>
        </aside>
      </div>
    </div>
  );
}
