using System.Net;
using System.Net.Http.Json;
using Biblioteca.Api.Contracts;
using Xunit;

namespace Biblioteca.E2ETests;

// These tests call a separately running API over real HTTP (Kestrel).
// Set E2E_BASE_URL to override the API launch profile's HTTP address.
public class LibraryWorkflowE2ETests
{
    private readonly HttpClient _client = new()
    {
        BaseAddress = new Uri(
            Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:52749")
    };

    [Fact]
    public async Task CompleteBookLifecycle_CreateReadAndDelete()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new CreateBookRequest
        {
            Title = $"Libro E2E {suffix}",
            Author = "Autor de pruebas",
            Isbn = $"978{Random.Shared.Next(1000000000, 1999999999)}",
            PublishedYear = 2022,
            AvailableCopies = 1
        };

        var create = await _client.PostAsJsonAsync("/api/books", request);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var created = await create.Content.ReadFromJsonAsync<BookDto>();
        Assert.NotNull(created);

        var get = await _client.GetAsync($"/api/books/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        var delete = await _client.DeleteAsync($"/api/books/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var getAfterDelete = await _client.GetAsync($"/api/books/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
    }

    [Fact]
    public async Task LoanLifecycle_CreateAndReturnLoan()
    {
        var request = new CreateLoanRequest
        {
            BookId = 2,
            Borrower = $"E2E {Guid.NewGuid():N}"[..12],
            LoanDate = DateOnly.FromDateTime(DateTime.Today),
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(14))
        };

        var create = await _client.PostAsJsonAsync("/api/loans", request);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var loan = await create.Content.ReadFromJsonAsync<LoanDto>();
        Assert.NotNull(loan);

        var returned = await _client.PutAsync($"/api/loans/{loan.Id}/return", null);
        Assert.Equal(HttpStatusCode.OK, returned.StatusCode);
    }

    [Fact]
    public async Task CompleteLoanLifecycle_UpdatesCopiesAndRejectsSecondReturn()
    {
        var book = await CreateBookAsync(2);

        var getBook = await _client.GetFromJsonAsync<BookDto>($"/api/books/{book.Id}");
        Assert.NotNull(getBook);
        Assert.Equal(2, getBook.AvailableCopies);

        var loanRequest = new CreateLoanRequest
        {
            BookId = book.Id,
            Borrower = $"E2E {Guid.NewGuid():N}"[..12],
            LoanDate = DateOnly.FromDateTime(DateTime.Today),
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(14))
        };
        var createLoan = await _client.PostAsJsonAsync("/api/loans", loanRequest);
        Assert.Equal(HttpStatusCode.Created, createLoan.StatusCode);
        var loan = await createLoan.Content.ReadFromJsonAsync<LoanDto>();
        Assert.NotNull(loan);

        var storedLoan = await _client.GetFromJsonAsync<LoanDto>($"/api/loans/{loan.Id}");
        Assert.NotNull(storedLoan);
        Assert.Equal("Activo", storedLoan.Status);

        var reducedBook = await _client.GetFromJsonAsync<BookDto>($"/api/books/{book.Id}");
        Assert.NotNull(reducedBook);
        Assert.Equal(1, reducedBook.AvailableCopies);

        var returned = await _client.PutAsync($"/api/loans/{loan.Id}/return", null);
        Assert.Equal(HttpStatusCode.OK, returned.StatusCode);

        var returnedLoan = await _client.GetFromJsonAsync<LoanDto>($"/api/loans/{loan.Id}");
        Assert.NotNull(returnedLoan);
        Assert.Equal("Devuelto", returnedLoan.Status);

        var restoredBook = await _client.GetFromJsonAsync<BookDto>($"/api/books/{book.Id}");
        Assert.NotNull(restoredBook);
        Assert.Equal(2, restoredBook.AvailableCopies);

        var secondReturn = await _client.PutAsync($"/api/loans/{loan.Id}/return", null);
        Assert.Equal(HttpStatusCode.Conflict, secondReturn.StatusCode);

        var delete = await _client.DeleteAsync($"/api/books/{book.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task DeleteBook_WithActiveLoan_IsRejectedUntilLoanIsReturned()
    {
        var book = await CreateBookAsync(1);
        var loanRequest = new CreateLoanRequest
        {
            BookId = book.Id,
            Borrower = $"E2E {Guid.NewGuid():N}"[..12],
            LoanDate = DateOnly.FromDateTime(DateTime.Today),
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(14))
        };
        var createLoan = await _client.PostAsJsonAsync("/api/loans", loanRequest);
        Assert.Equal(HttpStatusCode.Created, createLoan.StatusCode);
        var loan = await createLoan.Content.ReadFromJsonAsync<LoanDto>();
        Assert.NotNull(loan);

        var blockedDelete = await _client.DeleteAsync($"/api/books/{book.Id}");
        Assert.Equal(HttpStatusCode.Conflict, blockedDelete.StatusCode);

        var returned = await _client.PutAsync($"/api/loans/{loan.Id}/return", null);
        Assert.Equal(HttpStatusCode.OK, returned.StatusCode);

        var delete = await _client.DeleteAsync($"/api/books/{book.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var getAfterDelete = await _client.GetAsync($"/api/books/{book.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
    }

    private async Task<BookDto> CreateBookAsync(int availableCopies)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new CreateBookRequest
        {
            Title = $"Libro E2E préstamo {suffix}",
            Author = "Autor de pruebas",
            Isbn = $"978{Random.Shared.Next(1000000000, 1999999999)}",
            PublishedYear = 2022,
            AvailableCopies = availableCopies
        };

        var response = await _client.PostAsJsonAsync("/api/books", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var book = await response.Content.ReadFromJsonAsync<BookDto>();
        Assert.NotNull(book);
        return book;
    }

    private sealed record BookDto(int Id, string Title, string Author, string Isbn, int PublishedYear, int AvailableCopies);
    private sealed record LoanDto(int Id, int BookId, string Borrower, DateOnly LoanDate, DateOnly DueDate, DateOnly? ReturnDate, string Status);
}
