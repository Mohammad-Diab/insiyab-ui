import { forwardRef, type ElementType, type HTMLAttributes, type ReactNode } from 'react';
import { cx, gap, type Space } from './util.js';

/* The layout primitives. Each is one class on one element; the props are the
   modifiers the class has, and anything else goes on as an attribute. */

type Box = HTMLAttributes<HTMLElement> & { as?: ElementType };

export interface ContainerProps extends Box {
  size?: 'narrow' | 'wide';
}
export const Container = forwardRef<HTMLElement, ContainerProps>(function Container({ as: Tag = 'div', size, className, ...rest }, ref) {
  return <Tag ref={ref} className={cx('ins-container', size && 'ins-container--' + size, className)} {...rest} />;
});

export interface StackProps extends Box {
  gap?: Space;
}
export const Stack = forwardRef<HTMLElement, StackProps>(function Stack({ as: Tag = 'div', gap: g, className, ...rest }, ref) {
  return <Tag ref={ref} className={cx('ins-stack', gap(g), className)} {...rest} />;
});

export interface RowProps extends Box {
  gap?: Space;
  /** Keep the items on one line (`ins-flex-nowrap`). */
  nowrap?: boolean;
  justify?: 'start' | 'center' | 'end' | 'between';
  align?: 'start' | 'center' | 'end' | 'baseline' | 'stretch';
}
export const Row = forwardRef<HTMLElement, RowProps>(function Row({ as: Tag = 'div', gap: g, nowrap, justify, align, className, ...rest }, ref) {
  return (
    <Tag
      ref={ref}
      className={cx('ins-row', gap(g), nowrap && 'ins-flex-nowrap', justify && 'ins-justify-' + justify, align && 'ins-items-' + align, className)}
      {...rest}
    />
  );
});

export interface ColsProps extends Box {
  /** How many columns on a desktop. They drop to two at 900px and one on a phone. */
  n: 2 | 3 | 4 | 6 | 12;
  /** Hold the column count at every width. */
  fixed?: boolean;
  gap?: Space;
}
export const Cols = forwardRef<HTMLElement, ColsProps>(function Cols({ as: Tag = 'div', n, fixed, gap: g, className, ...rest }, ref) {
  return <Tag ref={ref} className={cx('ins-cols-' + n, fixed && 'ins-cols--fixed', gap(g), className)} {...rest} />;
});

export interface GridProps extends Box {
  /** Wider cells (`ins-grid--wide`). */
  wide?: boolean;
  gap?: Space;
}
export const Grid = forwardRef<HTMLElement, GridProps>(function Grid({ as: Tag = 'div', wide, gap: g, className, ...rest }, ref) {
  return <Tag ref={ref} className={cx('ins-grid', wide && 'ins-grid--wide', gap(g), className)} {...rest} />;
});

export interface ToolbarProps extends Box {
  /** Held at the far end (`.ins-toolbar-end`). */
  end?: ReactNode;
}
export const Toolbar = forwardRef<HTMLElement, ToolbarProps>(function Toolbar({ as: Tag = 'div', end, className, children, ...rest }, ref) {
  return (
    <Tag ref={ref} className={cx('ins-toolbar', className)} {...rest}>
      {children}
      {end != null && <div className="ins-toolbar-end">{end}</div>}
    </Tag>
  );
});

/* Fields side by side, wrapping on a narrow screen. */
export const FieldRow = forwardRef<HTMLDivElement, HTMLAttributes<HTMLDivElement>>(function FieldRow({ className, ...rest }, ref) {
  return <div ref={ref} className={cx('ins-field-row', className)} {...rest} />;
});

/* A form's submit row. */
export const FormCommit = forwardRef<HTMLDivElement, HTMLAttributes<HTMLDivElement>>(function FormCommit({ className, ...rest }, ref) {
  return <div ref={ref} className={cx('ins-form-commit', className)} {...rest} />;
});
