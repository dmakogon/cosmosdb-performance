using Newtonsoft.Json;

namespace writedemo
{
    public class SampleDocument
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string prop1 { get; set; }
        public int prop2 { get; set; }
        public string prop3 { get; set; }
        public int prop4 { get; set; }

        public string prop5 { get; set; }
        public float prop6 { get; set; }
        public string prop7 { get; set; }
        public float prop8 {get; set;}
        public string prop9 {get; set;}
        public int prop10 { get; set;}
        public string prop11 {get; set; }
        public int prop12 {get; set;}
        public string prop13 {get; set;}
        public float prop14 {get;set;}
        public string prop15 {get; set;}
        public float prop16 {get; set;}

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}