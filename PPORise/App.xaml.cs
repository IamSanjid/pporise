using System;
using System.Reflection;
using System.Windows;

namespace PPORise
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string Name { get; private set; }
        public static string Version { get; private set; }
        public static string Author { get; private set; }
        public static string Description { get; private set; }
        public static bool IsBeta => true;

        public static void InitializeVersion()
        {
            var assembly = typeof(App).Assembly;
            var assemblyName = assembly.GetName();
            Name = assemblyName.Name;
            Version = IsBeta ? assemblyName.Version.ToString(3) + "-beta2" : assemblyName.Version.ToString();
            Author = ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyCompanyAttribute), false)).Company;
            Description = ((AssemblyDescriptionAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyDescriptionAttribute), false)).Description;
        }
    }
}
