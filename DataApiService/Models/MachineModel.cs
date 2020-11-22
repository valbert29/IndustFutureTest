using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DataApiService.Models
{
    public class MachineModel
    {
        [JsonPropertyName("machine_model_id")]
        public int machine_model_id { get; set; }

        [JsonPropertyName("machine_model_name")]
        public string machine_model_name { get; set; }
    }
}
