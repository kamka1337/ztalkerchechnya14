using System.Text;
using Content.Client._Session.LimbHealth.Widgets;
using Content.Client.Examine;
using Content.Shared._Session.LimbHealth;
using Content.Shared.FixedPoint;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client._Session.LimbHealth;

public sealed class KuklaExamineSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystem _examine = default!;
    [Dependency] private readonly KuklaSystem _kukla = default!;

    private const string PanelName = "SessionKuklaExamine";

    private static readonly LimbType[] Order =
    {
        LimbType.Head, LimbType.Chest, LimbType.Abdomen,
        LimbType.LeftArm, LimbType.RightArm, LimbType.LeftLeg, LimbType.RightLeg,
    };

    public override void Initialize()
    {
        base.Initialize();
        _examine.OnExamineTooltipBuilt += OnExamineTooltipBuilt;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _examine.OnExamineTooltipBuilt -= OnExamineTooltipBuilt;
    }

    private void OnExamineTooltipBuilt(EntityUid player, EntityUid target, BoxContainer vBox, bool healthTab)
    {
        if (!healthTab)
            return;

        if (!TryComp<LimbHealthComponent>(target, out var comp))
            return;

        foreach (var child in vBox.Children)
        {
            if (child.Name == PanelName)
            {
                vBox.RemoveChild(child);
                break;
            }
        }

        var panel = new PanelContainer
        {
            Name = PanelName,
            Margin = new Thickness(4, 4, 4, 4),
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#13131aE0"),
                BorderColor = Color.FromHex("#50505a"),
                BorderThickness = new Thickness(1),
                ContentMarginLeftOverride = 6,
                ContentMarginRightOverride = 6,
                ContentMarginTopOverride = 6,
                ContentMarginBottomOverride = 6,
            },
        };

        var content = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 10,
        };
        panel.AddChild(content);

        var view = new KuklaView(interactive: true) { VerticalAlignment = Control.VAlignment.Center };
        view.UpdateState(comp);
        view.LimbClicked += limb => _kukla.SetSelectedLimb(limb);
        content.AddChild(view);

        var status = new RichTextLabel
        {
            VerticalAlignment = Control.VAlignment.Center,
            HorizontalExpand = true,
        };
        status.SetMessage(BuildStatus(comp));
        content.AddChild(status);

        vBox.AddChild(panel);
    }

    private FormattedMessage BuildStatus(LimbHealthComponent comp)
    {
        var sb = new StringBuilder();
        sb.Append("[bold]Состояние тела:[/bold]\n");

        foreach (var limb in Order)
        {
            if (!comp.Limbs.TryGetValue(limb, out var st))
                continue;

            var max = comp.MaxHealth.TryGetValue(limb, out var m) ? m : FixedPoint2.Zero;
            var hp = st.Health(max);
            var frac = max > 0 ? hp.Float() / max.Float() : 0f;

            var hpText = st.Destroyed
                ? $"[color=#bbbbbb]{DestroyedWord(limb)}[/color]"
                : $"[color={HpColor(frac)}]{hp.Float():0}/{max.Float():0}[/color]";

            var cond = "";
            if (!st.Destroyed)
            {
                if ((st.Effects & LimbEffect.HeavyBleeding) != 0)
                    cond = "[color=#ff5555]сильное кровотечение[/color]";
                else if ((st.Effects & LimbEffect.Bleeding) != 0)
                    cond = "[color=#ffaa55]лёгкое кровотечение[/color]";
            }

            sb.Append($"{LimbName(limb)}: {hpText}");
            if (cond.Length > 0)
                sb.Append($" ({cond})");
            sb.Append('\n');
        }

        return FormattedMessage.FromMarkupPermissive(sb.ToString().TrimEnd('\n'));
    }

    private static string HpColor(float frac) => frac switch
    {
        > 0.75f => "#88dd88",
        > 0.50f => "#dddd88",
        > 0.25f => "#ddaa66",
        _ => "#dd6666",
    };

    private static string LimbName(LimbType limb) => limb switch
    {
        LimbType.Head => "Голова",
        LimbType.Chest => "Грудь",
        LimbType.Abdomen => "Живот",
        LimbType.LeftArm => "Левая рука",
        LimbType.RightArm => "Правая рука",
        LimbType.LeftLeg => "Левая нога",
        LimbType.RightLeg => "Правая нога",
        _ => limb.ToString(),
    };

    private static string DestroyedWord(LimbType limb) => limb == LimbType.Abdomen ? "выбит" : "выбита";
}
