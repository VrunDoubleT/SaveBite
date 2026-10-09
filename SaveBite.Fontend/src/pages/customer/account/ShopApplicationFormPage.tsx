import { useEffect, useState } from "react";
import { LoaderCircle } from "lucide-react";
import { useNavigate, useParams } from "react-router-dom";
import { getApiErrorMessage } from "@/shared/api/httpClient";
import { APP_PATHS } from "@/app/router/paths";
import { shopApplicationApi } from "@/features/account/shop-application/api/shopApplicationApi";
import { ShopApplicationForm } from "@/features/account/shop-application/components/ShopApplicationForm";
import type { ShopApplication } from "@/features/account/shop-application/types/shopApplication.types";

export function ShopApplicationFormPage() {
  const navigate = useNavigate();
  const { applicationId } = useParams<{ applicationId: string }>();
  const [application, setApplication] = useState<ShopApplication | null>(null);
  const [loading, setLoading] = useState(Boolean(applicationId));
  const [error, setError] = useState("");

  useEffect(() => {
    if (!applicationId) return;

    shopApplicationApi
      .getMine()
      .then((current) => {
        if (!current || current.id !== applicationId) {
          setError("Cannot find shop application.");
          return;
        }

        if (!["Cancelled", "Rejected"].includes(current.status)) {
          setError("Current shop application cannot be edited.");
          return;
        }

        setApplication(current);
      })
      .catch((requestError) => setError(getApiErrorMessage(requestError)))
      .finally(() => setLoading(false));
  }, [applicationId]);

  function goToApplication() {
    navigate(APP_PATHS.ACCOUNT_SHOP_APPLICATION, { replace: true });
  }

  if (loading) {
    return (
      <div className="flex min-h-64 items-center justify-center rounded-2xl border border-border-default bg-bg-surface">
        <LoaderCircle className="animate-spin text-primary-600" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="rounded-2xl border border-danger/30 bg-danger/10 p-6 text-sm text-danger">
        {error}
        <button type="button" onClick={goToApplication} className="ml-3 font-semibold underline">
          Go back
        </button>
      </div>
    );
  }

  return (
    <ShopApplicationForm
      application={application}
      onSaved={goToApplication}
      onCancel={goToApplication}
    />
  );
}
