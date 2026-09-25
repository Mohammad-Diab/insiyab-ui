import { forwardRef, type ButtonHTMLAttributes, type HTMLAttributes, type ReactNode } from 'react';
import type { Tone } from './core.js';
import { cx } from './util.js';

/* The attributes the core already listens for, as props. They render exactly the
   attribute the plain-HTML docs show, so the behaviour is the core's own. */
export interface ActionProps {
  /** A tooltip: `true` reads the `aria-label`, a string is its own text. */
  tip?: boolean | string;
  tipSide?: 'top' | 'bottom' | 'start' | 'end';
  /** Ask first. On a yes the click goes ahead as if nothing had asked. */
  confirm?: string;
  confirmOk?: string;
  confirmTone?: 'danger';
  /** A toast when pressed. */
  toast?: string;
  toastTone?: Tone;
  /** Close what the control sits in (`true`), or the element a selector names. */
  dismiss?: boolean | string;
}

export function actionAttrs(p: ActionProps): Record<string, string | undefined> {
  const flag = (v: boolean | string | undefined) => (v === true ? '' : v === false ? undefined : v);
  return {
    'data-ins-tip': flag(p.tip),
    'data-ins-tip-side': p.tipSide,
    'data-ins-confirm': p.confirm,
    'data-ins-confirm-ok': p.confirmOk,
    'data-ins-confirm-tone': p.confirmTone,
    'data-ins-toast': p.toast,
    'data-ins-tone': p.toast ? p.toastTone : undefined,
    'data-ins-dismiss': flag(p.dismiss)
  };
}

export function splitAction<T extends ActionProps>(props: T): [ActionProps, Omit<T, keyof ActionProps>] {
  const { tip, tipSide, confirm, confirmOk, confirmTone, toast, toastTone, dismiss, ...rest } = props;
  return [{ tip, tipSide, confirm, confirmOk, confirmTone, toast, toastTone, dismiss }, rest];
}

export type ButtonVariant =
  | 'primary'
  | 'secondary'
  | 'ghost'
  | 'ghost-danger'
  | 'ghost-success'
  | 'ghost-warning'
  | 'ghost-info'
  | 'success'
  | 'danger'
  | 'warning'
  | 'info'
  | 'bare';

export interface ButtonLook {
  variant?: ButtonVariant;
  size?: 'sm' | 'lg';
  /** A square button holding only an icon. Give it an `aria-label`. */
  iconOnly?: boolean;
  /** As wide as its container. */
  full?: boolean;
  /** Rise under the pointer. */
  lift?: boolean;
}

export function buttonClass(p: ButtonLook, ...more: Array<string | undefined | false>): string | undefined {
  return cx(
    'ins-btn',
    p.variant && 'ins-btn--' + p.variant,
    p.size && 'ins-btn--' + p.size,
    p.iconOnly && 'ins-btn--icon',
    p.full && 'ins-btn--full',
    p.lift && 'ins-btn--lift',
    ...more
  );
}

export interface ButtonProps extends ButtonLook, ActionProps, ButtonHTMLAttributes<HTMLButtonElement> {
  /** A link that looks like a button. */
  href?: string;
  target?: string;
  rel?: string;
  /** A spinner in front, and the button disabled until it is done. */
  loading?: boolean;
}

export const Button = forwardRef<HTMLButtonElement | HTMLAnchorElement, ButtonProps>(function Button(props, ref) {
  const [action, own] = splitAction(props);
  const { variant, size, iconOnly, full, lift, loading, href, className, children, disabled, ...rest } = own;
  const cls = buttonClass({ variant, size, iconOnly, full, lift }, className);
  const inner = (
    <>
      {loading && <span className="ins-spinner ins-spinner--sm" aria-hidden="true" />}
      {children}
    </>
  );
  if (href != null) {
    return (
      <a ref={ref as never} className={cls} href={href} aria-disabled={disabled || undefined} {...actionAttrs(action)} {...(rest as HTMLAttributes<HTMLAnchorElement>)}>
        {inner}
      </a>
    );
  }
  return (
    <button ref={ref as never} className={cls} disabled={disabled || loading} {...actionAttrs(action)} {...rest}>
      {inner}
    </button>
  );
});

export interface ButtonGroupProps extends HTMLAttributes<HTMLDivElement> {
  size?: 'sm';
  /** Names the group for a screen reader; the group gets `role="group"` with it. */
  label?: string;
}
export const ButtonGroup = forwardRef<HTMLDivElement, ButtonGroupProps>(function ButtonGroup({ size, label, className, ...rest }, ref) {
  return (
    <div
      ref={ref}
      className={cx('ins-btn-group', size && 'ins-btn-group--' + size, className)}
      role={label ? 'group' : undefined}
      aria-label={label}
      {...rest}
    />
  );
});

export interface CloseButtonProps extends ActionProps, ButtonHTMLAttributes<HTMLButtonElement> {
  size?: 'sm';
  /** No circle behind the cross. */
  bare?: boolean;
  /** Its name for a screen reader. */
  label?: string;
}
export const CloseButton = forwardRef<HTMLButtonElement, CloseButtonProps>(function CloseButton(props, ref) {
  const [action, own] = splitAction(props);
  const { size, bare, label = 'إغلاق', className, ...rest } = own;
  return (
    <button
      ref={ref}
      className={cx('ins-close', size && 'ins-close--' + size, bare && 'ins-close--bare', className)}
      aria-label={label}
      {...actionAttrs(action)}
      {...rest}
    />
  );
});

/* An icon from the page's own sprite: `<Icon name="i-check" />` is
   `<svg class="ico"><use href="#i-check"/></svg>`. Insiyab ships no icons; this is
   the shape the docs use, for pages that keep a sprite. */
export interface IconProps extends HTMLAttributes<SVGSVGElement> {
  name: string;
  children?: ReactNode;
}
export const Icon = forwardRef<SVGSVGElement, IconProps>(function Icon({ name, className, ...rest }, ref) {
  return (
    <svg ref={ref} className={cx('ico', className)} aria-hidden="true" {...rest}>
      <use href={name.charAt(0) === '#' ? name : '#' + name} />
    </svg>
  );
});
