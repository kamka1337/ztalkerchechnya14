using System.Numerics;
using Content.Shared._Session.LimbHealth;
using Content.Shared.FixedPoint;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client._Session.LimbHealth.Widgets;

public sealed class KuklaWidget : UIWidget
{
    private const float Scale = 3f;
    private const string Base = "/Textures/_Session/Interface/LimbHealth/";

    private readonly LayoutContainer _root;
    private readonly TextureRect _selectionOutline;
    private readonly TextureRect _hoverOutline;
    private readonly Dictionary<LimbType, TextureRect> _color = new();
    private readonly Dictionary<LimbType, TextureRect> _effect = new();

    private LimbType _selected = LimbType.Chest;
    private LimbType? _hover;

    public event Action<LimbType>? LimbClicked;

    public KuklaWidget()
    {
        var w = KuklaLayout.Width * Scale;
        var h = KuklaLayout.Height * Scale;

        _root = new LayoutContainer { MinSize = new Vector2(w, h) };
        AddChild(_root);

        AddFullLayer(Base + "backplate.png");

        _selectionOutline = AddFullLayer(null);
        _selectionOutline.Visible = false;
        _hoverOutline = AddFullLayer(null);
        _hoverOutline.Visible = false;

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

        var input = new KuklaInputArea { MinSize = new Vector2(w, h) };
        input.Clicked += limb => LimbClicked?.Invoke(limb);
        input.Hovered += OnHover;
        _root.AddChild(input);
        LayoutContainer.SetPosition(input, Vector2.Zero);
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

    private void OnHover(LimbType? limb)
    {
        if (_hover == limb)
            return;
        _hover = limb;
        UpdateOutlines();
    }

    private void UpdateOutlines()
    {
        _selectionOutline.TexturePath = Base + _selected.SpriteFolder() + "/obvodka.png";
        _selectionOutline.Visible = true;

        if (_hover is { } hv && hv != _selected)
        {
            _hoverOutline.TexturePath = Base + hv.SpriteFolder() + "/obvodka_red.png";
            _hoverOutline.Visible = true;
        }
        else
        {
            _hoverOutline.Visible = false;
        }
    }

    public void UpdateState(LimbHealthComponent comp, LimbType selected)
    {
        _selected = selected;

        foreach (var limb in Enum.GetValues<LimbType>())
        {
            if (!comp.Limbs.TryGetValue(limb, out var st))
                continue;

            var maxFp = comp.MaxHealth.TryGetValue(limb, out var m) ? m : FixedPoint2.Zero;
            var max = maxFp.Float();
            var frac = max > 0 ? st.Health(maxFp).Float() / max : 0f;

            _color[limb].TexturePath = Base + limb.SpriteFolder() + "/" + ColorFor(st, frac) + ".png";

            var icon = EffectIcon(st);
            if (icon != null)
            {
                _effect[limb].TexturePath = Base + "effects/" + icon + ".png";
                _effect[limb].Visible = true;
            }
            else
            {
                _effect[limb].Visible = false;
            }
        }

        UpdateOutlines();
    }

    private static string ColorFor(LimbState st, float frac)
    {
        if (st.Destroyed)
            return "black";
        if (frac > 0.75f)
            return "green";
        if (frac > 0.50f)
            return "yellow";
        if (frac > 0.25f)
            return "orange";
        return "red";
    }

    private static string? EffectIcon(LimbState st)
    {
        if ((st.Effects & LimbEffect.Fracture) != 0)
            return "perelom";
        if ((st.Effects & LimbEffect.HeavyBleeding) != 0)
            return "BIG_KROVOTEK";
        if ((st.Effects & LimbEffect.Bleeding) != 0)
            return "SMALL_KROVOTEK";
        if ((st.Effects & LimbEffect.Painkiller) != 0)
            return "OBEZBOL";
        if ((st.Effects & LimbEffect.Good) != 0)
            return "good_effect";
        if ((st.Effects & LimbEffect.Bad) != 0)
            return "bad_effect";
        return null;
    }
}

internal sealed class KuklaInputArea : Control
{
    public event Action<LimbType>? Clicked;
    public event Action<LimbType?>? Hovered;

    private LimbType? _current;

    public KuklaInputArea()
    {
        MouseFilter = MouseFilterMode.Stop;
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);
        var limb = Nearest(args.RelativePosition);
        if (limb == _current)
            return;
        _current = limb;
        Hovered?.Invoke(limb);
    }

    protected override void MouseExited()
    {
        base.MouseExited();
        _current = null;
        Hovered?.Invoke(null);
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        if (Nearest(args.RelativePosition) is { } limb)
        {
            Clicked?.Invoke(limb);
            args.Handle();
        }
    }

    private LimbType? Nearest(Vector2 relative)
    {
        var size = Size;
        if (size.X <= 0 || size.Y <= 0)
            return null;

        var fx = relative.X / size.X;
        var fy = relative.Y / size.Y;

        LimbType? best = null;
        var bestDist = float.MaxValue;
        foreach (var (limb, center) in KuklaLayout.Centers)
        {
            var dx = center.X - fx;
            var dy = center.Y - fy;
            var dist = dx * dx + dy * dy;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = limb;
            }
        }

        return best;
    }
}
