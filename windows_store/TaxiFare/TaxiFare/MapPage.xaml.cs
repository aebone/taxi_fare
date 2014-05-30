using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Bing.Maps;
using BingMapsRESTService.Common.JSON;
using Windows.UI.Core;
using Windows.Devices.Geolocation;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI;

namespace TaxiFare
{
    public sealed partial class MapPage : Page
    {
        private MapShapeLayer routeLayer;
        Geolocator geolocator; // Geolocator class from "using Windows.Devices.Geolocation;"
        Pushpin GpsPushpin; // Pushpin class from "using Bing.Maps;"


        public MapPage()
        {
            this.InitializeComponent();

            // Create a pushpin for the GPS location and add it to the map
            GpsPushpin = new Pushpin()
            {
                Visibility = Windows.UI.Xaml.Visibility.Collapsed
            };

            PoaMap.Children.Add(GpsPushpin);


            routeLayer = new MapShapeLayer();
            PoaMap.ShapeLayers.Add(routeLayer);
        }

        private void GPS_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;

            if (cb.IsChecked.HasValue && cb.IsChecked.Value)
            {
                if (geolocator == null)
                {
                    //Create an instance of the GeoLocator class.
                    geolocator = new Geolocator();
                }

                //Add the position changed event
                geolocator.PositionChanged += geolocator_PositionChanged;
            }
            else
            {
                if (geolocator != null)
                {
                    //Remove the position changed event
                    geolocator.PositionChanged -= geolocator_PositionChanged;
                }

                //Hide the GPS pushpin
                GpsPushpin.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        private void geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            // Need to get back onto UI thread before updating location information
            this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(
            () =>
            {
                //Get the current location
                Bing.Maps.Location location = new Bing.Maps.Location(args.Position.Coordinate.Latitude, args.Position.Coordinate.Longitude);

                //Update the position of the GPS pushpin
                MapLayer.SetPosition(GpsPushpin, location);

                //Make GPS pushpin visible
                GpsPushpin.Visibility = Windows.UI.Xaml.Visibility.Visible;

                //Update the map view to the current GPS location
                PoaMap.SetView(location, 17);
            }));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private async void ShowMessage(string message)
        {
            MessageDialog dialog = new MessageDialog(message);
            await dialog.ShowAsync();
        }

        private async Task<Response> GetResponse(Uri uri)
        {
            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
            var response = await client.GetAsync(uri);
 
            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Response));
                return ser.ReadObject(stream) as Response;
            }
        }
        
        private void ClearMap()
        {
            PoaMap.Children.Clear();
            routeLayer.Shapes.Clear();
 
            //Clear the route instructions
            RouteResults.DataContext = null;
        }

        private void ClearMapBtn_Click(object sender, RoutedEventArgs e)
        {
            ClearMap();
        }

        private async void RouteBtn_Click(object sender, RoutedEventArgs e)
        {
            ClearMap();
 
            string from = FromTbx.Text;
            string to = ToTbx.Text;
 
            if (!string.IsNullOrWhiteSpace(from))
            {
                if (!string.IsNullOrWhiteSpace(to))
                {
                    //Create the Request URL for the routing service - IMPORTANT (c=pt to define a culture in a REST request)
                    Uri routeRequest = new Uri(string.Format("http://dev.virtualearth.net/REST/V1/Routes/Driving?wp.0={0}&wp.1={1}&rpo=Points&c=pt&key={2}", from, to, PoaMap.Credentials));

                    //Make a request and get the response
                    Response r = await GetResponse(routeRequest);
                    if (r != null &&
                        r.ResourceSets != null &&
                        r.ResourceSets.Length > 0 &&
                        r.ResourceSets[0].Resources != null &&
                        r.ResourceSets[0].Resources.Length > 0)
                    {
                        Route route = r.ResourceSets[0].Resources[0] as Route;

                        //Get the route line data
                        double[][] routePath = route.RoutePath.Line.Coordinates;
                        LocationCollection locations = new LocationCollection();

                        //Get the total route distance
                        double TotalDistance = route.TravelDistance;
                        TotalRoute.Text = "Distância Total: " + TotalDistance.ToString("0.00") + "Km";

                        //Calculate TaxiFare
                        double StartPrice = 4.22;
                        double B1 = 2.11; //2.11/km
                        double B2 = 2.74; //2.74/km
                        double TotalB1 = (TotalDistance * B1) + StartPrice;
                        double TotalB2 = (TotalDistance * B2) + StartPrice;

                        Bandeira1.Text = "Bandeira 1: R$ " + TotalB1.ToString("0.00");
                        Bandeira2.Text = "Bandeira 2: R$ " + TotalB2.ToString("0.00");

                        int Hour = DateTime.Now.Hour;
                        int Minute = DateTime.Now.Minute;
                        int Day = (int)System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(DateTime.Now);
                        string DayOfWeek;
                        switch (Day)
                        {
                            case 1:
                                DayOfWeek = "Segunda-feira";
                                Agora.Text = "Agora são " + Hour + "h" + Minute + "m de " + DayOfWeek + ":";
                                break;
                            case 2:
                                DayOfWeek = "Terça-feira";
                                Agora.Text = "Agora são " + Hour + "h" + Minute + "m de " + DayOfWeek + ":";
                                break;
                            case 3:
                                DayOfWeek = "Quarta-feira";
                                Agora.Text = "Agora são " + Hour + "h" + Minute + "m de " + DayOfWeek + ":";
                                break;
                            case 4:
                                DayOfWeek = "Quinta-feira";
                                Agora.Text = "Agora são " + Hour + "h" + Minute + "m de " + DayOfWeek + ":";
                                break;
                            case 5:
                                DayOfWeek = "Sexta-feira";
                                Agora.Text = "Agora são " + Hour + "h" + Minute + "m de " + DayOfWeek + ":";
                                break;
                            case 6:
                                DayOfWeek = "Sábado";
                                Agora.Text = "Agora são " + Hour + "h" + Minute + "m de " + DayOfWeek + ":";
                                break;
                            case 7:
                                DayOfWeek = "Domingo";
                                Agora.Text = "Agora são " + Hour + "h" + Minute + "m de " + DayOfWeek + ":";
                                break;
                        }
                        if (Day == 7)
                            Bandeira.Text = "Bandeira 2";
                        else if (Day == 6)
                        {
                            if (Hour >= 15 && Hour <= 24)
                                Bandeira.Text = "Bandeira 2";
                            else
                            {
                                Bandeira.Text = "Bandeira 1*";
                                Exceto.Text = "*Exceto Feriado";
                            }
                        }
                        else 
                        {
                            if (Hour >= 20 || Hour <= 6)
                                Bandeira.Text = "Bandeira 2";
                            else
                            {
                                Bandeira.Text = "Bandeira 1*";
                                Exceto.Text = "*Exceto Feriado";
                            }
                        }

                        //Not included
                        NaoInclui.Text = "O cálculo não inclui tempo parado (R$ 14,90 por hora), animais e volumes de grandes proporções: (R$ 6,00) e volume excedente (acima de 3 volumes de mão ou 1 mala:  R$ 1,20).";
                                                       
                        //Detailed route
                        for (int i = 0; i < routePath.Length; i++)
                        {
                            if (routePath[i].Length >= 2)
                            {
                                locations.Add(new Bing.Maps.Location(routePath[i][0],
                                              routePath[i][1]));
                            }
                        }
 
                        //Create a MapPolyline of the route and add it to the map
                        MapPolyline routeLine = new MapPolyline()
                        {
                            Color = Colors.Blue,
                            Locations = locations,
                            Width = 5
                        };
 
                        routeLayer.Shapes.Add(routeLine);
 
                        //Add start and end pushpins
                        Pushpin start = new Pushpin()
                        {
                            Text = "I",
                            Background = new SolidColorBrush(Colors.Green)
                        };
 
                        PoaMap.Children.Add(start);
                        MapLayer.SetPosition(start,
                            new Bing.Maps.Location(route.RouteLegs[0].ActualStart.Coordinates[0],
                                route.RouteLegs[0].ActualStart.Coordinates[1]));
 
                        Pushpin end = new Pushpin()
                        {
                            Text = "F",
                            Background = new SolidColorBrush(Colors.Red)
                        };
 
                        PoaMap.Children.Add(end);
                        MapLayer.SetPosition(end,
                            new Bing.Maps.Location(route.RouteLegs[0].ActualEnd.Coordinates[0],
                            route.RouteLegs[0].ActualEnd.Coordinates[1]));
 
                        //Set the map view for the locations
                        PoaMap.SetView(new LocationRect(locations));
 
                        //Pass the route to the Data context of the Route Results panel
                        RouteResults.DataContext = route;
                    }
                    else
                    {
                        ShowMessage("No Results found.");
                    }
                }
                else
                {
                    ShowMessage("Invalid 'To' location.");
                }
            }
            else
            {
                ShowMessage("Invalid 'From' location.");
            }
        }
    }
}
