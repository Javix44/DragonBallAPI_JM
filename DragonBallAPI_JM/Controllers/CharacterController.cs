using DragonBallAPI_JM.Domain.Entities;
using DragonBallAPI_JM.Domain.Repositories;
using DragonBallAPI_JM.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DragonBallAPI_JM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CharacterController : ControllerBase
    {
        private readonly ICharacterRepository _characterRepository;
        private readonly ApplicationDbContext _context;

        public CharacterController(ICharacterRepository characterRepository, ApplicationDbContext context)
        {
            _characterRepository = characterRepository;
            _context = context;
        }

        // GET: api/Character
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllCharacters()
        {
            var characters = await _context.Characters
                .Include(c => c.Transformations)
                .ToListAsync();

            return Ok(characters);

        }

        // GET: api/Character/5
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<Character>> GetCharacter(int id)
        {
            var character = await _context.Characters
            .Include(c => c.Transformations)
            .FirstOrDefaultAsync(c => c.Id == id);


            if (character == null)
            {
                return NotFound();
            }

            return Ok(character);
        }

        // GET: api/Character/byname/Goku
        [Authorize]
        [HttpGet("byname/{name}")]
        public async Task<ActionResult<Character>> GetCharacterByNameAsync(string name)
        {
            var character = await _context.Characters
            .Include(c => c.Transformations)
            .SingleOrDefaultAsync(c => c.Name == name);

            if (character == null)
            {
                return NotFound();
            }

            return Ok(character);
        }

        // GET: api/Character/byaffiliation/Z fighter
        [Authorize]
        [HttpGet("byaffiliation/{affiliation}")]
        public async Task<ActionResult<Character>> GetCharacterByAffilationAsync(string affiliation)
        {
            var characters = await _context.Characters
            .Include(c => c.Transformations)
            .Where(c => c.Affiliation == affiliation)
            .ToListAsync();

            if (!characters.Any())
            {
                return NotFound();
            }

            return Ok(characters);
        }

        // GET: api/Character/sync
        [Authorize]
        [HttpPost("sync")]
        public async Task<IActionResult> SyncCharacters()
        {
            if (await _context.Characters.CountAsync() > 0 || await _context.Transformations.CountAsync() > 0)
            {
                return BadRequest("Please clean up the data before syncing.");
            }

            var characters = await _characterRepository.FetchCharactersFromApi();

            foreach (var character in characters)
            {
                var race = character.Race?.ToLower();

                if ((race?.Contains("saiyan") ?? false))
                {
                    var exists = await _context.Characters.AnyAsync(c => c.Name == character.Name);
                    if (!exists)
                    {
                        character.Id = 0;
                        _context.Characters.Add(character);
                    }
                }
            }

            await _context.SaveChangesAsync();

            var savedCharacters = await _context.Characters
                .Where(c => c.Affiliation == "Z Fighter")
                .ToListAsync();

            // Llamamos a la API de transformaciones
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("https://dragonball-api.com/api/transformations");

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Error al obtener transformaciones desde la API.");
            }

            var json = await response.Content.ReadAsStringAsync();
            var allTransformations = System.Text.Json.JsonSerializer.Deserialize<List<Transformation>>(json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (allTransformations == null || !allTransformations.Any())
            {
                return NotFound("No se encontraron transformaciones en la API.");
            }

            int saved = 0;

            // Iniciar una transacción para asegurar la consistencia
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var character in savedCharacters)
                {
                    var relatedTransformations = allTransformations
                        .Where(t => t.Name.Contains(character.Name, StringComparison.OrdinalIgnoreCase))
                        .ToList();


                    foreach (var transformation in relatedTransformations)
                    {
                        // Asigna el CharacterId
                        transformation.CharacterId = character.Id;

                        // Verificamos si la transformación ya existe en la base de datos
                        var existingTransformation = await _context.Transformations
                            .AsNoTracking()
                            .FirstOrDefaultAsync(t => t.Id == transformation.Id);

                        if (existingTransformation == null)
                        {
                            _context.Transformations.Add(transformation);
                            saved++;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // Confirmar la transacción
                await transaction.CommitAsync();

            }
            catch (Exception ex)
            {
                // Revertir la transacción en caso de error
                await transaction.RollbackAsync();
                return StatusCode(500, "An error occurred during sync: " + ex.Message);
            }

            return Ok(new { Message = $"Characters and {saved} transformations synced successfully!" });
        }


    }



}
