using AutoMapper;
using DataService.Model.UsersModel;
using DataService.StorageRepositories;

namespace DataService.Services.UserServices
{
    public class UserDatabaseService : IUserDatabaseService
    {
        private readonly IUserStorageRepository _repository;
        private readonly IMapper _mapper;

        public UserDatabaseService(IUserStorageRepository repository, IMapper mapper)
        {
            this._repository = repository;
            this._mapper = mapper;
        }

        public async Task<User> Register(RegisterUserDto registerUserDto)
        {
            var user = _mapper.Map<User>(registerUserDto);

            user.PasswordHash = "blablabla";

            user = await _repository.AddNewUserAsync(user);

            if (user is null) throw new InvalidOperationException();

            return user;
        }

        public Task UpdateUser(User message) =>_repository.UpdateUser(message);
    }
}