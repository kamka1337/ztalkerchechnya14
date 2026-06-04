using System.Numerics;
using Content.Shared._Session.LimbHealth;
using Content.Shared.FixedPoint;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Session.LimbHealth.Widgets;

public sealed class KuklaView : Control
{
    private const float Scale = 3f;

    private readonly LayoutContainer _root;
    private readonly Dictionary<LimbType, TextureRect> _color = new();
    private readonly Dictionary<LimbType, TextureRect> _effect = new();

    public event Action<LimbType>? LimbClicked;

    public KuklaView(bool interactive = false)
    {
        var w = KuklaLayout.Width * Scale;
        var h = KuklaLayout.Height * Scale;

        MinSize = new Vector2(w, h);
        _root = new LayoutContainer { MinSize = new Vector2(w, h) };
        AddChild(_root);

        AddFullLayer(KuklaVisuals.Base + "backplate.png");

        foreach (var limb in Enum.GetValues<LimbType>())
            _color[limb] = AddFullLayer(null);

        var iconSize = 8 * Scale;
        foreach (var limb in Enum.GetValues<LimbType>())
        {
            var center = KuklaLayout.Centers[limb];
            var icon = new TextureRect
            {
                Stretch = TextureRect.StretchMode.Scale,
                MinSize = new Vector2(iconSize, iconSize),
                Visible = false,
            };
            _root.AddChild(icon);
            LayoutContainer.SetPosition(icon, new Vector2(center.X * w - iconSize / 2f, center.Y * h - iconSize / 2f));
            _effect[limb] = icon;
        }

        if (interactive)
        {
            var input = new KuklaInputArea { MinSize = new Vector2(w, h) };
            input.Clicked += limb => LimbClicked?.Invoke(limb);
            _root.AddChild(input);
            LayoutContainer.SetPosition(input, Vector2.Zero);
        }
    }

    private TextureRect AddFullLayer(string? path)
    {
        var t = new TextureRect
        {
            Stretch = TextureRect.StretchMode.Scale,
            MinSize = new Vector2(KuklaLayout.Width * Scale, KuklaLayout.Height * Scale),
        };
        if (path != null)
            t.TexturePath = path;
        _root.AddChild(t);
        LayoutContainer.SetPosition(t, Vector2.Zero);
        return t;
    }

    public void UpdateState(LimbHealthComponent comp)
    {
        foreach (var limb in Enum.GetValues<LimbType>())
        {
            if (!comp.Limbs.TryGetValue(limb, out var st))
                continue;

            var maxFp = comp.MaxHealth.TryGetValue(limb, out var m) ? m : FixedPoint2.Zero;
            var max = maxFp.Float();
            var frac = max > 0 ? st.Health(maxFp).Float() / max : 0f;

            _color[limb].TexturePath = KuklaVisuals.Base + limb.SpriteFolder() + "/" + KuklaVisuals.ColorState(st, frac) + ".png";

            var icon = KuklaVisuals.EffectIcon(st);
            if (icon != null)
            {
                _effect[limb].TexturePath = KuklaVisuals.Base + "effects/" + icon + ".png";
                _effect[limb].Visible = true;
            }
            else
            {
                _effect[limb].Visible = false;
            }
        }
    }
}
