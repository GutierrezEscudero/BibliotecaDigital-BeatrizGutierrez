using System.Net;
using System.Net.Http.Json;
using Biblioteca.Api.Contracts;
using Xunit;

namespace Biblioteca.IntegrationTests;

public class LoansApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateLoan_WithAvailableBook_ReturnsCreated()
    {
        var request = new CreateLoanRequest
        {
            BookId = 1, Borrower = "María Pérez",
            LoanDate = new DateOnly(2026, 11, 3),
            DueDate = new DateOnly(2026, 11, 17)
        };

        var response = await _client.PostAsJsonAsync("/api/loans", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateLoan_WithUnavailableBook_ReturnsBadRequest()
    {
        var request = new CreateLoanRequest
        {
            BookId = 3, Borrower = "María Pérez",
            LoanDate = new DateOnly(2026, 11, 3),
            DueDate = new DateOnly(2026, 11, 17)
        };

        var response = await _client.PostAsJsonAsync("/api/loans", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateLoan_WithUnknownBook_ReturnsBadRequest()
    {
        var request = new CreateLoanRequest
        {
            BookId = 9999, Borrower = "María Pérez",
            LoanDate = new DateOnly(2026, 11, 3),
            DueDate = new DateOnly(2026, 11, 17)
        };

        var response = await _client.PostAsJsonAsync("/api/loans", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReturnLoan_UnknownId_ReturnsNotFound()
    {
        var response = await _client.PutAsync("/api/loans/9999/return", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReturnLoan_Twice_ReturnsConflictOnSecondAttempt()
    {
        var bookRequest = new CreateBookRequest
        {
            Title = "Libro para devolución repetida", Author = "Autor de pruebas",
            Isbn = $"978{Random.Shared.Next(1_000_000_000, 1_999_999_999)}",
            PublishedYear = 2020, AvailableCopies = 1
        };
        var bookResponse = await _client.PostAsJsonAsync("/api/books", bookRequest);
        Assert.Equal(HttpStatusCode.Created, bookResponse.StatusCode);
        var book = await bookResponse.Content.ReadFromJsonAsync<BookResponse>();
        Assert.NotNull(book);

        var loanRequest = new CreateLoanRequest
        {
            BookId = book.Id, Borrower = "María Pérez",
            LoanDate = new DateOnly(2026, 11, 3),
            DueDate = new DateOnly(2026, 11, 17)
        };
        var loanResponse = await _client.PostAsJsonAsync("/api/loans", loanRequest);
        Assert.Equal(HttpStatusCode.Created, loanResponse.StatusCode);
        var loan = await loanResponse.Content.ReadFromJsonAsync<LoanResponse>();
        Assert.NotNull(loan);

        var firstReturn = await _client.PutAsync($"/api/loans/{loan.Id}/return", null);
        var secondReturn = await _client.PutAsync($"/api/loans/{loan.Id}/return", null);

        Assert.Equal(HttpStatusCode.OK, firstReturn.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondReturn.StatusCode);
    }

    private sealed record BookResponse(int Id, string Title, string Author, string Isbn, int PublishedYear, int AvailableCopies);

    private sealed record LoanResponse(int Id, int BookId, string Borrower, DateOnly LoanDate, DateOnly DueDate, DateOnly? ReturnDate, string Status);
}
