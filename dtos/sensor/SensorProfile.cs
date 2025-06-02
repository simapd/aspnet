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
      CreateMap<SensorRequestDto, Sensor>()
       .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember is not null));
    }
  }
}
