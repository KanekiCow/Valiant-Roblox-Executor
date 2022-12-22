using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Resources;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Valiant.Classes;

public static class Extensions
{
    public static async Task GetFileAsync(this HttpClient client, string url, string fileName)
    {
        using var stream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        using var content = await client.GetAsync(url);
        await content.Content.CopyToAsync(stream);
    }

    // thanks to stackoverflow
    // https://stackoverflow.com/questions/470256/process-waitforexit-asynchronously
    public static Task WaitForExitAsync(this Process process,
        CancellationToken cancellationToken = default)
    {
        if (process.HasExited) return Task.CompletedTask;

        var tcs = new TaskCompletionSource<object>();
        process.EnableRaisingEvents = true;
        process.Exited += (_, _) => tcs.TrySetResult(null);
        if (cancellationToken != default)
            cancellationToken.Register(() => tcs.SetCanceled());

        return process.HasExited ? Task.CompletedTask : tcs.Task;
    }
}

public class EnumBindingSourceExtension : MarkupExtension
{
    public Type EnumType { get; private set; }

    public EnumBindingSourceExtension(Type enumType)
    {
        EnumType = enumType;
    }

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        Enum.GetValues(EnumType);
}