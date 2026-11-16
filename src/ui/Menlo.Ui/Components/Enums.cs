namespace Menlo.Ui.Components;

/// <summary>How much emphasis a button has.</summary>
public enum MnlButtonVariant
{
    /// <summary>The main action in a view. Use one per view or form.</summary>
    Primary,

    /// <summary>An alternative action next to a primary one.</summary>
    Secondary,

    /// <summary>A low-emphasis action, such as Cancel or an action in a row.</summary>
    Ghost,

    /// <summary>An action that deletes or can't be undone.</summary>
    Destructive
}

/// <summary>A three-step size.</summary>
public enum MnlSize
{
    /// <summary>Small.</summary>
    Sm,

    /// <summary>Medium, the default.</summary>
    Md,

    /// <summary>Large.</summary>
    Lg
}

/// <summary>The meaning a colour carries.</summary>
public enum MnlTone
{
    /// <summary>No particular meaning.</summary>
    Neutral,

    /// <summary>Brand emphasis.</summary>
    Accent,

    /// <summary>Good, done or within limits.</summary>
    Success,

    /// <summary>Needs attention soon.</summary>
    Warning,

    /// <summary>Failed, over the limit or destructive.</summary>
    Error,

    /// <summary>Informational.</summary>
    Info
}

/// <summary>The HTML type of a button.</summary>
public enum MnlButtonType
{
    /// <summary>Does nothing by itself.</summary>
    Button,

    /// <summary>Submits its form.</summary>
    Submit,

    /// <summary>Resets its form.</summary>
    Reset
}

/// <summary>The direction a figure is moving in.</summary>
public enum MnlTrendDirection
{
    /// <summary>Rising.</summary>
    Up,

    /// <summary>Falling.</summary>
    Down,

    /// <summary>Flat.</summary>
    Neutral
}

/// <summary>How a panel is presented.</summary>
public enum MnlPanelMode
{
    /// <summary>A bottom sheet on small screens and a centred dialog on large ones.</summary>
    Auto,

    /// <summary>A narrow centred dialog at every size, for confirmations.</summary>
    Dialog
}

/// <summary>The text scale.</summary>
public enum MnlTextSize
{
    /// <summary>12px.</summary>
    Xs,

    /// <summary>14px.</summary>
    Sm,

    /// <summary>16px.</summary>
    Md,

    /// <summary>18px.</summary>
    Lg,

    /// <summary>20px.</summary>
    Xl,

    /// <summary>24px.</summary>
    Xxl,

    /// <summary>30px.</summary>
    Xxxl,

    /// <summary>36px, growing to 48px on wide screens.</summary>
    Display
}

/// <summary>The colour of a piece of text.</summary>
public enum MnlTextTone
{
    /// <summary>The colour of the surrounding text.</summary>
    Inherit,

    /// <summary>Body text.</summary>
    Default,

    /// <summary>Secondary text.</summary>
    Subtle,

    /// <summary>Brand emphasis.</summary>
    Accent,

    /// <summary>Good news.</summary>
    Success,

    /// <summary>A warning.</summary>
    Warning,

    /// <summary>An error.</summary>
    Error
}

/// <summary>Font weight.</summary>
public enum MnlWeight
{
    /// <summary>400.</summary>
    Regular,

    /// <summary>500.</summary>
    Medium,

    /// <summary>600.</summary>
    Semibold,

    /// <summary>700.</summary>
    Bold
}

/// <summary>The spacing scale used for gaps.</summary>
public enum MnlGap
{
    /// <summary>No gap.</summary>
    None,

    /// <summary>4px.</summary>
    Xs,

    /// <summary>8px.</summary>
    Sm,

    /// <summary>16px.</summary>
    Md,

    /// <summary>24px.</summary>
    Lg,

    /// <summary>32px.</summary>
    Xl
}

/// <summary>Cross-axis alignment.</summary>
public enum MnlAlign
{
    /// <summary>Fill the cross axis.</summary>
    Stretch,

    /// <summary>Align to the start.</summary>
    Start,

    /// <summary>Centre.</summary>
    Center,

    /// <summary>Align to the end.</summary>
    End,

    /// <summary>Align text baselines.</summary>
    Baseline
}

/// <summary>Main-axis distribution.</summary>
public enum MnlJustify
{
    /// <summary>Pack at the start.</summary>
    Start,

    /// <summary>Centre.</summary>
    Center,

    /// <summary>Pack at the end.</summary>
    End,

    /// <summary>First item at the start, last at the end.</summary>
    Between
}

/// <summary>Column behaviour of a grid.</summary>
public enum MnlGridColumns
{
    /// <summary>As many columns of at least 16rem as fit.</summary>
    Auto,

    /// <summary>As many columns of at least 12rem as fit.</summary>
    AutoNarrow,

    /// <summary>Two columns from medium screens up.</summary>
    Two,

    /// <summary>Three columns on wide screens, two on medium.</summary>
    Three,

    /// <summary>Four columns on wide screens, two on medium.</summary>
    Four
}

/// <summary>How a two-column split divides its width on wide screens.</summary>
public enum MnlSplitRatio
{
    /// <summary>Equal columns.</summary>
    Even,

    /// <summary>The first column is wider.</summary>
    WideStart,

    /// <summary>The second column is wider.</summary>
    WideEnd,

    /// <summary>The second column is only as wide as its content.</summary>
    AutoEnd
}
