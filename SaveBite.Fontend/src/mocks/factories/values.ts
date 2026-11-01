import { fakerVI as faker } from "@faker-js/faker";
faker.seed(42);
const names = Array.from({ length: 24 }, () => faker.person.fullName());
export const person = (index: number) => names[index % names.length];
// Relative dates keep previews useful on any day; session storage fixes them across reloads.
const epoch = Number(sessionStorage.getItem("savebite-mock-epoch")) || Date.now();
sessionStorage.setItem("savebite-mock-epoch", String(epoch));
export const past = new Date(epoch - 60 * 60 * 1000).toISOString();
export const future = new Date(epoch + 24 * 60 * 60 * 1000).toISOString();
