'use client';
import {
  forwardRef,
  useEffect,
  useImperativeHandle,
  useRef,
  useState,
  type AnchorHTMLAttributes,
  type ButtonHTMLAttributes,
  type CSSProperties,
  type ElementType,
  type HTMLAttributes,
  type ReactNode
} from 'react';
import { core, type ThemeMode } from './core.js';
import { cx } from './util.js';

/* The application shell: a glass sidebar, and a transparent top bar of islands
   that frosts once the page scrolls under it. Below 900px the sidebar is a drawer. */

export interface ShellProps extends HTMLAttributes<HTMLDivElement> {
  /** A `<Sidebar>`. */
  sidebar?: ReactNode;
  /** A `<Topbar>`. */
  topbar?: ReactNode;
  contentClassName?: string;
}
export const Shell = forwardRef<HTMLDivElement, ShellProps>(function Shell({ sidebar, topbar, contentClassName, className, children, ...rest }, ref) {
  return (
    <div ref={ref} className={cx('ins-shell', className)} {...rest}>
      {sidebar}
      {sidebar != null && <div className="ins-shell-scrim" />}
      <div className="ins-shell-main">
        {topbar}
        <main className={cx('ins-shell-content', contentClassName)}>{children}</main>
      </div>
    </div>
  );
});

export interface SidebarProps extends HTMLAttributes<HTMLElement> {
  label?: string;
  /** A `<SidebarBrand>` at the top. */
  brand?: ReactNode;
  /** Held at the bottom (`.ins-shell-bottom`). */
  bottom?: ReactNode;
}
/* The id is the drawer's `:target` below 900px, so it works with no script; it is
   `nav` unless given. */
export const Sidebar = forwardRef<HTMLElement, SidebarProps>(function Sidebar({ label, brand, bottom, id = 'nav', className, children, ...rest }, ref) {
  return (
    <aside ref={ref} className={cx('ins-shell-side', className)} id={id} aria-label={label} {...rest}>
      {brand}
      {children}
      {bottom != null && <div className="ins-shell-bottom">{bottom}</div>}
    </aside>
  );
});

export interface SidebarBrandProps extends AnchorHTMLAttributes<HTMLAnchorElement> {
  /** The letters in the brand mark. */
  mark?: ReactNode;
  name: ReactNode;
  /** A second line under the name. */
  sub?: ReactNode;
}
export const SidebarBrand = forwardRef<HTMLAnchorElement, SidebarBrandProps>(function SidebarBrand({ mark, name, sub, className, ...rest }, ref) {
  return (
    <a ref={ref} className={cx('ins-shell-brand', className)} {...rest}>
      {mark != null && <span className="ins-shell-mark">{mark}</span>}
      <span className="ins-shell-brand-text">
        <span className="ins-shell-brand-name">{name}</span>
        {sub != null && <span className="ins-shell-brand-role">{sub}</span>}
      </span>
    </a>
  );
});

export interface SidebarGroupProps {
  /** The heading over the links that follow it. */
  label: ReactNode;
  children?: ReactNode;
}
export function SidebarGroup({ label, children }: SidebarGroupProps) {
  return (
    <>
      <div className="ins-shell-group">{label}</div>
      {children}
    </>
  );
}

export interface SidebarLinkProps extends AnchorHTMLAttributes<HTMLAnchorElement> {
  icon?: ReactNode;
  /** The page being shown. When it moves to another link, the marker flies there. */
  current?: boolean;
  /** A count at the end (`.ins-shell-link-badge`). */
  badge?: ReactNode;
}
/* The selected link is drawn from the first render; after that the core owns the
   selection, so a `current` that moves is handed to `Insiyab.sidebarSelect`, which
   flies the marker from the old link to the new one as the Windows navigation
   pane's does. */
export const SidebarLink = forwardRef<HTMLAnchorElement, SidebarLinkProps>(function SidebarLink({ icon, current, badge, className, children, ...rest }, ref) {
  const el = useRef<HTMLAnchorElement>(null);
  useImperativeHandle(ref, () => el.current!, []);
  const [first] = useState(!!current);
  const mounted = useRef(false);
  useEffect(() => {
    if (!mounted.current) {
      mounted.current = true;
      return;
    }
    if (current && el.current) core()?.sidebarSelect(el.current);
  }, [current]);
  return (
    <a ref={el} className={cx('ins-shell-link', first && 'is-active', className)} aria-current={first ? 'page' : undefined} {...rest}>
      {icon}
      {children}
      {badge != null && <span className="ins-shell-link-badge">{badge}</span>}
    </a>
  );
});

export interface TopbarProps extends HTMLAttributes<HTMLElement> {
  start?: ReactNode;
  center?: ReactNode;
  end?: ReactNode;
}
export const Topbar = forwardRef<HTMLElement, TopbarProps>(function Topbar({ start, center, end, className, children, ...rest }, ref) {
  return (
    <header ref={ref} className={cx('ins-topbar', className)} {...rest}>
      {start != null && <div className="ins-zone ins-zone--start">{start}</div>}
      {center != null && <div className="ins-zone ins-zone--center">{center}</div>}
      {end != null && <div className="ins-zone ins-zone--end">{end}</div>}
      {children}
    </header>
  );
});

export interface IslandProps extends HTMLAttributes<HTMLElement> {
  /** `icon` for a round icon button, `brand` for the logo, `title` for the page's name. */
  variant?: 'icon' | 'title' | 'brand';
  /** `button`, `a`… a plain `span` by default. */
  as?: ElementType;
  href?: string;
  style?: CSSProperties;
}
/* One floating glass pill in the top bar. */
export const Island = forwardRef<HTMLElement, IslandProps>(function Island({ variant, as, className, ...rest }, ref) {
  const Tag = as ?? (rest.href != null ? 'a' : 'span');
  return <Tag ref={ref} className={cx('ins-island', variant && 'ins-island--' + variant, className)} {...rest} />;
});

export interface TopbarTitleProps extends Omit<HTMLAttributes<HTMLDivElement>, 'title'> {
  title: ReactNode;
  sub?: ReactNode;
}
/* The page's name, centred in the bar. */
export const TopbarTitle = forwardRef<HTMLDivElement, TopbarTitleProps>(function TopbarTitle({ title, sub, className, ...rest }, ref) {
  return (
    <div ref={ref} className={cx('ins-island', 'ins-island--title', className)} {...rest}>
      <span className="ins-shell-head">
        <span className="ins-shell-title">{title}</span>
        {sub != null && <span className="ins-shell-sub">{sub}</span>}
      </span>
    </div>
  );
});

export interface SidebarToggleProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  label?: string;
}
/* The menu button: collapses the sidebar on a desktop, opens the drawer on a phone. */
export const SidebarToggle = forwardRef<HTMLButtonElement, SidebarToggleProps>(function SidebarToggle({ label = 'القائمة', className, ...rest }, ref) {
  return <button ref={ref} type="button" className={cx('ins-island', 'ins-island--icon', className)} data-ins-sidebar="" aria-label={label} {...rest} />;
});

export interface ThemeToggleProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  /** `system` forgets the choice and follows the OS again; nothing flips between light and dark. */
  mode?: ThemeMode;
}
/* The theme switch, with the core's circle growing out of it. Its `aria-pressed` is
   the theme actually showing, which only the browser knows: the server's HTML and
   the first render cannot agree on it, and are not asked to. */
export const ThemeToggle = forwardRef<HTMLButtonElement, ThemeToggleProps>(function ThemeToggle({ mode, ...rest }, ref) {
  const el = useRef<HTMLButtonElement>(null);
  useImperativeHandle(ref, () => el.current!, []);
  /* The core writes this in its first scan, which ran before this button existed. */
  useEffect(() => {
    const api = core();
    if (api && el.current && !mode) el.current.setAttribute('aria-pressed', String(api.theme() === 'dark'));
  }, [mode]);
  return <button ref={el} type="button" data-ins-theme-toggle={mode ?? ''} suppressHydrationWarning {...rest} />;
});
