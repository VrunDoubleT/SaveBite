import type { ReactNode } from "react";
import {
  ArrowUpRight,
  Building2,
  Clock3,
  FileText,
  MapPin,
  ScrollText,
  type LucideIcon,
} from "lucide-react";
import type { OwnerShop } from "@/features/shops/types/ownerShop.types";
import { OpenStateBadge, ShopStatusBadge, getShopStatus, toMinutes } from "./ShopProfileHeader";

const NOT_PROVIDED = "Not provided";
const DAY = 1440;

const DOCUMENT_LABELS: Record<string, string> = {
  BusinessLicense: "Business license",
  FoodSafetyCertificate: "Food safety certificate",
  Other: "Other document",
};

const VALUE_CLASS = "text-sm font-medium leading-6 text-text-primary";
const MUTED_CLASS = "text-xs leading-5 text-text-muted";

function formatTime(value?: string | null): string | null {
  return value ? value.slice(0, 5) : null;
}

function formatCoordinate(value: number | string): string {
  const coordinate = Number(value);
  return Number.isFinite(coordinate) ? coordinate.toFixed(4) : String(value);
}

function Empty() {
  return <span className="text-sm leading-6 text-text-muted">{NOT_PROVIDED}</span>;
}

function SectionTitle({
  title,
  description,
  icon: Icon,
}: {
  title: string;
  description: string;
  icon: LucideIcon;
}) {
  return (
    <div className="mb-6 flex items-start gap-3.5">
      <span className="flex size-11 shrink-0 items-center justify-center rounded-2xl bg-primary-50 text-primary-700">
        <Icon size={21} aria-hidden />
      </span>

      <div className="min-w-0 pt-0.5">
        <h2 className="text-lg font-semibold leading-7 tracking-tight text-text-primary sm:text-xl">
          {title}
        </h2>
        <p className="mt-1 text-sm leading-6 text-text-secondary">{description}</p>
      </div>
    </div>
  );
}

function Row({
  icon: Icon,
  label,
  children,
}: {
  icon: LucideIcon;
  label: string;
  children: ReactNode;
}) {
  return (
    <div className="grid gap-3 py-5 sm:grid-cols-[10rem_minmax(0,1fr)] sm:items-start sm:gap-5">
      <dt className="flex items-center gap-2.5 text-sm font-medium leading-6 text-text-secondary">
        <span className="flex size-9 shrink-0 items-center justify-center rounded-xl bg-bg-subtle text-primary-700">
          <Icon size={17} aria-hidden />
        </span>
        <span>{label}</span>
      </dt>

      <dd className="min-w-0 break-words pt-1 text-text-primary">{children}</dd>
    </div>
  );
}

function StatusBand({ status }: { status?: string | null }) {
  const meta = getShopStatus(status);
  const Icon = meta.icon;

  return (
    <div className={`flex items-start gap-3.5 rounded-2xl border p-4 sm:gap-4 sm:p-5 ${meta.band}`}>
      <span
        className={`flex size-10 shrink-0 items-center justify-center rounded-xl sm:size-11 ${meta.tile}`}
      >
        <Icon size={21} aria-hidden />
      </span>

      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-center gap-x-3 gap-y-2">
          <p className="text-sm font-semibold leading-6 text-text-primary">Shop status</p>
          <ShopStatusBadge status={status} />
        </div>

        <p className="mt-1 text-sm leading-6 text-text-secondary">{meta.description}</p>
      </div>
    </div>
  );
}

function Hours({ open, close }: { open: string | null; close: string | null }) {
  const start = toMinutes(open);
  const end = toMinutes(close);

  if (start === null || end === null) {
    return <Empty />;
  }

  const segments =
    start <= end
      ? [{ from: start, to: end }]
      : [
          { from: start, to: DAY },
          { from: 0, to: end },
        ];

  return (
    <div className="w-full">
      <div className="flex flex-wrap items-center justify-between gap-x-3 gap-y-2">
        <p className="mt-0.5 flex items-center gap-2 text-base font-semibold leading-6 tracking-tight text-text-primary tabular-nums">
          <span>{open}</span>
          <span aria-hidden className="text-xs font-normal text-text-muted">
            to
          </span>
          <span>{close}</span>
        </p>

        <OpenStateBadge open={open} close={close} />
      </div>

      <div className="mt-6">
        <div
          role="img"
          aria-label={`Shop opening hours: ${open} to ${close}`}
          className="relative h-3 overflow-hidden rounded-full bg-border-default/60 ring-1 ring-inset ring-border-default/40"
        >
          {segments.map((segment) => (
            <span
              key={segment.from}
              className="absolute inset-y-0 rounded-full bg-primary-500"
              style={{
                left: `${(segment.from / DAY) * 100}%`,
                width: `${((segment.to - segment.from) / DAY) * 100}%`,
              }}
            />
          ))}
        </div>

        <div
          aria-hidden
          className="mt-2.5 grid grid-cols-5 text-[11px] font-medium leading-5 tabular-nums text-text-muted"
        >
          <span className="text-left">00:00</span>
          <span className="text-center">06:00</span>
          <span className="text-center">12:00</span>
          <span className="text-center">18:00</span>
          <span className="text-right">24:00</span>
        </div>
      </div>
    </div>
  );
}

function DocumentItem({
  documentType,
  fileName,
  fileUrl,
}: {
  documentType: string;
  fileName: string;
  fileUrl: string;
}) {
  return (
    <li>
      <a
        href={fileUrl}
        target="_blank"
        rel="noreferrer"
        className="group flex min-w-0 items-center gap-3 rounded-xl border border-border-default bg-bg-surface p-3 transition hover:border-primary-200 hover:bg-primary-50/40 focus-visible:outline-2 focus-visible:outline-primary-500"
      >
        <span className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-bg-subtle text-primary-700 transition group-hover:bg-primary-50">
          <FileText size={18} aria-hidden />
        </span>

        <span className="min-w-0 flex-1">
          <span className="block break-words text-sm font-medium leading-5 text-text-primary">
            {DOCUMENT_LABELS[documentType] ?? documentType}
          </span>
          <span className="mt-1 block truncate text-xs leading-5 text-text-secondary">
            {fileName}
          </span>
        </span>

        <ArrowUpRight
          size={17}
          aria-hidden
          className="shrink-0 text-text-muted transition group-hover:-translate-y-0.5 group-hover:translate-x-0.5 group-hover:text-primary-700"
        />
      </a>
    </li>
  );
}

export function ShopProfileDetails({ shop }: { shop: OwnerShop }) {
  const address = [shop.addressLine, shop.ward, shop.district, shop.city]
    .filter(Boolean)
    .join(", ");

  const hasCoordinates =
    shop.latitude !== null &&
    shop.latitude !== undefined &&
    shop.longitude !== null &&
    shop.longitude !== undefined;

  const documents = shop.documents ?? [];

  return (
    <div className="grid min-w-0 items-start gap-5 xl:grid-cols-[minmax(0,1.65fr)_minmax(19rem,1fr)]">
      <section className="min-w-0 rounded-3xl border border-border-default bg-bg-surface p-5 shadow-sm sm:p-7 lg:p-8">
        <SectionTitle
          icon={Building2}
          title="Business information"
          description="The details customers see on your shop profile."
        />

        <StatusBand status={shop.status} />

        <dl className="mt-6 divide-y divide-border-default border-t border-border-default">
          <Row icon={Building2} label="Shop name">
            {shop.name ? <p className={`${VALUE_CLASS} break-words`}>{shop.name}</p> : <Empty />}
          </Row>

          <Row icon={Clock3} label="Opening hours">
            <Hours open={formatTime(shop.openingTime)} close={formatTime(shop.closingTime)} />
          </Row>

          <Row icon={MapPin} label="Address">
            {address ? (
              <div className="min-w-0">
                <p className={`${VALUE_CLASS} break-words`}>{address}</p>

                {hasCoordinates && (
                  <p className={`${MUTED_CLASS} mt-1 break-all tabular-nums`}>
                    {formatCoordinate(shop.latitude!)}, {formatCoordinate(shop.longitude!)}
                  </p>
                )}
              </div>
            ) : (
              <Empty />
            )}
          </Row>

          {shop.description && (
            <Row icon={FileText} label="Description">
              <p className="whitespace-pre-wrap text-sm leading-6 text-text-secondary">
                {shop.description}
              </p>
            </Row>
          )}
        </dl>
      </section>

      <section className="min-w-0 rounded-3xl border border-border-default bg-bg-surface p-5 shadow-sm sm:p-7 lg:p-8">
        <SectionTitle
          icon={ScrollText}
          title="Shop documents"
          description="Documents submitted with your approved shop application."
        />

        <div className="flex items-center gap-3.5 rounded-2xl border border-primary-100 bg-primary-50/60 p-4">
          <span className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-bg-surface text-primary-700 shadow-sm">
            <ScrollText size={19} aria-hidden />
          </span>

          <div className="min-w-0 flex-1">
            <p className="text-xs font-medium leading-5 text-text-secondary">
              Business license number
            </p>
            <p
              className={`mt-1 break-words text-sm leading-6 ${
                shop.businessLicenseNo
                  ? "font-semibold text-text-primary"
                  : "font-normal text-text-muted"
              }`}
            >
              {shop.businessLicenseNo || NOT_PROVIDED}
            </p>
          </div>
        </div>

        <div className="mt-6">
          <h3 className="mb-3 text-sm font-semibold leading-6 text-text-primary">Uploaded files</h3>

          {documents.length === 0 ? (
            <div className="flex flex-col items-center gap-2 rounded-2xl border border-dashed border-border-default px-4 py-8 text-center">
              <span className="flex size-10 items-center justify-center rounded-xl bg-bg-subtle text-text-muted">
                <FileText size={20} aria-hidden />
              </span>
              <p className="text-sm leading-6 text-text-secondary">
                No registration documents are available.
              </p>
            </div>
          ) : (
            <ul className="space-y-2.5">
              {documents.map((document) => (
                <DocumentItem
                  key={document.id}
                  documentType={document.documentType}
                  fileName={document.originalFileName}
                  fileUrl={document.fileUrl}
                />
              ))}
            </ul>
          )}
        </div>
      </section>
    </div>
  );
}
