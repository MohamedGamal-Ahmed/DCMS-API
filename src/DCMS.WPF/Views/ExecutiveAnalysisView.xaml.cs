using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using LiveCharts.Wpf;

namespace DCMS.WPF.Views;

public partial class ExecutiveAnalysisView : UserControl
{
    public ExecutiveAnalysisView()
    {
        InitializeComponent();
        this.Loaded += ExecutiveAnalysisView_Loaded;
    }

    private void ExecutiveAnalysisView_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Determine the absolute path to the World.xml file
            string baseDir = System.AppDomain.CurrentDomain.BaseDirectory;
            string mapFilePath = System.IO.Path.Combine(baseDir, "Assets", "World.xml");
            
            // Fallback paths for development
            if (!System.IO.File.Exists(mapFilePath))
                mapFilePath = System.IO.Path.Combine(baseDir, "World.xml");

            // If still not found (Production/Single-File), extract from Resources
            if (!System.IO.File.Exists(mapFilePath))
            {
                try
                {
                    string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DCMS_Maps");
                    if (!System.IO.Directory.Exists(tempDir)) System.IO.Directory.CreateDirectory(tempDir);
                    
                    string tempFilePath = System.IO.Path.Combine(tempDir, "World.xml");
                    
                    // Extract resource to temp file
                    var resourceUri = new System.Uri("pack://application:,,,/Assets/World.xml");
                    var info = System.Windows.Application.GetResourceStream(resourceUri);
                    if (info != null)
                    {
                        using (var stream = info.Stream)
                        using (var fileStream = System.IO.File.Create(tempFilePath))
                        {
                            stream.CopyTo(fileStream);
                        }
                        mapFilePath = tempFilePath;
                        System.Diagnostics.Debug.WriteLine($"[MAP] Extracted World.xml to temp: {mapFilePath}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MAP] Failed to extract resource: {ex.Message}");
                }
            }

            if (!System.IO.File.Exists(mapFilePath))
            {
                // File not found - show the error message
                MapErrorMessage.Visibility = Visibility.Visible;
                System.Diagnostics.Debug.WriteLine("World.xml not found locally or in resources.");
                return;
            }

            // Create the GeoMap dynamically
            var geoMap = new GeoMap
            {
                Source = mapFilePath,
                Hoverable = true,
                LanguagePack = null,
                EnableZoomingAndPanning = true, // Enable mouse wheel zoom and click-drag pan
                ClipToBounds = true, // Keep the map within its container bounds
                FlowDirection = FlowDirection.LeftToRight // Force LTR to prevent horizontal flip in Arabic UI
            };

            // Set up the color gradient
            geoMap.GradientStopCollection = new GradientStopCollection
            {
                new GradientStop(Color.FromRgb(0xE1, 0xF5, 0xFE), 0), // Light Blue
                new GradientStop(Color.FromRgb(0x02, 0x88, 0xD1), 1)  // Dark Blue
            };

            // Bind the HeatMap property to the ViewModel
            var heatMapBinding = new Binding("HeatMapValues") { Source = this.DataContext };
            geoMap.SetBinding(GeoMap.HeatMapProperty, heatMapBinding);

            // Add the GeoMap to the container
            MapContainer.Children.Add(geoMap);
        }
        catch (System.Exception ex)
        {
            // Handle any exception gracefully
            System.Diagnostics.Debug.WriteLine($"GeoMap Creation Failed: {ex.Message}");
            MapErrorMessage.Visibility = Visibility.Visible;
        }
    }
}
