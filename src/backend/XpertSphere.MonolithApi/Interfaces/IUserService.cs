using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Utils.Pagination;

namespace XpertSphere.MonolithApi.Interfaces;

public interface IUserService
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="createUserDto"></param>
    /// <returns></returns>
    Task<User> Post(CreateUserDto createUserDto);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<User?> Get(Guid id);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userFilters"></param>
    /// <returns></returns>
    Task<ResponseResource<User>> GetAll(UserFilterDto userFilters);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    Task<User?> Put(Guid id, User user);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<bool> Delete(Guid id);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<bool> Exists(Guid id);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="email"></param>
    /// <param name="excludeUserId"></param>
    /// <returns></returns>
    Task<bool> EmailExists(string email, Guid? excludeUserId = null);

    /// <summary>
    /// s
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<User?> UpdateLastLogin(Guid id);
}
