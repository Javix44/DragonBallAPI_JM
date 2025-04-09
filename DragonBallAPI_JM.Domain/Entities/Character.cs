using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DragonBallAPI_JM.Domain.Entities
{
    public class Character
    {
        public int Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("ki")]
        public string Ki { get; set; }
        [JsonPropertyName("race")]
        public string Race { get; set; }
        [JsonPropertyName("gender")]
        public string Gender { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }
        [JsonPropertyName("affiliation")]
        public string Affiliation { get; set; }

        // Propiedad de navegación para las transformaciones
        public ICollection<Transformation> Transformations { get; set; } = new List<Transformation>();

    }
}
