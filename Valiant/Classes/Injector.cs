using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Valiant.Classes;

public enum InjectorType
{
    AxonInjector,
    FluxteamInjector
}

public abstract class Injector
{
    public abstract string Directory { get; }
    public abstract InjectorType Type { get; }
    public abstract ExploitApiType SupportedApi { get; }

    public abstract Task Download();
    public abstract void Inject(string dllPath);

    public static Injector CreateNewInstance(InjectorType type, string dir)
    {
        return type switch
        {
            InjectorType.AxonInjector => new AxonInjector(),
            InjectorType.FluxteamInjector => new FluxteamInjector(dir),
            _ => null
        };
    }
}

public class AxonInjector : Injector
{
    public override string Directory => null;

    public override InjectorType Type => InjectorType.AxonInjector;

    public override ExploitApiType SupportedApi =>
        ExploitApiType.EasyExploits |
        ExploitApiType.Krnl;

    public override Task Download() => Task.CompletedTask;
    public override void Inject(string dllPath) => Axon.DllInjector.GetInstance.Inject("RobloxPlayerBeta", dllPath);
}

public class FluxteamInjector : Injector
{
    public override string Directory { get; }

    public override InjectorType Type => InjectorType.AxonInjector;

    public override ExploitApiType SupportedApi => ExploitApiType.WeAreDevs;

    public override async Task Download()
    {
        dynamic json = JsonConvert.DeserializeObject(await App.HttpClient.GetStringAsync("https://cdn.wearedevs.net/software/exploitapi/latestdata.json"));

        await App.HttpClient.GetFileAsync((string)json!.qdRFzx_exe, Path.Combine(Directory, "finj.exe"));
        await App.HttpClient.GetFileAsync((string)json!.injDep, Path.Combine(Directory, "kernel64.sys.dll"));
        await App.HttpClient.GetFileAsync(
            "https://cdn.discordapp.com/attachments/1007601850402480158/1015899620422983740/VMProtectSDK32.dll",
            Path.Combine(Directory, "VMProtectSDK32.dll"));
    }

    public override void Inject(string dllPath)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(Directory, "finj.exe"),
            WorkingDirectory = Directory
        });
    }

    public FluxteamInjector(string dir)
    {
        Directory = dir;
    }
}