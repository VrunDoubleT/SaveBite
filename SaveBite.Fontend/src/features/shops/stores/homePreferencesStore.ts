import { create } from "zustand";
import { persist } from "zustand/middleware";

interface HomePreferences {
  storesExpanded: boolean;
  mobileFiltersExpanded: boolean;
  storesView: "list" | "map";
  setStoresExpanded: (value: boolean) => void;
  setMobileFiltersExpanded: (value: boolean) => void;
  setStoresView: (value: "list" | "map") => void;
}
export const useHomePreferencesStore = create<HomePreferences>()(
  persist(
    (set) => ({
      storesExpanded: false,
      mobileFiltersExpanded: false,
      storesView: "map",
      setStoresExpanded: (storesExpanded) => set({ storesExpanded }),
      setMobileFiltersExpanded: (mobileFiltersExpanded) => set({ mobileFiltersExpanded }),
      setStoresView: (storesView) => set({ storesView }),
    }),
    {
      name: "savebite-home-preferences",
      version: 2,
      migrate: (persisted) => ({ ...(persisted as Partial<HomePreferences>), storesView: "map" }),
    },
  ),
);
