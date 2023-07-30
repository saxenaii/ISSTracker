using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Maps;

namespace ISSTracker
{
    public partial class MainPage : ContentPage
    {
        CancellationTokenSource cts;

        // Current position of ISS plus latitude and longitude expressed as doubles
        Position issPosition { get; set; }
        double doubleILat { get; set; }
        double doubleILon { get; set; }

        // Current position of User plus latitude and longitude expressed as doubles
        Position userPosition { get; set; }
        double doubleULat { get; set; }
        double doubleULon { get; set; }

        // Current distance between User & ISS in miles expressed as a double

        double doubleIDistance { get; set; }

        public MainPage()
        {
            InitializeComponent();

        }

        // Grabbing JSON data from api.open-notify.org, parsing data & dropping pin for ISS on map.
        public async Task GetJsonAsync()
        {
            var uri = new Uri("http://api.open-notify.org/iss-now.json");
            HttpClient httpClient = new HttpClient();
            var response = await httpClient.GetAsync(uri);
            
            if (response != null && response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                string json = content.ToString();
                var jsonObject = JObject.Parse(json);
                string issPos = jsonObject["iss_position"].ToString();
                var issPosObj = JObject.Parse(issPos);
                string issLat = issPosObj["latitude"].ToString();
                string issLon = issPosObj["longitude"].ToString();
                doubleILat = double.Parse(issLat);
                doubleILon = double.Parse(issLon);
                issPosition = new Position(doubleILat, doubleILon);
                Pin issPin = new Pin
                {
                    Position = issPosition,
                    Label = "ISS Location",
                    Type = PinType.Generic
                };
                ISSMap.Pins.Add(issPin);
            }
            else
            {
                RetrieveLabel.Text = "ISS API Error";
            }
        }

        /*

        this one is a bit of a mess - first it calculates user location using GPS sensor data and stores the coordinates as strings.
        then parses latitude and longitude as doubles, storing position for user on map, and dropping a pin.

        finally, a try / catch for debug and moves the map focus to ISS location and calculates the distance between the two points.

        modifies the RetrieveLabel text to notify user of distance between them and ISS and writes line to console in case of exception error.

        also throws errors if there is something wrong with capturing sensor data (the catches at the very bottom).

        */
        private async void Button_Clicked_1(object sender, EventArgs e)
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
                cts = new CancellationTokenSource();
                var location = await Geolocation.GetLocationAsync(request, cts.Token);

                if (location != null)
                {
                    string latitude = $"{location.Latitude}";
                    string longitude = $"{location.Longitude}";
                    doubleULat = double.Parse(latitude);
                    doubleULon = double.Parse(longitude);
                    userPosition = new Position(doubleULat, doubleULon);
                    Pin userPin = new Pin
                    {
                        Position = userPosition,
                        Label = "User Location",
                        Type = PinType.Generic
                    };
                    ISSMap.Pins.Add(userPin);
                    try
                    {
                        await GetJsonAsync();
                        ISSMap.MoveToRegion(MapSpan.FromCenterAndRadius(issPosition, Distance.FromMiles(0.001)));
                        string issDistance = Location.CalculateDistance(doubleILat, doubleILon, doubleULat, doubleULon, DistanceUnits.Miles).ToString();
                        doubleIDistance = double.Parse(issDistance);
                        int intIDistance = Convert.ToInt32(doubleIDistance);
                        RetrieveLabel.Text = "You are " + intIDistance.ToString("N0") + " miles away from the Internation Space Station!";
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
            catch (FeatureNotSupportedException)
            {
                RetrieveLabel.Text = "Feature Not Supported";
            }
            catch (FeatureNotEnabledException)
            {
                RetrieveLabel.Text = "Feature Not Enabled";
            }
            catch (PermissionException)
            {
                RetrieveLabel.Text = "Permissions Error";
            }
            catch (Exception)
            {
                RetrieveLabel.Text = "Can't Retrieve Location";
            }
        }

        // honestly don't remember what this is for

        protected override void OnDisappearing()
        {
            if (cts != null && !cts.IsCancellationRequested)
                cts.Cancel();
            base.OnDisappearing();
        }
    }
}
