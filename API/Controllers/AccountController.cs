using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;
        public AccountController(DataContext context, ITokenService tokenService)
        {
            _tokenService = tokenService;
            _context = context;

        }
        [HttpPost("register")]
        public async Task<ActionResult<UserDTO>> Register(RegisterDTO registerDto)
        {
            if (await UserExists(registerDto.userName))
            {
                return BadRequest("User name is already taken");
            }
            else
            {
                using var hmac = new HMACSHA512();
                var user = new AppUser
                {
                    userName = registerDto.userName.ToLower(),
                    passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.password)),
                    passwordSalt = hmac.Key
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return new UserDTO
                {
                    userName=user.userName,
                    token=_tokenService.CreateToken(user)
                };
            }
        }
        [HttpPost("login")]
        public async Task<ActionResult<UserDTO>> Login(LoginDTO loginDto)
        {
            var user = await _context.Users.SingleOrDefaultAsync(x => x.userName == loginDto.userName);
            if (user == null)
            {
                return BadRequest("User name does not exist");
            }
            else
            {
                using var hmac = new HMACSHA512(user.passwordSalt);
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.password));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != user.passwordHash[i])
                    {
                        return BadRequest("Invalid Password");
                    }
                }
                 return new UserDTO
                {
                    userName=user.userName,
                    token=_tokenService.CreateToken(user)
                };
            }
        }
        private async Task<bool> UserExists(string username)
        {
            return await _context.Users.AnyAsync(x => x.userName == username.ToLower());
        }

    }
}