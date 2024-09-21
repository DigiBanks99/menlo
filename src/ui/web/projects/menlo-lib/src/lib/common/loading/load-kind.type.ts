export const LoadKind = {
    Default: 'default',
    Small: 'small'
} as const;
export type LoadKind = (typeof LoadKind)[keyof typeof LoadKind];
