using AutoMapper;
using Simapd.Dtos;
using Simapd.Models;

namespace Simapd.Profiles
{
  public class SensorProfile: Profile
  {
    public SensorProfile()
    {
      CreateMap<Sensor, SensorDto>();
      // CreateMap<RiskAreaRequestDto, RiskArea>()
      //  .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember is not null));
    }
  }
}
