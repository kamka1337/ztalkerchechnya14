using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.MainMenu.UI;

[CommonSheetlet]
public sealed class MainMenuSheetlet : Sheetlet<NanotrasenStylesheet>
{
    private readonly record struct ButtonStateStyle(
        Color Background,
        Color Border,
        float BorderThickness,
        Color TextColor);

    private readonly record struct ButtonTheme(
        int FontSize,
        float PadHorizontal,
        float PadVertical,
        ButtonStateStyle Normal,
        ButtonStateStyle Hover,
        ButtonStateStyle Pressed,
        ButtonStateStyle Disabled);

    public override StyleRule[] GetRules(NanotrasenStylesheet sheet, object config)
    {
        var cardBox = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#0B0D12EE"),
            BorderColor = Color.FromHex("#3A4250"),
            BorderThickness = new Thickness(2f),
        };

        var accentBarBox = new StyleBoxFlat(Color.FromHex("#D99B26"));
        var dividerBox = new StyleBoxFlat(Color.FromHex("#2A313D"));

        var lineEditBox = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#11151EF5"),
            BorderColor = Color.FromHex("#3D4656"),
            BorderThickness = new Thickness(1.5f),
            ContentMarginLeftOverride = 10,
            ContentMarginRightOverride = 10,
            ContentMarginTopOverride = 6,
            ContentMarginBottomOverride = 6,
        };

        var disabledThemeState = new ButtonStateStyle(
            Color.FromHex("#0C0E12CC"),
            Color.FromHex("#2C313C"),
            1.5f,
            Color.FromHex("#666E7A"));

        var connectTheme = new ButtonTheme(
            FontSize: 16,
            PadHorizontal: 20,
            PadVertical: 10,
            Normal: new ButtonStateStyle(
                Color.FromHex("#161B24F2"),
                Color.FromHex("#9E7322"),
                2f,
                Color.FromHex("#F2F5F8")),
            Hover: new ButtonStateStyle(
                Color.FromHex("#222936FA"),
                Color.FromHex("#F5B025"),
                2.5f,
                Color.FromHex("#FFF4D6")),
            Pressed: new ButtonStateStyle(
                Color.FromHex("#17361BFA"),
                Color.FromHex("#48C752"),
                2.5f,
                Color.FromHex("#A5FFA5")),
            Disabled: disabledThemeState);

        var secondaryTheme = new ButtonTheme(
            FontSize: 14,
            PadHorizontal: 16,
            PadVertical: 8,
            Normal: new ButtonStateStyle(
                Color.FromHex("#11141BF0"),
                Color.FromHex("#424958"),
                2f,
                Color.FromHex("#DFE3E8")),
            Hover: new ButtonStateStyle(
                Color.FromHex("#1C212BF8"),
                Color.FromHex("#8CA4C2"),
                2.5f,
                Color.White),
            Pressed: new ButtonStateStyle(
                Color.FromHex("#28303DF8"),
                Color.FromHex("#AEC5E0"),
                2.5f,
                Color.White),
            Disabled: disabledThemeState);

        var changelogNewTheme = new ButtonTheme(
            FontSize: 14,
            PadHorizontal: 16,
            PadVertical: 8,
            Normal: new ButtonStateStyle(
                Color.FromHex("#221912F2"),
                Color.FromHex("#D4882A"),
                2f,
                Color.FromHex("#FFE1B3")),
            Hover: new ButtonStateStyle(
                Color.FromHex("#302216F8"),
                Color.FromHex("#F5B025"),
                2.5f,
                Color.FromHex("#FFF4D6")),
            Pressed: new ButtonStateStyle(
                Color.FromHex("#3B291BF8"),
                Color.FromHex("#FFD166"),
                2.5f,
                Color.White),
            Disabled: disabledThemeState);

        var dangerTheme = new ButtonTheme(
            FontSize: 14,
            PadHorizontal: 16,
            PadVertical: 8,
            Normal: new ButtonStateStyle(
                Color.FromHex("#151115F0"),
                Color.FromHex("#5E383C"),
                2f,
                Color.FromHex("#E0CACA")),
            Hover: new ButtonStateStyle(
                Color.FromHex("#2A1619F8"),
                Color.FromHex("#D9534F"),
                2.5f,
                Color.FromHex("#FFEAEA")),
            Pressed: new ButtonStateStyle(
                Color.FromHex("#3B1B1FF8"),
                Color.FromHex("#FF6B6B"),
                2.5f,
                Color.White),
            Disabled: disabledThemeState);

        var rules = new List<StyleRule>
        {
            E<BoxContainer>()
                .Identifier(MainMenuControl.StyleIdentifierMainMenuVBox)
                .Prop(BoxContainer.StylePropertySeparation, 0),

            E<PanelContainer>()
                .Class(MainMenuControl.StyleClassMainMenuCard)
                .Panel(cardBox),

            E<PanelContainer>()
                .Class(MainMenuControl.StyleClassMainMenuAccentBar)
                .Panel(accentBarBox),

            E<PanelContainer>()
                .Class(MainMenuControl.StyleClassMainMenuDivider)
                .Panel(dividerBox),

            E<Label>()
                .Class(MainMenuControl.StyleClassMainMenuLabel)
                .Font(sheet.BaseFont.GetFont(13, FontKind.Bold))
                .FontColor(Color.FromHex("#AEB8C7")),

            E<LineEdit>()
                .Class(MainMenuControl.StyleClassMainMenuLineEdit)
                .Prop(LineEdit.StylePropertyStyleBox, lineEditBox)
                .Prop("font", sheet.BaseFont.GetFont(13))
                .Prop("font-color", Color.FromHex("#EEF2F7"))
                .Prop(LineEdit.StylePropertyCursorColor, Color.FromHex("#F5B025"))
                .Prop(LineEdit.StylePropertySelectionColor, Color.FromHex("#F5B02544")),

            E<LineEdit>()
                .Class(MainMenuControl.StyleClassMainMenuLineEdit)
                .Pseudo(LineEdit.StylePseudoClassPlaceholder)
                .Prop("font-color", Color.FromHex("#6B7586")),
        };

        AddButtonRules(rules, sheet, MainMenuControl.StyleClassMainMenuConnectButton, null, connectTheme);
        AddButtonRules(rules, sheet, MainMenuControl.StyleClassMainMenuSecondaryButton, null, secondaryTheme);
        AddButtonRules(rules, sheet, MainMenuControl.StyleClassMainMenuSecondaryButton, StyleClass.Negative, changelogNewTheme);
        AddButtonRules(rules, sheet, MainMenuControl.StyleClassMainMenuDangerButton, null, dangerTheme);

        return rules.ToArray();
    }

    /// <summary>
    /// Adds styled button box and label rules for all interactive states of a main menu button class.
    /// </summary>
    private static void AddButtonRules(
        List<StyleRule> rules,
        NanotrasenStylesheet sheet,
        string primaryClass,
        string? extraClass,
        ButtonTheme theme)
    {
        var font = sheet.BaseFont.GetFont(theme.FontSize, FontKind.Bold);
        var states = new (string PseudoClass, ButtonStateStyle Style)[]
        {
            (ContainerButton.StylePseudoClassNormal, theme.Normal),
            (ContainerButton.StylePseudoClassHover, theme.Hover),
            (ContainerButton.StylePseudoClassPressed, theme.Pressed),
            (ContainerButton.StylePseudoClassDisabled, theme.Disabled),
        };

        foreach (var (pseudo, state) in states)
        {
            var box = new StyleBoxFlat
            {
                BackgroundColor = state.Background,
                BorderColor = state.Border,
                BorderThickness = new Thickness(state.BorderThickness),
                ContentMarginTopOverride = theme.PadVertical,
                ContentMarginBottomOverride = theme.PadVertical,
                ContentMarginLeftOverride = theme.PadHorizontal,
                ContentMarginRightOverride = theme.PadHorizontal,
            };

            rules.Add(
                E<Button>()
                    .Class(primaryClass)
                    .MaybeClass(extraClass)
                    .Pseudo(pseudo)
                    .Box(box)
                    .Modulate(Color.White));

            rules.Add(
                E<Button>()
                    .Class(primaryClass)
                    .MaybeClass(extraClass)
                    .Pseudo(pseudo)
                    .ParentOf(E<Label>())
                    .Font(font)
                    .FontColor(state.TextColor));
        }
    }
}
