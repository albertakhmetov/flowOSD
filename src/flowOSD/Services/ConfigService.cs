/*  Copyright © 2021-2024, Albert Akhmetov <akhmetov@live.com>   
 *
 *  This file is part of flowOSD.
 *
 *  flowOSD is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  flowOSD is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with flowOSD. If not, see <https://www.gnu.org/licenses/>.   
 *
 */
namespace flowOSD.Services;

using System.Diagnostics;
using System.Management;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using flowOSD.Core.Configs;
using flowOSD.Core.Hardware;
using flowOSD.Core.Resources;
using flowOSD.Extensions;
using Microsoft.Win32;

sealed class ConfigService : IConfig, IDisposable
{
    private const string RUN_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    private ITextResources textResources;

    private CompositeDisposable? disposable = new CompositeDisposable();
    private FileInfo configFile;

    public ConfigService(ITextResources textResources)
    {
        this.textResources = textResources ?? throw new ArgumentNullException(nameof(textResources));

        AppFile = new FileInfo(typeof(ConfigService).Assembly.Location);
        AppFileInfo = FileVersionInfo.GetVersionInfo(AppFile.FullName);

        ProductName = AppFileInfo.ProductName ?? throw new AppException(textResources["Errors.ProductNameIsNotSet"]);
        ProductVersion = AppFileInfo.ProductVersion ?? throw new AppException(textResources["Errors.ProductVersionIsNotSet"]);
        FileVersion = new Version(
            AppFileInfo.FileMajorPart,
            AppFileInfo.FileMinorPart,
            AppFileInfo.FileBuildPart,
            AppFileInfo.FilePrivatePart);

        IsPreRelease = Regex.IsMatch(ProductVersion, "[a-zA-Z]");

        DataDirectory = new DirectoryInfo(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppFileInfo.ProductName!));

        if (!DataDirectory.Exists)
        {
            DataDirectory.Create();
        }

        ModelName = GetModelName() ?? string.Empty;

#if DEBUG
        configFile = new FileInfo(Path.Combine(DataDirectory.FullName, "config-debug.json"));
#else
        configFile = new FileInfo(Path.Combine(DataDirectory.FullName, "config.json"));
#endif

        var poco = Load();

        Common = poco.Common ?? new CommonConfig();
        Common.RunAtStartup = GetStartupOption();
        Common.PropertyChanged
            .Where(x => x == nameof(Common.RunAtStartup))
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(_ => UpdateStartupOption(Common.RunAtStartup))
            .DisposeWith(disposable);
        Common.PropertyChanged
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(x => Save())
            .DisposeWith(disposable);

        Notifications = poco.Notifications ?? new EnumConfig<NotificationType>();
        Notifications.PropertyChanged
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(x => Save())
            .DisposeWith(disposable);

        Warnings = poco.Warnings ?? new EnumConfig<WarningType>();
        Warnings.PropertyChanged
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(x => Save())
            .DisposeWith(disposable);

        HotKeys = poco.HotKeys ?? new HotKeysConfig();
        HotKeys.PropertyChanged
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(x => Save())
            .DisposeWith(disposable);

        Performance = new PerformanceConfig(poco.Performance?.Profiles);
        if (poco.Performance != null)
        {
            Performance.ChargerProfile = poco.Performance.ChargerProfile;
            Performance.BatteryProfile = poco.Performance.BatteryProfile;
            Performance.TabletProfile = poco.Performance.TabletProfile;
        }

        Performance.PropertyChanged
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(x => Save())
            .DisposeWith(disposable);
    }

    public CommonConfig Common { get; }

    public EnumConfig<NotificationType> Notifications { get; }

    public EnumConfig<WarningType> Warnings { get; }

    public HotKeysConfig HotKeys { get; }

    public PerformanceConfig Performance { get; }

    public FileInfo AppFile { get; }

    public FileVersionInfo AppFileInfo { get; }

    public DirectoryInfo DataDirectory { get; }

    public bool IsPreRelease { get; }

    public string ProductName { get; }

    public string ProductVersion { get; }

    public Version FileVersion { get; }

    public string ModelName { get; }

    public void Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    private POCO Load()
    {
        configFile.Refresh();
        try
        {
            using var stream = configFile.OpenRead();

            var options = new JsonSerializerOptions { WriteIndented = true };
            options.Converters.Add(new EnumConfigConverter<NotificationType>(textResources));
            options.Converters.Add(new EnumConfigConverter<WarningType>(textResources));
            options.Converters.Add(new HotKeysConfigConverter(textResources));

            return JsonSerializer.Deserialize<POCO>(stream, options) ?? new POCO();
        }
        catch (Exception)
        {
            return new POCO();
        }
    }

    private void Save()
    {
        var tempFile = new FileInfo(Path.GetTempFileName());
        using (var stream = tempFile.Create())
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            options.Converters.Add(new EnumConfigConverter<NotificationType>(textResources));
            options.Converters.Add(new EnumConfigConverter<WarningType>(textResources));
            options.Converters.Add(new HotKeysConfigConverter(textResources));

            var poco = new POCO
            {
                Common = this.Common,
                Notifications = this.Notifications,
                Warnings = this.Warnings,
                HotKeys = this.HotKeys,
                Performance = new PerformancePOCO
                {
                    ChargerProfile = this.Performance.ChargerProfile,
                    BatteryProfile = this.Performance.BatteryProfile,
                    TabletProfile = this.Performance.TabletProfile,
                    Profiles = this.Performance.GetProfiles().ToArray()
                }
            };

            JsonSerializer.Serialize<POCO>(stream, poco, options);
        }

        configFile.Refresh();
        if (configFile.Exists)
        {
            tempFile.Replace(configFile.FullName, configFile.FullName + ".backup");
        }
        else
        {
            tempFile.MoveTo(configFile.FullName);
        }
    }

    private bool GetStartupOption()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true);

        return key?.GetValue(AppFileInfo.ProductName) != null;
    }

    private void UpdateStartupOption(bool runAtStartup)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true);
        if (key == null)
        {
            throw new AppException(textResources["Errors.CanNotWriteStartupKey"]);
        }

        var exe = Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrEmpty(exe) || !exe.EndsWith(".exe"))
        {
            throw new AppException(textResources["Errors.CanNotRetriveAppPath"]);
        }

        if (runAtStartup)
        {
            key.SetValue(AppFileInfo.ProductName!, exe);
        }
        else
        {
            key.DeleteValue(AppFileInfo.ProductName!, false);
        }
    }

    private string? GetModelName()
    {
        using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
        foreach (var i in searcher.Get())
        {
            if (i.Properties["Model"].Value is string modelName)
            {
                var underlineIndex = modelName?.IndexOf("_");
                return underlineIndex > 0 ? modelName?.Substring(0, underlineIndex.Value) : modelName;
            }
        }

        return null;
    }

    private class POCO
    {
        public CommonConfig? Common { get; set; }

        public EnumConfig<NotificationType>? Notifications { get; set; }

        public EnumConfig<WarningType>? Warnings { get; set; }

        public HotKeysConfig? HotKeys { get; set; }

        public PerformancePOCO? Performance { get; set; }
    }

    private class PerformancePOCO
    {
        public Guid ChargerProfile { get; set; }

        public Guid BatteryProfile { get; set; }

        public Guid TabletProfile { get; set; }

        public PerformanceProfile[]? Profiles { get; set; }
    }

    private class EnumConfigConverter<T> : JsonConverter<EnumConfig<T>> where T : struct, Enum
    {
        private ITextResources textResources;

        public EnumConfigConverter(ITextResources textResources)
        {
            this.textResources = textResources ?? throw new ArgumentNullException(nameof(textResources));
        }

        public override EnumConfig<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var config = new EnumConfig<T>();

            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new AppException(textResources["Errors.ConfigIsCorrupted"]);
            }

            while (reader.Read() && reader.TokenType == JsonTokenType.String)
            {
                var v = reader.GetString();
                if (Enum.TryParse<T>(v, out var type))
                {
                    config[type] = false;
                }
            }

            if (reader.TokenType != JsonTokenType.EndArray)
            {
                throw new AppException(textResources["Errors.ConfigIsCorrupted"]);
            }

            return config;
        }

        public override void Write(Utf8JsonWriter writer, EnumConfig<T> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();

            foreach (var i in Enum.GetValues<T>())
            {
                if (!value[i])
                {
                    writer.WriteStringValue(Enum.GetName(i));
                }
            }

            writer.WriteEndArray();
        }
    }

    private class HotKeysConfigConverter : JsonConverter<HotKeysConfig>
    {
        private const string KEY = "Key";
        private ITextResources textResources;

        public HotKeysConfigConverter(ITextResources textResources)
        {
            this.textResources = textResources ?? throw new ArgumentNullException(nameof(textResources));
        }

        public override HotKeysConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var config = new HotKeysConfig();

            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new AppException(textResources["Errors.ConfigIsCorrupted"]);
            }

            var item = new Dictionary<string, string>();

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();

                    if (propertyName != null && reader.Read()
                        && (reader.TokenType == JsonTokenType.String || reader.TokenType == JsonTokenType.Null))
                    {
                        if (reader.TokenType != JsonTokenType.Null)
                        {
                            item[propertyName] = reader.GetString()!;
                        }
                    }
                    else
                    {
                        throw new AppException(textResources["Errors.ConfigIsCorrupted"]);
                    }
                }

                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    if (item.TryGetValue(KEY, out var keyRaw) && Enum.TryParse<AtkKey>(keyRaw, out var key)
                        && item.TryGetValue(nameof(HotKeysConfig.Command.Name), out var commandName))
                    {
                        config[key] = new HotKeysConfig.Command(
                            commandName,
                            item.ContainsKey(nameof(HotKeysConfig.Command.Parameter)) ? item[nameof(HotKeysConfig.Command.Parameter)] : null);
                    }
                    else
                    {
                        throw new AppException(textResources["Errors.ConfigIsCorrupted"]);
                    }

                    item = new Dictionary<string, string>();
                }
            }

            if (reader.TokenType != JsonTokenType.EndArray)
            {
                throw new AppException(textResources["Errors.ConfigIsCorrupted"]);
            }

            return config;
        }

        public override void Write(Utf8JsonWriter writer, HotKeysConfig value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();

            foreach (var i in Enum.GetValues<AtkKey>())
            {
                var item = value[i];
                if (item != null)
                {
                    writer.WriteStartObject();

                    writer.WritePropertyName(KEY);
                    writer.WriteStringValue(Enum.GetName(i));

                    writer.WritePropertyName(nameof(HotKeysConfig.Command.Name));
                    writer.WriteStringValue(item.Name);

                    writer.WritePropertyName(nameof(HotKeysConfig.Command.Parameter));
                    writer.WriteStringValue(item.Parameter?.ToString());

                    writer.WriteEndObject();
                }
            }

            writer.WriteEndArray();
        }
    }
}