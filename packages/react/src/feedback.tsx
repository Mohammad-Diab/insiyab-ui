'use client';
import { forwardRef, useImperativeHandle, useRef, type CSSProperties, type ElementType, type HTMLAttributes, type ProgressHTMLAttributes, type ReactNode } from 'react';
import { CloseButton } from './buttons.js';
import type { Tone } from './core.js';
import { cx, useInsEvent } from './util.js';

export interface AlertProps extends HTMLAttributes<HTMLDivElement> {
  tone?: Tone;
  icon?: ReactNode;
  /** A close button. The core hides the alert (`hidden`); clear it to bring the alert back. */
  dismissible?: boolean;
  closeLabel?: string;
  onDismiss?: () => void;
}
/* The notched alert. */
export const Alert = forwardRef<HTMLDivElement, AlertProps>(function Alert({ tone, icon, dismissible, closeLabel, onDismiss, className, children, ...rest }, ref) {
  const el = useRef<HTMLDivElement>(null);
  useImperativeHandle(ref, () => el.current!, []);
  useInsEvent<{ target: Element }>('ins:dismiss', onDismiss && (() => onDismiss()), (d) => d.target === el.current);
  return (
    <div ref={el} className={cx('ins-alert', tone && 'ins-alert--' + tone, className)} {...rest}>
      {icon != null && <span className="ins-alert-ico">{icon}</span>}
      <div className="ins-alert-msg">
        <span className="ins-alert-text">{children}</span>
        {dismissible && <CloseButton size="sm" bare dismiss label={closeLabel} />}
      </div>
    </div>
  );
});

export interface EmptyProps extends Omit<HTMLAttributes<HTMLDivElement>, 'title'> {
  icon?: ReactNode;
  title: ReactNode;
  hint?: ReactNode;
  /** Under the text: the buttons that fill the space. */
  children?: ReactNode;
}
/* An empty state: nothing here yet, and what to do about it. */
export const Empty = forwardRef<HTMLDivElement, EmptyProps>(function Empty({ icon, title, hint, className, children, ...rest }, ref) {
  return (
    <div ref={ref} className={cx('ins-empty', className)} {...rest}>
      {icon != null && <span className="ins-empty-ico">{icon}</span>}
      <span className="ins-empty-title">{title}</span>
      {hint != null && <span className="ins-empty-hint">{hint}</span>}
      {children}
    </div>
  );
});

export interface ProgressProps extends Omit<ProgressHTMLAttributes<HTMLProgressElement>, 'value'> {
  /** Leave it out for the moving "working on it" bar. */
  value?: number;
  tone?: 'ok' | 'warn' | 'bad';
  size?: 'sm' | 'lg';
}
/* The native <progress>, which a screen reader already understands. */
export const Progress = forwardRef<HTMLProgressElement, ProgressProps>(function Progress({ value, max = 100, tone, size, className, children, ...rest }, ref) {
  const pct = value === undefined ? null : Math.round((value / Number(max)) * 100) + '%';
  return (
    <progress ref={ref} className={cx('ins-progress', size && 'ins-progress--' + size, tone && 'ins-progress--' + tone, className)} value={value} max={max} {...rest}>
      {children ?? pct}
    </progress>
  );
});

export interface RingProps extends HTMLAttributes<HTMLDivElement> {
  /** 0 to 100. */
  value: number;
  /** What it measures, for a screen reader. */
  label: string;
  size?: 'lg';
}
export const Ring = forwardRef<HTMLDivElement, RingProps>(function Ring({ value, label, size, className, style, children, ...rest }, ref) {
  return (
    <div
      ref={ref}
      className={cx('ins-ring', size && 'ins-ring--' + size, className)}
      role="progressbar"
      aria-label={label}
      aria-valuenow={value}
      aria-valuemin={0}
      aria-valuemax={100}
      style={{ '--ins-value': value, ...style } as CSSProperties}
      {...rest}
    >
      {children ?? value + '%'}
    </div>
  );
});

export interface SkeletonProps extends HTMLAttributes<HTMLElement> {
  shape?: 'text' | 'title' | 'circle' | 'block';
  /** A block of its own, for a line inside inline content (`ins-block`). */
  block?: boolean;
  as?: ElementType;
}
export const Skeleton = forwardRef<HTMLElement, SkeletonProps>(function Skeleton({ shape, block, as: Tag = 'span', className, ...rest }, ref) {
  return <Tag ref={ref} className={cx('ins-skel', shape && 'ins-skel--' + shape, block && 'ins-block', className)} {...rest} />;
});

export interface SpinnerProps extends HTMLAttributes<HTMLSpanElement> {
  size?: 'sm';
  /** Read out as a status. Leave it out for a spinner beside text that says the same. */
  label?: string;
}
export const Spinner = forwardRef<HTMLSpanElement, SpinnerProps>(function Spinner({ size, label, className, ...rest }, ref) {
  return (
    <span
      ref={ref}
      className={cx('ins-spinner', size && 'ins-spinner--' + size, className)}
      role={label ? 'status' : undefined}
      aria-label={label}
      aria-hidden={label ? undefined : true}
      {...rest}
    />
  );
});

export interface LoadingProps extends HTMLAttributes<HTMLDivElement> {
  label?: string;
}
/* A panel's worth of waiting: a spinner in the middle of the space. */
export const Loading = forwardRef<HTMLDivElement, LoadingProps>(function Loading({ label = 'جارٍ التحميل', className, ...rest }, ref) {
  return (
    <div ref={ref} className={cx('ins-loading', className)} {...rest}>
      <Spinner label={label} />
    </div>
  );
});
