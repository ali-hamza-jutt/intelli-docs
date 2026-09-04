using DocuMind.Application.Interfaces;

namespace DocuMind.Application.Services;

public class HealthService : IHealthService
{
    public string GetStatus()
    {
        return "DocuMind API is running";
    }
}