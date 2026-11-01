import { useState } from "react";
import { Bell, Check, Plus, ShoppingBag } from "lucide-react";
import {
  Button,
  IconButton,
  Input,
  Textarea,
  Select,
  Checkbox,
  Switch,
  FormField,
  Badge,
  Card,
  PageHeader,
  EmptyState,
  ErrorState,
  Skeleton,
  StatCard,
  DataTable,
  Tabs,
  Modal,
  Drawer,
  Avatar,
  Pagination,
} from "@/shared/components/ui";
import { toast } from "@/shared/stores/toastStore";
import { WorkspaceLayout } from "@/shared/layouts/WorkspaceLayout";
import { Panel } from "@/shared/components/analytics/Panel";
import { StatusBadge } from "@/shared/components/analytics/StatusBadge";
import { SearchInput } from "@/shared/components/modal/SearchInput";

const rows = [
  { id: "1", name: "Fresh vegetable box", status: "Active", price: "45,000 ₫" },
  { id: "2", name: "Bakery surprise bag", status: "Pending", price: "30,000 ₫" },
];
const columns = [
  { key: "name", header: "Product", cell: (row: (typeof rows)[number]) => row.name },
  {
    key: "status",
    header: "Status",
    cell: (row: (typeof rows)[number]) => <StatusBadge status={row.status} />,
  },
  { key: "price", header: "Price", cell: (row: (typeof rows)[number]) => row.price },
];
const navigation = [
  { to: "/__ui", label: "UI reference", icon: ShoppingBag, end: true },
  { to: "/", label: "Customer home", icon: Plus },
];

/** Rendered exclusively when DEV && VITE_ENABLE_MOCK === "true". No backend fixtures are installed here. */
export function UiShowcasePage() {
  const [tab, setTab] = useState("normal");
  const [page, setPage] = useState(1);
  const [modal, setModal] = useState(false);
  const [drawer, setDrawer] = useState(false);
  const [query, setQuery] = useState("");
  return (
    <WorkspaceLayout
      navigation={navigation}
      navigationLabel="UI reference navigation"
      title="SaveBite UI"
      subtitle="Components and states"
    >
      <div className="mx-auto max-w-320 space-y-6">
        <PageHeader
          title="UI reference"
          description="Theme tokens, shared components and interaction states."
          breadcrumb={<span>SaveBite / UI reference</span>}
          actions={
            <Button onClick={() => toast.success("Changes saved successfully.")}>
              <Check className="size-4" />
              Save example
            </Button>
          }
        />
        <Card aria-label="Theme colors">
          <h2 className="mb-4 text-section font-semibold">Color palette</h2>
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
            <Swatch label="Primary · green" className="bg-primary-700" />
            <Swatch label="Accent · warm sun" className="bg-accent-500" />
            <Swatch label="Secondary · indigo" className="bg-secondary-500" />
            <Swatch label="Info · blue" className="bg-info" />
          </div>
        </Card>
        <Card>
          <h2 className="mb-4 text-section font-semibold">Buttons</h2>
          <div className="flex flex-wrap items-center gap-3">
            <Button>Primary</Button>
            <Button variant="secondary">Secondary</Button>
            <Button variant="ghost">Ghost</Button>
            <Button variant="danger">Delete</Button>
            <Button size="sm">Small</Button>
            <Button size="lg">Large</Button>
            <Button loading>Saving</Button>
            <Button disabled>Disabled</Button>
            <IconButton aria-label="Notifications example">
              <Bell className="size-5" />
            </IconButton>
          </div>
        </Card>
        <Card>
          <h2 className="mb-4 text-section font-semibold">Form controls</h2>
          <div className="grid gap-6 md:grid-cols-2">
            <FormField label="Product name" hint="Use a short, descriptive name.">
              {(props) => <Input {...props} placeholder="Vegetable box" />}
            </FormField>
            <FormField label="Email" error="Enter a valid email address.">
              {(props) => <Input {...props} defaultValue="invalid-email" />}
            </FormField>
            <FormField label="Category">
              {(props) => (
                <Select {...props}>
                  <option>Fresh food</option>
                  <option>Bakery</option>
                </Select>
              )}
            </FormField>
            <FormField label="Unavailable field">
              {(props) => <Input {...props} disabled placeholder="Disabled" />}
            </FormField>
            <FormField label="Description">
              {(props) => <Textarea {...props} placeholder="Describe your product" />}
            </FormField>
            <div className="space-y-4">
              <label className="flex items-center gap-3">
                <Checkbox defaultChecked />
                Accept terms
              </label>
              <label className="flex items-center gap-3">
                <Switch defaultChecked />
                Notifications
              </label>
              <label className="flex items-center gap-3">
                <Switch disabled />
                Unavailable switch
              </label>
              <SearchInput value={query} onChange={setQuery} placeholder="Search products" />
            </div>
          </div>
        </Card>
        <Card>
          <h2 className="mb-4 text-section font-semibold">Badges & avatar</h2>
          <div className="flex flex-wrap items-center gap-3">
            {(["primary", "success", "warning", "danger", "info", "neutral"] as const).map(
              (tone) => (
                <Badge key={tone} tone={tone}>
                  {tone}
                </Badge>
              ),
            )}
            <Avatar fullName="Nguyễn Minh Anh" />
          </div>
        </Card>
        <div className="grid gap-4 sm:grid-cols-2">
          <StatCard label="Orders today" value="24" hint="Example data" icon={ShoppingBag} />
          <StatCard label="Revenue" value="1,250,000 ₫" hint="Example data" />
        </div>
        <Panel title="Table & states" description="Try each state with keyboard or pointer.">
          <Tabs
            value={tab}
            onChange={setTab}
            label="Table states"
            items={[
              { value: "normal", label: "Normal" },
              { value: "loading", label: "Loading" },
              { value: "empty", label: "Empty" },
              { value: "error", label: "Error" },
            ]}
          >
            <DataTable
              caption="Example products"
              columns={columns}
              rows={tab === "empty" ? [] : rows}
              rowKey={(row) => row.id}
              loading={tab === "loading"}
              error={tab === "error" ? "The server could not be reached." : undefined}
              onRetry={() => setTab("normal")}
            />
          </Tabs>
          <Pagination
            currentPage={page}
            totalPages={5}
            onPageChange={setPage}
            showTotalItems={false}
          />
        </Panel>
        <div className="grid gap-4 md:grid-cols-2">
          <EmptyState
            title="No orders yet"
            description="Orders will appear here."
            action={<Button variant="secondary">Browse deals</Button>}
          />
          <ErrorState
            description="Please check your connection."
            onRetry={() => toast.info("Retry requested.")}
          />
        </div>
        <Card>
          <h2 className="mb-4 text-section font-semibold">Loading skeleton</h2>
          <div role="status" aria-label="Loading example" className="space-y-3">
            <Skeleton className="h-5 w-40" />
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-2/3" />
          </div>
        </Card>
        <Card>
          <h2 className="mb-4 text-section font-semibold">Overlays & notifications</h2>
          <div className="flex flex-wrap items-center gap-3">
            <Button variant="secondary" onClick={() => setModal(true)}>
              Open modal
            </Button>
            <Button variant="secondary" onClick={() => setDrawer(true)}>
              Open drawer
            </Button>
            <Button
              variant="secondary"
              onClick={() => toast.success("Changes saved successfully.")}
            >
              Success toast
            </Button>
            <Button variant="secondary" onClick={() => toast.error("Unable to save changes.")}>
              Error toast
            </Button>
            <Button variant="secondary" onClick={() => toast.info("Your data is up to date.")}>
              Info toast
            </Button>
          </div>
        </Card>
        <Modal isOpen={modal} onClose={() => setModal(false)} title="Confirm changes">
          <p className="mb-6 text-text-secondary">Review your changes before saving.</p>
          <div className="flex justify-end gap-3">
            <Button variant="secondary" onClick={() => setModal(false)}>
              Cancel
            </Button>
            <Button
              onClick={() => {
                setModal(false);
                toast.success("Changes saved successfully.");
              }}
            >
              Confirm
            </Button>
          </div>
        </Modal>
        <Drawer isOpen={drawer} onClose={() => setDrawer(false)} title="Details">
          <p className="text-text-secondary">A shared drawer for details and mobile navigation.</p>
          <Button className="mt-6" onClick={() => setDrawer(false)}>
            Done
          </Button>
        </Drawer>
      </div>
    </WorkspaceLayout>
  );
}
function Swatch({ label, className }: { label: string; className: string }) {
  return (
    <div>
      <div aria-hidden="true" className={`h-12 rounded-md ${className}`} />
      <p className="mt-2 text-caption text-text-secondary">{label}</p>
    </div>
  );
}
