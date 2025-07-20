using System;
using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AccountController(AppDbContext context) : BaseApiController
{
	[HttpPost("register")]
	public async Task<ActionResult<AppUser>> Register(RegisterDTO registerDTO)
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
		return user;
	}

	private async Task<bool> EmailExists(string Email)
	{
		return await context.Users.AnyAsync(x => x.Email.ToLower() == Email.ToLower());
	}
}
