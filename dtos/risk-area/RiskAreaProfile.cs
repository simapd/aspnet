using AutoMapper;
using Simapd.Dtos;
using Simapd.Models;

namespace Simapd.Profiles
{
  public class RiskAreaProfile: Profile
  {
    public RiskAreaProfile()
    {
      CreateMap<RiskArea, RiskAreaDto>();
      CreateMap<RiskAreaRequestDto, RiskArea>();
    }
  }
}
