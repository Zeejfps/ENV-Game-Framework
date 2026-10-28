using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Settings;
using static ZGF.ILSpy.Theming;

namespace ZGF.ILSpy.App.Tests;

public sealed class BuiltInThemeTests
{
    public static TheoryData<string> Ids
    {
        get
        {
            var ids = new TheoryData<string>();
            foreach (var file in BuiltInThemes.Files)
                ids.Add(file.Id);
            return ids;
        }
    }

    [Fact]
    public void The_built_in_themes_load_without_errors_the_default_ones_first()
    {
        var catalog = new ThemeCatalog(BuiltInThemes.Files);

        Assert.Empty(BuiltInThemes.Errors);
        Assert.Empty(catalog.Errors);
        Assert.Equal(
            ["dark", "light", "catppuccin-mocha", "dracula", "github-dark", "monokai", "one-dark-pro", "github-light", "solarized-light"],
            catalog.Ids);
    }

    [Theory]
    [MemberData(nameof(Ids))]
    public void Every_built_in_theme_keeps_its_text_readable(string id)
    {
        var theme = Resolve(id).Ui;

        Assert.InRange(Contrast(theme.Text, theme.Background), 4.5f, 21f);
        Assert.InRange(Contrast(theme.Text, theme.EditorBackground), 4.5f, 21f);
        Assert.InRange(Contrast(theme.TextMuted, theme.Background), 4.5f, 21f);
        Assert.InRange(Contrast(theme.TextMuted, theme.Surface), 4.5f, 21f);
        Assert.InRange(Contrast(theme.SelectionText, theme.Selection), 4.5f, 21f);
        Assert.InRange(Contrast(theme.Text, theme.TextSelection), 3.5f, 21f);
        Assert.InRange(Contrast(theme.OnAccent, theme.Accent), 4.5f, 21f);
        Assert.InRange(Contrast(theme.AccentText, theme.Background), 4.5f, 21f);
        Assert.Equal(TypeOf(id) == ThemeType.Dark, theme.IsDark);
    }

    [Theory]
    [MemberData(nameof(Ids))]
    public void Every_built_in_theme_shows_selected_text_on_a_selected_row(string id)
    {
        var theme = Resolve(id).Ui;

        Assert.InRange(Contrast(theme.TextSelection, theme.Selection), 1.15f, 21f);
        Assert.InRange(Contrast(theme.TextSelection, theme.SelectionInactive), 1.15f, 21f);
        Assert.InRange(Contrast(theme.TextSelection, theme.Background), 1.15f, 21f);
    }

    [Theory]
    [MemberData(nameof(Ids))]
    public void Every_built_in_theme_colors_every_syntax_kind_and_icon(string id)
    {
        var theme = Resolve(id);

        foreach (var kind in Enum.GetValues<SpanKind>())
            Assert.NotEqual(0u, theme.Code[kind]);
        foreach (var kind in Enum.GetValues<NodeKind>())
            Assert.NotEqual(0u, theme.Icons.For(kind));
        Assert.NotEqual(0u, theme.Icons.Restricted);
        Assert.NotEqual(0u, theme.Code.CurrentLine);
        Assert.NotEqual(0u, theme.Code.Bookmark);
    }

    [Fact]
    public void The_default_themes_are_named_in_the_interface_language_and_the_others_by_their_proper_names()
    {
        var catalog = new ThemeCatalog(BuiltInThemes.Files);

        var german = catalog.ListNames(Strings.For(Locale.De).Culture);
        var japanese = catalog.ListNames(Strings.For(Locale.Ja).Culture);

        Assert.Equal(["Dunkel", "Hell"], german.Take(2));
        Assert.Equal(["ダーク", "ライト"], japanese.Take(2));
        Assert.Contains("GitHub Dark", german);
        Assert.Contains("GitHub Dark", japanese);
    }

    private static AppTheme Resolve(string id) => new ThemeCatalog(BuiltInThemes.Files).Find(id);

    private static ThemeType? TypeOf(string id) => BuiltInThemes.Files.Single(f => f.Id == id).Type;

    private static float Contrast(uint a, uint b)
    {
        var (la, lb) = (UiColor.Luminance(a), UiColor.Luminance(b));
        return (Math.Max(la, lb) + 0.05f) / (Math.Min(la, lb) + 0.05f);
    }
}

public sealed class ThemeLoadingTests
{
    private const string BaseRoles = """
        "background": "#101010", "surface": "#141414", "control": "#202020", "field": "#0C0C0C", "border": "#404040",
        "text": "#F0F0F0", "textMuted": "#B0B0B0", "accent": "#3070F0", "onAccent": "#FFFFFF"
        """;

    private static ThemeFile User(string id, string json) => ThemeFile.Read(id, id + ".json", Encoding.UTF8.GetBytes(json));

    private static ThemeCatalog Load(params ThemeFile[] users)
    {
        var catalog = new ThemeCatalog(BuiltInThemes.Files);
        catalog.LoadUserThemes(users);
        return catalog;
    }

    [Fact]
    public void A_partial_theme_takes_the_rest_from_the_theme_it_extends()
    {
        var catalog = Load(User("my-dracula", """
            { "extends": "dracula", "ui": { "accent": "#FF0000" }, "code": { "keyword": "#00FF00" } }
            """));
        var dracula = catalog.Find("dracula");

        var mine = catalog.Find("my-dracula");

        Assert.Empty(catalog.Errors);
        Assert.Equal(0xFFFF0000u, mine.Ui.Accent);
        Assert.Equal(dracula.Ui.Background, mine.Ui.Background);
        Assert.Equal(0xFF00FF00u, mine.Code.Keyword);
        Assert.Equal(dracula.Code.String, mine.Code.String);
        Assert.Equal(dracula.Icons.Type, mine.Icons.Type);
        Assert.Equal("my-dracula", catalog.Ids.Last());
    }

    [Fact]
    public void A_child_that_changes_a_base_role_gets_matching_derived_colors()
    {
        var catalog = Load(User("dim", """{ "extends": "github-dark", "ui": { "control": "#505050" } }"""));

        var dim = catalog.Find("dim").Ui;

        Assert.Equal(UiColor.Mix(0xFF505050, dim.Text, 0.06f), dim.ControlHovered);
        Assert.Equal(UiColor.Mix(0xFF505050, dim.Text, 0.12f), dim.ControlPressed);
    }

    [Fact]
    public void Palette_names_take_the_palette_merged_over_the_extended_themes()
    {
        var catalog = Load(
            User("base", $$"""{ "type": "dark", "palette": { "bg": "#111111" }, "ui": { {{BaseRoles}}, "editorBackground": "bg" } }"""),
            User("child", """{ "extends": "base", "palette": { "bg": "#222222" } }"""));

        Assert.Empty(catalog.Errors);
        Assert.Equal(0xFF111111u, catalog.Find("base").Ui.EditorBackground);
        Assert.Equal(0xFF222222u, catalog.Find("child").Ui.EditorBackground);
    }

    [Fact]
    public void A_theme_that_extends_none_takes_its_code_and_icon_colors_from_the_built_in_one_of_its_type()
    {
        var catalog = Load(User("paper", $$"""{ "type": "light", "ui": { {{BaseRoles}} } }"""));
        var light = catalog.Find("light");

        var paper = catalog.Find("paper");

        Assert.Empty(catalog.Errors);
        Assert.Equal(0xFF101010u, paper.Ui.Background);
        Assert.Equal(UiColor.Mix(0xFF202020, 0xFFF0F0F0, 0.06f), paper.Ui.ControlHovered);
        Assert.Equal(light.Code.Keyword, paper.Code.Keyword);
        Assert.Equal(light.Icons.Method, paper.Icons.Method);
    }

    [Fact]
    public void A_theme_missing_base_roles_is_reported_and_falls_back_to_the_built_in_one_of_its_type()
    {
        var catalog = Load(User("sparse", """{ "type": "light", "ui": { "background": "#FFFFFF" } }"""));

        var error = Assert.Single(catalog.Errors);

        Assert.Equal(ThemeProblem.MissingColors, error.Problem);
        Assert.Equal("ui.surface, ui.control, ui.field, ui.border, ui.text, ui.textMuted, ui.accent, ui.onAccent", error.Detail);
        Assert.Same(catalog.Find("light"), catalog.Find("sparse"));
    }

    [Fact]
    public void A_theme_that_cannot_be_read_falls_back_to_the_theme_it_extends()
    {
        var catalog = Load(User("broken", """
            { "extends": "monokai",
              "ui": { "accent": } }
            """));

        var error = Assert.Single(catalog.Errors);

        Assert.Equal(ThemeProblem.InvalidJson, error.Problem);
        Assert.Equal(2, error.Line);
        Assert.Same(catalog.Find("monokai"), catalog.Find("broken"));
    }

    public static TheoryData<string, ThemeProblem, string> BadInput => new()
    {
        { """[]""", ThemeProblem.RootNotAnObject, "" },
        { """{ "colour": "#FFFFFF" }""", ThemeProblem.UnknownKey, "colour" },
        { """{ "ui": { "backgroundd": "#FFFFFF" } }""", ThemeProblem.UnknownKey, "ui.backgroundd" },
        { """{ "ui": { "background": "#FFFFF" } }""", ThemeProblem.NotAColor, "ui.background" },
        { """{ "palette": { "bg": "bg2" } }""", ThemeProblem.NotAColor, "palette.bg" },
        { """{ "ui": { "background": "#00000000" } }""", ThemeProblem.TransparentBlack, "ui.background" },
        { """{ "ui": { "background": 3 } }""", ThemeProblem.NotAString, "ui.background" },
        { """{ "ui": [] }""", ThemeProblem.NotAnObject, "ui" },
        { """{ "type": "dim" }""", ThemeProblem.UnknownType, "dim" },
        { """{ "name": 3 }""", ThemeProblem.InvalidName, "" },
        { """{ "name": { "de": 3 } }""", ThemeProblem.NotAString, "name.de" },
    };

    [Theory]
    [MemberData(nameof(BadInput))]
    public void Bad_input_is_reported_with_the_key_and_left_out(string json, ThemeProblem problem, string subject)
    {
        var file = User("bad", json);

        var error = Assert.Single(file.Errors);

        Assert.Equal(problem, error.Problem);
        Assert.Equal(subject, error.Subject);
        Assert.Equal("bad.json", error.File);
        Assert.Equal(1, error.Line);
        Assert.Empty(file.Ui);
    }

    [Fact]
    public void Comments_and_trailing_commas_are_allowed_and_errors_tell_their_line()
    {
        var file = User("commented", """
            {
              // builds on the default
              "extends": "dark",
              "ui": {
                "accent": "#12345",
                "text": "#EEEEEE",
              },
            }
            """);

        var error = Assert.Single(file.Errors);

        Assert.False(file.IsUnreadable);
        Assert.Equal((ThemeProblem.NotAColor, 5, "#12345"), (error.Problem, error.Line, error.Detail));
        Assert.Equal(["text"], file.Ui.Keys);
    }

    [Fact]
    public void An_unknown_palette_name_is_reported_where_it_is_written_and_the_color_is_left_to_the_parent()
    {
        var catalog = Load(User("named", """
            { "extends": "dark",
              "ui": { "accent": "purple" } }
            """));

        var error = Assert.Single(catalog.Errors);

        Assert.Equal((ThemeProblem.UnknownPaletteName, 2, "ui.accent", "purple"), (error.Problem, error.Line, error.Subject, error.Detail));
        Assert.Equal(catalog.Find("dark").Ui.Accent, catalog.Find("named").Ui.Accent);
    }

    [Theory]
    [InlineData("dark")]
    [InlineData("Dark")]
    public void A_user_theme_with_a_built_in_ones_id_is_reported_and_left_out(string id)
    {
        var catalog = Load(User(id, """{ "extends": "light" }"""));

        var error = Assert.Single(catalog.Errors);

        Assert.Equal((ThemeProblem.IdTaken, id), (error.Problem, error.Subject));
        Assert.Equal(BuiltInThemes.Files.Count, catalog.Ids.Count);
        Assert.True(catalog.Find("dark").Ui.IsDark);
    }

    [Fact]
    public void Themes_that_extend_each_other_in_a_circle_are_reported_and_fall_back()
    {
        var catalog = Load(
            User("a", """{ "type": "dark", "extends": "b" }"""),
            User("b", """{ "type": "light", "extends": "a" }"""));

        var cycles = catalog.Errors
            .Where(e => e.Problem == ThemeProblem.ExtendsCycle)
            .Select(e => e.Detail);

        Assert.Equal(["a → b → a", "b → a → b"], cycles);
        Assert.Same(catalog.Find("light"), catalog.Find("a"));
        Assert.Same(catalog.Find("light"), catalog.Find("b"));
    }

    [Fact]
    public void A_theme_extending_no_known_theme_is_reported_and_falls_back_by_its_type()
    {
        var catalog = Load(User("orphan", """
            { "type": "light",
              "extends": "nope" }
            """));

        var error = Assert.Single(catalog.Errors);

        Assert.Equal((ThemeProblem.UnknownParent, 2, "nope"), (error.Problem, error.Line, error.Subject));
        Assert.Same(catalog.Find("light"), catalog.Find("orphan"));
    }

    [Fact]
    public void A_theme_with_neither_a_type_nor_a_parent_is_reported_and_falls_back_to_dark()
    {
        var catalog = Load(User("typeless", $$"""{ "ui": { {{BaseRoles}} } }"""));

        var error = Assert.Single(catalog.Errors);

        Assert.Equal(ThemeProblem.NoType, error.Problem);
        Assert.Same(catalog.Find("dark"), catalog.Find("typeless"));
    }

    [Fact]
    public void An_id_no_theme_has_shows_the_dark_theme()
    {
        var catalog = Load();

        Assert.Same(catalog.Find("dark"), catalog.Find("gone"));
        Assert.Equal(-1, catalog.IndexOf("gone"));
    }

    [Fact]
    public void A_language_named_twice_in_any_case_is_reported_and_its_first_name_kept()
    {
        var file = User("twice", """
            { "extends": "dark",
              "name": { "en": "Night",
                        "EN": "Nacht" } }
            """);
        var catalog = Load(file);

        var error = Assert.Single(catalog.Errors);

        Assert.Equal((ThemeProblem.DuplicateLanguage, 3, "name.EN"), (error.Problem, error.Line, error.Subject));
        Assert.Equal("Night", file.Name.In(CultureInfo.GetCultureInfo("en")));
    }

    [Fact]
    public void A_name_per_language_falls_back_to_english()
    {
        var catalog = Load(User("named", """{ "extends": "dark", "name": { "en": "Night", "de": "Nacht" } }"""));

        Assert.Equal("Nacht", catalog.ListNames(CultureInfo.GetCultureInfo("de")).Last());
        Assert.Equal("Night", catalog.ListNames(CultureInfo.GetCultureInfo("fr")).Last());
    }
}

public sealed class UserThemeTests : IDisposable
{
    private readonly TempDir _temp = new();

    public void Dispose() => _temp.Dispose();

    private string WriteTheme(string id, string json) => _temp.Write(Path.Combine("themes", id + ".json"), json);

    private AppSettings Settings()
    {
        var settings = _temp.Settings();
        settings.Locale = Locale.En;
        return settings;
    }

    [Theory]
    [InlineData("GitHubDark", "github-dark")]
    [InlineData("SolarizedLight", "solarized-light")]
    [InlineData("Dark", "dark")]
    [InlineData("my-theme", "my-theme")]
    public void A_saved_theme_loads_as_the_id_of_its_file(string saved, string id)
    {
        var path = _temp.Write("settings.json", $$"""{ "Theme": "{{saved}}" }""");

        Assert.Equal(id, AppSettings.Load(path).Settings.Theme);
    }

    [Fact]
    public void A_missing_theme_loads_as_the_default()
    {
        var path = _temp.Write("settings.json", """{ "Theme": null }""");

        Assert.Equal("dark", AppSettings.Load(path).Settings.Theme);
    }

    [Fact]
    public void User_themes_in_the_folder_beside_the_settings_are_listed_after_the_built_in_ones()
    {
        WriteTheme("my-dracula", """{ "name": "My Dracula", "extends": "dracula", "ui": { "accent": "#FF0000" } }""");
        var appState = new AppState(Settings());

        Assert.Equal("my-dracula", appState.Themes.Ids.Last());
        Assert.Equal("My Dracula", appState.Themes.ListNames(CultureInfo.InvariantCulture).Last());
        Assert.Empty(appState.Errors.Entries);
    }

    [Fact]
    public void A_chosen_theme_whose_file_is_gone_shows_dark_and_stays_chosen()
    {
        var settings = Settings();
        settings.Theme = "gone";
        var appState = new AppState(settings);

        Assert.Same(appState.Themes.Find("dark"), ResolveTheme(appState));
        SaveSettings(appState);
        Assert.Equal("gone", _temp.Settings().Theme);
    }

    [Fact]
    public void A_problem_in_a_theme_is_reported_once_until_it_changes()
    {
        WriteTheme("broken", """{ "extends": "dark", "ui": { "accent": "purple" } }""");
        var appState = new AppState(Settings());
        var error = Assert.Single(appState.Errors.Entries);

        ReloadUserThemes(appState);
        Assert.Single(appState.Errors.Entries);

        WriteTheme("broken", """{ "extends": "dark", "ui": { "accent": "violet" } }""");
        ReloadUserThemes(appState);

        Assert.Equal("Problems in the theme broken.json", error.Words().Title);
        Assert.EndsWith("broken.json(1): ui.accent: the palette has no color named purple", error.Words().Details);
        Assert.Equal(2, appState.Errors.Entries.Count);
    }

    [Fact]
    public void The_themes_folder_is_made_with_the_schema_and_a_readme_that_are_no_themes()
    {
        var appState = new AppState(Settings());

        var folder = PrepareThemesFolder(appState);
        ReloadUserThemes(appState);

        Assert.Equal(_temp.PathOf("themes"), folder);
        Assert.Equal(BuiltInThemes.ReadSchema(), File.ReadAllText(Path.Combine(folder!, "theme.schema.json")));
        Assert.Equal(BuiltInThemes.ReadGuide(), File.ReadAllText(Path.Combine(folder!, "README.md")));
        Assert.Equal(BuiltInThemes.Files.Count, appState.Themes.Ids.Count);
        Assert.Empty(appState.Errors.Entries);
    }

    [Fact]
    public void A_custom_theme_extends_the_one_shown_lists_every_color_and_is_switched_to()
    {
        var settings = Settings();
        settings.Theme = "dracula";
        var appState = new AppState(settings);
        var dracula = appState.Themes.Find("dracula");

        var path = CreateCustomTheme(appState, parent => Strings.En.ThemeCustomName(appState.Themes.NameOf(parent, Strings.En.Culture)));

        var text = File.ReadAllText(path!);
        Assert.Equal(_temp.PathOf(Path.Combine("themes", "dracula-custom.json")), path);
        Assert.Equal("dracula-custom", appState.Theme);
        Assert.Empty(appState.Errors.Entries);
        Assert.Equal("Dracula (custom)", appState.Themes.NameOf("dracula-custom", CultureInfo.GetCultureInfo("en")));
        Assert.Equal(dracula.Ui, ResolveTheme(appState).Ui);
        Assert.Equal(dracula.Code.Keyword, ResolveTheme(appState).Code.Keyword);
        Assert.Contains("\"extends\": \"dracula\",", text);
        Assert.Contains("// \"rowHover\": \"#2C2E3B\",", text);
        Assert.Contains("// \"popupShadowColor\": \"#00000066\",", text);
        Assert.Contains("// \"keyword\": \"#FF79C6\",", text);
        Assert.Contains("// \"restricted\": \"#F1FA8C\",", text);
        Assert.Equal(CountKeys(), CountCommentedColors(text));
    }

    private static int CountKeys() => UiTheme.ColorKeys.Count + CodeViewPalette.ColorKeys.Count + NodeIconPalette.ColorKeys.Count;

    private static int CountCommentedColors(string text) => text.Split('\n').Count(IsCommentedColor);

    private static bool IsCommentedColor(string line) => line.TrimStart().StartsWith("// \"", StringComparison.Ordinal);

    [Theory]
    [InlineData("dark")]
    [InlineData("solarized-light")]
    public void Each_color_of_a_custom_theme_can_be_uncommented_alone(string parent)
    {
        var catalog = new ThemeCatalog(BuiltInThemes.Files);
        var lines = ThemeDraft.Compose("Mine", parent, catalog.Find(parent)).Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            if (!IsCommentedColor(lines[i]))
                continue;
            var edited = lines.ToArray();
            edited[i] = lines[i].Replace("// ", "");
            var file = ThemeFile.Read("mine", "mine.json", Encoding.UTF8.GetBytes(string.Join('\n', edited)));
            catalog.LoadUserThemes([file]);
            Assert.True(catalog.Errors.Count == 0, edited[i] + ": " + string.Join("; ", catalog.Errors));
            Assert.Equal(catalog.Find(parent).Ui.ToColors(), catalog.Find("mine").Ui.ToColors());
        }
    }

    [Fact]
    public void A_custom_theme_takes_the_next_number_while_its_id_is_taken()
    {
        WriteTheme("dark-custom", "{}");
        WriteTheme("dark-custom-2", "{}");
        var appState = new AppState(Settings());

        var path = CreateCustomTheme(appState, parent => Strings.En.ThemeCustomName(appState.Themes.NameOf(parent, Strings.En.Culture)));

        Assert.Equal("dark-custom-3.json", Path.GetFileName(path));
        Assert.Equal("dark-custom-3", appState.Theme);
    }

    [Fact]
    public void A_custom_theme_of_a_theme_that_falls_back_extends_the_one_shown()
    {
        WriteTheme("broken", """{ "type": "light", "extends": "nope" }""");
        var settings = Settings();
        settings.Theme = "broken";
        var appState = new AppState(settings);

        var path = CreateCustomTheme(appState, parent => Strings.En.ThemeCustomName(appState.Themes.NameOf(parent, Strings.En.Culture)));

        Assert.Equal("light-custom.json", Path.GetFileName(path));
        Assert.Contains("\"extends\": \"light\",", File.ReadAllText(path!));
    }

    [Fact]
    public void A_custom_theme_that_cannot_be_written_is_reported()
    {
        var appState = new AppState(Settings());
        Directory.CreateDirectory(_temp.PathOf(Path.Combine("themes", "dark-custom.json")));

        var path = CreateCustomTheme(appState, parent => Strings.En.ThemeCustomName(appState.Themes.NameOf(parent, Strings.En.Culture)));

        Assert.Null(path);
        Assert.Equal("dark", appState.Theme);
        Assert.Equal("Cannot write the custom theme", Assert.Single(appState.Errors.Entries).Words().Title);
    }

    [Fact]
    public void No_custom_theme_is_made_while_the_settings_live_in_memory()
    {
        var appState = new AppState(new AppSettings { Locale = Locale.En });

        Assert.Null(CreateCustomTheme(appState, parent => Strings.En.ThemeCustomName(appState.Themes.NameOf(parent, Strings.En.Culture))));
        Assert.Equal("dark", appState.Theme);
    }

    [Fact]
    public async Task A_themes_folder_that_cannot_be_made_is_reported_and_left_unwatched()
    {
        _temp.Write("themes", "not a folder");
        var d = new AppStateDriver(Settings());

        WatchUserThemes(d.AppState);

        var error = Assert.Single(d.AppState.Errors.Entries);
        Assert.Equal("Cannot make the themes folder", error.Words().Title);
        Assert.Equal(ThemeFolderEventKind.Stopped, (await d.AppState.ThemesWatch.NextChange()).Kind);
    }

    [Fact]
    public void A_removed_themes_folder_is_made_again_and_watched()
    {
        var settings = Settings();
        settings.Theme = "back";
        var d = new AppStateDriver(settings);
        WatchUserThemes(d.AppState);
        var folder = _temp.PathOf("themes");

        Directory.Delete(folder, recursive: true);
        d.RunUntil(() => Directory.Exists(folder));
        WriteTheme("back", """{ "extends": "dark", "ui": { "accent": "#00FF00" } }""");
        d.RunUntil(() => ResolveTheme(d.AppState).Ui.Accent == 0xFF00FF00);
        StopWatchingUserThemes(d.AppState);

        Assert.Empty(d.AppState.Errors.Entries);
    }

    [Fact]
    public void Saving_the_chosen_themes_file_shows_it_at_once()
    {
        var settings = Settings();
        settings.Theme = "live";
        var d = new AppStateDriver(settings);
        WatchUserThemes(d.AppState);
        Assert.Same(d.AppState.Themes.Find("dark"), ResolveTheme(d.AppState));

        WriteTheme("live", """{ "extends": "dark", "ui": { "accent": "#FF00FF" } }""");
        d.RunUntil(() => ResolveTheme(d.AppState).Ui.Accent == 0xFFFF00FF);
        StopWatchingUserThemes(d.AppState);

        Assert.Equal("live", d.AppState.Theme);
    }
}

public sealed class ThemeFolderWatchTests : IDisposable
{
    private static readonly TimeSpan Settled = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

    private readonly TempDir _temp = new();
    private readonly ThemeFolderWatch _watch = new();

    public void Dispose()
    {
        _watch.Stop();
        _temp.Dispose();
    }

    private string Folder => Path.GetDirectoryName(_temp.Write("themes/.keep", ""))!;

    [Fact]
    public async Task A_change_that_settles_while_no_one_waits_is_kept_for_the_next_wait()
    {
        Assert.Null(_watch.Start(Folder));
        var first = _watch.NextChange();
        _temp.Write("themes/a.json", "{}");
        Assert.Equal(ThemeFolderEventKind.Changed, (await first.WaitAsync(Patience)).Kind);

        _temp.Write("themes/b.json", "{}");
        await Task.Delay(Settled);
        var second = _watch.NextChange();

        Assert.Equal(ThemeFolderEventKind.Changed, (await second.WaitAsync(Patience)).Kind);
    }

    [Fact]
    public async Task A_change_is_taken_once()
    {
        Assert.Null(_watch.Start(Folder));
        _temp.Write("themes/a.json", "{}");
        await Task.Delay(Settled);
        Assert.Equal(ThemeFolderEventKind.Changed, (await _watch.NextChange().WaitAsync(Patience)).Kind);

        var next = _watch.NextChange();
        var first = await Task.WhenAny(next, Task.Delay(Settled));

        Assert.NotSame(next, first);
    }

    [Fact]
    public async Task Stopping_ends_the_wait_and_drops_a_pending_change()
    {
        Assert.Null(_watch.Start(Folder));
        var waiting = _watch.NextChange();

        _watch.Stop();

        Assert.Equal(ThemeFolderEventKind.Stopped, (await waiting.WaitAsync(Patience)).Kind);
        Assert.Equal(ThemeFolderEventKind.Stopped, (await _watch.NextChange()).Kind);
    }

    [Fact]
    public async Task A_folder_that_cannot_be_watched_is_told_and_nothing_is_watched()
    {
        var missing = _temp.PathOf("missing");

        var error = _watch.Start(missing);

        Assert.NotNull(error);
        Assert.StartsWith(missing, error);
        Assert.Equal(ThemeFolderEventKind.Stopped, (await _watch.NextChange()).Kind);
    }

    [Fact]
    public async Task Removing_the_watched_folder_loses_the_watch_and_says_so()
    {
        var folder = Folder;
        Assert.Null(_watch.Start(folder));
        var waiting = _watch.NextChange();

        Directory.Delete(folder, recursive: true);
        var news = await waiting.WaitAsync(Patience);

        Assert.Equal(ThemeFolderEventKind.Lost, news.Kind);
        Assert.StartsWith(folder, news.Reason);
        Assert.Equal(ThemeFolderEventKind.Stopped, (await _watch.NextChange()).Kind);
    }
}

public sealed class ThemeSchemaTests
{
    private const string HexPattern = "^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$";
    private const string PaletteNamePattern = "^[^#]";

    [Fact]
    public void The_checked_in_schema_lists_every_themeable_color()
    {
        var generated = Generate();
        var checkedIn = BuiltInThemes.ReadSchema().ReplaceLineEndings("\n");

        if (generated != checkedIn)
        {
            var received = Path.Combine(Path.GetTempPath(), "theme.schema.received.json");
            File.WriteAllText(received, generated);
            Assert.Fail($"ZGF.ILSpy/Assets/Themes/theme.schema.json is out of date; the generated schema is at {received}.");
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
        w.WriteString("title", "ILSpy color theme");
        w.WriteString("description", "A color theme. Comments and trailing commas are allowed. The file's name without .json is the theme's id.");
        w.WriteString("type", "object");
        w.WriteBoolean("additionalProperties", false);
        w.WriteStartObject("properties");
        WriteString(w, "$schema", "The schema theme files follow.");
        WriteName(w);
        WriteType(w);
        WriteString(w, "extends", "The id of a built-in or user theme to build on; what this theme leaves out comes from it.");
        WritePalette(w);
        WriteSection(w, "ui", DescribeUi(), UiTheme.ColorKeys);
        WriteSection(w, "code", "The code view's colors. Those left out come from the theme extended, else from the built-in theme of the type.", CodeViewPalette.ColorKeys);
        WriteSection(w, "icons", "The tree's icon colors. Those left out come from the theme extended, else from the built-in theme of the type.", NodeIconPalette.ColorKeys);
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
        w.WriteString("description", "The name the theme is listed by: one for every language, or one per language tag, such as en or pt-BR, where English stands in for a language without one.");
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

    private static void WriteType(Utf8JsonWriter w)
    {
        w.WriteStartObject("type");
        w.WriteString("description", "Whether the theme is dark or light. A theme that extends no other takes the code and icon colors of the built-in theme of its type.");
        w.WriteStartArray("enum");
        w.WriteStringValue("dark");
        w.WriteStringValue("light");
        w.WriteEndArray();
        w.WriteEndObject();
    }

    private static void WritePalette(Utf8JsonWriter w)
    {
        w.WriteStartObject("palette");
        w.WriteString("description", "Named colors the ui, code and icons sections can use by name. A theme's palette is merged over those of the themes it extends.");
        w.WriteString("type", "object");
        w.WriteStartObject("additionalProperties");
        w.WriteString("$ref", "#/definitions/hexColor");
        w.WriteEndObject();
        w.WriteEndObject();
    }

    private static string DescribeUi()
    {
        var required = UiTheme.ColorKeys
            .Where(k => k.IsRequired)
            .Select(k => k.Key);
        return $"The interface's colors. Required unless an extended theme sets them: {string.Join(", ", required)}. The others are derived from these when left out.";
    }

    private static void WriteSection(Utf8JsonWriter w, string name, string description, IReadOnlyList<ThemeColorKey> keys)
    {
        w.WriteStartObject(name);
        w.WriteString("description", description);
        w.WriteString("type", "object");
        w.WriteBoolean("additionalProperties", false);
        w.WriteStartObject("properties");
        foreach (var key in keys)
            WriteColorKey(w, key);
        w.WriteEndObject();
        w.WriteEndObject();
    }

    private static void WriteColorKey(Utf8JsonWriter w, ThemeColorKey key)
    {
        w.WriteStartObject(key.Key);
        if (key.Description.Length > 0)
            w.WriteString("description", key.Description);
        w.WriteString("$ref", "#/definitions/color");
        w.WriteEndObject();
    }

    private static void WriteDefinitions(Utf8JsonWriter w)
    {
        w.WriteStartObject("definitions");
        w.WriteStartObject("hexColor");
        w.WriteString("description", "#RRGGBB, or #RRGGBBAA with alpha. #00000000 reads as unset, so it is refused; for an invisible color write another transparent one, such as #FFFFFF00.");
        w.WriteString("type", "string");
        w.WriteString("pattern", HexPattern);
        w.WriteStartObject("not");
        w.WriteString("const", "#00000000");
        w.WriteEndObject();
        w.WriteEndObject();
        w.WriteStartObject("color");
        w.WriteStartArray("anyOf");
        w.WriteStartObject();
        w.WriteString("$ref", "#/definitions/hexColor");
        w.WriteEndObject();
        w.WriteStartObject();
        w.WriteString("description", "The name of a palette color.");
        w.WriteString("type", "string");
        w.WriteString("pattern", PaletteNamePattern);
        w.WriteEndObject();
        w.WriteEndArray();
        w.WriteEndObject();
        w.WriteEndObject();
    }
}
