export type MnlToastVariant = 'success' | 'warning' | 'error' | 'info';

export interface MnlToastOptions {
  readonly dismissible?: boolean;
  readonly duration?: number;
  readonly variant?: MnlToastVariant;
}

export interface MnlToastEntry {
  readonly dismissible: boolean;
  readonly duration: number;
  readonly id: number;
  readonly message: string;
  readonly variant: MnlToastVariant;
}
