using Microsoft.AspNetCore.Http;

namespace Application.Services.Abstractions;

public interface ICsvImportService
{
   Task<int> ImportAsync<T>(IFormFile file) where T : class;
}
