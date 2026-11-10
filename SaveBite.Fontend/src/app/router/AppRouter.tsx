import { DocumentTitle } from "./DocumentTitle";
import { createBrowserRouter, RouterProvider } from "react-router-dom";
import { routes } from "./routes";
import { Outlet } from "react-router-dom";
import { UploadDraftResume } from "@/shared/uploads/UploadDraftResume";

function AppRouteHost() {
  return (
    <>
      <DocumentTitle />
      <Outlet />
      <UploadDraftResume />
    </>
  );
}

const router = createBrowserRouter([{ element: <AppRouteHost />, children: routes }]);

export function AppRouter() {
  return <RouterProvider router={router} />;
}
