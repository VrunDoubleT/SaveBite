export function CustomerHomePage() {
  return (
    <section className="grid min-h-[calc(100vh-10rem)] place-items-center py-20 text-center">
      <div className="max-w-2xl">
        <span className="rounded-full bg-primary-50 px-4 py-2 text-xs font-medium uppercase tracking-wider text-primary-700">
          SaveBite Customer
        </span>
        <h1 className="mt-6 text-4xl font-extrabold leading-tight tracking-tight text-neutral-900 md:text-5xl">
          Rescue great food and save every day.
        </h1>
        <p className="mx-auto mt-5 max-w-xl text-base leading-relaxed text-neutral-600 md:text-lg">
          Discover quality meals at great prices from stores near you.
        </p>
      </div>
    </section>
  );
}
