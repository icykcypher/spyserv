using AutoMapper;
using DataService.Model.UsersModel;

namespace DataService.MappingProfiles
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