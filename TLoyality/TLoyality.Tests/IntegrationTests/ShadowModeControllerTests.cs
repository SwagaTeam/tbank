using System.Net;
using Application.Models;
using Application.Services.Abstractions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using Xunit;

namespace TLoyality.Tests.IntegrationTests;

public class ShadowModeControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IShadowModeService _shadowServiceMock = Substitute.For<IShadowModeService>();

    public ShadowModeControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IShadowModeService));
                if (descriptor != null) services.Remove(descriptor);
                services.AddSingleton(_shadowServiceMock);
            });
        });
    }

    [Fact]
    public async Task GetRecommendation_ShouldReturnOk_WhenDataExists()
    {
        var client = _factory.CreateClient();
        var expectedResponse = new ShadowRecommendationResponse("Ok", "Loss", "Rec", 1);
        _shadowServiceMock.GetShadowRecommendation(1).Returns(expectedResponse);

        var response = await client.GetAsync("/api/shadow-mode/recommendation/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ShadowRecommendationResponse>();
        result!.CurrentStatus.Should().Be("Ok");
    }

    [Fact]
    public async Task AskShadow_ShouldReturnResponse_WhenValidRequest()
    {
        var client = _factory.CreateClient();
        var userMessage = "Test message";
        _shadowServiceMock.GetChatResponse(1, userMessage).Returns("AI Response");

        var response = await client.PostAsJsonAsync("/api/shadow-mode/chat/1", userMessage);

        var result = await response.Content.ReadFromJsonAsync<ChatResponseDto>();
        result!.Text.Should().Be("AI Response");
    }

    private record ChatResponseDto(string Text);
}