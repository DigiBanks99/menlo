namespace Menlo.Ui.Components;

/// <summary>
/// The icons the design system ships. They are Lucide icons (ISC licence), inlined as SVG.
/// To add one, add a value here and its shapes to <see cref="MnlIconShapes"/>.
/// </summary>
public enum MnlIconName
{
    ArrowDownRight,
    ArrowUpRight,
    Calendar,
    Check,
    ChevronDown,
    ChevronRight,
    CircleAlert,
    CircleCheck,
    House,
    Info,
    Lightbulb,
    ListChecks,
    LogOut,
    Minus,
    Moon,
    Pencil,
    Plus,
    Search,
    Sun,
    Trash,
    TrendingUp,
    TriangleAlert,
    Users,
    Wallet,
    X,
    Zap
}

/// <summary>
/// The SVG shapes of each icon, drawn on a 24 by 24 grid with a 2px stroke.
/// </summary>
internal static class MnlIconShapes
{
    private static readonly Dictionary<MnlIconName, string> Shapes = new()
    {
        [MnlIconName.ArrowDownRight] = """<path d="m7 7 10 10"/><path d="M17 7v10H7"/>""",
        [MnlIconName.ArrowUpRight] = """<path d="M7 7h10v10"/><path d="M7 17 17 7"/>""",
        [MnlIconName.Calendar] = """<path d="M8 2v4"/><path d="M16 2v4"/><rect width="18" height="18" x="3" y="4" rx="2"/><path d="M3 10h18"/>""",
        [MnlIconName.Check] = """<path d="M20 6 9 17l-5-5"/>""",
        [MnlIconName.ChevronDown] = """<path d="m6 9 6 6 6-6"/>""",
        [MnlIconName.ChevronRight] = """<path d="m9 18 6-6-6-6"/>""",
        [MnlIconName.CircleAlert] = """<circle cx="12" cy="12" r="10"/><line x1="12" x2="12" y1="8" y2="12"/><line x1="12" x2="12.01" y1="16" y2="16"/>""",
        [MnlIconName.CircleCheck] = """<circle cx="12" cy="12" r="10"/><path d="m9 12 2 2 4-4"/>""",
        [MnlIconName.House] = """<path d="M15 21v-8a1 1 0 0 0-1-1h-4a1 1 0 0 0-1 1v8"/><path d="M3 10a2 2 0 0 1 .709-1.528l7-6a2 2 0 0 1 2.582 0l7 6A2 2 0 0 1 21 10v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/>""",
        [MnlIconName.Info] = """<circle cx="12" cy="12" r="10"/><path d="M12 16v-4"/><path d="M12 8h.01"/>""",
        [MnlIconName.Lightbulb] = """<path d="M15 14c.2-1 .7-1.7 1.5-2.5 1-.9 1.5-2.2 1.5-3.5A6 6 0 0 0 6 8c0 1 .2 2.2 1.5 3.5.7.7 1.3 1.5 1.5 2.5"/><path d="M9 18h6"/><path d="M10 22h4"/>""",
        [MnlIconName.ListChecks] = """<path d="m3 17 2 2 4-4"/><path d="m3 7 2 2 4-4"/><path d="M13 6h8"/><path d="M13 12h8"/><path d="M13 18h8"/>""",
        [MnlIconName.LogOut] = """<path d="m16 17 5-5-5-5"/><path d="M21 12H9"/><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/>""",
        [MnlIconName.Minus] = """<path d="M5 12h14"/>""",
        [MnlIconName.Moon] = """<path d="M20.985 12.486a9 9 0 1 1-9.473-9.472c.405-.022.617.46.402.803a6 6 0 0 0 8.268 8.268c.344-.215.825-.004.803.401"/>""",
        [MnlIconName.Pencil] = """<path d="M21.174 6.812a1 1 0 0 0-3.986-3.987L3.842 16.174a2 2 0 0 0-.5.83l-1.321 4.352a.5.5 0 0 0 .623.622l4.353-1.32a2 2 0 0 0 .83-.497z"/><path d="m15 5 4 4"/>""",
        [MnlIconName.Plus] = """<path d="M5 12h14"/><path d="M12 5v14"/>""",
        [MnlIconName.Search] = """<path d="m21 21-4.34-4.34"/><circle cx="11" cy="11" r="8"/>""",
        [MnlIconName.Sun] = """<circle cx="12" cy="12" r="4"/><path d="M12 2v2"/><path d="M12 20v2"/><path d="m4.93 4.93 1.41 1.41"/><path d="m17.66 17.66 1.41 1.41"/><path d="M2 12h2"/><path d="M20 12h2"/><path d="m6.34 17.66-1.41 1.41"/><path d="m19.07 4.93-1.41 1.41"/>""",
        [MnlIconName.Trash] = """<path d="M10 11v6"/><path d="M14 11v6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6"/><path d="M3 6h18"/><path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/>""",
        [MnlIconName.TrendingUp] = """<path d="M16 7h6v6"/><path d="m22 7-8.5 8.5-5-5L2 17"/>""",
        [MnlIconName.TriangleAlert] = """<path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3"/><path d="M12 9v4"/><path d="M12 17h.01"/>""",
        [MnlIconName.Users] = """<path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><path d="M16 3.128a4 4 0 0 1 0 7.744"/><path d="M22 21v-2a4 4 0 0 0-3-3.87"/><circle cx="9" cy="7" r="4"/>""",
        [MnlIconName.Wallet] = """<path d="M19 7V4a1 1 0 0 0-1-1H5a2 2 0 0 0 0 4h15a1 1 0 0 1 1 1v4h-3a2 2 0 0 0 0 4h3a1 1 0 0 0 1-1v-2a1 1 0 0 0-1-1"/><path d="M3 5v14a2 2 0 0 0 2 2h15a1 1 0 0 0 1-1v-4"/>""",
        [MnlIconName.X] = """<path d="M18 6 6 18"/><path d="m6 6 12 12"/>""",
        [MnlIconName.Zap] = """<path d="M4 14a1 1 0 0 1-.78-1.63l9.9-10.2a.5.5 0 0 1 .86.46l-1.92 6.02A1 1 0 0 0 13 10h7a1 1 0 0 1 .78 1.63l-9.9 10.2a.5.5 0 0 1-.86-.46l1.92-6.02A1 1 0 0 0 11 14z"/>"""
    };

    public static string For(MnlIconName name) => Shapes[name];
}
