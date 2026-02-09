using System.Text.Json.Serialization;



namespace Business.ExchangeService.DTOs
{
    public class ExRateResponseDTO
    {

        [JsonPropertyName("result")]
        public string? Result { get; set; }

        [JsonPropertyName("provider")]
        public string? Provider { get; set; }

        [JsonPropertyName("documentation")]
        public string? Documentation { get; set; }

        [JsonPropertyName("terms_of_use")]
        public string? TermsOfUse { get; set; }



        [JsonPropertyName("time_last_update_unix")]
        public long? TimeLastUpdateUnix { get; set; }

        [JsonPropertyName("time_next_update_unix")]
        public long? TimeNextUpdateUnix { get; set; }

        [JsonPropertyName("time_eol_unix")]
        public long? TimeEolUnix { get; set; }

        [JsonPropertyName("base_code")]
        public string? BaseCode { get; set; }

        [JsonPropertyName("rates")]
        public Dictionary<string, decimal>? Rates { get; set; }



        // In case of error in response:
        [JsonPropertyName("error-type")]
        public string? ErrorType { get; set; }


    }
}
