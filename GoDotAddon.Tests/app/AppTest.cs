namespace Chickensoft.GoDotAddon.Tests {
  using System;
  using System.Collections.Generic;
  using System.IO.Abstractions;
  using System.IO.Abstractions.TestingHelpers;
  using System.Threading.Tasks;
  using Chickensoft.GoDotAddon;
  using CliFx.Exceptions;
  using Moq;
  using Newtonsoft.Json;
  using Shouldly;
  using Xunit;

  public class AppTest {
    private const string FILENAME = @"c:\test.txt";
    private const string DATA = "data";
    private readonly string _testData = $"{{ \"test\": \"{DATA}\" }}";

    private class TestObject {
      [JsonProperty("test")]
      public string Test { get; init; } = "";
    }

    [Fact]
    public void AppInitializes() {
      var app = new App();
      app.WorkingDir.ShouldBe(Environment.CurrentDirectory);
      app.FS.ShouldBeOfType<FileSystem>();
    }

    [Fact]
    public void AppCreatesShell() {
      var app = new App(workingDir: ".", fs: new Mock<IFileSystem>().Object);
      var shell = app.CreateShell(".");
      shell.ShouldBeOfType(typeof(Shell));
    }

    [Fact]
    public void AppLoadsFile() {
      var fs = new MockFileSystem(new Dictionary<string, MockFileData> {
        { FILENAME, new MockFileData(_testData) },
      });
      var app = new App(workingDir: ".", fs: fs);
      var file = app.LoadFile<TestObject>(FILENAME);
      file.ShouldNotBeNull();
      file.Test.ShouldBe(DATA);
    }

    [Fact]
    public void AppThrowsErrorWhenReadingFileDoesNotWork() {
      var fs = new MockFileSystem(new Dictionary<string, MockFileData> { });
      var app = new App(workingDir: ".", fs: fs);
      Should.Throw<CommandException>(
        () => Task.FromResult(app.LoadFile<TestObject>(FILENAME))
      );
    }

    [Fact]
    public void AppThrowsErrorWhenDeserializationFails() {
      var fs = new MockFileSystem(new Dictionary<string, MockFileData> {
        { FILENAME, new MockFileData("") },
      });
      var app = new App(workingDir: ".", fs: fs);
      Should.Throw<CommandException>(
        () => Task.FromResult(app.LoadFile<TestObject>(FILENAME))
      )
      .InnerException?.ShouldNotBeNull()
      .Message.ShouldBe($"Couldn't load file `{FILENAME}`");
    }

    [Fact]
    public void AppSavesFile() {
      var fs = new MockFileSystem(new Dictionary<string, MockFileData> { });
      var app = new App(workingDir: ".", fs: fs);
      app.SaveFile(FILENAME, DATA);
      var file = fs.File.ReadAllText(FILENAME);
      file.ShouldBe(DATA);
    }

    [Fact]
    public void AppThrowsErrorWhenSavingFileDoesNotWork() {
      var fs = new Mock<IFileSystem>(MockBehavior.Strict);
      var file = new Mock<IFile>(MockBehavior.Strict);
      file.Setup(f => f.WriteAllText(FILENAME, DATA)).Throws<Exception>();
      fs.Setup(fs => fs.File).Returns(file.Object);
      var app = new App(workingDir: ".", fs: fs.Object);
      Should.Throw<CommandException>(
        () => app.SaveFile(FILENAME, DATA)
      );
    }
  }
}
