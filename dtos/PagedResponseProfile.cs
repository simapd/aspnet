using AutoMapper;
using Simapd.Dtos;
using Simapd.Models;

namespace Simapd.Repositories
{
  public class PagedResponseProfile : Profile
  {
     public PagedResponseProfile()
     {
         CreateMap(typeof(PagedResponse<>), typeof(PagedResponseDto<>));
     }
  }
}
