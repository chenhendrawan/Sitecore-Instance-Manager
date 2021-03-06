using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using Ionic.Zip;
using Sitecore.Diagnostics.Base;
using Sitecore.Diagnostics.Base.Annotations;

namespace SIM
{
  #region

  

  #endregion

  public static class ApplicationManager
  {
    #region Delegates

    public static event EventHandler<CancelEventArgs> AttemptToClose;

    #endregion

    #region Constants

    public const string AppDataRoot = @"%AppData%\Sitecore\Sitecore Instance Manager";
    public const string DefaultConfigurations = "Configurations";
    public const string DefaultPackages = "Packages";
    public const string StockPlugins = "Plugins";

    #endregion

    #region Fields

    [NotNull]
    public static readonly string AppLabel;

    [NotNull]
    public static readonly string AppRevision;

    [NotNull]
    public static readonly string AppShortVersion;

    [NotNull]
    public static readonly string AppVersion;

    [NotNull]
    public static readonly string CachesFolder;

    [NotNull]
    public static readonly string ConfigurationPackagesFolder;

    [NotNull]
    public static readonly string DataFolder;

    [NotNull]
    public static readonly string FilePackagesFolder;

    public static readonly bool IsDebugging;

    [NotNull]
    public static readonly string LogsFolder;

    [NotNull]
    public static readonly string PluginsFolder;

    [NotNull]
    public static readonly string ProfilesFolder;

    [NotNull]
    public static readonly string TempFolder;

    [NotNull]
    public static readonly string UserManifestsFolder;

    #endregion

    #region Constructors

    static ApplicationManager()
    {
      DataFolder = InitializeFolder(Environment.ExpandEnvironmentVariables(AppDataRoot));
      PluginsFolder = InitializeDataFolder("Plugins");
      CachesFolder = InitializeDataFolder("Caches");
      FilePackagesFolder = InitializeDataFolder("Custom Packages");
      ConfigurationPackagesFolder = InitializeDataFolder("Custom Configurations");
      ProfilesFolder = InitializeDataFolder("Profiles");
      LogsFolder = InitializeDataFolder("Logs");
      UserManifestsFolder = InitializeDataFolder("Manifests");
      AppRevision = GetRevision();
      AppVersion = GetVersion();
      AppShortVersion = GetShortVersion();
      AppLabel = GetLabel();
      IsDebugging = Environment.GetCommandLineArgs()[0].ContainsIgnoreCase("vshost.exe");
      TempFolder = InitializeDataFolder("Temp");
    }

    #endregion

    #region Public methods

    public static void RaiseAttemptToClose(CancelEventArgs e)
    {
      EventHelper.RaiseEvent(AttemptToClose, typeof(ApplicationManager), e);
    }

    public static string GetEmbeddedApp(string packageName, string assemblyName, string executableName)
    {
      Assert.ArgumentNotNull(packageName, "packageName");
      Assert.ArgumentNotNull(assemblyName, "assemblyName");
      Assert.ArgumentNotNull(executableName, "executableName");

      var folder = Path.Combine(TempFolder, assemblyName);
      var filePath = Path.Combine(folder, executableName);
      if (File.Exists(filePath))
      {
        return filePath;
      }

      var assembly = Assembly.Load(assemblyName);
      using (var stream = assembly.GetManifestResourceStream(assemblyName + @"." + packageName))
      {
        Assert.IsNotNull(stream, "stream");

        var tmp = Path.GetTempFileName();
        using (var fileStream = new FileStream(tmp, System.IO.FileMode.Create))
        {
          int len;
          var buffer = new byte[2048];

          while ((len = stream.Read(buffer, 0, buffer.Length)) > 0)
          {
            fileStream.Write(buffer, 0, len);
          }
        }

        using (var fs = new ZipFile(tmp))
        {
          fs.ExtractAll(folder);
        }

        Assert.IsTrue(File.Exists(filePath), "The {0} file path doesn't exist after successful extracting {1} package into {2} folder", filePath, tmp, folder);

        if (File.Exists(tmp))
        {
          File.Delete(tmp);
        }

        return filePath;
      }
    }

    #endregion

    #region Private methods

    private static string GetLabel()
    {
      var assembly = Assembly.GetExecutingAssembly();
      var type = typeof(AssemblyDescriptionAttribute);
      var descriptionAttribute = assembly.GetCustomAttributes(type, true);
      if (descriptionAttribute.Length == 0)
      {
        return string.Empty;
      }

      var label = descriptionAttribute[0] as AssemblyDescriptionAttribute;
      return label != null ? label.Description : string.Empty;
    }

    private static string GetRevision()
    {
      var assembly = Assembly.GetExecutingAssembly();
      var type = typeof(AssemblyInformationalVersionAttribute);
      var revisionAttribute = assembly.GetCustomAttributes(type, true);
      if (revisionAttribute.Length == 0)
      {
        return DateTime.Now.ToString("yyMMdd");
      }

      var revision = revisionAttribute[0] as AssemblyInformationalVersionAttribute;
      var rev = "rev. ";
      return revision != null ? revision.InformationalVersion.Remove(0, revision.InformationalVersion.IndexOf(rev, StringComparison.Ordinal) + rev.Length) : string.Empty;
    }

    private static string GetShortVersion()
    {
      var version = GetVersion();
      if (string.IsNullOrEmpty(version))
      {
        return string.Empty;
      }

      return version.Substring(0, 3);
    }

    private static string GetVersion()
    {
      var assembly = Assembly.GetExecutingAssembly();
      var type = typeof(AssemblyFileVersionAttribute);
      var versionAttribute = assembly.GetCustomAttributes(type, true);
      if (versionAttribute.Length == 0)
      {
        return string.Empty;
      }

      var version = versionAttribute[0] as AssemblyFileVersionAttribute;
      return version != null ? version.Version : string.Empty;
    }

    #endregion

    #region Methods

    [NotNull]
    private static string InitializeDataFolder([NotNull] string folder)
    {
      Assert.ArgumentNotNull(folder, "folder");

      return InitializeFolder(Path.Combine(DataFolder, folder));
    }

    [NotNull]
    private static string InitializeFolder([NotNull] string folder)
    {
      Assert.ArgumentNotNull(folder, "folder");

      string path = Path.Combine(Environment.CurrentDirectory, folder);
      if (!FileSystem.FileSystem.Local.Directory.Exists(folder))
      {
        FileSystem.FileSystem.Local.Directory.CreateDirectory(folder);
      }

      return path;
    }

    #endregion
  }
}