using AutoMapper;
using UserService.Model;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using UserService.AsyncDataServices;
using UserService.Services.UserManagmentService;
using Microsoft.AspNetCore.Cors;

namespace UserService.Controllers
{
    [ApiController]
    [Route("api/u")]
    public class UserController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IUserMessageBusSubscriber _busSubscriber;
        private readonly IUserManagmentService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserMessageBusSubscriber busSubscriber, IUserManagmentService userService, IMapper mapper, ILogger<UserController> logger)
        {
            this._busSubscriber = busSubscriber;
            this._userService = userService;
            this._mapper = mapper;
            this._logger = logger;
        }

        [HttpPost("sign-up")]
        [EnableCors("AllowLocalhost")]
        public async Task<IActionResult> RegisterUser([FromBody] RegisterUserDto registerUserDto)
        {
            try
            {
                var user = await _userService.Register(registerUserDto);

                return Created($"api/u/{user.Id}", new { Message = "User was succesfully registered", Data = user });
            }
            catch (InvalidOperationException e)
            {
                _logger.LogError(e.Message);
                return NoContent();
            }
            catch (ArgumentOutOfRangeException e)
            {
                _logger.LogError(e.Message);

                return StatusCode(403, "User with this email already exists.");
            }
            catch (ArgumentNullException e)
            {
                _logger.LogError(e.Message);

                return BadRequest();
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);

                Console.WriteLine(e.ToString());

                return StatusCode(500, e.ToString());
            }
        }

        //[Authorize]
        //[HttpGet("{id:guid}")]
        //public async Task<IActionResult> GetUserById()
        //{
        //    try
        //    {
        //        var handler = new JwtSecurityTokenHandler();
        //        var jwtToken = handler.ReadJwtToken(HttpContext.Request.Cookies["homka-lox"]);
        //        var userId = Guid.Parse(jwtToken?.Claims?.First(c => c.Type == "id")?.Value ?? throw new ArgumentNullException());

        //        var user = await _userStorageRepository.GetUserByIdAsync(userId);
        //        if (user is null) return NotFound();

        //        var userDto = _mapper.Map<UserDto>(user);

        //        return Ok(new { Message = "1 User was found", Data = userDto });
        //    }
        //    catch (ArgumentNullException)
        //    {
        //        return NotFound();
        //    }
        //    catch (Exception e)
        //    {
        //        return StatusCode(500, e.Message);
        //    }
        //}

        [Authorize]
        [EnableCors("AllowLocalhost")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteUserById([FromRoute] Guid id)
        {
            try
            {
                var user = await _busSubscriber.DeleteUserAsync(id);

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
        [EnableCors("AllowLocalhost")]
        public async Task<IActionResult> SignIn([FromBody] SignInUserDto signInUser)
        {
            try
            {
                var token = await _userService.Login(signInUser);

                HttpContext.Response.Cookies.Append("homka-lox", token.Item1, new() 
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                });

                return Ok(new {Username = token.Item2.Name});
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