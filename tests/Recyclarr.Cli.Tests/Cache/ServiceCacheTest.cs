using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Cache;
using Recyclarr.Cli.Pipelines.CustomFormat.Cache;

namespace Recyclarr.Cli.Tests.Cache;

[TestFixture]
public class ServiceCacheTest
{
    [SuppressMessage("SonarLint", "S2094", Justification =
        "Used for unit test scenario")]
    private sealed class ObjectWithoutAttribute;

    private const string ValidObjectName = "azAZ_09";

    [CacheObjectName(ValidObjectName)]
    private sealed class ObjectWithAttribute
    {
        public string TestValue { get; init; } = "";
    }

    [CacheObjectName("invalid+name")]
    private sealed class ObjectWithAttributeInvalidChars;

    [Test, AutoMockData]
    public void Load_returns_null_when_file_does_not_exist(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        ServiceCache sut)
    {
        var result = sut.Load<ObjectWithAttribute>();
        result.Should().BeNull();
    }

    [Test, AutoMockData]
    public void Loading_with_attribute_parses_correctly(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] ICacheStoragePath storage,
        ServiceCache sut)
    {
        const string testJson =
            """
            {"test_value": "Foo"}
            """;

        const string testJsonPath = "cacheFile.json";
        fs.AddFile(testJsonPath, new MockFileData(testJson));

        storage.CalculatePath(default!).ReturnsForAnyArgs(fs.FileInfo.New(testJsonPath));

        var obj = sut.Load<ObjectWithAttribute>();

        obj.Should().NotBeNull();
        obj!.TestValue.Should().Be("Foo");
    }

    [Test, AutoMockData]
    public void Loading_with_invalid_object_name_throws(
        ServiceCache sut)
    {
        Action act = () => sut.Load<ObjectWithAttributeInvalidChars>();

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*'invalid+name' has unacceptable characters*");
    }

    [Test, AutoMockData]
    public void Loading_without_attribute_throws(
        ServiceCache sut)
    {
        Action act = () => sut.Load<ObjectWithoutAttribute>();

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("CacheObjectNameAttribute is missing*");
    }

    [Test, AutoMockData]
    public void Properties_are_saved_using_snake_case(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] ICacheStoragePath storage,
        ServiceCache sut)
    {
        storage.CalculatePath(default!)
            .ReturnsForAnyArgs(_ => fs.FileInfo.New($"{ValidObjectName}.json"));

        sut.Save(new ObjectWithAttribute {TestValue = "Foo"});

        fs.AllFiles.Should().ContainMatch($"*{ValidObjectName}.json");

        var file = fs.GetFile(storage.CalculatePath("").FullName);
        file.Should().NotBeNull();
        file.TextContents.Should().Contain("\"test_value\"");
    }

    [Test, AutoMockData]
    public void Saving_with_attribute_parses_correctly(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] ICacheStoragePath storage,
        ServiceCache sut)
    {
        const string testJsonPath = "cacheFile.json";
        storage.CalculatePath(default!).ReturnsForAnyArgs(fs.FileInfo.New(testJsonPath));

        sut.Save(new ObjectWithAttribute {TestValue = "Foo"});

        var expectedFile = fs.GetFile(testJsonPath);
        expectedFile.Should().NotBeNull();
        expectedFile.TextContents.Should().Be(
            """
            {
              "test_value": "Foo"
            }
            """);
    }

    [Test, AutoMockData]
    public void Saving_with_invalid_object_name_throws(
        ServiceCache sut)
    {
        var act = () => sut.Save(new ObjectWithAttributeInvalidChars());

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*'invalid+name' has unacceptable characters*");
    }

    [Test, AutoMockData]
    public void Saving_without_attribute_throws(
        ServiceCache sut)
    {
        var act = () => sut.Save(new ObjectWithoutAttribute());

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("CacheObjectNameAttribute is missing*");
    }

    [Test, AutoMockData]
    public void Switching_config_and_base_url_should_yield_different_cache_paths(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] ICacheStoragePath storage,
        ServiceCache sut)
    {
        storage.CalculatePath(default!).ReturnsForAnyArgs(fs.FileInfo.New("Foo.json"));
        sut.Save(new ObjectWithAttribute {TestValue = "Foo"});

        storage.CalculatePath(default!).ReturnsForAnyArgs(fs.FileInfo.New("Bar.json"));
        sut.Save(new ObjectWithAttribute {TestValue = "Bar"});

        var expectedFiles = new[] {"*Foo.json", "*Bar.json"};
        foreach (var expectedFile in expectedFiles)
        {
            fs.AllFiles.Should().ContainMatch(expectedFile);
        }
    }

    [Test, AutoMockData]
    public void When_cache_file_is_empty_do_not_throw(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] ICacheStoragePath storage,
        ServiceCache sut)
    {
        storage.CalculatePath(default!).ReturnsForAnyArgs(fs.FileInfo.New("cacheFile.json"));
        fs.AddFile("cacheFile.json", new MockFileData(""));

        Action act = () => sut.Load<ObjectWithAttribute>();

        act.Should().NotThrow();
    }

    [Test, AutoMockData]
    public void Name_properties_are_set_on_load(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] ICacheStoragePath storage,
        ServiceCache sut)
    {
        const string cacheJson =
            """
            {
              "version": 1,
              "trash_id_mappings": [
                {
                  "custom_format_name": "4K Remaster",
                  "trash_id": "eca37840c13c6ef2dd0262b141a5482f",
                  "custom_format_id": 4
                }
              ]
            }
            """;

        fs.AddFile("cacheFile.json", new MockFileData(cacheJson));
        storage.CalculatePath(default!).ReturnsForAnyArgs(fs.FileInfo.New("cacheFile.json"));

        var result = sut.Load<CustomFormatCacheData>();

        result.Should().BeEquivalentTo(new
        {
            TrashIdMappings = new Collection<TrashIdMapping>
            {
                new("eca37840c13c6ef2dd0262b141a5482f", "4K Remaster", 4)
            }
        });
    }
}
