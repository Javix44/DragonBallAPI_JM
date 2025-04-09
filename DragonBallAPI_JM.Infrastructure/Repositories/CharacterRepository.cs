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
            // Configurar la URL de la API
            _apiUrl = configuration["DragonBallApiUrl"];
            if (string.IsNullOrEmpty(_apiUrl))
            {
                _apiUrl = "https://dragonball-api.com/api/characters"; // URL predeterminada
            }

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
                // Realizamos la llamada HTTP a la API externa
                var responseString = await _httpClient.GetStringAsync(_apiUrl);
                Console.WriteLine(responseString); // Muestra la respuesta JSON para depuración
                // Verificar si la respuesta es válida
                if (string.IsNullOrEmpty(responseString))
                    throw new Exception("No data received from the API.");

                // Configurar JsonSerializerOptions para ignorar propiedades desconocidas
                var options = new JsonSerializerOptions
                {
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve,  // Esta opción maneja ciclos de referencia
                    MaxDepth = 32,
                    PropertyNameCaseInsensitive = true,  // Ignorar diferencias de mayúsculas/minúsculas
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull // Ignorar valores nulos
                };

                // Deserializar la respuesta de la API en un objeto ApiResponse
                var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseString, options);

                // Validar que la deserialización fue exitosa
                if (apiResponse == null || apiResponse.Items == null)
                    throw new Exception("Failed to deserialize the character data.");

                // Filtrar los personajes por raza Saiyan
                var filteredCharacters = apiResponse.Items.Where(c => c.Race == "Saiyan").ToList();

                // 1. Guardar personajes sin ID
                foreach (var apiCharacter in filteredCharacters)
                {
                    var character = new Character
                    {
                        Name = apiCharacter.Name,
                        Ki = apiCharacter.Ki,
                        Race = apiCharacter.Race,
                        Gender = apiCharacter.Gender,
                        Description = apiCharacter.Description,
                        Affiliation = apiCharacter.Affiliation
                    };

                    await _context.Characters.AddAsync(character);
                }

                // Guardar los cambios en la base de datos
                await _context.SaveChangesAsync();
                await _transformationRepository.SyncAndAssignTransformationsAsync();
                // Devolver la lista de personajes filtrados
                return filteredCharacters;
            }
            catch (Exception ex)
            {
                // Manejar errores de conexión o deserialización
                throw new Exception("An error occurred while fetching characters from the API.", ex);
            }
        }

    }
}
