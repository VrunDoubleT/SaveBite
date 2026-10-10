using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Services.Interfaces;

public interface IJwtService
{
    AccessTokenResult CreateAccessToken(User user);
}
