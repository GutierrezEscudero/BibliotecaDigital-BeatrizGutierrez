using System.Net;
using System.Net.Http.Json;
using Biblioteca.Api.Contracts;
using Xunit;

namespace Biblioteca.IntegrationTests;

public class BooksApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetBooks_ReturnsSeededCatalog()
    {
        var response = await _client.GetAsync("/api/books");
        response.EnsureSuccessStatusCode();

        var books = await response.Content.ReadFromJsonAsync<List<BookResponse>>();
        Assert.NotNull(books);
        Assert.True(books.Count >= 3);
        Assert.Contains(books, x => x.Title == "Cien años de soledad");
    }

    [Fact]
    public async Task GetBook_WithUnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/books/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateBook_WithValidData_ReturnsCreated()
    {
        var request = new CreateBookRequest
        {
            Title = "El código Da Vinci", Author = "Dan Brown",
            Isbn = "9780307474278", PublishedYear = 2003, AvailableCopies = 4
        };

        var response = await _client.PostAsJsonAsync("/api/books", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task CreateBook_WithDuplicateIsbn_ReturnsConflict()
    {
        var request = new CreateBookRequest
        {
            Title = "Duplicado", Author = "Autor",
            Isbn = "9780307474728", PublishedYear = 2020, AvailableCopies = 1
        };

        var response = await _client.PostAsJsonAsync("/api/books", request);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBook_WithUnknownId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/books/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBook_WithoutActiveLoans_ReturnsNoContent()
    {
        var request = new CreateBookRequest
        {
            Title = "Libro para eliminar",
            Author = "Autor de pruebas",
            Isbn = $"978{Random.Shared.Next(1_000_000_000, 1_999_999_999)}",
            PublishedYear = 2020,
            AvailableCopies = 1
        };
        var createResponse = await _client.PostAsJsonAsync("/api/books", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdBook = await createResponse.Content.ReadFromJsonAsync<BookResponse>();
        Assert.NotNull(createdBook);

        var deleteResponse = await _client.DeleteAsync($"/api/books/{createdBook.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteBook_WithActiveLoan_ReturnsConflict()
    {
        var request = new CreateLoanRequest
        {
            BookId = 1,
            Borrower = "Lector de pruebas",
            LoanDate = new DateOnly(2026, 11, 3),
            DueDate = new DateOnly(2026, 11, 17)
        };
        var loanResponse = await _client.PostAsJsonAsync("/api/loans", request);
        Assert.Equal(HttpStatusCode.Created, loanResponse.StatusCode);

        var deleteResponse = await _client.DeleteAsync("/api/books/1");

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
    }

    private sealed record BookResponse(int Id, string Title, string Author, string Isbn, int PublishedYear, int AvailableCopies);
}
