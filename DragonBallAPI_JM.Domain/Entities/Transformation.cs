using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DragonBallAPI_JM.Domain.Entities
{
    public class Transformation
    {
        [JsonIgnore]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Ki { get; set; }

        // Llave foránea hacia Character
        public int CharacterId { get; set; }
    }

}
