export interface FoundationThemePreview {
  readonly label: 'Latte' | 'Mocha';
  readonly mode: 'light' | 'dark';
  readonly backgroundHex: string;
  readonly previewStyle: string;
}

export interface PaletteTokenRow {
  readonly token: string;
  readonly latte: string;
  readonly mocha: string;
  readonly latteContrast: string;
  readonly mochaContrast: string;
}

export interface TypographyRole {
  readonly role: string;
  readonly classes: string;
  readonly sample: string;
  readonly note: string;
}

export interface SpacingScaleItem {
  readonly step: number;
  readonly pixels: number;
  readonly rem: string;
  readonly classNames: string;
}

export interface TokenExample {
  readonly label: string;
  readonly classes: string;
  readonly note: string;
}

const latteBackground = '#eff1f5';
const mochaBackground = '#1e1e2e';

const previewThemes = [
  {
    label: 'Latte',
    mode: 'light',
    backgroundHex: latteBackground,
    variables: {
      '--mnl-color-bg': '#eff1f5',
      '--mnl-color-surface': '#ffffff',
      '--mnl-color-surface-alt': '#e6e9ef',
      '--mnl-color-surface-muted': '#ccd0da',
      '--mnl-color-border': '#bcc0cc',
      '--mnl-color-text': '#4c4f69',
      '--mnl-color-subtext': '#6c6f85',
      '--mnl-color-accent': '#ea76cb',
      '--mnl-color-accent-strong': '#8839ef',
      '--mnl-color-success': '#40a02b',
      '--mnl-color-warning': '#df8e1d',
      '--mnl-color-error': '#d20f39',
      '--mnl-color-info': '#1e66f5',
    },
  },
  {
    label: 'Mocha',
    mode: 'dark',
    backgroundHex: mochaBackground,
    variables: {
      '--mnl-color-bg': '#1e1e2e',
      '--mnl-color-surface': '#313244',
      '--mnl-color-surface-alt': '#45475a',
      '--mnl-color-surface-muted': '#585b70',
      '--mnl-color-border': '#6c7086',
      '--mnl-color-text': '#cdd6f4',
      '--mnl-color-subtext': '#a6adc8',
      '--mnl-color-accent': '#f5c2e7',
      '--mnl-color-accent-strong': '#cba6f7',
      '--mnl-color-success': '#a6e3a1',
      '--mnl-color-warning': '#f9e2af',
      '--mnl-color-error': '#f38ba8',
      '--mnl-color-info': '#89b4fa',
    },
  },
] as const;

const paletteTokens = [
  ['rosewater', '#dc8a78', '#f5e0dc'],
  ['flamingo', '#dd7878', '#f2cdcd'],
  ['pink', '#ea76cb', '#f5c2e7'],
  ['mauve', '#8839ef', '#cba6f7'],
  ['red', '#d20f39', '#f38ba8'],
  ['maroon', '#e64553', '#eba0ac'],
  ['peach', '#fe640b', '#fab387'],
  ['yellow', '#df8e1d', '#f9e2af'],
  ['green', '#40a02b', '#a6e3a1'],
  ['teal', '#179299', '#94e2d5'],
  ['sky', '#04a5e5', '#89dceb'],
  ['sapphire', '#209fb5', '#74c7ec'],
  ['blue', '#1e66f5', '#89b4fa'],
  ['lavender', '#7287fd', '#b4befe'],
  ['text', '#4c4f69', '#cdd6f4'],
  ['subtext1', '#5c5f77', '#bac2de'],
  ['subtext0', '#6c6f85', '#a6adc8'],
  ['overlay2', '#7c7f93', '#9399b2'],
  ['overlay1', '#8c8fa1', '#7f849c'],
  ['overlay0', '#9ca0b0', '#6c7086'],
  ['surface2', '#acb0be', '#585b70'],
  ['surface1', '#bcc0cc', '#45475a'],
  ['surface0', '#ccd0da', '#313244'],
  ['base', '#eff1f5', '#1e1e2e'],
  ['mantle', '#e6e9ef', '#181825'],
  ['crust', '#dce0e8', '#11111b'],
] as const;

export const foundationThemes: readonly FoundationThemePreview[] = previewThemes.map((theme) => ({
  label: theme.label,
  mode: theme.mode,
  backgroundHex: theme.backgroundHex,
  previewStyle: toInlineStyle({
    ...theme.variables,
    'background-color': 'var(--mnl-color-bg)',
    color: 'var(--mnl-color-text)',
    'color-scheme': theme.mode,
  }),
}));

export const semanticTokenExamples: readonly TokenExample[] = [
  {
    label: 'App background',
    classes: 'bg-mnl-bg text-mnl-text',
    note: 'Primary page canvas',
  },
  {
    label: 'Surface',
    classes: 'bg-mnl-surface ring-1 ring-mnl-border',
    note: 'Cards, panels, and containers',
  },
  {
    label: 'Accent',
    classes: 'bg-mnl-accent text-[#11111b]',
    note: 'Primary actions and highlights',
  },
  {
    label: 'Success',
    classes: 'bg-mnl-success text-[#11111b]',
    note: 'Positive budget states',
  },
  {
    label: 'Warning',
    classes: 'bg-mnl-warning text-[#11111b]',
    note: 'Attention states',
  },
  {
    label: 'Error',
    classes: 'bg-mnl-error text-[#11111b]',
    note: 'Destructive and error states',
  },
  {
    label: 'Info',
    classes: 'bg-mnl-info text-[#11111b]',
    note: 'Informational status',
  },
];

export const paletteTokenRows: readonly PaletteTokenRow[] = paletteTokens.map(
  ([token, latte, mocha]) => ({
    token,
    latte,
    mocha,
    latteContrast: formatContrastRatio(latte, latteBackground),
    mochaContrast: formatContrastRatio(mocha, mochaBackground),
  }),
);

export const typographyRoles: readonly TypographyRole[] = [
  {
    role: 'Page title',
    classes: 'text-4xl font-bold tracking-tight',
    sample: 'Household overview',
    note: 'Top-level dashboard and feature headers',
  },
  {
    role: 'Section heading',
    classes: 'text-2xl font-semibold',
    sample: 'Monthly budget',
    note: 'Section and panel headers',
  },
  {
    role: 'Card title',
    classes: 'text-lg font-semibold',
    sample: 'Emergency fund',
    note: 'Reusable island component headings',
  },
  {
    role: 'Body',
    classes: 'text-base font-normal leading-7',
    sample: 'Track spending, plan ahead, and keep the family aligned on financial priorities.',
    note: 'Primary descriptive copy',
  },
  {
    role: 'Caption',
    classes: 'text-sm font-medium text-mnl-subtext',
    sample: 'Last synced 5 minutes ago',
    note: 'Secondary metadata and helper text',
  },
  {
    role: 'Large value',
    classes: 'text-5xl font-bold tracking-tight',
    sample: 'R 28 450',
    note: 'Prominent financial statistics',
  },
];

export const spacingScale: readonly SpacingScaleItem[] = Array.from({ length: 16 }, (_, index) => {
  const step = index + 1;
  const pixels = step * 4;

  return {
    step,
    pixels,
    rem: `${(pixels / 16).toFixed(2).replace(/\.00$/, '')}rem`,
    classNames: `p-${step} / gap-${step} / space-y-${step}`,
  };
});

export const shadowExamples: readonly TokenExample[] = [
  {
    label: 'Card shadow',
    classes: 'rounded-2xl bg-mnl-surface shadow-sm ring-1 ring-mnl-border',
    note: 'Default container elevation',
  },
  {
    label: 'Elevated shadow',
    classes: 'rounded-2xl bg-mnl-surface shadow-md ring-1 ring-mnl-border',
    note: 'Dialogs, menus, and lifted interactions',
  },
];

export const radiusExamples: readonly TokenExample[] = [
  {
    label: 'Button radius',
    classes: 'rounded-xl bg-mnl-surface-alt ring-1 ring-mnl-border',
    note: '12px default for interactive controls',
  },
  {
    label: 'Card radius',
    classes: 'rounded-2xl bg-mnl-surface-alt ring-1 ring-mnl-border',
    note: '16px default for cards and panels',
  },
  {
    label: 'Pill radius',
    classes: 'rounded-full bg-mnl-surface-alt ring-1 ring-mnl-border',
    note: 'Badges, chips, and avatars',
  },
];

function formatContrastRatio(foregroundHex: string, backgroundHex: string): string {
  const ratio = getContrastRatio(foregroundHex, backgroundHex);
  return `${ratio.toFixed(2)}:1`;
}

function getContrastRatio(foregroundHex: string, backgroundHex: string): number {
  const foregroundLuminance = getRelativeLuminance(foregroundHex);
  const backgroundLuminance = getRelativeLuminance(backgroundHex);
  const lighter = Math.max(foregroundLuminance, backgroundLuminance);
  const darker = Math.min(foregroundLuminance, backgroundLuminance);

  return (lighter + 0.05) / (darker + 0.05);
}

function getRelativeLuminance(hex: string): number {
  const [red, green, blue] = hexToRgb(hex).map((channel) => {
    const value = channel / 255;
    return value <= 0.03928 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4;
  });

  return 0.2126 * red + 0.7152 * green + 0.0722 * blue;
}

function hexToRgb(hex: string): [number, number, number] {
  const normalized = hex.replace('#', '');
  const value = normalized.length === 3 ? normalized.replace(/(.)/g, '$1$1') : normalized;

  return [
    Number.parseInt(value.slice(0, 2), 16),
    Number.parseInt(value.slice(2, 4), 16),
    Number.parseInt(value.slice(4, 6), 16),
  ];
}

function toInlineStyle(values: Record<string, string>): string {
  return Object.entries(values)
    .map(([key, value]) => `${key}: ${value}`)
    .join('; ');
}
