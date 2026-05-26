using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;

namespace GitGui;

internal sealed class BranchesHeader : MultiChildView, IBind<BranchesHeaderViewModel>
{
    private const float HeaderHeight = 44f;
    private const int HorizontalPadding = 8;

    private readonly TextView _iconView;
    private readonly TextView _prefixView;
    private readonly TextView _nameView;
    private readonly PaddingView _content;

    public BranchesHeader()
    {
        PreferredHeight = HeaderHeight;

        _iconView = new TextView
        {
            Text = LucideIcons.Branch,
            FontFamily = LucideIcons.FontFamily,
            FontSize = 15f,
            VerticalTextAlignment = TextAlignment.Center,
        };
        _iconView.StyleClasses.Add(StyleClassNames.DialogHeaderIcon);

        _prefixView = new TextView { VerticalTextAlignment = TextAlignment.Center };
        _prefixView.StyleClasses.Add(StyleClassNames.DialogHeaderPrefix);

        _nameView = new TextView
        {
            FontSize = 18f,
            FontWeight = FontWeight.Bold,
            VerticalTextAlignment = TextAlignment.Center,
        };
        _nameView.StyleClasses.Add(StyleClassNames.DialogHeaderName);

        _content = new PaddingView
        {
            Padding = new PaddingStyle { Left = 6, Right = 6 },
            Children =
            {
                new RowView
                {
                    Gap = 6,
                    Children = { _iconView, _prefixView, _nameView },
                },
            },
        };

        var header = new RectView
        {
            Padding = new PaddingStyle { Left = HorizontalPadding, Right = HorizontalPadding },
            Children =
            {
                new FlexRowView
                {
                    CrossAxisAlignment = CrossAxisAlignment.Center,
                    Children = { _content },
                },
            },
        };
        header.StyleClasses.Add(StyleClassNames.DialogHeader);
        AddChildToSelf(header);

        this.UseViewModel(this);
    }

    public void Bind(BranchesHeaderViewModel vm)
    {
        _prefixView.BindText(vm.IsDetached, d => d ? "at" : "on");
        _nameView.BindText(vm.BranchName);

        // The "detached" modifier on the icon and name views drives their dim color through
        // the sheet (.dialog-header-icon.detached and .dialog-header-name.detached rules).
        _iconView.BindModifier(ModifierNames.Detached, vm.IsDetached);
        _nameView.BindModifier(ModifierNames.Detached, vm.IsDetached);

        _content.BindIsVisible(vm.BranchName, n => !string.IsNullOrEmpty(n));
    }
}
