using AutoMapper;
using UserService.Model;
using UserService.AsyncDataServices;
using UserService.StorageRepositories;
using UserService.Services.JwtProvider;
using UserService.Services.PasswordHasher;

namespace UserService.Services.UserService
{
    public class UserManagmentService : IUserManagmentService
    {
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUserMessageBusSubscriber _messageBus;
        private readonly IUserStorageRepository _repository;
        private readonly IJwtProvider _jwtProvider;
        private readonly IMapper _mapper;

        public UserManagmentService(IPasswordHasher passwordHasher, IUserMessageBusSubscriber messageBus,IUserStorageRepository repository, IJwtProvider jwtProvider, IMapper mapper)
        {
            this._passwordHasher = passwordHasher;
            this._messageBus = messageBus;
            this._repository = repository;
            this._jwtProvider = jwtProvider;
            this._mapper = mapper;
        }

        public async Task<User> Register(RegisterUserDto registerUserDto)
        {
            var user = _mapper.Map<User>(registerUserDto);

            user.PasswordHash = _passwordHasher.Generate(registerUserDto.Password);

            user.Id = await _messageBus.SendNewUserAsync(user);

            if (user is null) throw new InvalidOperationException();

            return user;
        }

        public async Task<string> Login(SignInUserDto signInUserDto)
        {
            var user = await _repository.GetUserByEmail(signInUserDto.Email);

            if (user is null) throw new InvalidOperationException();

            if (!_passwordHasher.Verify(signInUserDto.Password, user.PasswordHash))
                throw new ArgumentException();

            var token = _jwtProvider.GenerateToken(user);

            return token;
        }
    }
}