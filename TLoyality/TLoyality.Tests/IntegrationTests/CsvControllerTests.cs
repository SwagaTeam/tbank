using System.Net;
using System.Net.Http.Headers;
using Application.Services.Abstractions;
using Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using Xunit;

namespace TLoyality.Tests.IntegrationTests;

public class CsvControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ICsvImportService _csvServiceMock = Substitute.For<ICsvImportService>();

    public CsvControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Заменяем реальный сервис импорта на мок
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICsvImportService));
                if (descriptor != null) services.Remove(descriptor);
                services.AddSingleton(_csvServiceMock);
            });
        });
    }

    /// <summary>
    /// Хелпер для создания контента файла (MultipartFormDataContent)
    /// </summary>
    private MultipartFormDataContent CreateFileContent(string fileName, string content, string paramName = "file")
    {
        var fileContent = new StreamContent(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

        var multipart = new MultipartFormDataContent();
        multipart.Add(fileContent, paramName, fileName);
        return multipart;
    }

    [Fact]
    public async Task UploadUsers_ShouldReturnCount_WhenFileIsValid()
    {
        // Arrange
        var client = _factory.CreateClient();
        var expectedCount = 5;

        // Настраиваем мок для работы с любым IFormFile для типа User
        _csvServiceMock.ImportAsync<User>(Arg.Any<IFormFile>()).Returns(expectedCount);

        var content = CreateFileContent("users.csv", "Id,FullName\n1,Ivan Ivanov");

        // Act
        var response = await client.PostAsync("/api/csv/upload-users", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CountResponse>();
        result!.Count.Should().Be(expectedCount);
    }

    [Fact]
    public async Task UploadAll_ShouldProcessMultipleFiles_AndReturnTotalSum()
    {
        // Arrange
        var client = _factory.CreateClient();
        _csvServiceMock.ImportAsync<User>(Arg.Any<IFormFile>()).Returns(10);
        _csvServiceMock.ImportAsync<Accounts>(Arg.Any<IFormFile>()).Returns(20);

        var multipart = new MultipartFormDataContent();

        // Добавляем два файла под разными именами параметров (как в контроллере)
        var usersFile = new StreamContent(new MemoryStream("data"u8.ToArray()));
        multipart.Add(usersFile, "users", "users.csv");

        var accountsFile = new StreamContent(new MemoryStream("data"u8.ToArray()));
        multipart.Add(accountsFile, "accounts", "accounts.csv");

        // Act
        var response = await client.PostAsync("/api/csv/upload-all", multipart);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TotalRecordsResponse>();
        result!.TotalRecords.Should().Be(30); // 10 + 20

        // Проверяем, что методы вызывались
        await _csvServiceMock.Received(1).ImportAsync<User>(Arg.Any<IFormFile>());
        await _csvServiceMock.Received(1).ImportAsync<Accounts>(Arg.Any<IFormFile>());
    }

    // Вспомогательные классы для десериализации ответа
    private record CountResponse(int Count);

    private record TotalRecordsResponse(int TotalRecords);
}