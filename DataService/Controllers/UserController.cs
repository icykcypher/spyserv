using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using DataService.Model.UsersModel;
using DataService.StorageRepositories;
using DataService.Services.UserServices;
using Microsoft.AspNetCore.Authorization;

namespace DataService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserStorageRepository _userStorageRepository;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;

        public UserController(IUserStorageRepository userStorageRepository, IUserService userService, IMapper mapper, ILogger logger)
        {
            this._userStorageRepository = userStorageRepository;
            this._userService = userService;
            this._mapper = mapper;
            this._logger = logger;
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

        [HttpPost("register")]
        public async Task<IActionResult> AddNewUser([FromBody] RegisterUserDto registerUserDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var user = await _userService.Register(registerUserDto);

                return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, new { Message = "User was successfully registered", Data = user });
            }
            catch (InvalidOperationException)
            {
                return BadRequest(new { Message = "User registration failed" });
            }
            catch (Exception e)
            {
                _logger.LogError($"Error occurred during user registration: {e.Message}");
                return StatusCode(500, new { Message = "An unexpected error occurred", Details = e.Message });
            }
        }


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
    }
}