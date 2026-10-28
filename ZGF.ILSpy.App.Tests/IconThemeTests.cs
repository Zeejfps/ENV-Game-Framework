using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.App.Tests.BuiltInGlyphs;
using static ZGF.ILSpy.IconTheming;
using static ZGF.ILSpy.ScreenLayout;
using static ZGF.ILSpy.Settings;

namespace ZGF.ILSpy.App.Tests;

/// <summary>Fonts and pictures held in memory by path, for icon theme files read from bytes.</summary>
internal sealed class MemoryIconAssets : IconAssets
{
    public const string Picture = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10"><rect width="10" height="10" fill="currentColor"/></svg>""";

    private readonly Dictionary<string, byte[]> _files = new();

    public MemoryIconAssets With(string path, byte[] bytes)
    {
        _files[path] = bytes;
        return this;
    }

    public MemoryIconAssets With(string path, string text) => With(path, Encoding.UTF8.GetBytes(text));

    public override IconAsset Read(string path)
    {
        if (_files.TryGetValue(path, out var bytes))
            return new IconAsset(bytes);
        return IconAsset.Failed(ThemeProblem.AssetUnreadable, path);
    }

    public override string FamilyOf(string fontId, string path) => "memory:" + path;
}

public sealed class BuiltInIconThemeTests
{
    [Fact]
    public void The_reference_icon_theme_loads_without_errors_and_draws_every_key_in_its_font()
    {
        var catalog = new IconThemeCatalog(BuiltInIconThemes.Files);
        var reference = catalog.Find(IconThemeCatalog.ReferenceId);

        Assert.Empty(BuiltInIconThemes.Errors);
        Assert.Empty(catalog.Errors);
        Assert.Equal(["lucide"], catalog.Ids);
        Assert.All(AppIcons.Keys, k => Assert.True(reference.Set.Contains(k.Key), k.Key));
        Assert.All(AppIcons.Keys, k => Assert.Equal("lucide", reference.Set.Find(k.Key).Family));
        Assert.Equal(["lucide"], reference.Fonts.Select(f => f.Family));
    }

    [Fact]
    public void Every_key_is_dotted_by_area_and_described()
    {
        Assert.All(AppIcons.Keys, k => Assert.Matches("^[a-z]+\\.[a-z][A-Za-z]*$", k.Key));
        Assert.All(AppIcons.Keys, k => Assert.NotEmpty(k.Description));
        Assert.Equal("A class.", AppIcons.Keys.Single(k => k.Key == "node.class").Description);
    }

    [Fact]
    public void Every_node_kind_draws_its_own_node_key()
    {
        var icons = BuiltInIconThemes.Reference.Icons;

        foreach (var kind in Enum.GetValues<NodeKind>())
        {
            var key = "node." + char.ToLowerInvariant(kind.ToString()[0]) + kind.ToString().Substring(1);
            Assert.Equal(BuiltInIconThemes.Reference.Set.Find(key), NodeIcons.Glyph(kind, icons));
        }
    }
}

public sealed class IconThemeLoadingTests
{
    private const int Box = 0xE061;
    private const int List = 0xE106;
    private const int TruncatedLength = 2000;

    private static readonly byte[] FontBytes = BuiltInIconThemes.Reference.Fonts[0].Data;

    private static string Glyph(int codepoint) => char.ConvertFromUtf32(codepoint);

    private static IconThemeFile User(string id, string json, MemoryIconAssets? assets = null) =>
        IconThemeFile.Read(id, id + ".json", Encoding.UTF8.GetBytes(json), assets ?? new MemoryIconAssets());

    private static IconThemeCatalog Load(params IconThemeFile[] users)
    {
        var catalog = new IconThemeCatalog(BuiltInIconThemes.Files);
        catalog.LoadUserThemes(users);
        return catalog;
    }

    private static IconGlyph Reference(string key) => BuiltInIconThemes.Reference.Set.Find(key);

    [Fact]
    public void A_partial_theme_draws_its_own_keys_and_the_reference_themes_for_the_rest()
    {
        var assets = new MemoryIconAssets().With("mine.ttf", FontBytes);
        var catalog = Load(User("mine", """
            { "fonts": { "m": "mine.ttf" },
              "icons": { "node.class": { "font": "m", "glyph": "E106" } } }
            """, assets));

        var mine = catalog.Find("mine");

        Assert.Empty(catalog.Errors);
        Assert.Equal(IconGlyph.FromFont(Glyph(List), "memory:mine.ttf"), mine.Icons.NodeClass);
        Assert.Equal(Reference("node.method"), mine.Icons.NodeMethod);
        Assert.Equal(["lucide", "memory:mine.ttf"], mine.Fonts.Select(f => f.Family));
        Assert.Equal("mine", catalog.Ids.Last());
    }

    [Fact]
    public void A_theme_draws_in_the_reference_themes_font_by_its_id()
    {
        var catalog = Load(User("swap", """{ "icons": { "node.class": { "font": "lucide", "glyph": "E106" } } }"""));

        var swap = catalog.Find("swap");

        Assert.Empty(catalog.Errors);
        Assert.Equal(Reference("node.metadataDirectory"), swap.Icons.NodeClass);
    }

    [Fact]
    public void A_child_takes_the_rest_from_the_theme_it_extends_and_redeclaring_a_font_draws_the_parents_icons_in_it()
    {
        var assets = new MemoryIconAssets()
            .With("a.ttf", FontBytes)
            .With("b.ttf", FontBytes);
        var catalog = Load(
            User("parent", """
                { "fonts": { "f": "a.ttf" },
                  "icons": { "node.class": { "font": "f", "glyph": "E106" }, "node.method": { "font": "f", "glyph": "E061" } } }
                """, assets),
            User("child", """
                { "extends": "parent", "fonts": { "f": "b.ttf" },
                  "icons": { "node.method": { "font": "lucide", "glyph": "E061" } } }
                """, assets));

        var child = catalog.Find("child");

        Assert.Empty(catalog.Errors);
        Assert.Equal(IconGlyph.FromFont(Glyph(List), "memory:b.ttf"), child.Icons.NodeClass);
        Assert.Equal(Reference("node.class"), child.Icons.NodeMethod);
        Assert.Equal(IconGlyph.FromFont(Glyph(List), "memory:a.ttf"), catalog.Find("parent").Icons.NodeClass);
    }

    [Fact]
    public void An_svg_icon_is_a_picture_whose_value_is_its_path()
    {
        var assets = new MemoryIconAssets().With("svg/class.svg", MemoryIconAssets.Picture);
        var catalog = Load(User("pictures", """{ "icons": { "node.class": { "svg": "svg/class.svg" } } }""", assets));

        var theme = catalog.Find("pictures");

        Assert.Empty(catalog.Errors);
        Assert.NotNull(theme.Icons.NodeClass.Svg);
        Assert.True(theme.Icons.NodeClass.Svg!.UsesCurrentColor);
        Assert.Equal("svg/class.svg", theme.Values["node.class"].SvgPath);
        Assert.Equal("lucide", theme.Values["node.method"].Font);
    }

    public static TheoryData<string, ThemeProblem, string, string> BadInput => new()
    {
        { """{ "icons": { "node.klass": { "font": "lucide", "glyph": "E061" } } }""", ThemeProblem.UnknownKey, "icons.node.klass", "" },
        { """{ "icons": { "node.class": { "font": "lucide", "glyph": "XYZ" } } }""", ThemeProblem.NotACodepoint, "icons.node.class", "XYZ" },
        { """{ "icons": { "node.class": { "font": "lucide", "glyph": "D800" } } }""", ThemeProblem.NotACodepoint, "icons.node.class", "D800" },
        { """{ "icons": { "node.class": { "font": "lucide", "glyph": "1234567" } } }""", ThemeProblem.NotACodepoint, "icons.node.class", "1234567" },
        { """{ "icons": { "node.class": { "font": "lucide" } } }""", ThemeProblem.InvalidIcon, "icons.node.class", "" },
        { """{ "icons": { "node.class": { "svg": "a.svg", "font": "lucide", "glyph": "E061" } } }""", ThemeProblem.InvalidIcon, "icons.node.class", "" },
        { """{ "icons": { "node.class": "E061" } }""", ThemeProblem.NotAnObject, "icons.node.class", "" },
        { """{ "icons": { "node.class": { "svg": "missing.svg" } } }""", ThemeProblem.AssetUnreadable, "icons.node.class", "missing.svg" },
        { """{ "icons": { "node.class": { "svg": "bad.svg" } } }""", ThemeProblem.InvalidSvg, "icons.node.class", "bad.svg: " },
        { """{ "fonts": { "f": "missing.ttf" } }""", ThemeProblem.AssetUnreadable, "fonts.f", "missing.ttf" },
        { """{ "fonts": { "f": "bad.svg" } }""", ThemeProblem.NotAFont, "fonts.f", "bad.svg" },
        { """{ "fonts": [] }""", ThemeProblem.NotAnObject, "fonts", "" },
        { """{ "glyphs": {} }""", ThemeProblem.UnknownKey, "glyphs", "" },
    };

    [Theory]
    [MemberData(nameof(BadInput))]
    public void Bad_input_is_reported_with_the_key_and_left_out(string json, ThemeProblem problem, string subject, string detail)
    {
        var assets = new MemoryIconAssets().With("bad.svg", "<svg");
        var file = User("bad", json, assets);

        var error = Assert.Single(file.Errors);

        Assert.Equal(problem, error.Problem);
        Assert.Equal(subject, error.Subject);
        Assert.StartsWith(detail, error.Detail);
        Assert.Equal(("bad.json", 1), (error.File, error.Line));
        Assert.Empty(file.Icons);
    }

    [Fact]
    public void An_icon_the_app_no_longer_draws_is_passed_over_without_an_error()
    {
        var file = User("older", """{ "icons": { "file.exit": { "font": "lucide", "glyph": "E10E" }, "node.class": { "font": "lucide", "glyph": "E061" } } }""");

        Assert.Empty(file.Errors);
        Assert.Equal(["node.class"], file.Icons.Keys);
    }

    [Fact]
    public void An_unknown_field_of_an_icon_is_reported_and_the_icon_kept()
    {
        var file = User("sized", """{ "icons": { "node.class": { "font": "lucide", "glyph": "E061", "size": 3 } } }""");

        var error = Assert.Single(file.Errors);

        Assert.Equal((ThemeProblem.UnknownKey, "icons.node.class.size"), (error.Problem, error.Subject));
        Assert.Equal(0xE061, file.Icons["node.class"].Codepoint);
    }

    [Fact]
    public void An_icon_in_a_font_no_theme_declares_is_reported_where_it_is_written_and_drawn_as_beneath_it()
    {
        var catalog = Load(User("orphan", """
            {
              "icons": { "node.class": { "font": "nope", "glyph": "E106" } }
            }
            """));

        var error = Assert.Single(catalog.Errors);

        Assert.Equal((ThemeProblem.UnknownFont, 2, "icons.node.class", "nope"), (error.Problem, error.Line, error.Subject, error.Detail));
        Assert.Equal(Reference("node.class"), catalog.Find("orphan").Icons.NodeClass);
    }

    [Fact]
    public void An_icon_in_a_font_that_could_not_be_read_is_drawn_as_beneath_it_and_reported_once()
    {
        var catalog = Load(User("broken", """
            { "fonts": { "f": "missing.ttf" },
              "icons": { "node.class": { "font": "f", "glyph": "E106" } } }
            """));

        var error = Assert.Single(catalog.Errors);

        Assert.Equal(ThemeProblem.AssetUnreadable, error.Problem);
        Assert.Equal(Reference("node.class"), catalog.Find("broken").Icons.NodeClass);
    }

    [Fact]
    public void A_font_cut_short_is_reported_as_no_font_and_its_icons_are_drawn_as_beneath_them()
    {
        var assets = new MemoryIconAssets().With("cut.ttf", FontBytes.AsSpan(0, TruncatedLength).ToArray());
        var catalog = Load(User("cut", """
            { "fonts": { "f": "cut.ttf" },
              "icons": { "node.class": { "font": "f", "glyph": "E106" } } }
            """, assets));

        var error = Assert.Single(catalog.Errors);

        Assert.Equal((ThemeProblem.NotAFont, 1, "fonts.f", "cut.ttf"), (error.Problem, error.Line, error.Subject, error.Detail));
        Assert.Equal(Reference("node.class"), catalog.Find("cut").Icons.NodeClass);
        Assert.Equal(["lucide"], catalog.Find("cut").Fonts.Select(f => f.Family));
    }

    [Fact]
    public void An_icon_whose_font_has_no_glyph_for_it_is_reported_and_drawn_as_beneath_it()
    {
        var catalog = Load(User("tofu", """
            {
              "icons": { "node.class": { "font": "lucide", "glyph": "41" },
                         "node.method": { "font": "lucide", "glyph": "E061" } }
            }
            """));

        var error = Assert.Single(catalog.Errors);

        Assert.Equal((ThemeProblem.MissingGlyph, 2, "icons.node.class", "0041"), (error.Problem, error.Line, error.Subject, error.Detail));
        Assert.Equal(Reference("node.class"), catalog.Find("tofu").Icons.NodeClass);
        Assert.Equal(IconGlyph.FromFont(Glyph(Box), "lucide"), catalog.Find("tofu").Icons.NodeMethod);
    }

    [Fact]
    public void Comments_and_trailing_commas_are_allowed_and_codepoints_are_hex_in_any_case()
    {
        var file = User("commented", """
            {
              // the class icon only
              "icons": {
                "node.class": { "font": "lucide", "glyph": "e061", },
              },
            }
            """);

        Assert.Empty(file.Errors);
        Assert.Equal(0xE061, file.Icons["node.class"].Codepoint);
    }

    [Theory]
    [InlineData("lucide")]
    [InlineData("Lucide")]
    public void A_user_icon_theme_with_the_reference_ones_id_is_reported_and_left_out(string id)
    {
        var catalog = Load(User(id, """{ "icons": {} }"""));

        var error = Assert.Single(catalog.Errors);

        Assert.Equal((ThemeProblem.IdTaken, id), (error.Problem, error.Subject));
        Assert.Single(catalog.Ids);
    }

    [Fact]
    public void Icon_themes_that_extend_each_other_in_a_circle_are_reported_and_fall_back_to_the_reference()
    {
        var catalog = Load(
            User("a", """{ "extends": "b" }"""),
            User("b", """{ "extends": "a" }"""));

        var cycles = catalog.Errors
            .Where(e => e.Problem == ThemeProblem.ExtendsCycle)
            .Select(e => e.Detail);

        Assert.Equal(["a → b → a", "b → a → b"], cycles);
        Assert.Same(catalog.Find("lucide"), catalog.Find("a"));
        Assert.Equal("lucide", catalog.FindShownId("b"));
    }

    [Fact]
    public void An_icon_theme_extending_no_known_theme_is_reported_and_falls_back_to_the_reference()
    {
        var catalog = Load(User("orphan", """{ "extends": "nope" }"""));

        var error = Assert.Single(catalog.Errors);

        Assert.Equal((ThemeProblem.UnknownParent, "nope"), (error.Problem, error.Subject));
        Assert.Same(catalog.Find("lucide"), catalog.Find("orphan"));
    }

    [Fact]
    public void An_unreadable_icon_theme_falls_back_to_the_theme_it_extends()
    {
        var catalog = Load(
            User("base", """{ "icons": { "node.class": { "font": "lucide", "glyph": "E106" } } }"""),
            User("broken", """
                { "extends": "base",
                  "icons": }
                """));

        var error = Assert.Single(catalog.Errors);

        Assert.Equal((ThemeProblem.InvalidJson, 2), (error.Problem, error.Line));
        Assert.Same(catalog.Find("base"), catalog.Find("broken"));
    }

    [Fact]
    public void A_name_per_language_falls_back_to_english()
    {
        var catalog = Load(User("named", """{ "name": { "en": "Shapes", "de": "Formen" } }"""));

        Assert.Equal(["Lucide", "Formen"], catalog.ListNames(CultureInfo.GetCultureInfo("de")));
        Assert.Equal("Shapes", catalog.NameOf("named", CultureInfo.GetCultureInfo("fr")));
    }
}

public sealed class IconAssetFolderTests : IDisposable
{
    private readonly TempDir _temp = new();

    public void Dispose() => _temp.Dispose();

    [Theory]
    [InlineData("../outside.svg")]
    [InlineData("svg/../../outside.svg")]
    [InlineData("C:/outside.svg")]
    [InlineData("/outside.svg")]
    public void A_path_that_leaves_the_folder_is_refused(string path)
    {
        _temp.Write("outside.svg", MemoryIconAssets.Picture);
        var folder = new IconAssetFolder(_temp.PathOf("icon-themes"));

        var asset = folder.Read(path);

        Assert.Null(asset.Bytes);
        Assert.Equal((ThemeProblem.AssetOutsideFolder, path), (asset.Problem, asset.Detail));
    }

    [Fact]
    public void A_path_no_path_may_be_is_unreadable_rather_than_thrown()
    {
        var folder = new IconAssetFolder(_temp.PathOf("icon-themes"));
        var path = "a" + (char)0 + ".svg";

        var asset = folder.Read(path);

        Assert.Null(asset.Bytes);
        Assert.Equal(ThemeProblem.AssetUnreadable, asset.Problem);
        Assert.StartsWith(path + ": ", asset.Detail);
    }

    [Fact]
    public void An_icon_theme_naming_a_path_no_path_may_be_is_reported_and_the_rest_still_loads()
    {
        var escapedNul = (char)92 + "u0000";
        _temp.Write("icon-themes/bad.json", "{ \"icons\": { \"node.class\": { \"svg\": \"a" + escapedNul + ".svg\" } } }");
        _temp.Write("icon-themes/good.json", "{}");
        var settings = _temp.Settings();
        settings.Locale = Locale.En;

        var appState = new AppState(settings);

        var error = Assert.Single(appState.Errors.Entries);
        Assert.Equal("Problems in the icon theme bad.json", error.Words().Title);
        Assert.Equal(["lucide", "bad", "good"], appState.IconThemes.Ids);
    }

    [Fact]
    public void A_path_inside_the_folder_is_read_and_a_font_is_drawn_as_a_family_of_its_own_file()
    {
        _temp.Write("icon-themes/svg/a.svg", MemoryIconAssets.Picture);
        var folder = new IconAssetFolder(_temp.PathOf("icon-themes"));

        var asset = folder.Read("svg/../svg/a.svg");

        Assert.NotNull(asset.Bytes);
        Assert.Equal("icon-font:" + _temp.PathOf(Path.Combine("icon-themes", "fonts", "f.ttf")), folder.FamilyOf("f", "fonts/f.ttf"));
    }
}

public sealed class UserIconThemeTests : IDisposable
{
    private const string ClassPicture = "icon-themes/svg/class.svg";

    private readonly TempDir _temp = new();

    public void Dispose() => _temp.Dispose();

    private string WriteIconTheme(string id, string json) => _temp.Write(Path.Combine("icon-themes", id + ".json"), json);

    private AppSettings Settings()
    {
        var settings = _temp.Settings();
        settings.Locale = Locale.En;
        return settings;
    }

    private static byte[] LucideFont() => BuiltInIconThemes.Reference.Fonts[0].Data;

    [Fact]
    public void The_icon_theme_is_lucide_by_default_and_a_chosen_one_is_saved()
    {
        var appState = new AppState(Settings());
        Assert.Equal("lucide", appState.IconTheme);
        Assert.Equal(BuiltInIconThemes.Reference.Icons.NodeClass, ResolveIconTheme(appState).Icons.NodeClass);

        Preferences.SetIconTheme(appState, "mine");
        SaveSettingsIfChanged(appState, 0);

        Assert.Equal("mine", Settings().IconTheme);
    }

    [Fact]
    public void A_missing_icon_theme_loads_as_the_default()
    {
        var path = _temp.Write("settings.json", """{ "IconTheme": null }""");

        Assert.Equal("lucide", AppSettings.Load(path).Settings.IconTheme);
    }

    [Fact]
    public void A_chosen_icon_theme_whose_file_is_gone_shows_lucide_and_stays_chosen()
    {
        var settings = Settings();
        settings.IconTheme = "gone";
        var appState = new AppState(settings);

        Assert.Same(appState.IconThemes.Find("lucide"), ResolveIconTheme(appState));
        SaveSettings(appState);
        Assert.Equal("gone", Settings().IconTheme);
    }

    [Fact]
    public void User_icon_themes_beside_the_settings_are_listed_after_the_built_in_ones_with_their_pictures()
    {
        _temp.Write(ClassPicture, MemoryIconAssets.Picture);
        WriteIconTheme("shapes", """{ "name": "Shapes", "icons": { "node.class": { "svg": "svg/class.svg" } } }""");
        var settings = Settings();
        settings.IconTheme = "shapes";
        var appState = new AppState(settings);

        Assert.Equal(["lucide", "shapes"], appState.IconThemes.Ids);
        Assert.Equal("Shapes", appState.IconThemes.ListNames(CultureInfo.InvariantCulture).Last());
        Assert.NotNull(ResolveIconTheme(appState).Icons.NodeClass.Svg);
        Assert.Empty(appState.Errors.Entries);
    }

    [Fact]
    public void A_problem_in_an_icon_theme_is_reported_once_under_its_file_until_it_changes()
    {
        WriteIconTheme("escape", """{ "icons": { "node.class": { "svg": "../class.svg" } } }""");
        var appState = new AppState(Settings());
        var error = Assert.Single(appState.Errors.Entries);

        ReloadUserIconThemes(appState);

        Assert.Single(appState.Errors.Entries);
        Assert.Equal("Problems in the icon theme escape.json", error.Words().Title);
        Assert.EndsWith("escape.json(1): icons.node.class: ../class.svg is not inside the theme's folder", error.Words().Details);
    }

    [Fact]
    public void The_icon_themes_folder_is_made_with_the_schema_and_a_readme_that_are_no_themes()
    {
        var appState = new AppState(Settings());

        var folder = PrepareIconThemesFolder(appState);
        ReloadUserIconThemes(appState);

        Assert.Equal(_temp.PathOf("icon-themes"), folder);
        Assert.Equal(BuiltInIconThemes.ReadSchema(), File.ReadAllText(Path.Combine(folder!, "icon-theme.schema.json")));
        Assert.Equal(BuiltInIconThemes.ReadGuide(), File.ReadAllText(Path.Combine(folder!, "README.md")));
        Assert.Single(appState.IconThemes.Ids);
        Assert.Empty(appState.Errors.Entries);
    }

    [Fact]
    public void A_custom_icon_theme_extends_the_one_shown_lists_every_icon_and_is_switched_to()
    {
        var appState = new AppState(Settings());

        var path = CreateCustomIconTheme(appState);

        var text = File.ReadAllText(path!);
        Assert.Equal(_temp.PathOf(Path.Combine("icon-themes", "lucide-custom.json")), path);
        Assert.Equal("lucide-custom", appState.IconTheme);
        Assert.Empty(appState.Errors.Entries);
        Assert.Equal("Lucide (custom)", appState.IconThemes.NameOf("lucide-custom", CultureInfo.GetCultureInfo("en")));
        Assert.Equal(BuiltInIconThemes.Reference.Icons.ToolbarBack, ResolveIconTheme(appState).Icons.ToolbarBack);
        Assert.Contains("\"extends\": \"lucide\",", text);
        Assert.Contains("// \"toolbar.back\": { \"font\": \"lucide\", \"glyph\": \"E048\" },", text);
        Assert.Equal(AppIcons.Keys.Count, CountCommentedIcons(text));
    }

    private static int CountCommentedIcons(string text) => text.Split('\n').Count(IsCommentedIcon);

    private static bool IsCommentedIcon(string line) => line.TrimStart().StartsWith("// \"", StringComparison.Ordinal);

    [Fact]
    public void Each_icon_of_a_custom_icon_theme_can_be_uncommented_alone_and_a_picture_keeps_its_path()
    {
        _temp.Write(ClassPicture, MemoryIconAssets.Picture);
        WriteIconTheme("shapes", """{ "icons": { "node.class": { "svg": "svg/class.svg" } } }""");
        var appState = new AppState(Settings());
        var catalog = appState.IconThemes;
        var shapes = catalog.Find("shapes");
        var lines = IconThemeDraft.Compose("Mine", "shapes", shapes).Split('\n');
        var assets = new IconAssetFolder(_temp.PathOf("icon-themes"));

        for (var i = 0; i < lines.Length; i++)
        {
            if (!IsCommentedIcon(lines[i]))
                continue;
            var edited = lines.ToArray();
            edited[i] = lines[i].Replace("// ", "");
            var file = IconThemeFile.Read("mine", "mine.json", Encoding.UTF8.GetBytes(string.Join('\n', edited)), assets);
            catalog.LoadUserThemes([IconThemeFile.ReadFile(_temp.PathOf("icon-themes/shapes.json")), file]);
            Assert.True(catalog.Errors.Count == 0, edited[i] + ": " + string.Join("; ", catalog.Errors));
        }
        Assert.Contains(lines, l => l.Trim() == "// \"node.class\": { \"svg\": \"svg/class.svg\" },");
    }

    [Fact]
    public void No_custom_icon_theme_is_made_while_the_settings_live_in_memory()
    {
        var appState = new AppState(new AppSettings { Locale = Locale.En });

        Assert.Null(CreateCustomIconTheme(appState));
        Assert.Null(PrepareIconThemesFolder(appState));
        Assert.Equal("lucide", appState.IconTheme);
    }

    [Fact]
    public void Saving_a_picture_of_the_chosen_icon_theme_draws_it_at_once()
    {
        _temp.Write(ClassPicture, MemoryIconAssets.Picture);
        WriteIconTheme("live", """{ "icons": { "node.class": { "svg": "svg/class.svg" } } }""");
        var settings = Settings();
        settings.IconTheme = "live";
        var d = new AppStateDriver(settings);
        WatchUserIconThemes(d.AppState);
        var before = ResolveIconTheme(d.AppState).Icons.NodeClass.Svg;

        _temp.Write(ClassPicture, MemoryIconAssets.Picture.Replace("currentColor", "#FF0000"));
        d.RunUntil(() => ResolveIconTheme(d.AppState).Icons.NodeClass.Svg != before);
        StopWatchingUserIconThemes(d.AppState);

        Assert.False(ResolveIconTheme(d.AppState).Icons.NodeClass.Svg!.UsesCurrentColor);
        Assert.Empty(d.AppState.Errors.Entries);
    }

    [Fact]
    public void Saving_a_font_of_the_chosen_icon_theme_registers_it_again_with_the_screen()
    {
        var font = _temp.Write("icon-themes/fonts/mine.ttf", LucideFont());
        WriteIconTheme("live", """{ "fonts": { "m": "fonts/mine.ttf" }, "icons": { "node.class": { "font": "m", "glyph": "E106" } } }""");
        var settings = Settings();
        settings.IconTheme = "live";
        var app = new AppDriver(settings: settings);
        WatchUserIconThemes(app.AppState);
        app.Run(2);
        var family = "icon-font:" + Path.GetFullPath(font);
        Assert.Contains(app.Frames.RegisteredFonts, f => f.Family == family);
        var registered = app.Frames.RegisteredFonts.Count;

        File.WriteAllBytes(font, LucideFont().Append((byte)0).ToArray());
        app.RunUntil(() => app.Frames.RegisteredFonts.Count > registered);
        StopWatchingUserIconThemes(app.AppState);

        Assert.Equal(LucideFont().Length + 1, app.Frames.RegisteredFonts.Last(f => f.Family == family).Data.Length);
        Assert.Equal(family, app.Ui.Icons.NodeClass.Family);
    }

    [Fact]
    public void Leaving_an_icon_theme_lets_go_of_its_fonts_but_never_of_lucides()
    {
        var font = _temp.Write("icon-themes/fonts/mine.ttf", LucideFont());
        WriteIconTheme("mine", """{ "fonts": { "m": "fonts/mine.ttf" }, "icons": { "node.class": { "font": "m", "glyph": "E106" } } }""");
        var settings = Settings();
        settings.IconTheme = "mine";
        var app = new AppDriver(settings: settings);
        app.Run(2);
        Assert.Empty(app.Frames.UnregisteredFonts);

        Preferences.SetIconTheme(app.AppState, "lucide");
        app.Run(2);

        Assert.Equal(["icon-font:" + Path.GetFullPath(font)], app.Frames.UnregisteredFonts);
        Assert.Equal("lucide", app.Ui.Icons.NodeClass.Family);
    }

    [Fact]
    public void The_icon_theme_is_picked_from_the_display_options_with_its_buttons()
    {
        _temp.Write(ClassPicture, MemoryIconAssets.Picture);
        WriteIconTheme("shapes", """{ "name": "Shapes", "icons": { "node.class": { "svg": "svg/class.svg" } } }""");
        var app = new AppDriver(width: 1600f, settings: Settings());
        OpenDisplayOptions(app.Ui);
        app.Run();

        app.ClickStartingWith("Lu").Frame();
        app.Click("Shapes").Frame(2);

        Assert.Equal("shapes", app.AppState.IconTheme);
        Assert.NotNull(app.Ui.Icons.NodeClass.Svg);
        Assert.True(app.Frames.Drew(Text(Icons.OptionsCustomizeIconTheme)));
    }

    [Fact]
    public void Customizing_the_icon_theme_switches_to_a_new_one_and_opens_its_file()
    {
        var app = new AppDriver(width: 1600f, settings: Settings());
        OpenDisplayOptions(app.Ui);
        app.Run();

        app.ClickLast(Icons.OptionsCustomizeIconTheme).Frame();

        Assert.Equal("lucide-custom", app.AppState.IconTheme);
        Assert.Equal([_temp.PathOf(Path.Combine("icon-themes", "lucide-custom.json"))], app.Frames.OpenedFiles);
    }

    [Fact]
    public void The_icon_theme_buttons_hide_while_the_settings_live_in_memory()
    {
        var app = new AppDriver(width: 1600f);
        OpenDisplayOptions(app.Ui);
        app.Run();

        Assert.True(app.Frames.Drew("Icon theme"));
        Assert.False(app.Frames.Drew(Text(Icons.OptionsCustomizeIconTheme)));
    }
}

public sealed class IconThemeSchemaTests
{
    private const string CodepointPattern = "^[0-9A-Fa-f]{1,6}$";

    [Fact]
    public void The_checked_in_schema_lists_every_icon_key()
    {
        var generated = Generate();
        var checkedIn = BuiltInIconThemes.ReadSchema().ReplaceLineEndings("\n");

        if (generated != checkedIn)
        {
            var received = Path.Combine(Path.GetTempPath(), "icon-theme.schema.received.json");
            File.WriteAllText(received, generated);
            Assert.Fail($"ZGF.ILSpy/Assets/IconThemes/icon-theme.schema.json is out of date; the generated schema is at {received}.");
        }
    }

    private static string Generate()
    {
        using var stream = new MemoryStream();
        var options = new JsonWriterOptions { Indented = true, NewLine = "\n", Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        using (var writer = new Utf8JsonWriter(stream, options))
            WriteSchema(writer);
        return Encoding.UTF8.GetString(stream.ToArray()) + "\n";
    }

    private static void WriteSchema(Utf8JsonWriter w)
    {
        w.WriteStartObject();
        w.WriteString("$schema", "http://json-schema.org/draft-07/schema#");
        w.WriteString("title", "ILSpy icon theme");
        w.WriteString("description", "An icon theme: what each icon draws, a glyph of a font or an SVG picture. Its colors come from the color theme. Comments and trailing commas are allowed. The file's name without .json is the theme's id.");
        w.WriteString("type", "object");
        w.WriteBoolean("additionalProperties", false);
        w.WriteStartObject("properties");
        WriteString(w, "$schema", "The schema icon theme files follow.");
        WriteName(w);
        WriteString(w, "extends", "The id of a built-in or user icon theme to build on; what this theme leaves out comes from it, and what no theme sets from lucide.");
        WriteFonts(w);
        WriteIcons(w);
        w.WriteEndObject();
        WriteDefinitions(w);
        w.WriteEndObject();
    }

    private static void WriteString(Utf8JsonWriter w, string name, string description)
    {
        w.WriteStartObject(name);
        w.WriteString("description", description);
        w.WriteString("type", "string");
        w.WriteEndObject();
    }

    private static void WriteName(Utf8JsonWriter w)
    {
        w.WriteStartObject("name");
        w.WriteString("description", "The name the icon theme is listed by: one for every language, or one per language tag, such as en or pt-BR, where English stands in for a language without one.");
        w.WriteStartArray("oneOf");
        w.WriteStartObject();
        w.WriteString("type", "string");
        w.WriteEndObject();
        w.WriteStartObject();
        w.WriteString("type", "object");
        w.WriteStartObject("additionalProperties");
        w.WriteString("type", "string");
        w.WriteEndObject();
        w.WriteEndObject();
        w.WriteEndArray();
        w.WriteEndObject();
    }

    private static void WriteFonts(Utf8JsonWriter w)
    {
        w.WriteStartObject("fonts");
        w.WriteString("description", "TrueType or OpenType font files by id, for icons to name. A path is relative to this file and must stay inside its folder. The fonts of the themes extended, and lucide's font, lucide, can be named too; declaring one's id again draws its icons in this font.");
        w.WriteString("type", "object");
        w.WriteStartObject("additionalProperties");
        w.WriteString("type", "string");
        w.WriteEndObject();
        w.WriteEndObject();
    }

    private static void WriteIcons(Utf8JsonWriter w)
    {
        w.WriteStartObject("icons");
        w.WriteString("description", "What each icon draws. Those left out come from the theme extended, else from lucide.");
        w.WriteString("type", "object");
        w.WriteBoolean("additionalProperties", false);
        w.WriteStartObject("properties");
        foreach (var key in AppIcons.Keys)
            WriteIconKey(w, key);
        w.WriteEndObject();
        w.WriteEndObject();
    }

    private static void WriteIconKey(Utf8JsonWriter w, IconKey key)
    {
        w.WriteStartObject(key.Key);
        w.WriteString("description", key.Description);
        w.WriteString("$ref", "#/definitions/icon");
        w.WriteEndObject();
    }

    private static void WriteDefinitions(Utf8JsonWriter w)
    {
        w.WriteStartObject("definitions");
        w.WriteStartObject("icon");
        w.WriteStartArray("oneOf");
        WriteGlyphIcon(w);
        WriteSvgIcon(w);
        w.WriteEndArray();
        w.WriteEndObject();
        w.WriteEndObject();
    }

    private static void WriteGlyphIcon(Utf8JsonWriter w)
    {
        w.WriteStartObject();
        w.WriteString("description", "A glyph of a font.");
        w.WriteString("type", "object");
        w.WriteBoolean("additionalProperties", false);
        w.WriteStartArray("required");
        w.WriteStringValue("font");
        w.WriteStringValue("glyph");
        w.WriteEndArray();
        w.WriteStartObject("properties");
        WriteString(w, "font", "The id of a font in fonts.");
        w.WriteStartObject("glyph");
        w.WriteString("description", "The glyph's codepoint in hex digits, such as E0F2.");
        w.WriteString("type", "string");
        w.WriteString("pattern", CodepointPattern);
        w.WriteEndObject();
        w.WriteEndObject();
        w.WriteEndObject();
    }

    private static void WriteSvgIcon(Utf8JsonWriter w)
    {
        w.WriteStartObject();
        w.WriteString("description", "An SVG picture. One that uses currentColor is drawn in the icon's color; one that does not keeps its own colors.");
        w.WriteString("type", "object");
        w.WriteBoolean("additionalProperties", false);
        w.WriteStartArray("required");
        w.WriteStringValue("svg");
        w.WriteEndArray();
        w.WriteStartObject("properties");
        WriteString(w, "svg", "The SVG file, relative to this file and inside its folder.");
        w.WriteEndObject();
        w.WriteEndObject();
    }
}
