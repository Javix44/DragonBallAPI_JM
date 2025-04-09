using DragonBallAPI_JM.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragonBallAPI_JM.Domain.Repositories
{
    public interface ICharacterRepository
    {
        Task<List<Character>> FetchCharactersFromApi();
        Task<List<Character>> GetAllCharactersAsync();
        Task<Character> GetCharacterByIdAsync(int id);
        Task<Character> GetCharacterByNameAsync(string name);
        Task<List<Character>> GetCharacterByAffilationAsync(string affilation);

        Task AddCharacterAsync(Character character);
        Task<bool> CheckIfEmptyAsync();
    }
}
