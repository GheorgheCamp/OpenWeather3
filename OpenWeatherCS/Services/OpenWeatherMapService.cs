using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenWeatherCS.Models;
using System.Net.Http;
using System.IO;
using System.Xml.Linq;
using OpenWeatherCS.Utils;
using System.Net;
using System.Diagnostics;

namespace OpenWeatherCS.Services
{
    public class OpenWeatherMapService : IWeatherService
    {
        private const string APP_ID = "3e85c2f9a34014d236f3af64ac49db61";
        private const int MAX_FORECAST_DAYS = 7;
        private HttpClient client;

        public OpenWeatherMapService()
        {
            client = new HttpClient();
            client.BaseAddress = new Uri("http://api.openweathermap.org/data/2.5/");
        }

        public async Task<IEnumerable<WeatherForecast>> GetForecastAsync(string location, int days)
        {
            if (location == null) throw new ArgumentNullException("Location can't be null.");
            if (location == string.Empty) throw new ArgumentException("Location can't be an empty string.");
            if (days <= 0) throw new ArgumentOutOfRangeException("Days should be greater than zero.");
            if (days > MAX_FORECAST_DAYS) throw new ArgumentOutOfRangeException($"Days can't be greater than {MAX_FORECAST_DAYS}");

            var query = $"forecast/daily?q={location}&mode=xml&units=metric&cnt={days}&appid={APP_ID}";
            var response = await client.GetAsync(query);            

            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    throw new UnauthorizedApiAccessException("Invalid API key.");
                case HttpStatusCode.NotFound:
                    throw new LocationNotFoundException("Location not found.");
                case HttpStatusCode.OK:
                    var s = await response.Content.ReadAsStringAsync();
                    var x = XElement.Load(new StringReader(s));                  

                    var data = x.Descendants("time").Select(w => new WeatherForecast
                    {
                        Description = w.Element("symbol").Attribute("name").Value,
                        ID = int.Parse(w.Element("symbol").Attribute("number").Value),
                        IconID = w.Element("symbol").Attribute("var").Value,
                        Date = DateTime.Parse(w.Attribute("day").Value),
                        WindType = w.Element("windSpeed").Attribute("name").Value,
                        WindSpeed = double.Parse(w.Element("windSpeed").Attribute("mps").Value.Replace('.', ',')),
                        WindDirection = w.Element("windDirection").Attribute("code").Value,
                        DayTemperature = double.Parse(w.Element("temperature").Attribute("day").Value.Replace('.', ',')),
                        NightTemperature = double.Parse(w.Element("temperature").Attribute("night").Value.Replace('.',',')),
                        MaxTemperature = double.Parse(w.Element("temperature").Attribute("max").Value.Replace('.', ',')),
                        MinTemperature = double.Parse(w.Element("temperature").Attribute("min").Value.Replace('.', ',')),
                        Pressure = double.Parse(w.Element("pressure").Attribute("value").Value.Replace('.', ',')),
                        Humidity = double.Parse(w.Element("humidity").Attribute("value").Value.Replace('.', ','))
                    });

                    return data;
                default:
                    throw new NotImplementedException(response.StatusCode.ToString());
            }           
        }
    }
}