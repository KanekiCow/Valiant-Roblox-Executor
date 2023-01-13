using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Valiant.Classes;
public class ScriptPage
{
    [JsonProperty("totalPages")]
    public int TotalPages { get; set; }

    [JsonProperty("max")]
    public int Max { get; set; }

    [JsonProperty("scripts")]
    public List<Script> Scripts { get; set; }

    public static ScriptPage Parse(string str) =>
        JToken
            .Parse(str)!
            .Value<JToken>("result")!
            .ToObject<ScriptPage>();

    public static readonly Dictionary<string, string> CachedContent = new();
}

public class BaseObject
{

    [JsonProperty("_id")]
    public string Id { get; set; }
}

public class ScriptGame
{
    [JsonProperty("imageUrl")]
    public string RelativeImageUrl { get; set; }

    [JsonProperty("gameId")]
    public long GameId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    public string ImageUrl => RelativeImageUrl[0] == '/' ?
        $"https://scriptblox.com/{RelativeImageUrl}" :
        RelativeImageUrl;
}

public class ScriptUser : BaseObject
{
    [JsonProperty("username")]
    public string Name { get; set; }
}

public class Script : BaseObject
{
    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("owner")]
    public ScriptUser Owner { get; set; }

    [JsonProperty("game")]
    public ScriptGame Game { get; set; }

    [JsonProperty("slug")]
    public string Slug { get; set; }

    [JsonProperty("verified")]
    public bool IsVerified { get; set; }

    [JsonProperty("views")]
    public int Views { get; set; }

    [JsonProperty("scriptType")]
    public string Type { get; set; } // returns either "paid" or "free"

    [JsonProperty("isPatched")]
    public bool IsPatched { get; set; }

    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("likeCount")]
    public int LikeCount { get; set; }

    [JsonProperty("dislikeCount")]
    public int DislikeCount { get; set; }

    // ScriptBlox is really, REALLY ASS
    public async Task<string> GetContent()
    {
        if (ScriptPage.CachedContent.ContainsKey(Id))
            return ScriptPage.CachedContent[Id];
        
        var content = await App.HttpClient.GetStringAsync("https://rawscripts.net/raw/" + Id);

        // Add to cache
        ScriptPage.CachedContent[Id] = content;

        // I hate this lesser now ig.
        return content;
    }
}