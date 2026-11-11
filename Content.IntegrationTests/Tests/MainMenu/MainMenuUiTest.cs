using System.Linq;
using Content.Client.MainMenu;
using Content.Client.MainMenu.UI;
using Content.Client.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.IntegrationTests.Tests.MainMenu;

public sealed class MainMenuUiTest
{
    [Test]
    public async Task MainMenuControlsAndStylesLoadTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false
        });

        var client = pair.Client;
        var stateMan = client.Resolve<IStateManager>();
        var uiMan = client.Resolve<IUserInterfaceManager>();
        var stylesheetMan = client.Resolve<IStylesheetManager>();

        await client.WaitAssertion(() =>
        {
            Assert.That(stateMan.CurrentState, Is.TypeOf<MainScreen>());
            Assert.That(stylesheetMan.UnusedSheetlets, Is.Empty);

            var menu = uiMan.StateRoot.Children.OfType<MainMenuControl>().SingleOrDefault();
            Assert.That(menu, Is.Not.Null);

            menu!.ForceRunStyleUpdate();
            menu.DirectConnectButton.ForceRunStyleUpdate();
            menu.OptionsButton.ForceRunStyleUpdate();
            menu.ChangelogButton.ForceRunStyleUpdate();
            menu.QuitButton.ForceRunStyleUpdate();
            menu.UsernameBox.ForceRunStyleUpdate();
            menu.AddressBox.ForceRunStyleUpdate();

            Assert.Multiple(() =>
            {
                Assert.That(menu.DirectConnectButton.Visible, Is.True);
                Assert.That(menu.OptionsButton.Visible, Is.True);
                Assert.That(menu.ChangelogButton.Visible, Is.True);
                Assert.That(menu.QuitButton.Visible, Is.True);
                Assert.That(menu.UsernameBox.Visible, Is.True);
                Assert.That(menu.AddressBox.Visible, Is.True);

                Assert.That(menu.DirectConnectButton.Size.X, Is.GreaterThan(0));
                Assert.That(menu.OptionsButton.Size.X, Is.GreaterThan(0));
                Assert.That(menu.ChangelogButton.Size.X, Is.GreaterThan(0));
                Assert.That(menu.QuitButton.Size.X, Is.GreaterThan(0));

                Assert.That(
                    menu.DirectConnectButton.TryGetStyleProperty<StyleBox>(
                        ContainerButton.StylePropertyStyleBox,
                        out var connectBox),
                    Is.True);
                Assert.That(connectBox, Is.TypeOf<StyleBoxFlat>());

                Assert.That(
                    menu.OptionsButton.TryGetStyleProperty<StyleBox>(
                        ContainerButton.StylePropertyStyleBox,
                        out var optionsBox),
                    Is.True);
                Assert.That(optionsBox, Is.TypeOf<StyleBoxFlat>());

                Assert.That(
                    menu.ChangelogButton.TryGetStyleProperty<StyleBox>(
                        ContainerButton.StylePropertyStyleBox,
                        out var changelogBox),
                    Is.True);
                Assert.That(changelogBox, Is.TypeOf<StyleBoxFlat>());

                Assert.That(
                    menu.QuitButton.TryGetStyleProperty<StyleBox>(
                        ContainerButton.StylePropertyStyleBox,
                        out var quitBox),
                    Is.True);
                Assert.That(quitBox, Is.TypeOf<StyleBoxFlat>());

                Assert.That(
                    menu.UsernameBox.TryGetStyleProperty<StyleBox>(
                        LineEdit.StylePropertyStyleBox,
                        out var usernameBox),
                    Is.True);
                Assert.That(usernameBox, Is.TypeOf<StyleBoxFlat>());

                Assert.That(
                    menu.AddressBox.TryGetStyleProperty<StyleBox>(
                        LineEdit.StylePropertyStyleBox,
                        out var addressBox),
                    Is.True);
                Assert.That(addressBox, Is.TypeOf<StyleBoxFlat>());
            });
        });

        await pair.CleanReturnAsync();
    }
}
