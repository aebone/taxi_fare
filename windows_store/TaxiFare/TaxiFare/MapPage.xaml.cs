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
 
            //Clear the geocode results ItemSource
            GeocodeResults.ItemsSource = null;
 
            //Clear the route instructions
            RouteResults.DataContext = null;
        }

        private void ClearMapBtn_Click(object sender, RoutedEventArgs e)
        {
            ClearMap();
        }

        private async void GeocodeBtn_Click(object sender, RoutedEventArgs e)
        {
            ClearMap();
 
            string query = GeocodeTbx.Text;
 
            if (!string.IsNullOrWhiteSpace(query))
            {
                //Create the request URL for the Geocoding service
                Uri geocodeRequest = new Uri(
                    string.Format("http://dev.virtualearth.net/REST/v1/Locations?q={0}&key={1}",
                    query, PoaMap.Credentials));
 
                //Make a request and get the response
                Response r = await GetResponse(geocodeRequest);
 
                if (r != null &&
                    r.ResourceSets != null &&
                    r.ResourceSets.Length > 0 &&
                    r.ResourceSets[0].Resources != null &&
                    r.ResourceSets[0].Resources.Length > 0)
                {
                    LocationCollection locations = new LocationCollection();
 
                    int i = 1;
 
                    foreach (BingMapsRESTService.Common.JSON.Location l
                             in r.ResourceSets[0].Resources)
                    {
                        //Get the location of each result
                        Bing.Maps.Location location =
                              new Bing.Maps.Location(l.Point.Coordinates[0], l.Point.Coordinates[1]);
 
                        //Create a pushpin each location
                        Pushpin pin = new Pushpin(){
                            Tag = l.Name,
                            Text = i.ToString()
                        };
 
                        i++;
 
                        //Add a tapped event that will display the name of the location
                        pin.Tapped += (s, a) =>
                        {
                            var p = s as Pushpin;
                            ShowMessage(p.Tag as string);
                        };
 
                        //Set the location of the pushpin
                        MapLayer.SetPosition(pin, location);
 
                        //Add the pushpin to the map
                        PoaMap.Children.Add(pin);                     
 
                        //Add the coordinates of the location to a location collection
                        locations.Add(location);
                    }
 
                    //Set the map view based on the location collection
                    PoaMap.SetView(new LocationRect(locations));
 
                    //Pass the results to the item source of the GeocodeResult ListBox
                    GeocodeResults.ItemsSource = r.ResourceSets[0].Resources;
                }
                else
                {
                    ShowMessage("No Results found.");
                }
            }
            else
           {
                ShowMessage("Invalid Geocode Input.");
            }
        }

        private void GeocodeResultSelected(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
 
            if (listBox.SelectedItems.Count > 0)
            {
                //Get the Selected Item
                var item = listBox.Items[listBox.SelectedIndex]
                           as BingMapsRESTService.Common.JSON.Location;
 
                //Get the items location
                Bing.Maps.Location location =
                       new Bing.Maps.Location(item.Point.Coordinates[0], item.Point.Coordinates[1]);
 
                //Zoom into the location
                PoaMap.SetView(location, 18);
            }
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
                    //Create the Request URL for the routing service
                    Uri routeRequest = new Uri(string.Format("http://dev.virtualearth.net/REST/V1/Routes/Driving?wp.0={0}&wp.1={1}&rpo=Points&key={2}", from, to, PoaMap.Credentials));
                   
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
                            Text = "S",
                            Background = new SolidColorBrush(Colors.Green)
                        };
 
                        PoaMap.Children.Add(start);
                        MapLayer.SetPosition(start,
                            new Bing.Maps.Location(route.RouteLegs[0].ActualStart.Coordinates[0],
                                route.RouteLegs[0].ActualStart.Coordinates[1]));
 
                        Pushpin end = new Pushpin()
                        {
                            Text = "E",
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
