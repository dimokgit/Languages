using Microsoft.VisualStudio.TestTools.UnitTesting;
using HedgeHog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonExtensions;
using Newtonsoft.Json;

namespace HedgeHog.Tests {
  static class JsonSrtings {
    public static string json = @"{
'en': 'english',
'es': 'spanisg',
'pt': 'string'
}";
    public static string jsonError = @"{
'en': 'english',
'es': 'spanisg',
'pt': 'string',
'ru': 'russian'
}";
  }
  public class EMAIL : RunTestable<EMAIL> {
    public static class LANGS {
      public class CLICK : Helpers.Languages<CLICK> {
        static CLICK() {
          Set = (i) => new Dictionary<Language, Message> {
              { Language.en,new Message("English","") },
              { Language.es,new Message("Spanish","") },
              { Language.pt,new Message("Brazillian","") }
            };
        }
      }
      public static class JSON_CLASSES {
        public class JSON : Helpers.Languages<JSON> {
          static JSON() {
            Set = (i) => LoadFromJson(JsonSrtings.json);
          }
        }
        public class JSON_ERROR : Helpers.Languages<JSON_ERROR> {
          static JSON_ERROR() {
            Set = (i) => LoadFromJson(JsonSrtings.jsonError);
          }
        }
        public class JSON_FILE : Helpers.Languages<JSON_FILE> {
          static JSON_FILE() {
            Set = (i) => LoadFromFile("Message.json");
          }
        }
        public class JSON_PATH : Helpers.Languages<JSON_PATH> {
          static JSON_PATH() {
            Set = (i) => LoadFromPath("txt");
          }
        }
        public class JSON_PATH_EMPTY : Helpers.Languages<JSON_PATH_EMPTY> {
          static JSON_PATH_EMPTY() {
            Set = (i) => LoadFromPath("txt");
          }
        }
        public class JSON_FILE_MALFORM : Helpers.Languages<JSON_FILE_MALFORM> {
          static JSON_FILE_MALFORM() {
            Set = (i) => LoadFromFile("MessageError.json");
          }
        }
        public class JSON_FILE_MISSING : Helpers.Languages<JSON_FILE_MISSING> {
          static JSON_FILE_MISSING() {
            Set = (i) => LoadFromFile("MessageMissing.json");
          }
        }
        public class JSON_FILES : Helpers.Languages<JSON_FILES> {
          static JSON_FILES() {
            Set = (i) => LoadFromFiles("Message-{0}.txt");
          }
        }
      }
    }
  }

  [TestClass()]
  public class LanguagesTests {
    class DummyLang : Helpers.Languages<DummyLang> { }
    [TestMethod()]
    public void LanguageRunTest() {
      AggregateException errors = new AggregateException();
      var testResults = EMAIL.RunTests(exc => errors = exc);
      testResults.ForEach(tr => Console.WriteLine(tr.ToJson()));
      errors.InnerExceptions.ForEach(error => Console.WriteLine(error.ToMessages() + "\n\n"));
      Assert.AreEqual(4, errors.InnerExceptions.Count);
      ExceptionAssert.Propagates<AggregateException>(() => EMAIL.RunTests(), exc => {
        Assert.AreEqual(4, exc.InnerExceptions.Count);
      });
    }
    [TestMethod()]
    public void LanguageJson() {
      var message = EMAIL.LANGS.JSON_CLASSES.JSON.Get(EMAIL.LANGS.JSON_CLASSES.JSON.Language.en);
      Assert.AreEqual("english", message);
    }
    [TestMethod()]
    public void LanguageFromPath() {
      var message = EMAIL.LANGS.JSON_CLASSES.JSON_PATH.Get(EMAIL.LANGS.JSON_CLASSES.JSON_PATH.Language.pt);
      Assert.AreEqual(Encoding.UTF7.GetString(Encoding.Default.GetBytes("questionário")), message);
    }
    [TestMethod()]
    public void LanguageFromPathEmpty() {
      ExceptionAssert.Propagates<Exception>(() => EMAIL.LANGS.JSON_CLASSES.JSON_PATH_EMPTY.RunTest(), exc => {
        Assert.IsTrue(exc.InnerException.Message.Contains("pt.txt"), "en.txt not found in exception message");
      });
    }
    [TestMethod()]
    public void LanguageJsonFromFile() {
      var message = EMAIL.LANGS.JSON_CLASSES.JSON_FILE.Get(EMAIL.LANGS.JSON_CLASSES.JSON_FILE.Language.en);
      Assert.AreEqual("english2", message);
    }
    [TestMethod()]
    public void LanguageJsonFromFiles() {
      var message = EMAIL.LANGS.JSON_CLASSES.JSON_FILES.Get(EMAIL.LANGS.JSON_CLASSES.JSON_FILES.Language.en);
      Assert.AreEqual("english3", message);
      message = EMAIL.LANGS.JSON_CLASSES.JSON_FILES.Get(EMAIL.LANGS.JSON_CLASSES.JSON_FILES.Language.es);
      Assert.AreEqual("spanish3", message);
    }
    [TestMethod()]
    public void LanguageJsonFromMissing() {
      ExceptionAssert.Propagates<Exception>(() => EMAIL.LANGS.JSON_CLASSES.JSON_FILE_MISSING.RunTest(), exc => {
        Assert.IsTrue(exc.Message.Contains("JSON_FILE_MISSING"));
        Assert.AreEqual(typeof(System.IO.FileNotFoundException), exc.InnerException.GetType());
        Console.WriteLine(exc.ToMessages());
      });
    }
    [TestMethod()]
    public void LanguageJsonFromFileMalform() {
      ExceptionAssert.Propagates<Exception>(() => EMAIL.LANGS.JSON_CLASSES.JSON_FILE_MALFORM.RunTest(), exc => {
        Assert.IsTrue(exc.Message.Contains("JSON_FILE_MALFORM"));
        Assert.AreEqual(typeof(JsonSerializationException), exc.InnerException.GetType());
        Console.WriteLine(exc + "");
      });
    }
    [TestMethod()]
    public void LanguageJsonError() {
      ExceptionAssert.Propagates<Exception>(() => EMAIL.LANGS.JSON_CLASSES.JSON_ERROR.RunTest(), exc => {
        Assert.IsTrue(exc.Message.Contains("JSON_ERROR"));
        Assert.AreEqual(typeof(JsonSerializationException), exc.InnerException.GetType());
        Console.WriteLine(exc + "");
      });
    }
    [TestMethod()]
    public void Language() {
      Assert.AreEqual("English", EMAIL.LANGS.CLICK.Get("en"));
      Assert.AreEqual("Spanish", EMAIL.LANGS.CLICK.Get(EMAIL.LANGS.CLICK.Language.es));
      Assert.AreEqual("Brazillian", EMAIL.LANGS.CLICK.Get("português"));
      ExceptionAssert.Propagates<Exception>(() => EMAIL.LANGS.CLICK.Get("Bla"), exc => {
        Assert.IsTrue(exc.Message.Contains("Is not mapped"));
        Assert.IsTrue(exc.Message.Contains("Bla"));
      });
    }
    [TestMethod()]
    public void MakeFiles() {
      var names = DummyLang.MakeFileNames("dimok-{0}.txt");
      names.Select(t => t.Item1).Zip(new[] { DummyLang.Language.en, DummyLang.Language.es, DummyLang.Language.pt }, (e1, e2) => new { e1, e2 }).ForEach(x => Assert.AreEqual(x.e1, x.e2));
      names.Select(t => t.Item2).Zip(new[] { "dimok-en.txt", "dimok-es.txt", "dimok-pt.txt" }, (e1, e2) => new { e1, e2 }).ForEach(x => Assert.AreEqual(x.e1, x.e2));
    }
  }
}