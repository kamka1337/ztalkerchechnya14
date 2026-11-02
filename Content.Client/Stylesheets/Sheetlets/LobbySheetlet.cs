using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class LobbySheetlet<T> : Sheetlet<T> where T : CommonStylesheet
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        var cardBox = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#0B0D12E0"),
            BorderColor = Color.FromHex("#3A4250"),
            BorderThickness = new Thickness(2f),
            ContentMarginTopOverride = 20,
            ContentMarginBottomOverride = 20,
            ContentMarginLeftOverride = 24,
            ContentMarginRightOverride = 24,
        };

        var chatPanelBox = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#0C0E14E8"),
            BorderColor = Color.FromHex("#303744"),
            BorderThickness = new Thickness(2f, 0, 0, 0),
            ContentMarginTopOverride = 12,
            ContentMarginBottomOverride = 12,
            ContentMarginLeftOverride = 14,
            ContentMarginRightOverride = 14,
        };

        // Play / Ready Button Styles
        var playNormalBox = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#14171EEE"),
            BorderColor = Color.FromHex("#525C6E"),
            BorderThickness = new Thickness(2f),
            ContentMarginTopOverride = 16,
            ContentMarginBottomOverride = 16,
            ContentMarginLeftOverride = 28,
            ContentMarginRightOverride = 28,
        };

        var playHoverBox = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#202632F4"),
            BorderColor = Color.FromHex("#F5B025"),
            BorderThickness = new Thickness(2.5f),
            ContentMarginTopOverride = 16,
            ContentMarginBottomOverride = 16,
            ContentMarginLeftOverride = 28,
            ContentMarginRightOverride = 28,
        };

        var playPressedBox = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#17361BF4"),
            BorderColor = Color.FromHex("#48C752"),
            BorderThickness = new Thickness(2.5f),
            ContentMarginTopOverride = 16,
            ContentMarginBottomOverride = 16,
            ContentMarginLeftOverride = 28,
            ContentMarginRightOverride = 28,
        };

        var playDisabledBox = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#0C0E12AA"),
            BorderColor = Color.FromHex("#2C313C"),
            BorderThickness = new Thickness(1.5f),
            ContentMarginTopOverride = 16,
            ContentMarginBottomOverride = 16,
            ContentMarginLeftOverride = 28,
            ContentMarginRightOverride = 28,
        };

        // Options / Menu Button Styles
        var menuNormalBox = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#111318EE"),
            BorderColor = Color.FromHex("#424958"),
            BorderThickness = new Thickness(2f),
            ContentMarginTopOverride = 16,
            ContentMarginBottomOverride = 16,
            ContentMarginLeftOverride = 28,
            ContentMarginRightOverride = 28,
        };

        var menuHoverBox = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#1C212BF4"),
            BorderColor = Color.FromHex("#8CA4C2"),
            BorderThickness = new Thickness(2.5f),
            ContentMarginTopOverride = 16,
            ContentMarginBottomOverride = 16,
            ContentMarginLeftOverride = 28,
            ContentMarginRightOverride = 28,
        };

        var menuPressedBox = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#28303DF4"),
            BorderColor = Color.FromHex("#AEC5E0"),
            BorderThickness = new Thickness(2.5f),
            ContentMarginTopOverride = 16,
            ContentMarginBottomOverride = 16,
            ContentMarginLeftOverride = 28,
            ContentMarginRightOverride = 28,
        };

        var menuDisabledBox = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#0C0E12AA"),
            BorderColor = Color.FromHex("#2C313C"),
            BorderThickness = new Thickness(1.5f),
            ContentMarginTopOverride = 16,
            ContentMarginBottomOverride = 16,
            ContentMarginLeftOverride = 28,
            ContentMarginRightOverride = 28,
        };

        return
        [
            E<PanelContainer>()
                .Class(StyleClass.LobbyButtonCard)
                .Prop(PanelContainer.StylePropertyPanel, cardBox),

            E<PanelContainer>()
                .Class(StyleClass.LobbyChatPanel)
                .Prop(PanelContainer.StylePropertyPanel, chatPanelBox),

            // Lobby Play Button
            E<Button>()
                .Class(StyleClass.LobbyPlayButton)
                .PseudoNormal()
                .Prop(ContainerButton.StylePropertyStyleBox, playNormalBox)
                .Prop(Control.StylePropertyModulateSelf, Color.White),

            E<Button>()
                .Class(StyleClass.LobbyPlayButton)
                .PseudoHovered()
                .Prop(ContainerButton.StylePropertyStyleBox, playHoverBox)
                .Prop(Control.StylePropertyModulateSelf, Color.White),

            E<Button>()
                .Class(StyleClass.LobbyPlayButton)
                .PseudoPressed()
                .Prop(ContainerButton.StylePropertyStyleBox, playPressedBox)
                .Prop(Control.StylePropertyModulateSelf, Color.White),

            E<Button>()
                .Class(StyleClass.LobbyPlayButton)
                .PseudoDisabled()
                .Prop(ContainerButton.StylePropertyStyleBox, playDisabledBox)
                .Prop(Control.StylePropertyModulateSelf, Color.White),

            E<Button>()
                .Class(StyleClass.LobbyPlayButton)
                .ParentOf(E<Label>())
                .Font(sheet.BaseFont.GetFont(18, FontKind.Bold))
                .FontColor(Color.FromHex("#EAEFF5")),

            E<Button>()
                .Class(StyleClass.LobbyPlayButton)
                .PseudoHovered()
                .ParentOf(E<Label>())
                .FontColor(Color.FromHex("#FFF4D6")),

            E<Button>()
                .Class(StyleClass.LobbyPlayButton)
                .PseudoPressed()
                .ParentOf(E<Label>())
                .FontColor(Color.FromHex("#A5FFA5")),

            E<Button>()
                .Class(StyleClass.LobbyPlayButton)
                .PseudoDisabled()
                .ParentOf(E<Label>())
                .FontColor(Color.FromHex("#555555")),

            // Lobby Menu Button (Options)
            E<Button>()
                .Class(StyleClass.LobbyMenuButton)
                .PseudoNormal()
                .Prop(ContainerButton.StylePropertyStyleBox, menuNormalBox)
                .Prop(Control.StylePropertyModulateSelf, Color.White),

            E<Button>()
                .Class(StyleClass.LobbyMenuButton)
                .PseudoHovered()
                .Prop(ContainerButton.StylePropertyStyleBox, menuHoverBox)
                .Prop(Control.StylePropertyModulateSelf, Color.White),

            E<Button>()
                .Class(StyleClass.LobbyMenuButton)
                .PseudoPressed()
                .Prop(ContainerButton.StylePropertyStyleBox, menuPressedBox)
                .Prop(Control.StylePropertyModulateSelf, Color.White),

            E<Button>()
                .Class(StyleClass.LobbyMenuButton)
                .PseudoDisabled()
                .Prop(ContainerButton.StylePropertyStyleBox, menuDisabledBox)
                .Prop(Control.StylePropertyModulateSelf, Color.White),

            E<Button>()
                .Class(StyleClass.LobbyMenuButton)
                .ParentOf(E<Label>())
                .Font(sheet.BaseFont.GetFont(18, FontKind.Bold))
                .FontColor(Color.FromHex("#DFE3E8")),

            E<Button>()
                .Class(StyleClass.LobbyMenuButton)
                .PseudoHovered()
                .ParentOf(E<Label>())
                .FontColor(Color.White),

            E<Button>()
                .Class(StyleClass.LobbyMenuButton)
                .PseudoPressed()
                .ParentOf(E<Label>())
                .FontColor(Color.White),

            E<Button>()
                .Class(StyleClass.LobbyMenuButton)
                .PseudoDisabled()
                .ParentOf(E<Label>())
                .FontColor(Color.FromHex("#555555")),
        ];
    }
}
