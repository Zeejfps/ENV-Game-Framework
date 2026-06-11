using ZGF.Observable;

namespace LLMit.ViewModels;

public sealed class AppViewModel
{
    public ObservableList<ChatTabViewModel> Tabs { get; } = new();
    public State<string> SelectedModel { get; } = new("Gemini");
    public State<bool> IsStartScreenVisible { get; } = new(true);
    public IReadOnlyList<string> AvailableModels { get; } = ["Gemini", "GPT 5", "Claude Opus"];

    public AppViewModel()
    {
        Tabs.Add(new ChatTabViewModel("New Chat", isActive: true));
    }

    public void StartNewChat(string text)
    {
        foreach (var tab in Tabs)
            tab.IsActive.Value = false;

        Tabs.Add(new ChatTabViewModel(SelectedModel.Value, isActive: true));
        IsStartScreenVisible.Value = false;
    }
}

public sealed class ChatTabViewModel
{
    public string Title { get; }
    public State<bool> IsActive { get; }

    public ChatTabViewModel(string title, bool isActive = false)
    {
        Title = title;
        IsActive = new State<bool>(isActive);
    }
}
