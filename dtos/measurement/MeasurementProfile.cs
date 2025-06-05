using AutoMapper;
using Simapd.Dtos;
using Simapd.Models;

namespace Simapd.Profiles
{
  public class MeasurementProfile: Profile
  {
    public MeasurementProfile()
    {
      CreateMap<Measurement, MeasurementDto>();
      CreateMap<MeasurementRequestDto, Measurement>()
       .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember is not null));
    }
  }
}
