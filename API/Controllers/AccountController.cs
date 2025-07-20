using System;
using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AccountController(AppDbContext context, iTokenService tokenService) : BaseApiController
{
	[HttpPost("register")]
	public async Task<ActionResult<UserDTO>> Register(RegisterDTO registerDTO)
	{
		if (await EmailExists(registerDTO.Email)) return BadRequest("Email already taken");

		using var hmac = new HMACSHA512();

		var user = new AppUser
		{
			DisplayName = registerDTO.DisplayName,
			Email = registerDTO.Email,
			passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDTO.Password)),
			passwordSalt = hmac.Key
		};
		context.Users.Add(user);
		await context.SaveChangesAsync();
		return user.ToDto(tokenService);
	}

	[HttpPost("login")]

	public async Task<ActionResult<UserDTO>> Login(LoginDTO loginDTO)
	{
		var user = await context.Users.SingleOrDefaultAsync(x => x.Email == loginDTO.Email);

		if (user == null) return Unauthorized("Invalid email address");

		using var hmac = new HMACSHA512(user.passwordSalt);

		var ComputeHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDTO.Password));

		for (var i = 0; i < ComputeHash.Length; i++)
		{
			if (ComputeHash[i] != user.passwordHash[i]) return Unauthorized("Password is incorrect");
		}
		return user.ToDto(tokenService);
	}

	private async Task<bool> EmailExists(string Email)
	{
		return await context.Users.AnyAsync(x => x.Email.ToLower() == Email.ToLower());
	}
}
