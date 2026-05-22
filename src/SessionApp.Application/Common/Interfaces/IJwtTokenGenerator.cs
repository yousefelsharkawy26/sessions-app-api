using SessionApp.Domain.Entities;

namespace SessionApp.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(ApplicationUser user);
}
