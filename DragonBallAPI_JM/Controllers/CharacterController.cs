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
            .FirstOrDefaultAsync(c => c.Name == name);

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

            if (characters == null)
            {
                return NotFound();
            }

            return Ok(characters);
        }
        [Authorize]
        [HttpPost("sync")]
        public async Task<IActionResult> SyncCharacters()
        {
            if (_context.Characters.Any() || _context.Transformations.Any())
            {
                return BadRequest("Please clean up the data before syncing.");
            }

            var characters = await _characterRepository.FetchCharactersFromApi();

            foreach (var character in characters)
            {
                if (character.Race == "Saiyan" && !await _context.Characters.AnyAsync(c => c.Name == character.Name))
                {
                    _context.Characters.Add(character);
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
                        else
                        {
                            Console.WriteLine($"Transformation with ID {transformation.Id} already exists. Skipping.");
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
                Console.WriteLine($"Error durante la sincronización: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                throw;
            }

            return Ok(new { Message = $"Characters and {saved} transformations synced successfully!" });
        }


    }



}
