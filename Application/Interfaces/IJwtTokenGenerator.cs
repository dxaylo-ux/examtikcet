using Application.Identity;

namespace Application.Interfaces;

public interface IJwtTokenGenerator
{
    string Generate(User user, IList<string> roles);
}
