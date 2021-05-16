using API.Models;

namespace API.Helpers
{
    public interface IBuildExchangeRequests
    {
        BuildResult Build(RequestInfo info);
    }
}