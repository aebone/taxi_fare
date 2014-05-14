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
using Windows.UI.Core;
using Windows.Devices.Geolocation;

namespace TaxiFare
{
    public sealed partial class MapPage : Page
    {
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

            poaMap.Children.Add(GpsPushpin);
        }

        // Navigation (Back to MainPage button)
        private void BtnVoltar_Click(object sender, RoutedEventArgs e)
        {
            MainPage mp = new MainPage();
            Map.Children.Clear();
            Map.Children.Add(mp);
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
                Location location = new Location(args.Position.Coordinate.Latitude, args.Position.Coordinate.Longitude);

                //Update the position of the GPS pushpin
                MapLayer.SetPosition(GpsPushpin, location);

                //Make GPS pushpin visible
                GpsPushpin.Visibility = Windows.UI.Xaml.Visibility.Visible;

                //Update the map view to the current GPS location
                poaMap.SetView(location, 17);
            }));
        }
    }
}
