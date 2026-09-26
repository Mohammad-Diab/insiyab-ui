'use client';
import {
  forwardRef,
  useEffect,
  useImperativeHandle,
  useRef,
  useState,
  type AnchorHTMLAttributes,
  type ButtonHTMLAttributes,
  type DetailsHTMLAttributes,
  type DialogHTMLAttributes,
  type ElementType,
  type HTMLAttributes,
  type ReactNode,
  type Ref,
  type RefObject
} from 'react';
import { buttonClass, CloseButton, type ButtonLook } from './buttons.js';
import { core } from './core.js';
import { cx, useInsEvent, useIsoLayoutEffect, useLatest } from './util.js';

/* ---------------------------------------------------------- dialog, drawer */

export interface DialogProps extends Omit<DialogHTMLAttributes<HTMLDialogElement>, 'title' | 'open'> {
  open?: boolean;
  defaultOpen?: boolean;
  /** Told whenever it opens or closes, however that happened: Escape, a close button, `data-ins-dialog` elsewhere. */
  onOpenChange?: (open: boolean) => void;
  title?: ReactNode;
  titleAs?: ElementType;
  /** An icon chip before the title. */
  icon?: ReactNode;
  footer?: ReactNode;
  size?: 'sm' | 'lg';
  /** The close button in the head. On by default when there is a head. */
  closeButton?: boolean;
  closeLabel?: string;
  /** The children straight into the dialog, with no head, body or foot around them. */
  bare?: boolean;
  bodyClassName?: string;
}

function useModal(el: RefObject<HTMLDialogElement | null>, open: boolean | undefined, defaultOpen: boolean | undefined, onOpenChange?: (open: boolean) => void) {
  const report = useLatest(onOpenChange);
  const want = open ?? defaultOpen;
  useIsoLayoutEffect(() => {
    const d = el.current;
    if (!d || want === undefined || d.open === want) return;
    const api = core();
    if (api) api.dialog(d, want ? 'open' : 'close');
    else if (want && typeof d.showModal === 'function') d.showModal();
    else if (!want) d.close();
  }, [want]);
  /* `open` is an attribute, so one observer hears every way a dialog opens or
     closes, the core's and the platform's alike. */
  useEffect(() => {
    const d = el.current;
    if (!d) return;
    let last = d.open;
    const mo = new MutationObserver(() => {
      if (d.open !== last) {
        last = d.open;
        report.current?.(last);
      }
    });
    mo.observe(d, { attributes: true, attributeFilter: ['open'] });
    return () => mo.disconnect();
  }, []);
}

function Modal({ kind, props, ref }: { kind: string; props: DialogProps; ref: Ref<HTMLDialogElement> }) {
  const { open, defaultOpen, onOpenChange, title, titleAs: T = 'h3', icon, footer, size, closeButton, closeLabel, bare, bodyClassName, className, children, ...rest } = props;
  const el = useRef<HTMLDialogElement>(null);
  useImperativeHandle(ref, () => el.current!, []);
  useModal(el, open, defaultOpen, onOpenChange);
  const head = title != null || icon != null;
  return (
    <dialog ref={el} className={cx(kind, size && kind + '--' + size, className)} {...rest}>
      {bare ? (
        children
      ) : (
        <>
          {head && (
            <div className="ins-dialog-head">
              {icon != null && <span className="ins-panel-ico">{icon}</span>}
              {title != null && <T className="ins-dialog-title">{title}</T>}
              {closeButton !== false && <CloseButton dismiss label={closeLabel} />}
            </div>
          )}
          <div className={cx('ins-dialog-body', bodyClassName)}>{children}</div>
          {footer != null && <div className="ins-dialog-foot">{footer}</div>}
        </>
      )}
    </dialog>
  );
}

/* A real <dialog>: Escape, the focus trap and the top layer are the platform's. */
export const Dialog = forwardRef<HTMLDialogElement, DialogProps>(function Dialog(props, ref) {
  return <Modal kind="ins-dialog" props={props} ref={ref} />;
});

export interface DrawerProps extends Omit<DialogProps, 'size'> {
  /** The edge it comes in from. The start edge by default: the right, in Arabic. */
  side?: 'start' | 'end' | 'bottom';
}
/* The same dialog, arriving from an edge. */
export const Drawer = forwardRef<HTMLDialogElement, DrawerProps>(function Drawer({ side, className, ...props }, ref) {
  return <Modal kind="ins-drawer" props={{ ...props, className: cx(side && side !== 'start' && 'ins-drawer--' + side, className) }} ref={ref} />;
});

/* ------------------------------------------------------------ menu, popover */

export interface MenuProps extends Omit<DetailsHTMLAttributes<HTMLDetailsElement>, 'title'>, ButtonLook {
  /** What the button says. */
  label: ReactNode;
  /** The button's name for a screen reader, for an icon-only button. */
  buttonLabel?: string;
  /** Replaces the button look, for a trigger styled as something else (an island, say). */
  summaryClassName?: string;
  /** Hang from the trigger's trailing edge instead of its leading one (`ins-pop--end`),
      for a trigger at the far end of a bar. */
  end?: boolean;
  /** The default alignment, kept so older code still compiles (`ins-pop--start`). */
  start?: boolean;
  onOpenChange?: (open: boolean) => void;
  bodyClassName?: string;
}

function Pop({ card, props, ref }: { card: boolean; props: MenuProps; ref: Ref<HTMLDetailsElement> }) {
  const { label, buttonLabel, summaryClassName, start, end, onOpenChange, bodyClassName, variant = 'secondary', size, iconOnly, full, lift, className, children, onToggle, ...rest } = props;
  return (
    <details
      ref={ref}
      className={cx('ins-pop', start && 'ins-pop--start', end && 'ins-pop--end', className)}
      onToggle={(e) => {
        onToggle?.(e);
        onOpenChange?.(e.currentTarget.open);
      }}
      {...rest}
    >
      <summary className={summaryClassName ?? buttonClass({ variant, size, iconOnly, full, lift })} aria-label={buttonLabel} aria-haspopup={card ? undefined : 'menu'}>
        {label}
      </summary>
      <div className={cx('ins-pop-body', card && 'ins-pop-body--card', bodyClassName)} role={card ? undefined : 'menu'}>
        {children}
      </div>
    </details>
  );
}

/* A menu button on a <details>: it opens with no script; the core adds Escape, the
   arrow keys, closing on a choice, and flipping at the viewport's edge. */
export const Menu = forwardRef<HTMLDetailsElement, MenuProps>(function Menu(props, ref) {
  return <Pop card={false} props={props} ref={ref} />;
});

/* The same trigger, opening a card instead of a list of items. */
export const Popover = forwardRef<HTMLDetailsElement, MenuProps>(function Popover(props, ref) {
  return <Pop card props={props} ref={ref} />;
});

type ItemProps = ButtonHTMLAttributes<HTMLButtonElement> &
  Pick<AnchorHTMLAttributes<HTMLAnchorElement>, 'href' | 'target' | 'rel'> & {
    /** The destructive look. */
    tone?: 'bad';
  };

export const MenuItem = forwardRef<HTMLButtonElement | HTMLAnchorElement, ItemProps>(function MenuItem({ tone, href, className, ...rest }, ref) {
  const cls = cx('ins-pop-item', tone && 'ins-pop-item--' + tone, className);
  if (href != null) return <a ref={ref as never} className={cls} href={href} role="menuitem" tabIndex={-1} {...(rest as HTMLAttributes<HTMLAnchorElement>)} />;
  return <button ref={ref as never} type="button" className={cls} role="menuitem" tabIndex={-1} {...rest} />;
});

export interface MenuCheckProps extends Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'role'> {
  checked?: boolean;
  defaultChecked?: boolean;
  onCheckedChange?: (checked: boolean) => void;
}

function useMenuCheck(role: 'menuitemcheckbox' | 'menuitemradio', p: MenuCheckProps, ref: Ref<HTMLButtonElement>) {
  const { checked, defaultChecked, onCheckedChange, className, ...rest } = p;
  const el = useRef<HTMLButtonElement>(null);
  useImperativeHandle(ref, () => el.current!, []);
  const [, force] = useState(0);
  useInsEvent<{ item: Element; checked: boolean }>(
    'ins:menu',
    (d) => {
      onCheckedChange?.(d.checked);
      if (checked !== undefined) force((n) => n + 1);
    },
    (d) => d.item === el.current
  );
  /* A radio's neighbours are unchecked by the core without an event of their own. */
  useIsoLayoutEffect(() => {
    if (checked !== undefined) el.current?.setAttribute('aria-checked', String(checked));
  });
  return (
    <button
      ref={el}
      type="button"
      className={cx('ins-pop-item', className)}
      role={role}
      aria-checked={checked ?? defaultChecked ?? false}
      tabIndex={-1}
      {...rest}
    />
  );
}

/* A setting in a menu. The menu stays open for the next one. */
export const MenuCheckbox = forwardRef<HTMLButtonElement, MenuCheckProps>(function MenuCheckbox(props, ref) {
  return useMenuCheck('menuitemcheckbox', props, ref);
});

/* One of a set; the core moves the check within its `MenuGroup`, or within the menu. */
export const MenuRadio = forwardRef<HTMLButtonElement, MenuCheckProps>(function MenuRadio(props, ref) {
  return useMenuCheck('menuitemradio', props, ref);
});

export const MenuLabel = forwardRef<HTMLDivElement, HTMLAttributes<HTMLDivElement>>(function MenuLabel({ className, ...rest }, ref) {
  return <div ref={ref} className={cx('ins-pop-label', className)} {...rest} />;
});

export const MenuSeparator = forwardRef<HTMLHRElement, HTMLAttributes<HTMLHRElement>>(function MenuSeparator({ className, ...rest }, ref) {
  return <hr ref={ref} className={cx('ins-pop-sep', className)} role="separator" {...rest} />;
});

export const MenuGroup = forwardRef<HTMLDivElement, HTMLAttributes<HTMLDivElement>>(function MenuGroup(props, ref) {
  return <div ref={ref} role="group" {...props} />;
});
