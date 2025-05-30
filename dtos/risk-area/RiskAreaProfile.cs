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
      CreateMap<RiskAreaRequestDto, RiskArea>()
       .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember is not null));
    }
  }
}
