using AutoMapper;
using UserService.Model;
using UserService.AsyncDataServices;
using UserService.Services.JwtProvider;
using UserService.Services.PasswordHasher;
using UserService.SyncDataServices.Grpc;

namespace UserService.Services.UserManagmentService
{
    public class UserManagmentService(IPasswordHasher passwordHasher, IUserMessageBusSubscriber messageBus, 
        IJwtProvider jwtProvider, IMapper mapper, IGrpcUserCommunicationService grpcService) : IUserManagmentService
    { 
        private readonly IPasswordHasher _passwordHasher = passwordHasher;
        private readonly IUserMessageBusSubscriber _messageBus = messageBus;
        private readonly IJwtProvider _jwtProvider = jwtProvider;
        private readonly IMapper _mapper = mapper;
        private readonly IGrpcUserCommunicationService _grpcService = grpcService;

        public async Task<User> Register(RegisterUserDto registerUserDto)
        {
            var user = _mapper.Map<User>(registerUserDto);

            user.Roles.Add(new RoleEntity { });
            
            user.PasswordHash = _passwordHasher.Generate(registerUserDto.Password);

            user.Id = await _messageBus.SendNewUserAsync(user);

            if (user is null) throw new InvalidOperationException();
            if (user.Id == Guid.Empty) throw new ArgumentOutOfRangeException(nameof(user.Id), "User's email already registered");
            return user;
        }

        public async Task<(string, User)> Login(SignInUserDto signInUserDto)
        {
            var user = await _grpcService.GetUserByEmailAsync(signInUserDto.Email) ?? throw new InvalidOperationException();

            if (!_passwordHasher.Verify(signInUserDto.Password, user.PasswordHash))
                throw new ArgumentException();

            var token = _jwtProvider.GenerateToken(user);

            return (token, user);
        }
    }
}