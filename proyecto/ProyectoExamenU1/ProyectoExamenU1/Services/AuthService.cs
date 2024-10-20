﻿using BlogUNAH.API.Dtos.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProyectoExamenU1.Dtos.Common;
using ProyectoExamenU1.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ProyectoExamenU1.Services
{
    public class AuthService : IAuthService
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AuthService(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration
            )
        {
            this._signInManager = signInManager;
            this._userManager = userManager;
            this._roleManager = roleManager;
            this._configuration = configuration;
        }


        public async Task<ResponseDto<LoginResponseDto>> LoginAsync(LoginDto dto)
        {
            var result = await _signInManager
                .PasswordSignInAsync(dto.Email,
                                     dto.Password,
                                     isPersistent: false,
                                     lockoutOnFailure: false);

            if (result.Succeeded)
            {
                // Generación del token
                var userEntity = await _userManager.FindByEmailAsync(dto.Email);

                // ClaimList creation
                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Email, userEntity.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim("UserId", userEntity.Id),
                };

                var userRoles = await _userManager.GetRolesAsync(userEntity);
                foreach (var role in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, role));
                }

                var jwtToken = GetToken(authClaims);

                return new ResponseDto<LoginResponseDto>
                {
                    StatusCode = 200,
                    Status = true,
                    Message = "Inicio de sesion satisfactorio",
                    Data = new LoginResponseDto
                    {
                        Email = userEntity.Email,
                        Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                        TokenExpiration = jwtToken.ValidTo,
                    }
                };

            }

            return new ResponseDto<LoginResponseDto>
            {
                Status = false,
                StatusCode = 401,
                Message = "Fallo el inicio de sesión"
            };
        
    }

        public async Task<ResponseDto<IdentityUser>> DeleteAsync(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());

            if (user == null)
            {
                return new ResponseDto<IdentityUser>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = "Usuario no encontrado"
                };
            }

            // Obtener roles asociados al usuario
            var userRoles = await _userManager.GetRolesAsync(user);

            // Eliminar el usuario de cada rol
            foreach (var role in userRoles)
            {
                await _userManager.RemoveFromRoleAsync(user, role);
            }

            // Eliminar el usuario
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return new ResponseDto<IdentityUser>
                {
                    StatusCode = 200,
                    Status = true,
                    Message = "Usuario eliminado exitosamente"
                };
            }

            return new ResponseDto<IdentityUser>
            {
                StatusCode = 500,
                Status = false,
                Message = "No se pudo eliminar el usuario"
            };
        }


        private JwtSecurityToken GetToken(List<Claim> authClaims)
        {
            var authSigninKey = new SymmetricSecurityKey(Encoding.UTF8
                .GetBytes(_configuration["JWT:Secret"]));

            return new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddHours(1),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigninKey,
                    SecurityAlgorithms.HmacSha256)
            );
        }
    }
}
