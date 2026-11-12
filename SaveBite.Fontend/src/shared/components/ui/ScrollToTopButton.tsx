import { useEffect, useState } from "react";
import { ArrowUp } from "lucide-react";
import { Button } from "./Controls";

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
      // Show after scrolling well into the deals section.
      const threshold = target
        ? target.getBoundingClientRect().top + window.scrollY + 600
        : 600;

      if (window.scrollY > threshold) {
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
    const behavior: ScrollBehavior = window.matchMedia("(prefers-reduced-motion: reduce)").matches
      ? "instant" : "smooth";
    const target = targetId ? document.getElementById(targetId) : null;
    if (target) {
      const targetPos = target.getBoundingClientRect().top + window.scrollY + offset;
      // If user is already near the flash deals top (within 120px), scroll to top of page
      if (Math.abs(window.scrollY - targetPos) < 120) {
        window.scrollTo({ top: 0, behavior });
      } else {
        window.scrollTo({ top: Math.max(0, targetPos), behavior });
      }
    } else {
      window.scrollTo({ top: 0, behavior });
    }
  };

  return (
    <Button
      variant="primary"
      type="button"
      onClick={scrollToTarget}
      aria-label={label}
      title={label}
      tabIndex={isVisible ? 0 : -1}
      aria-hidden={!isVisible}
      className={`fixed bottom-5 right-4 z-40 min-h-11 min-w-11 px-3 shadow-md transition-[opacity,transform] duration-200 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-700 motion-reduce:transition-none sm:bottom-6 sm:right-6 sm:px-4 ${
        isVisible
          ? "pointer-events-auto translate-y-0 opacity-100"
          : "pointer-events-none translate-y-4 opacity-0"
      }`}
    >
      <ArrowUp className="size-4 shrink-0" aria-hidden="true" />
      <span className="hidden sm:inline">Top of deals</span>
    </Button>
  );
}
