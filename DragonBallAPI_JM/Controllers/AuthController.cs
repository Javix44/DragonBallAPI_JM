using DragonBallAPI_JM.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DragonBallAPI_JM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        // Inyección de dependencias para obtener la configuración de la aplicación
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest login)
        {
            if (login.Username == "admin" && login.Password == "1234")
            {
                // Si las credenciales son válidas, creamos los 'claims' para el token JWT
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, login.Username)
                };

                // Obtención de la clave secreta de la configuración de JWT para firmar el token
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]));

                // Definición de las credenciales de firma usando HMAC SHA256
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                // Creación del token JWT con los 'claims', la expiración y las credenciales de firma
                var token = new JwtSecurityToken(
                    claims: claims,
                    expires: DateTime.Now.AddHours(2), // El token expirará en 2 horas
                    signingCredentials: creds);

                // Devolvemos el token JWT como respuesta al cliente
                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token) // Se convierte el objeto JWT en un string
                });
            }

            // Si las credenciales no son válidas, devolvemos un código de estado 401 (No autorizado)
            return Unauthorized("Credenciales inválidas");
        }
    }
}
