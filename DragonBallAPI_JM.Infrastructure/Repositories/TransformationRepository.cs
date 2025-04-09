using DragonBallAPI_JM.Domain.Entities;
using DragonBallAPI_JM.Domain.Repositories;
using DragonBallAPI_JM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace DragonBallAPI_JM.Infrastructure.Repositories
{
    public class TransformationRepository : ITransformationRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;

        public TransformationRepository(ApplicationDbContext context, HttpClient httpClient, IConfiguration configuration)
        {
            _context = context;
            _httpClient = httpClient;
            _apiUrl = configuration["DragonBallTransformationsApiUrl"];
            if (string.IsNullOrEmpty(_apiUrl))
            {
                _apiUrl = "https://dragonball-api.com/api/transformations"; // URL por defecto
            }
        }

        // Obtener todas las transformaciones desde la base de datos
        public async Task<List<Transformation>> GetAllTransformationsAsync()
        {
            return await _context.Transformations.ToListAsync();
        }

        // Verifica si la tabla de transformaciones está vacía
        public async Task<bool> CheckIfEmptyAsync()
        {
            return !await _context.Transformations.AnyAsync();
        }

        // Método para sincronizar transformaciones desde la API
        public async Task<List<Transformation>> FetchTransformationsFromApi()
        {
            try
            {
                var responseString = await _httpClient.GetStringAsync(_apiUrl);
                Console.WriteLine(responseString); // Para depuración

                if (string.IsNullOrEmpty(responseString))
                    throw new Exception("No se recibió información de la API.");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var apiResponse = JsonSerializer.Deserialize<List<Transformation>>(responseString, options);

                if (apiResponse == null)
                    throw new Exception("Error al deserializar transformaciones.");

                // Filtramos duplicados antes de guardar
                var savedCount = 0;
                foreach (var transformation in apiResponse)
                {
                    var exists = await _context.Transformations.AnyAsync(t => t.Name == transformation.Name);
                    if (!exists)
                    {
                        transformation.Id = 0; // Dejar que la BD lo genere
                        await _context.Transformations.AddAsync(transformation);
                        savedCount++;
                    }
                }

                await _context.SaveChangesAsync();

                return apiResponse;
            }
            catch (Exception ex)
            {
                throw new Exception("Ocurrió un error al obtener las transformaciones de la API.", ex);
            }
        }
        public async Task SyncAndAssignTransformationsAsync()
        {
            var responseString = await _httpClient.GetStringAsync(_apiUrl);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var apiTransformations = JsonSerializer.Deserialize<List<Transformation>>(responseString, options);

            if (apiTransformations == null)
                throw new Exception("Error al deserializar transformaciones.");

            foreach (var apiTransformation in apiTransformations)
            {
                // Buscar si el personaje ya existe en la base de datos
                var character = await _context.Characters
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == apiTransformation.Name.ToLower());

                if (character != null)
                {
                    // Verificar si ya existe una transformación con ese nombre y CharacterId
                    var exists = await _context.Transformations.AnyAsync(t =>
                        t.Name == apiTransformation.Name && t.CharacterId == character.Id);

                    if (!exists)
                    {
                        var existingTransformation = await _context.Transformations
                            .FirstOrDefaultAsync(t => t.Name == apiTransformation.Name && t.CharacterId == character.Id);

                        if (existingTransformation != null)
                        {
                            // Si la transformación existe, actualízala en lugar de agregarla
                            existingTransformation.Ki = apiTransformation.Ki;
                            existingTransformation.Name = apiTransformation.Name;
                        }
                        else
                        {
                            // Si no existe, crea una nueva instancia
                            var newTransformation = new Transformation
                            {
                                Name = apiTransformation.Name,
                                Ki = apiTransformation.Ki,
                                CharacterId = character.Id
                            };

                            await _context.Transformations.AddAsync(newTransformation);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

    }
}
