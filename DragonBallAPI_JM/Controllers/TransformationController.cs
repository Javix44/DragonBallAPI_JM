using DragonBallAPI_JM.Domain.Entities;
using DragonBallAPI_JM.Domain.Repositories;
using DragonBallAPI_JM.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DragonBallAPI_JM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransformationController : ControllerBase
    {
        private readonly ITransformationRepository _transformationRepository;

        public TransformationController(ITransformationRepository transformationRepository)
        {
            _transformationRepository = transformationRepository;
        }

        // GET: api/Transformation
        [HttpGet]
        public async Task<IActionResult> GetAllTransformations()
        {
            var transformations = await _transformationRepository.GetAllTransformationsAsync();

            if (transformations == null || !transformations.Any())
            {
                return NotFound("No transformations found.");
            }

            return Ok(transformations);
        }

    }
}
