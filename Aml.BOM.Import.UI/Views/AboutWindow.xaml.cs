using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace Aml.BOM.Import.UI.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        LoadVersionInformation();
    }

    private void LoadVersionInformation()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName();
            var version = assemblyName.Version;

            VersionTextBlock.Text = $"Version {version?.Major}.{version?.Minor}.{version?.Build}";

            DotNetVersionTextBlock.Text = Environment.Version.ToString();

            OSVersionTextBlock.Text = Environment.OSVersion.ToString();

            var buildDate = GetBuildDate(assembly);
            BuildDateTextBlock.Text = buildDate.ToString("MMMM dd, yyyy");

            var copyrightAttribute = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
            CopyrightTextBlock.Text = copyrightAttribute?.Copyright ?? $"© {DateTime.Now.Year} AML Corporation";
        }
        catch (Exception)
        {
            VersionTextBlock.Text = "Version 1.0.0";
            DotNetVersionTextBlock.Text = ".NET 8.0";
            OSVersionTextBlock.Text = Environment.OSVersion.ToString();
            BuildDateTextBlock.Text = DateTime.Now.ToString("MMMM dd, yyyy");
            CopyrightTextBlock.Text = $"© {DateTime.Now.Year} AML Corporation";
        }
    }

    private DateTime GetBuildDate(Assembly assembly)
    {
        var filePath = assembly.Location;
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            return File.GetLastWriteTime(filePath);
        }

        return DateTime.Now;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
