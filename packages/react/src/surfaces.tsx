import { forwardRef, type ElementType, type HTMLAttributes, type ReactNode } from 'react';
import { cx } from './util.js';

type Box = HTMLAttributes<HTMLElement> & { as?: ElementType };

export interface GlassProps extends Box {
  /** The flat look, for a surface inside a surface (`ins-glass-inner`). */
  inner?: boolean;
}
/* The frosted surface. Nested glass drops its own frost by itself; `inner` is the
   flat look on purpose. */
export const Glass = forwardRef<HTMLElement, GlassProps>(function Glass({ as: Tag = 'div', inner, className, ...rest }, ref) {
  return <Tag ref={ref} className={cx(inner ? 'ins-glass-inner' : 'ins-glass', className)} {...rest} />;
});

export interface CardProps extends Box {
  /** Rise under the pointer. Only for a card that is not itself a link or a button, which rise anyway. */
  hoverable?: boolean;
}
export const Card = forwardRef<HTMLElement, CardProps>(function Card({ as: Tag = 'div', hoverable, className, ...rest }, ref) {
  return <Tag ref={ref} className={cx('ins-card', hoverable && 'ins-hoverable', className)} {...rest} />;
});

export interface PanelProps extends Omit<Box, 'title'> {
  title?: ReactNode;
  /** The element for the title. */
  titleAs?: ElementType;
  /** An icon chip before the title. */
  icon?: ReactNode;
  /** At the end of the head: a button, a badge, a segmented control. */
  actions?: ReactNode;
  /** A row of buttons under the body (`.ins-panel-foot`). */
  footer?: ReactNode;
  /** A line of small print at the top of the body (`.ins-panel-note`). */
  note?: ReactNode;
  /** The warning look (`ins-panel--alert`). */
  alert?: boolean;
  /** A body with no padding, for a table or a list that runs edge to edge. */
  flush?: boolean;
  /** Put the children straight into the panel, with no `.ins-panel-body` around them. */
  bare?: boolean;
  bodyClassName?: string;
}

/* A panel: an optional head (icon, title, actions), a body, an optional foot. */
export const Panel = forwardRef<HTMLElement, PanelProps>(function Panel(
  { as: Tag = 'div', title, titleAs, icon, actions, footer, note, alert, flush, bare, bodyClassName, className, children, ...rest },
  ref
) {
  const head = title != null || icon != null || actions != null;
  return (
    <Tag ref={ref} className={cx('ins-panel', alert && 'ins-panel--alert', className)} {...rest}>
      {head && <PanelHead title={title} titleAs={titleAs} icon={icon} actions={actions} />}
      {bare ? (
        children
      ) : (
        <PanelBody flush={flush} className={bodyClassName}>
          {note != null && <p className="ins-panel-note">{note}</p>}
          {children}
        </PanelBody>
      )}
      {footer != null && <div className="ins-panel-foot">{footer}</div>}
    </Tag>
  );
});

export interface PanelHeadProps extends Omit<HTMLAttributes<HTMLDivElement>, 'title'> {
  title?: ReactNode;
  titleAs?: ElementType;
  icon?: ReactNode;
  actions?: ReactNode;
}
export const PanelHead = forwardRef<HTMLDivElement, PanelHeadProps>(function PanelHead({ title, titleAs: T = 'h3', icon, actions, className, children, ...rest }, ref) {
  return (
    <div ref={ref} className={cx('ins-panel-head', className)} {...rest}>
      {icon != null && <span className="ins-panel-ico">{icon}</span>}
      {title != null && <T className="ins-panel-title">{title}</T>}
      {children}
      {actions != null && <div className="ins-panel-actions">{actions}</div>}
    </div>
  );
});

export interface PanelBodyProps extends HTMLAttributes<HTMLDivElement> {
  flush?: boolean;
}
export const PanelBody = forwardRef<HTMLDivElement, PanelBodyProps>(function PanelBody({ flush, className, ...rest }, ref) {
  return <div ref={ref} className={cx('ins-panel-body', flush && 'ins-panel-body--flush', className)} {...rest} />;
});

export const PanelFoot = forwardRef<HTMLDivElement, HTMLAttributes<HTMLDivElement>>(function PanelFoot({ className, ...rest }, ref) {
  return <div ref={ref} className={cx('ins-panel-foot', className)} {...rest} />;
});
