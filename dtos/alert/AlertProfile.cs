using AutoMapper;
using Simapd.Dtos;
using Simapd.Models;

namespace Simapd.Profiles
{
  public class AlertProfile: Profile
  {
    public AlertProfile()
    {
      CreateMap<Alert, AlertDto>();
      CreateMap<AlertRequestDto, Alert>()
       .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember is not null));
    }
  }
}
