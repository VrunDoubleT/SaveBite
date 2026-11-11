import { useEffect, useState } from "react";
import { ArrowUp } from "lucide-react";

interface ScrollToTopButtonProps {
  targetId?: string;
  offset?: number;
  label?: string;
}

export function ScrollToTopButton({
  targetId = "flash-deals-heading",
  offset = -90,
  label = "Back to top of Flash Deals",
}: ScrollToTopButtonProps) {
  const [isVisible, setIsVisible] = useState(false);

  useEffect(() => {
    const handleScroll = () => {
      const target = targetId ? document.getElementById(targetId) : null;
      // Show button if user has scrolled down past the target or > 300px
      const threshold = target
        ? target.getBoundingClientRect().top + window.scrollY - 100
        : 300;

      if (window.scrollY > Math.max(250, threshold)) {
        setIsVisible(true);
      } else {
        setIsVisible(false);
      }
    };

    window.addEventListener("scroll", handleScroll, { passive: true });
    handleScroll();

    return () => window.removeEventListener("scroll", handleScroll);
  }, [targetId]);

  const scrollToTarget = () => {
    const target = targetId ? document.getElementById(targetId) : null;
    if (target) {
      const targetPos = target.getBoundingClientRect().top + window.scrollY + offset;
      // If user is already near the flash deals top (within 120px), scroll to top of page
      if (Math.abs(window.scrollY - targetPos) < 120) {
        window.scrollTo({ top: 0, behavior: "smooth" });
      } else {
        window.scrollTo({ top: Math.max(0, targetPos), behavior: "smooth" });
      }
    } else {
      window.scrollTo({ top: 0, behavior: "smooth" });
    }
  };

  return (
    <button
      type="button"
      onClick={scrollToTarget}
      aria-label={label}
      title={label}
      className={`group fixed bottom-6 right-6 z-40 flex items-center gap-2 rounded-full border border-primary-200/90 bg-white/95 px-3 py-2.5 text-primary-700 shadow-xl backdrop-blur-md transition-all duration-300 hover:scale-105 hover:border-primary-300 hover:bg-primary-50 hover:text-primary-800 hover:shadow-2xl focus:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 sm:px-4 sm:py-3 ${
        isVisible
          ? "pointer-events-auto translate-y-0 opacity-100"
          : "pointer-events-none translate-y-4 opacity-0"
      }`}
    >
      <span className="flex size-6 items-center justify-center rounded-full bg-primary-100 text-primary-700 transition-transform duration-200 group-hover:-translate-y-0.5 group-hover:bg-primary-200">
        <ArrowUp className="size-4" aria-hidden="true" />
      </span>
      <span className="hidden text-xs font-bold tracking-tight sm:inline">
        Top of Deals
      </span>
    </button>
  );
}
