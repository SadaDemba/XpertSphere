using Microsoft.AspNetCore.Mvc;
using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Interfaces;

namespace XpertSphere.MonolithApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Get all users with filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<object>> GetUsers([FromQuery] UserFilterDto filter)
    {
        var (users, totalCount) = await _userService.GetAllAsync(filter);
        
        return Ok(new
        {
            Data = users,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize)
        });
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetUser(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound($"User with ID {id} not found");
        }

        return Ok(user);
    }

    /// <summary>
    /// Get user by email
    /// </summary>
    [HttpGet("by-email/{email}")]
    public async Task<ActionResult<UserDto>> GetUserByEmail(string email)
    {
        var user = await _userService.GetByEmailAsync(email);
        if (user == null)
        {
            return NotFound($"User with email {email} not found");
        }

        return Ok(user);
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto createUserDto)
    {
        // Check if email already exists
        if (await _userService.EmailExistsAsync(createUserDto.Email))
        {
            return BadRequest($"User with email {createUserDto.Email} already exists");
        }

        var user = await _userService.CreateAsync(createUserDto);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    /// <summary>
    /// Update an existing user
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDto>> UpdateUser(Guid id, UpdateUserDto updateUserDto)
    {
        // Check if email already exists for another user
        if (updateUserDto.Email != null && await _userService.EmailExistsAsync(updateUserDto.Email, id))
        {
            return BadRequest($"User with email {updateUserDto.Email} already exists");
        }

        var user = await _userService.UpdateAsync(id, updateUserDto);
        if (user == null)
        {
            return NotFound($"User with ID {id} not found");
        }

        return Ok(user);
    }

    /// <summary>
    /// Delete a user
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var success = await _userService.DeleteAsync(id);
        if (!success)
        {
            return NotFound($"User with ID {id} not found");
        }

        return NoContent();
    }

    /// <summary>
    /// Update user's last login timestamp
    /// </summary>
    [HttpPatch("{id:guid}/last-login")]
    public async Task<ActionResult<UserDto>> UpdateLastLogin(Guid id)
    {
        var user = await _userService.UpdateLastLoginAsync(id);
        if (user == null)
        {
            return NotFound($"User with ID {id} not found");
        }

        return Ok(user);
    }

    /// <summary>
    /// Get all users from a specific organization
    /// </summary>
    [HttpGet("organization/{organizationId:guid}")]
    public async Task<ActionResult<List<UserDto>>> GetUsersByOrganization(Guid organizationId)
    {
        var users = await _userService.GetByOrganizationAsync(organizationId);
        return Ok(users);
    }

    /// <summary>
    /// Get all candidates (external users)
    /// </summary>
    [HttpGet("candidates")]
    public async Task<ActionResult<List<UserDto>>> GetCandidates([FromQuery] UserFilterDto filter)
    {
        var candidates = await _userService.GetCandidatesAsync(filter);
        return Ok(candidates);
    }

    /// <summary>
    /// Get all internal users
    /// </summary>
    [HttpGet("internal")]
    public async Task<ActionResult<List<UserDto>>> GetInternalUsers([FromQuery] UserFilterDto filter)
    {
        var internalUsers = await _userService.GetInternalUsersAsync(filter);
        return Ok(internalUsers);
    }

    /// <summary>
    /// Check if a user exists
    /// </summary>
    [HttpHead("{id:guid}")]
    public async Task<IActionResult> UserExists(Guid id)
    {
        var exists = await _userService.ExistsAsync(id);
        return exists ? Ok() : NotFound();
    }

    /// <summary>
    /// Check if an email is available
    /// </summary>
    [HttpGet("email-available")]
    public async Task<ActionResult<object>> EmailAvailable([FromQuery] string email, [FromQuery] Guid? excludeUserId = null)
    {
        var exists = await _userService.EmailExistsAsync(email, excludeUserId);
        return Ok(new { Available = !exists });
    }
}
