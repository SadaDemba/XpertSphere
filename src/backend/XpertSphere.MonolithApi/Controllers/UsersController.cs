using Microsoft.AspNetCore.Mvc;
using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Utils.Pagination;

namespace XpertSphere.MonolithApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserService userService) : ControllerBase
{
    private readonly IUserService _userService = userService;

    /// <summary>
    /// Get all users with filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ResponseResource<User>>> GetUsers([FromQuery] UserFilterDto filter)
    {
        return Ok(await _userService.GetAll(filter));
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetUser(Guid id)
    {
        return Ok(await _userService.Get(id));
    }

   
    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto createUserDto)
    {

        var user = await _userService.Post(createUserDto);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    /// <summary>
    /// Update an existing user
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDto>> UpdateUser(Guid id, User updatedUser)
    {


        var user = await _userService.Put(id, updatedUser);

        return Ok(user);
    }

    /// <summary>
    /// Delete a user
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        await _userService.Delete(id);
        return Ok();
    }

    /// <summary>
    /// Update user's last login timestamp
    /// </summary>
    [HttpPatch("{id:guid}/last-login")]
    public async Task<ActionResult<UserDto>> UpdateLastLogin(Guid id)
    {
        var user = await _userService.UpdateLastLogin(id);

        return Ok(user);
    }

    /// <summary>
    /// Check if a user exists
    /// </summary>
    [HttpHead("{id:guid}")]
    public async Task<IActionResult> UserExists(Guid id)
    {
        var exists = await _userService.Exists(id);
        return exists ? Ok() : NotFound();
    }

    /// <summary>
    /// Check if an email is available
    /// </summary>
    [HttpGet("email-available")]
    public async Task<ActionResult<object>> EmailAvailable([FromQuery] string email, [FromQuery] Guid? excludeUserId = null)
    {
        var exists = await _userService.EmailExists(email, excludeUserId);
        return Ok(new { Available = !exists });
    }
}
