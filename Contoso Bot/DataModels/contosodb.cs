using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Contoso_Bot.DataModels
{
    public class contosodb
    {
        [JsonProperty(PropertyName = "Id")]
        public string ID { get; set; }

        [JsonProperty(PropertyName = "createdAt")]
        public DateTime Date { get; set; }

        [JsonProperty(PropertyName = "user")]
        public string username { get; set; }

        [JsonProperty(PropertyName = "pass")]
        public string password { get; set; }

        [JsonProperty(PropertyName = "salt")]
        public string salt { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string name { get; set; }

        [JsonProperty(PropertyName = "phone")]
        public string phone { get; set; }

        [JsonProperty(PropertyName = "email")]
        public string email { get; set; }

        [JsonProperty(PropertyName = "address")]
        public string address { get; set; }

        [JsonProperty(PropertyName = "account")]
        public string account { get; set; }
    }
}