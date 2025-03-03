using AutoMapper;
using DataService.Model.UsersModel;
using DataService.StorageRepositories;

namespace DataService.Services.UserServices
{
    public class UserService : IUserService
    {
        private readonly IUserStorageRepository _repository;
        private readonly IMapper _mapper;

        public UserService(IUserStorageRepository repository, IMapper mapper)
        {
            this._repository = repository;
            this._mapper = mapper;
        }

        public async Task<User> Register(RegisterUserDto registerUserDto)
        {
            var user = _mapper.Map<User>(registerUserDto);


            user = await _repository.AddNewUserAsync(user);

            if (user is null) throw new InvalidOperationException();

            return user;
        }
    }
}