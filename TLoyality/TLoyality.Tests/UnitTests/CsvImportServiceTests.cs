using System.Text;
using Application.Services.Implementations;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Repositories.Abstractions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace TLoyality.Tests.UnitTests;

public class CsvImportServiceTests
{
    private readonly IRepository<LoyaltyHistory> _historyRepoMock;
    private readonly ITransactionRepository _transactionRepoMock;
    private readonly CsvImportService _service;

    public CsvImportServiceTests()
    {
        var serviceProviderMock = Substitute.For<IServiceProvider>();
        _historyRepoMock = Substitute.For<IRepository<LoyaltyHistory>>();
        _transactionRepoMock = Substitute.For<ITransactionRepository>();

        serviceProviderMock.GetService(typeof(IRepository<LoyaltyHistory>)).Returns(_historyRepoMock);
        serviceProviderMock.GetService(typeof(ITransactionRepository)).Returns(_transactionRepoMock);

        _service = new CsvImportService(serviceProviderMock);
    }

    private IFormFile CreateMockCsv(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", "import.csv");
    }

    /// <summary>
    /// Проверка успешного маппинга полей и вызова генерации транзакций.
    /// </summary>
    [Fact]
    public async Task ImportAsync_ShouldCorrectlyMapFields_AndTriggerTransactions()
    {
        // Arrange
        var csvContent = "transaction_id,account_id,cashback_amount,payout_date\n" +
                         "101,1,50,2023-10-10";
        var file = CreateMockCsv(csvContent);

        // Act
        var result = await _service.ImportAsync<LoyaltyHistory>(file);

        // Assert
        result.Should().Be(1);
    
        await _historyRepoMock.Received(1).AddRangeAsync(Arg.Is<ICollection<LoyaltyHistory>>(list => 
            list.First().TransactionId == 101 && 
            list.First().AccountId == 1 &&
            list.First().CashbackAmount == 50
        ));

        await _transactionRepoMock.Received(1).AddRangeAsync(Arg.Any<IEnumerable<Transaction>>());
    }

    /// <summary>
    /// Тест-кейс: Проверка генерации транзакций при импорте истории.
    /// </summary>
    [Fact]
    public async Task ImportAsync_ShouldTriggerTransactionGeneration_WhenTypeIsLoyaltyHistory()
    {
        // Arrange
        var csvContent = "account_id,cashback_amount,payout_date,loyalty_program_name,currency\n" +
                         "1,100,2023-10-10,tinkoff_black,rub";
        var file = CreateMockCsv(csvContent);

        // Act
        await _service.ImportAsync<LoyaltyHistory>(file);

        // Assert
        await _transactionRepoMock.Received(1).AddRangeAsync(Arg.Is<IEnumerable<Transaction>>(t => t.Any()));
        await _transactionRepoMock.Received(1).SaveChangesAsync();
    }

    /// <summary>
    /// Негативный кейс: Пустой файл не должен вызывать методы репозитория.
    /// </summary>
    [Fact]
    public async Task ImportAsync_ShouldReturnZero_WhenFileIsNull()
    {
        // Act
        var result = await _service.ImportAsync<LoyaltyHistory>(null);

        // Assert
        result.Should().Be(0);
        await _historyRepoMock.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<LoyaltyHistory>>());
    }

    /// <summary>
    /// Пограничный случай: Файл только с заголовками.
    /// </summary>
    [Fact]
    public async Task ImportAsync_ShouldReturnZero_WhenFileHasOnlyHeaders()
    {
        // Arrange
        var csvContent = "account_id,cashback_amount,payout_date,loyalty_program_name,currency";
        var file = CreateMockCsv(csvContent);

        // Act
        var result = await _service.ImportAsync<LoyaltyHistory>(file);

        // Assert
        result.Should().Be(0);
        await _historyRepoMock.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<LoyaltyHistory>>());
    }
}