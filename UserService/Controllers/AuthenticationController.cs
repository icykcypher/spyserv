using AutoMapper;
using UserService.Model;
using Microsoft.AspNetCore.Mvc;
using UserService.Services.UserService;
using UserService.StorageRepositories;
using Microsoft.AspNetCore.Authorization;

namespace UserService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IUserStorageRepository _userStorageRepository;
        private readonly IUserManagmentService _userService;
        private readonly IMapper _mapper;

        public AuthenticationController(IUserStorageRepository userStorageRepository, IUserManagmentService userService, IMapper mapper)
        {
            this._userStorageRepository = userStorageRepository;
            this._userService = userService;
            this._mapper = mapper;
        }

        [HttpGet]
        [Authorize("AdminPolicy")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userStorageRepository.GetAllUsersAsync();

                var userDto = _mapper.Map<List<UserDto>>(users);

                return Ok(new { Message = "All users that are registered to our service", Data = userDto });
            }
            catch (ArgumentNullException)
            {
                return NotFound();
            }
            catch (Exception e)
            {
                return StatusCode(500, e);
            }
        }

        [HttpPost("sign-up")]
        public async Task<IActionResult> RegisterUser([FromBody] RegisterUserDto registerUserDto)
        {
            try
            {
                var user = await _userService.Register(registerUserDto);

                return Created($"api/authentication/{user.Id}", new { Message = "User was succesfully registered", Data = user });
            }
            catch (InvalidOperationException)
            {
                return NoContent();
            }
            catch (Exception e)
            {
                return StatusCode(500, e);
            }
        }

        [Authorize]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetUserById([FromRoute] Guid id)
        {
            try
            {
                var user = await _userStorageRepository.GetUserByIdAsync(id);
                if (user is null) return NotFound();

                var userDto = _mapper.Map<UserDto>(user);

                return Ok(new { Message = "1 User was found", Data = userDto });
            }
            catch (ArgumentNullException)
            {
                return NotFound();
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [Authorize]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteUserById([FromRoute] Guid id)
        {
            try
            {
                var user = await _userStorageRepository.DeleteUserByIdAsync(id);

                if (user is null) return NotFound();

                return Ok(new { Message = "1 User was removed", Data = user });
            }
            catch (ArgumentNullException)
            {
                return NotFound();
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpPost("sign-in")]
        public async Task<IActionResult> SignIn([FromBody] SignInUserDto signInUser)
        {
            try
            {
                var token = await _userService.Login(signInUser);

                HttpContext.Response.Cookies.Append("homka-lox", token);

                return Ok();
            }
            catch (ArgumentException)
            {
                return StatusCode(403, "Incorect password.");
            }
            catch (InvalidOperationException)
            {
                return NoContent();
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }
    }
}