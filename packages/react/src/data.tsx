import {
  forwardRef,
  type AnchorHTMLAttributes,
  type ElementType,
  type HTMLAttributes,
  type ReactNode,
  type TableHTMLAttributes
} from 'react';
import { cx } from './util.js';

export interface TableProps extends TableHTMLAttributes<HTMLTableElement> {
  /** The scrolling wrap around the table. On by default; off for a table that is already in one. */
  wrap?: boolean;
  wrapClassName?: string;
}
/* A selected row is `<tr aria-selected="true">`, `aria-current` or `.is-selected`:
   it gets the brand marker, as the sidebar's selected item does. */
export const Table = forwardRef<HTMLTableElement, TableProps>(function Table({ wrap = true, wrapClassName, className, ...rest }, ref) {
  const table = <table ref={ref} className={cx('ins-table', className)} {...rest} />;
  return wrap ? <div className={cx('ins-table-wrap', wrapClassName)}>{table}</div> : table;
});

export interface StatProps extends Omit<HTMLAttributes<HTMLDivElement>, 'title'> {
  label: ReactNode;
  value: ReactNode;
  icon?: ReactNode;
  /** A line under the value. */
  hint?: ReactNode;
  /** Shown after the value, which is then set as money. */
  currency?: ReactNode;
  tone?: 'ok' | 'warn' | 'bad' | 'info' | 'mute' | 'zero';
  size?: 'sm' | 'lg';
  /** The glass surface under it. On by default. */
  glass?: boolean;
}
export const Stat = forwardRef<HTMLDivElement, StatProps>(function Stat(
  { label, value, icon, hint, currency, tone, size, glass = true, className, ...rest },
  ref
) {
  return (
    <div ref={ref} className={cx('ins-stat', glass && 'ins-glass', tone && 'ins-stat--' + tone, size && 'ins-stat--' + size, className)} {...rest}>
      {icon != null && <span className="ins-stat-ico">{icon}</span>}
      <span className="ins-stat-body">
        <span className="ins-stat-label">{label}</span>
        {currency != null ? (
          <span className="ins-stat-value ins-money">
            <span className="ins-money-val">{value}</span>
            <span className="ins-money-cur">{currency}</span>
          </span>
        ) : (
          <span className="ins-stat-value">{value}</span>
        )}
        {hint != null && <span className="ins-stat-hint">{hint}</span>}
      </span>
    </div>
  );
});

export interface PillProps extends HTMLAttributes<HTMLElement> {
  tone?: 'ok' | 'warn' | 'bad' | 'info' | 'brand';
  /** The dot before the text. On by default. */
  dot?: boolean;
  /** `button` for a pill that filters, say. */
  as?: ElementType;
}
export const Pill = forwardRef<HTMLElement, PillProps>(function Pill({ tone, dot = true, as: Tag = 'span', className, children, ...rest }, ref) {
  return (
    <Tag ref={ref} className={cx('ins-pill', tone && 'ins-pill--' + tone, className)} {...rest}>
      {dot && <span className="ins-pill-dot" />}
      {children}
    </Tag>
  );
});

export interface BadgeProps extends HTMLAttributes<HTMLSpanElement> {
  tone?: 'ok' | 'warn' | 'bad' | 'mute';
}
export const Badge = forwardRef<HTMLSpanElement, BadgeProps>(function Badge({ tone, className, ...rest }, ref) {
  return <span ref={ref} className={cx('ins-badge', tone && 'ins-badge--' + tone, className)} {...rest} />;
});

export interface AvatarProps extends HTMLAttributes<HTMLSpanElement> {
  size?: 'sm' | 'lg';
}
export const Avatar = forwardRef<HTMLSpanElement, AvatarProps>(function Avatar({ size, className, ...rest }, ref) {
  return <span ref={ref} className={cx('ins-avatar', size && 'ins-avatar--' + size, className)} {...rest} />;
});

export interface ListProps extends HTMLAttributes<HTMLElement> {
  /** `nav` for a list of links, whose items are then `<a>`s. */
  as?: ElementType;
}
export const List = forwardRef<HTMLElement, ListProps>(function List({ as: Tag = 'ul', className, ...rest }, ref) {
  return <Tag ref={ref} className={cx('ins-list', className)} {...rest} />;
});

export interface ListItemProps extends Omit<AnchorHTMLAttributes<HTMLElement>, 'title'> {
  title?: ReactNode;
  desc?: ReactNode;
  /** Before the text: an avatar or an icon chip. */
  start?: ReactNode;
  /** At the far end: a pill, a badge, a button. */
  end?: ReactNode;
  /** With `href` the item is a link, and `current` marks it as this page. */
  current?: boolean;
}
export const ListItem = forwardRef<HTMLElement, ListItemProps>(function ListItem(
  { title, desc, start, end, href, current, className, children, ...rest },
  ref
) {
  const Tag: ElementType = href != null ? 'a' : 'li';
  return (
    <Tag ref={ref as never} className={cx('ins-list-item', className)} href={href} aria-current={current ? 'page' : undefined} {...rest}>
      {start}
      {(title != null || desc != null) && (
        <span className="ins-list-text">
          {title != null && <span className="ins-list-title">{title}</span>}
          {desc != null && <span className="ins-list-desc">{desc}</span>}
        </span>
      )}
      {children}
      {end != null && <span className="ins-list-end">{end}</span>}
    </Tag>
  );
});
