using System.Net;
using FindMyPet.Business;
using FindMyPet.Models;
using FindMyPet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FindMyPet.Controllers;

[ApiController]
public class AccountController : ControllerBase
{
    private readonly AccoutBusiness _accoutBusiness;


    public AccountController(AccoutBusiness accoutBusiness)
    {
        _accoutBusiness = accoutBusiness;
    }

    [HttpPost("v1/authenticate")]
    public async Task<IActionResult> Login([FromServices] TokenService tokenService, [FromBody] UserData credentials)
    {
        try
        {
            UserAuthenticate authenticate = await _accoutBusiness.Authentication(credentials.user, credentials.pass);

            if (authenticate != null)
            {
                TokenBody token = await _accoutBusiness.GenerateToken(tokenService, authenticate);

                return Ok(token);
            }
            else
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Acesso não autorizado!");
            }
        }
        catch (Exception)
        {
            return BadRequest("Ops.. aconteceu algum problema, tente novamente mais tarde!");
        }
    }

    [HttpPost("v1/create")]
    public async Task<IActionResult> Create(CreatedUser user)
    {
        try
        {
            object result = await _accoutBusiness.Created(user);

            object messageError = result.GetType().GetProperty("message");

            if(messageError != null)
            {
                return BadRequest(result);
            }
            else
            {
                return Ok(result);
            }
        }
        catch (Exception ex)
        {
            return BadRequest(ex);
        }

    }
}