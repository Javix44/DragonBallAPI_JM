using DragonBallAPI_JM.Domain.Entities;
using DragonBallAPI_JM.Domain.Repositories;
using DragonBallAPI_JM.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DragonBallAPI_JM.Infrastructure.Repositories
{
    public class CharacterRepository : ICharacterRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;
        private readonly ITransformationRepository _transformationRepository;
        public CharacterRepository(ApplicationDbContext context, HttpClient httpClient, IConfiguration configuration, ITransformationRepository transformationRepository)
        {
            _context = context;
            _httpClient = httpClient;
            _transformationRepository = transformationRepository;
            _apiUrl = configuration["ConnectionStrings:DragonBallCharacterApiUrl"];
        }

        // Método para obtener todos los personajes
        public async Task<List<Character>> GetAllCharactersAsync()
        {
            return await _context.Characters.ToListAsync();
        }

        // Método para obtener un personaje por su ID
        public async Task<Character> GetCharacterByIdAsync(int id)
        {
            return await _context.Characters
                .FirstOrDefaultAsync(c => c.Id == id);
        }
        // Método para obtener un personaje por su Name
        public async Task<Character> GetCharacterByNameAsync(string name)
        {

            var character = await _context.Characters
                .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());

            return character;
        }
        // Método para obtener un personaje por su Affilation
        public async Task<List<Character>> GetCharacterByAffilationAsync(string affilation)
        {
            return await _context.Characters
                   .Where(c => c.Affiliation.ToLower() == affilation.ToLower())
                   .ToListAsync();
        }

        // Método para agregar un personaje a la base de datos
        public async Task AddCharacterAsync(Character character)
        {
            await _context.Characters.AddAsync(character);
            await _context.SaveChangesAsync();
        }
        // Método adicional para verificar si la tabla de personajes está vacía
        public async Task<bool> CheckIfEmptyAsync()
        {
            return !await _context.Characters.AnyAsync();
        }

        // Método para obtener los personajes desde la API externa
        public async Task<List<Character>> FetchCharactersFromApi()
        {
            try
            {
                var allCharacters = new List<Character>();
                var currentUrl = _apiUrl;

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                while (!string.IsNullOrEmpty(currentUrl))
                {
                    var responseString = await _httpClient.GetStringAsync(currentUrl);

                    if (string.IsNullOrEmpty(responseString))
                        throw new Exception("No data received from the API.");

                    var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseString, options);

                    if (apiResponse == null || apiResponse.Items == null)
                        throw new Exception("Failed to deserialize the character data.");

                    var filteredCharacters = apiResponse.Items
                        .Where(c => c.Race == "Saiyan"
                        && c.Affiliation == "Z Fighter")
                        .Select(apiCharacter => new Character
                        {
                            Name = apiCharacter.Name,
                            Ki = apiCharacter.Ki,
                            Race = apiCharacter.Race,
                            Gender = apiCharacter.Gender,
                            Description = apiCharacter.Description,
                            Affiliation = apiCharacter.Affiliation
                        })
                        .ToList();

                    allCharacters.AddRange(filteredCharacters);

                    // Actualizar la URL para la siguiente página (si existe)
                    var nextUrl = JsonDocument.Parse(responseString)
                        .RootElement
                        .GetProperty("links")
                        .GetProperty("next")
                        .GetString();

                    currentUrl = string.IsNullOrWhiteSpace(nextUrl) ? null : nextUrl;
                }

                // Guardar todos los personajes en la DB
                foreach (var character in allCharacters)
                {
                    await _context.Characters.AddAsync(character);
                }

                await _context.SaveChangesAsync();
                await _transformationRepository.SyncAndAssignTransformationsAsync();

                return allCharacters;
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while fetching characters from the API.", ex);
            }
        }

    }
}
