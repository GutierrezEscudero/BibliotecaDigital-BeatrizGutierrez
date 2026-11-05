import { beforeAll, beforeEach, describe, expect, it } from "vitest";

const pageMarkup = `
    <input id="search-input" />
    <input id="available-filter" type="checkbox" />
    <div id="book-list"></div>
    <p id="empty-state" hidden></p>
    <span id="current-year"></span>
`;

let renderBooks;

beforeAll(async () => {
    document.body.innerHTML = pageMarkup;
    ({ renderBooks } = await import("./app.js?test-catalog"));
});

beforeEach(() => {
    document.getElementById("search-input").value = "";
    document.getElementById("available-filter").checked = false;
    renderBooks();
});

describe("catálogo de Biblioteca Digital", () => {
    it("renderiza el catálogo inicial y el año actual", () => {
        expect(document.querySelectorAll(".book-card")).toHaveLength(3);
        expect(document.querySelector(".status-available")).not.toBeNull();
        expect(document.querySelector(".status-unavailable")).not.toBeNull();
        expect(document.getElementById("current-year").textContent)
            .toBe(String(new Date().getFullYear()));
    });

    it("filtra libros por título ignorando mayúsculas y espacios", () => {
        const searchInput = document.getElementById("search-input");
        searchInput.value = "  SOMBRA  ";
        searchInput.dispatchEvent(new Event("input"));

        expect(document.querySelectorAll(".book-card")).toHaveLength(1);
        expect(document.querySelector(".book-card h3").textContent)
            .toBe("La sombra del viento");
    });

    it("muestra solo libros disponibles cuando se activa el filtro", () => {
        const filter = document.getElementById("available-filter");
        filter.checked = true;
        filter.dispatchEvent(new Event("change"));

        expect(document.querySelectorAll(".book-card")).toHaveLength(2);
        expect(document.body.textContent).not.toContain("El nombre del viento");
    });

    it("muestra el estado vacío cuando no hay coincidencias", () => {
        const searchInput = document.getElementById("search-input");
        searchInput.value = "libro inexistente";
        searchInput.dispatchEvent(new Event("input"));

        expect(document.querySelectorAll(".book-card")).toHaveLength(0);
        expect(document.getElementById("empty-state").hidden).toBe(false);
    });
});
