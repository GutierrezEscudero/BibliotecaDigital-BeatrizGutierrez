// JavaScript source code
const books = [
    {
        id: 1,
        title: "Cien años de soledad",
        availableCopies: 3
    },
    {
        id: 2,
        title: "La sombra del viento",
        availableCopies: 2
    },
    {
        id: 3,
        title: "El nombre del viento",
        availableCopies: 0
    }
];
const bookList = document.getElementById("book-list");
const searchInput = document.getElementById("search-input");
const availableFilter = document.getElementById("available-filter");
const emptyState = document.getElementById("empty-state");
function renderBooks() {
    const searchText = searchInput.value
        .trim()
        .toLocaleLowerCase("es");
    const onlyAvailable = availableFilter.checked;
    const filteredBooks = books.filter(book => {
        const matchesSearch = book.title
            .toLocaleLowerCase("es")
            .includes(searchText);
        const matchesAvailability =
            !onlyAvailable || book.availableCopies > 0;
        return matchesSearch && matchesAvailability;
    });
    bookList.innerHTML = "";
    emptyState.hidden = filteredBooks.length > 0;
    filteredBooks.forEach(book => {
        const card = document.createElement("article");
        card.className = "book-card";
        const statusClass = book.availableCopies > 0
            ? "status-available"
            : "status-unavailable";
        const statusText = book.availableCopies > 0
            ? "Disponible"
            : "No disponible";
        card.innerHTML = `
 <h3>${book.title}</h3>
 <p>Identificador: ${book.id}</p>
 <p>
 Ejemplares disponibles:
 <strong>${book.availableCopies}</strong>
 </p>
 <span class="book-status ${statusClass}">
 ${statusText}
 </span>
 `;
        bookList.appendChild(card);
    });
}
searchInput.addEventListener("input", renderBooks);
availableFilter.addEventListener("change", renderBooks);
document.getElementById("current-year").textContent =
    new Date().getFullYear();
renderBooks();