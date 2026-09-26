using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface IJwtService
{
    AccessTokenResult CreateAccessToken(User user);
}
