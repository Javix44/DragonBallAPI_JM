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
using System.Text.Json.Serialization;
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
            _apiUrl = configuration["ConnectionStrings:DragonBallTransformationsApiUrl"];
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
        public async Task SyncAndAssignTransformationsAsync()
        {
            // Realizamos la llamada HTTP a la API externa
            var responseString = await _httpClient.GetStringAsync(_apiUrl);
            // Console.WriteLine(responseString); // Muestra la respuesta JSON para depuración
            // Verificar si la respuesta es válida
            if (string.IsNullOrEmpty(responseString))
                throw new Exception("No data received from the API.");

            // Configurar JsonSerializerOptions para ignorar propiedades desconocidas
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,  // Ignorar diferencias de mayúsculas/minúsculas
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull // Ignorar valores nulos
            };
            
            // Deserializar la respuesta de la API en un objeto ApiResponse
            var apiTransformations = JsonSerializer.Deserialize<List<Transformation>>(responseString, options);
            
            // Validar que la deserialización fue exitosa
            if (apiTransformations == null)
            throw new Exception("Failed to deserialize the character data.");

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
                        }
                        else
                        {
                            // Si no existe, crea una nueva instancia sin ID (BD Posee Identity)
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
