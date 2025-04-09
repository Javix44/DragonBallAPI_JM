using DragonBallAPI_JM.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragonBallAPI_JM.Domain.Repositories
{
    public interface ITransformationRepository
    {
        Task<List<Transformation>> GetAllTransformationsAsync();
        Task<bool> CheckIfEmptyAsync();  
        Task SyncAndAssignTransformationsAsync();
    }
}
