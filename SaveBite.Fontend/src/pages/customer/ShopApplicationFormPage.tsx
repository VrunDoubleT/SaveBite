import { FormPageSkeleton } from "@/shared/components/ui/PageLoadingSkeleton";
import { toast } from "@/shared/stores/toastStore";
import { UploadDraftChoice } from "@/shared/uploads/UploadDraftChoice";

import { UploadTaskGuard } from "@/shared/uploads/UploadTaskGuard";
import { useEffect, useState } from "react";
import { FileText } from "lucide-react";
import { useNavigate, useParams } from "react-router-dom";
import { getApiErrorMessage } from "@/shared/api/httpClient";
import { APP_PATHS } from "@/app/router/paths";
import { shopApplicationApi } from "@/features/shop-applications/api/shopApplicationApi";
import { ShopApplicationForm } from "@/features/shop-applications/components/ShopApplicationForm";
import type { ShopApplication } from "@/features/shop-applications/types/shopApplication.types";

export function ShopApplicationFormPage() {
  const { applicationId } = useParams<{ applicationId: string }>();
  return (
    <UploadTaskGuard key={applicationId ?? "new"} scope={{ purpose: "shop-application" }}>
      <ShopApplicationFormContent applicationId={applicationId} />
    </UploadTaskGuard>
  );
}

function ShopApplicationFormContent({ applicationId }: { applicationId?: string }) {
  const navigate = useNavigate();
  const [application, setApplication] = useState<ShopApplication | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;
    shopApplicationApi
      .getHistory()
      .then((applications) => {
        const current = applicationId
          ? (applications.find((item) => item.id === applicationId) ?? null)
          : (applications.find((item) =>
              ["Pending", "NeedsRevision", "Approved"].includes(item.status),
            ) ?? null);
        if (!active) return;
        if (!applicationId) {
          setApplication(current);
          return;
        }
        if (!current || current.id !== applicationId) {
          setError("Cannot find shop application.");
          return;
        }

        if (!["Cancelled", "Rejected", "NeedsRevision"].includes(current.status)) {
          setError("Current shop application cannot be edited.");
          return;
        }

        setApplication(current);
      })
      .catch((requestError) => {
        if (active) setError(getApiErrorMessage(requestError));
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [applicationId]);

  function goToApplication() {
    navigate(APP_PATHS.ACCOUNT_SHOP_APPLICATION, { replace: true });
  }

  if (loading) {
    return <FormPageSkeleton label="Loading shop application" />;
  }

  if (error) {
    return (
      <div className="rounded-lg border border-danger/30 bg-danger/10 p-4 text-sm text-danger">
        {error}
        <button type="button" onClick={goToApplication} className="ml-3 font-semibold underline">
          Go back
        </button>
      </div>
    );
  }

  if (
    !applicationId &&
    application &&
    ["Pending", "NeedsRevision", "Approved"].includes(application.status)
  ) {
    const message =
      application.status === "Pending"
        ? "Your shop application is being processed"
        : application.status === "NeedsRevision"
          ? "Your existing shop application needs an update"
          : "Your shop application has already been approved";
    return (
      <section
        role="status"
        className="rounded-lg border border-primary-200 bg-primary-50 p-5 text-primary-800"
      >
        <h2 className="flex items-center gap-2 text-base font-semibold">
          <FileText aria-hidden="true" className="size-5 shrink-0" />
          {message}
        </h2>
        <p className="mt-2 text-sm">
          {application.status === "Pending"
            ? "You already have 1 application under review. Please wait for the result before submitting another application."
            : application.status === "NeedsRevision"
              ? "Please edit and resubmit your existing application instead of creating a new one."
              : "You can now manage your shop. A new application is not needed."}
        </p>
        <button
          type="button"
          onClick={goToApplication}
          className="mt-4 min-h-11 rounded-md bg-primary-700 px-4 py-2 text-sm font-semibold text-white hover:bg-primary-800 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-500"
        >
          View existing application
        </button>
      </section>
    );
  }

  const form = (
    <ShopApplicationForm
      application={applicationId ? application : null}
      onQueued={() => {
        toast.info(
          "Your application is uploading in the background. Follow its progress in the upload panel.",
        );
        goToApplication();
      }}
      onSaved={goToApplication}
      onCancel={goToApplication}
    />
  );
  return (
    <UploadDraftChoice
      scope={{
        purpose: "shop-application",
        ...(applicationId ? { targetIds: [applicationId] } : { withoutTarget: true }),
      }}
    >
      {(draftId) => <div key={draftId ?? "new"}>{form}</div>}
    </UploadDraftChoice>
  );
}
