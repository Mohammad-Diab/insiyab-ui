import { forwardRef, type ElementType, type HTMLAttributes, type ReactNode } from 'react';
import { cx } from './util.js';

type Box = HTMLAttributes<HTMLElement> & { as?: ElementType };

export const Display = forwardRef<HTMLElement, Box>(function Display({ as: Tag = 'p', className, ...rest }, ref) {
  return <Tag ref={ref} className={cx('ins-display', className)} {...rest} />;
});

export interface HeadingProps extends Box {
  /** The size, `ins-h1` to `ins-h4`. The element is `h1`–`h4` to match, unless `as` says otherwise. */
  level: 1 | 2 | 3 | 4;
}
export const Heading = forwardRef<HTMLElement, HeadingProps>(function Heading({ level, as, className, ...rest }, ref) {
  const Tag = as || (('h' + level) as ElementType);
  return <Tag ref={ref} className={cx('ins-h' + level, className)} {...rest} />;
});

export const Lead = forwardRef<HTMLElement, Box>(function Lead({ as: Tag = 'p', className, ...rest }, ref) {
  return <Tag ref={ref} className={cx('ins-lead', className)} {...rest} />;
});

export const Eyebrow = forwardRef<HTMLElement, Box>(function Eyebrow({ as: Tag = 'span', className, ...rest }, ref) {
  return <Tag ref={ref} className={cx('ins-eyebrow', className)} {...rest} />;
});

/* Written content: paragraphs, lists, quotes, code and tables inside it are styled,
   and nothing outside it is. */
export const Prose = forwardRef<HTMLElement, Box>(function Prose({ as: Tag = 'div', className, ...rest }, ref) {
  return <Tag ref={ref} className={cx('ins-prose', className)} {...rest} />;
});

export const Kbd = forwardRef<HTMLElement, HTMLAttributes<HTMLElement>>(function Kbd({ className, ...rest }, ref) {
  return <kbd ref={ref} className={cx('ins-kbd', className)} {...rest} />;
});

/* A figure: Latin digits, tabular, in the numerals' face. */
export const Num = forwardRef<HTMLElement, Box>(function Num({ as: Tag = 'span', className, ...rest }, ref) {
  return <Tag ref={ref} className={cx('ins-num', className)} {...rest} />;
});

export interface MoneyProps extends HTMLAttributes<HTMLSpanElement> {
  value: ReactNode;
  currency: ReactNode;
  /** Colour it as a gain or a loss. The sign itself is part of `value`. */
  sign?: 'pos' | 'neg';
}
export const Money = forwardRef<HTMLSpanElement, MoneyProps>(function Money({ value, currency, sign, className, ...rest }, ref) {
  return (
    <span ref={ref} className={cx('ins-money', sign && 'ins-money--' + sign, className)} {...rest}>
      <span className="ins-money-val">{value}</span>
      <span className="ins-money-cur">{currency}</span>
    </span>
  );
});

export interface DividerProps extends HTMLAttributes<HTMLElement> {
  /** A word in the line («أو»). */
  label?: ReactNode;
  /** The label at the start rather than the centre. */
  start?: boolean;
  /** A vertical rule, between items in a row. */
  vertical?: boolean;
}
export const Divider = forwardRef<HTMLElement, DividerProps>(function Divider({ label, start, vertical, className, ...rest }, ref) {
  if (vertical) {
    return <hr ref={ref as never} className={cx('ins-divider', 'ins-divider--v', className)} aria-orientation="vertical" {...rest} />;
  }
  if (label == null) return <hr ref={ref as never} className={cx('ins-divider', className)} {...rest} />;
  return (
    <div ref={ref as never} className={cx('ins-divider', start && 'ins-divider--start', className)} {...rest}>
      {label}
    </div>
  );
});
