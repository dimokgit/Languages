using CommonExtensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HedgeHog {
  public class Languages<TTest> where TTest : Languages<TTest>, new() {
    public enum Language { en, es, pt };
    public class Message {
      public string Text { get; set; }
      public string Source { get; set; }
      public Message(string text, string source) {
        this.Text = text;
        this.Source = source;
      }
      public override string ToString() {
        return new { Text, Source } + "";
      }
    }
    private static readonly TTest instance = new TTest();
    public static TTest Instance { get { return instance; } }
    static string GetExecPath(string fileName) {
      var suffix = System.IO.Path.Combine("App_Data", fileName);
      var rootPath = System.Web.Hosting.HostingEnvironment.MapPath("~/");
      if (string.IsNullOrWhiteSpace(rootPath)) return Helpers.GetExecPath(suffix);
      return System.IO.Path.Combine(rootPath, suffix);
    }
    public static ExpandoObject RunTest() {
      Get(Language.en);
      Func<Message, object> text = m => {
        var lines = (m.Text ?? "").Split('\n');
        return lines.Length < 2 ? (object)m.Text : lines;
      };
      var dict = SetInternal(Instance).Select(kv => new { Lang = kv.Key + "", Text = text(kv.Value), kv.Value.Source }).ToArray();
      return new { type = typeof(TTest), dict }.ToExpando();
    }

    protected Dictionary<Language, Message> _dict;
    static Dictionary<string, string> _langsMap = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {
        { "english","en"},
        { "Español","es"},
        { "spanish","es"},
        { "Portuguese","es"},
        { "Português","pt"}
      };
    public static string Get(string language) {
      if (language.Length > 2) {
        string l;
        if (!_langsMap.TryGetValue(language, out l))
          throw new Exception(new { language, message = "Is not mapped" } + "");
        language = l;
      }
      Language e;
      if (!Enum.TryParse(language, true, out e))
        throw new Exception((new { language, message = "Is not of " + e.GetType().Name + " type" } + ""));
      return Get(e);
    }
    public static string Get(Language lang) {
      try {
        return SetInternal(Instance)[lang].Text;
      } catch (Exception exc) {
        var typeName = new { type = typeof(TTest).FullName } + "";
        throw new Exception(typeName, exc);
      };
    }
    static Func<TTest, Dictionary<Language, Message>> SetInternal = i => i._dict ?? (i._dict = Set(i));
    protected static Func<TTest, Dictionary<Language, Message>> Set;
    protected static Dictionary<Language, Message> LoadFromFile(string fileName) {
      return LoadFromJson(ReadUtf8Text(fileName));
    }

    private static string ReadUtf8Text(string fileName) {
      var text = File.ReadAllText(GetExecPath(fileName), Encoding.UTF7);
      return text;
    }

    /// <summary>
    /// Generates names for file in every language from Language enum
    /// </summary>
    /// <param name="fileNameTemplate">ex: message-{0}.txt</param>
    /// <returns></returns>
    protected static Dictionary<Language, Message> LoadFromFiles(string fileNameTemplate) {
      return FillDict(MakeFileNames(fileNameTemplate));
    }
    protected static Dictionary<Language, Message> LoadFromPath(string ext) {
      return FillDict(EnsureFilePath(ext));
    }

    private static Dictionary<Language, Message> FillDict(Tuple<Language, string>[] pathes) {
      return (from x in pathes
              let text = ReadUtf8Text(x.Item2).ThrowIf(s => string.IsNullOrEmpty(s), "{0}", new { path = x.Item2 })
              select new { l = x.Item1, m = new Message(text, x.Item2) }
              ).ToDictionary(x => x.l, x => x.m);
    }

    protected static Dictionary<Language, Message> LoadFromJson(string json) {
      return JsonConvert.DeserializeObject<Dictionary<Language, string>>(json)
        .ToDictionary(kv => kv.Key, kv => new Message(kv.Value, json));
    }
    public static Tuple<Language, string>[] MakeFileNames(string nameTemplate) {
      return GetLanguages().Select(name => Tuple.Create(name, nameTemplate.Formatter(name))).ToArray();
    }

    private static IEnumerable<Language> GetLanguages() {
      return Enum.GetValues(typeof(Language)).Cast<Language>();
    }

    static Tuple<Language, string>[] EnsureFilePath(string ext) {
      var path = Directory.CreateDirectory(GetExecPath(typeof(TTest).FullName.Replace(".", "\\"))).FullName;
      Action<string> ensureFile = fileName => {
        if (!File.Exists(fileName))
          File.CreateText(fileName).Close();
      };
      return GetLanguages()
        .Select(l => new { l, path = System.IO.Path.Combine(path, l + "." + ext) })
        .Do(x => ensureFile(x.path))
        .Select(x => Tuple.Create(x.l, x.path))
        .ToArray();
    }
  }
}
