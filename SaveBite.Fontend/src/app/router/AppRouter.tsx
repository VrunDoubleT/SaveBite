import { createBrowserRouter, RouterProvider } from "react-router-dom";
import { routes } from "./routes";
import { Outlet } from "react-router-dom";
import { UploadDraftResume } from "@/shared/uploads/UploadDraftResume";

function AppRouteHost() {
  return (
    <>
      <Outlet />
      <UploadDraftResume />
    </>
  );
}

const router = createBrowserRouter([{ element: <AppRouteHost />, children: routes }]);

export function AppRouter() {
  return <RouterProvider router={router} />;
}
