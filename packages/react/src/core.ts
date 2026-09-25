/* The core, as React sees it: `window.Insiyab`, typed. The wrapper never bundles
   the core. The page loads it (a <script> in <head>, or `import 'insiyab'`), and
   every component asks for it here, at the moment it needs it, so a component
   rendered on a server or before the script arrives simply renders its markup. */

export type Tone = 'ok' | 'warn' | 'bad' | 'info';
export type ThemeMode = 'dark' | 'light' | 'system';
type Target = Element | string;

export interface ConfirmOptions {
  title?: string;
  confirm?: string;
  cancel?: string;
  tone?: 'danger';
}

export interface PhoneValue {
  value: string;
  country: string;
  valid: boolean;
}

export interface PaletteItemSpec {
  label: string;
  href?: string;
  run?: (event: MouseEvent) => void;
  group?: string;
  icon?: string | Element;
  keywords?: string | string[];
  hint?: string;
  key?: string;
}

export interface InsiyabApi {
  version: string;
  init<T extends Element | Document>(scope?: T): T;
  define(name: string, fn: (scope: Element | Document) => void): void;
  theme(mode?: ThemeMode): 'dark' | 'light';
  toggleTheme(): void;
  sidebar(state?: 'open' | 'collapsed' | 'toggle'): unknown;
  sidebarSelect(link: Target): unknown;
  brand(hex?: string): unknown;
  toast(message: string, tone?: Tone): HTMLElement | null;
  dialog(target: Target, action?: 'open' | 'close'): HTMLDialogElement | null;
  dismiss(target: Target): Element | null;
  tab(tab: Target): Element | null;
  navbar(target: Target, open?: boolean): unknown;
  confirm(message: string, options?: ConfirmOptions): Promise<boolean> | null;
  calendar(name: string, calendar?: unknown): unknown;
  norm(text: string): string;
  wizard(target: Target, step?: number): number | null;
  date(input: Target, iso?: string): string | null;
  scrollTop(): number;
  color: {
    derive(hex: string, theme: 'dark' | 'light'): Record<string, string>;
    shift(hex: string, amount: number): string;
    contrast(a: string, b: string): number | null;
    labelFor(hex: string): string;
  };
  /* The plugins add these when they are loaded. */
  otp?(field: Target, value?: string): string | null;
  phone?(field: Target, value?: string): PhoneValue | null;
  file?: ((field: Target, files?: File[]) => File[] | null) & {
    progress(file: File, fraction: number): void;
    done(file: File, id: string): void;
    fail(file: File, message?: string): void;
  };
  colorField?(field: Target, value?: string): string | null;
  tree?(target: Target, action?: string): unknown;
  carousel?(el: Target, index?: number): number | null;
  scrollspy?(nav: Target): Element | null;
  time?(scope?: Target): unknown;
  palette?: ((target?: Target | 'open' | 'close' | 'toggle', action?: 'open' | 'close' | 'toggle') => HTMLDialogElement | null) & {
    add(items: PaletteItemSpec | PaletteItemSpec[], target?: Target): HTMLElement[];
  };
}

declare global {
  interface Window {
    Insiyab?: InsiyabApi;
  }
}

let warned = false;

/* The core, or undefined on a server, before the script has run, or on a page
   that never loaded it. Said once in the console, in that last case. */
export function core(): InsiyabApi | undefined {
  if (typeof window === 'undefined') return undefined;
  const api = window.Insiyab;
  if (!api && !warned && typeof document !== 'undefined' && document.readyState !== 'loading') {
    warned = true;
    console.warn('[insiyab/react] window.Insiyab is missing: load insiyab.js in <head>, or import "insiyab", before the app renders.');
  }
  return api;
}

/* ------------------------------------------------------------ the functions */
/* The imperative calls, safe to make anywhere: on a server or with no core they do
   nothing and return what "nothing happened" looks like. */

export function toast(message: string, tone?: Tone): HTMLElement | null {
  return core()?.toast(message, tone) ?? null;
}

export function confirm(message: string, options?: ConfirmOptions): Promise<boolean> {
  return core()?.confirm(message, options) ?? Promise.resolve(false);
}

export function theme(mode?: ThemeMode): 'dark' | 'light' | undefined {
  return core()?.theme(mode);
}

export function toggleTheme(): void {
  core()?.toggleTheme();
}

export function brand(hex: string): void {
  core()?.brand(hex);
}

export function sidebar(state?: 'open' | 'collapsed' | 'toggle'): void {
  core()?.sidebar(state);
}
