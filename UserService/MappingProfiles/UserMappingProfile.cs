using AutoMapper;
using UserService.Model;

namespace UserService.MappingProfiles
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<User, RegisterUserDto>().ReverseMap();
            CreateMap<User, UserDto>().ReverseMap();
        }
    }
}