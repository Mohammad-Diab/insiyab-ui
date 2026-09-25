'use client';
import {
  Children,
  forwardRef,
  useEffect,
  useImperativeHandle,
  useMemo,
  useRef,
  type CSSProperties,
  type DialogHTMLAttributes,
  type HTMLAttributes,
  type InputHTMLAttributes,
  type ReactNode,
  type RefObject,
  type TimeHTMLAttributes
} from 'react';
import { core, type PaletteItemSpec, type PhoneValue } from './core.js';
import { useFieldControl } from './forms.js';
import { childrenWithClass, cx, useEnhance, useInsEvent, useIsoLayoutEffect, useLatest, useOwned, useWhenGone, withClass } from './util.js';

/* The plugins, each needing its own file loaded after insiyab.js (and its
   stylesheet), exactly as on a plain page. Each component renders the markup the
   plugin starts from, already in the shape the plugin keeps, and asks for the build
   once it is mounted. */

interface Outer {
  className?: string;
  style?: CSSProperties;
  inputClassName?: string;
}

/* A value given from outside, written through the plugin's own setter, which is
   quiet: it fires nothing back. */
function useSync<T>(el: RefObject<HTMLInputElement | null>, value: T | undefined, read: (el: HTMLInputElement) => T | null | undefined, write: (el: HTMLInputElement, v: T) => void) {
  useIsoLayoutEffect(() => {
    if (value === undefined || !el.current) return;
    if (read(el.current) === value) return;
    write(el.current, value);
  }, [value]);
}

/* --------------------------------------------------------- one-time code */

export interface OtpProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'value' | 'defaultValue' | 'size'>, Outer {
  /** The number of boxes, 4 to 10. */
  length?: number;
  /** Letters too, upper-cased. */
  alnum?: boolean;
  /** Submit the form when the last box fills. */
  autoSubmit?: boolean;
  value?: string;
  defaultValue?: string;
  /** The code is complete. */
  onComplete?: (code: string) => void;
}
/* One real input under drawn boxes, so autofill from Messages, a paste and a screen
   reader all see an ordinary field. */
export const Otp = forwardRef<HTMLInputElement, OtpProps>(function Otp(
  { length = 6, alnum, autoSubmit, value, defaultValue, onComplete, className, style, inputClassName, ...rest },
  ref
) {
  const f = useFieldControl(rest);
  const input = useRef<HTMLInputElement>(null);
  useImperativeHandle(ref, () => input.current!, []);
  const owned = useOwned(input);
  useEnhance(input, { 'data-ins-otp': String(length) });
  useInsEvent<{ input: Element; value: string }>('ins:otp', (d) => onComplete?.(d.value), (d) => d.input === input.current);
  useSync(input, value, (el) => core()?.otp?.(el), (el, v) => core()?.otp?.(el, v));
  return (
    <div className={cx('ins-otp', className)} style={style}>
      <input
        ref={input}
        className={inputClassName}
        defaultValue={value ?? defaultValue}
        data-ins-otp-chars={alnum ? 'alnum' : undefined}
        data-ins-otp-submit={autoSubmit ? '' : undefined}
        {...rest}
        {...f}
        type={owned('type', undefined)}
      />
    </div>
  );
});

/* ------------------------------------------------------------------ phone */

export interface PhoneFieldProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'value' | 'defaultValue' | 'type'>, Outer {
  /** E.164 or national. The form sends E.164. */
  value?: string;
  defaultValue?: string;
  /** The country to start in, two letters. */
  country?: string;
  /** Listed first. */
  preferred?: string[];
  /** The only countries offered. */
  only?: string[];
  onValueChange?: (value: PhoneValue) => void;
  invalid?: boolean;
}
export const PhoneField = forwardRef<HTMLInputElement, PhoneFieldProps>(function PhoneField(
  { value, defaultValue, country, preferred, only, onValueChange, invalid, className, style, inputClassName, ...rest },
  ref
) {
  const f = useFieldControl({ ...rest, invalid });
  const input = useRef<HTMLInputElement>(null);
  const group = useRef<HTMLDivElement>(null);
  const hidden = useRef<Element | null>(null);
  useImperativeHandle(ref, () => input.current!, []);
  const owned = useOwned(input);
  const { name, ...attrs } = rest;
  /* The plugin sends the number from a hidden input it puts after the group, which
     is outside what this component rendered; it goes when the field does. */
  useEnhance(input, { 'data-ins-phone': '' }, {
    onReady: () => {
      const next = group.current?.nextElementSibling;
      if (next && next.matches('input[type="hidden"]')) hidden.current = next;
    }
  });
  useWhenGone(group, () => hidden.current?.remove());
  useInsEvent<PhoneValue & { input: Element }>('ins:phone', (d) => onValueChange?.({ value: d.value, country: d.country, valid: d.valid }), (d) => d.input === input.current);
  useSync(input, value, (el) => core()?.phone?.(el)?.value, (el, v) => core()?.phone?.(el, v));
  return (
    <div ref={group} className={cx('ins-input-group', 'ins-phone', className)} dir="ltr" style={style}>
      <input
        ref={input}
        className={cx('ins-input', inputClassName)}
        defaultValue={value ?? defaultValue}
        data-ins-phone-country={country}
        data-ins-phone-preferred={preferred?.join(',')}
        data-ins-phone-only={only?.join(',')}
        {...attrs}
        {...f}
        type={owned('type', undefined)}
        name={owned('name', name)}
      />
    </div>
  );
});

/* ----------------------------------------------------------------- colour */

export interface ColorFieldProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'value' | 'defaultValue' | 'type'>, Outer {
  /** `#rrggbb`. */
  value?: string;
  defaultValue?: string;
  /** Preset swatches, or `none`. The page's brand and a palette around it otherwise. */
  swatches?: string[] | 'none';
  onValueChange?: (hex: string) => void;
}
/* A hex field with a picker that says whether the colour can carry text. */
export const ColorField = forwardRef<HTMLInputElement, ColorFieldProps>(function ColorField(
  { value, defaultValue, swatches, onValueChange, className, style, inputClassName, ...rest },
  ref
) {
  const f = useFieldControl(rest);
  const input = useRef<HTMLInputElement>(null);
  useImperativeHandle(ref, () => input.current!, []);
  const owned = useOwned(input);
  useEnhance(input, { 'data-ins-color': '' });
  useInsEvent<{ input: Element; value: string }>('ins:color', (d) => onValueChange?.(d.value), (d) => d.input === input.current);
  useSync(input, value, (el) => core()?.colorField?.(el), (el, v) => core()?.colorField?.(el, v));
  return (
    <div className={cx('ins-input-group', 'ins-color', className)} dir="ltr" style={style}>
      <input
        ref={input}
        className={inputClassName}
        defaultValue={value ?? defaultValue}
        data-ins-color-swatches={Array.isArray(swatches) ? swatches.join(',') : swatches}
        {...rest}
        {...f}
        type={owned('type', 'color')}
      />
    </div>
  );
});

/* ------------------------------------------------------------ file upload */

export interface FileDetail {
  input: HTMLInputElement;
  file: File;
  item: HTMLElement;
  upload: boolean;
}
export interface FileFieldProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'>, Outer {
  /** The largest file: `5MB`, `800KB`. */
  maxSize?: string;
  /** The most files, with `multiple`. */
  maxCount?: number;
  /** Replaces the small print built from the limits. */
  note?: string;
  /** A button and the list, with no drop zone. */
  compact?: boolean;
  /** Upload each file as it is added: `onFile` sends it, and reports back with `Insiyab.file.progress/done/fail`. */
  upload?: boolean;
  /** Every file added. */
  onFile?: (detail: FileDetail) => void;
}
/* A drop zone and a list, with the files kept in the real input, so the form sends
   exactly what is listed. */
export const FileField = forwardRef<HTMLInputElement, FileFieldProps>(function FileField(
  { maxSize, maxCount, note, compact, upload, onFile, className, style, inputClassName, ...rest },
  ref
) {
  const f = useFieldControl(rest);
  const input = useRef<HTMLInputElement>(null);
  useImperativeHandle(ref, () => input.current!, []);
  const owned = useOwned(input);
  const { name, ...attrs } = rest;
  useEnhance(input, { 'data-ins-file': '' });
  useInsEvent<FileDetail>('ins:file', onFile, (d) => d.input === input.current);
  return (
    <div className={cx('ins-file', compact && 'ins-file--compact', className)} style={style}>
      <input
        ref={input}
        type="file"
        className={inputClassName}
        data-ins-file-max={maxSize}
        data-ins-file-count={maxCount}
        data-ins-file-note={note}
        data-ins-file-upload={upload ? '' : undefined}
        {...attrs}
        {...f}
        name={owned('name', name)}
      />
    </div>
  );
});

/* ------------------------------------------------------------------- tree */

export interface TreeNode {
  label: ReactNode;
  /** A link, followed with Enter. */
  href?: string;
  /** Before the label. */
  icon?: ReactNode;
  /** After the label: a count, a pill. */
  end?: ReactNode;
  children?: TreeNode[];
  /** Open from the start. */
  open?: boolean;
  selected?: boolean;
  /** In a tree of checkboxes: the box's value, and whether it starts ticked. */
  value?: string;
  checked?: boolean;
  id?: string;
}
export interface TreeDetail {
  tree: HTMLElement;
  item: HTMLElement;
  label: string;
  action: 'select' | 'open' | 'close' | 'check';
}
export interface TreeProps extends Omit<HTMLAttributes<HTMLUListElement>, 'onSelect'> {
  items: TreeNode[];
  /** A checkbox on every row, cascading down and marking a partly ticked branch as mixed. */
  checks?: boolean;
  /** The checkboxes' name. */
  name?: string;
  /** A tree that only navigates. */
  selectable?: boolean;
  label: string;
  onAction?: (detail: TreeDetail) => void;
}

function shape(nodes: TreeNode[] | undefined): string {
  return (nodes || []).map((n) => (n.id ?? n.value ?? n.href ?? '') + '(' + shape(n.children) + ')').join(',');
}

function TreeItems({ nodes, checks, name }: { nodes: TreeNode[]; checks?: boolean; name?: string }) {
  return (
    <>
      {nodes.map((n, i) => {
        const label = checks ? (
          <label className="ins-check">
            <input type="checkbox" name={name} value={n.value} defaultChecked={n.checked} />
            <span className="ins-check-text">
              {n.icon}
              {n.label}
            </span>
          </label>
        ) : n.href != null ? (
          <a href={n.href}>
            {n.icon}
            {n.label}
          </a>
        ) : (
          <span>
            {n.icon}
            {n.label}
          </span>
        );
        return (
          <li key={n.id ?? n.value ?? i} id={n.id} data-open={n.open ? '' : undefined} data-selected={n.selected ? '' : undefined}>
            <div className="ins-tree-row">
              {label}
              {n.end}
            </div>
            {n.children && n.children.length > 0 && (
              <ul>
                <TreeItems nodes={n.children} checks={checks} name={name} />
              </ul>
            )}
          </li>
        );
      })}
    </>
  );
}

/* Nested lists as a keyboard tree whose arrows follow the page's direction. The rows
   are rendered as the plugin keeps them, so it never moves a node React owns; a
   change to the tree's shape remounts it, and the plugin builds it afresh. */
export function Tree({ items, checks, name, selectable = true, label, onAction, className, ...rest }: TreeProps) {
  return <TreeRoot key={shape(items)} {...{ items, checks, name, selectable, label, onAction, className, ...rest }} />;
}

function TreeRoot({ items, checks, name, selectable, label, onAction, className, ...rest }: TreeProps) {
  const el = useRef<HTMLUListElement>(null);
  useEnhance(el, { 'data-ins-tree': '' });
  useInsEvent<TreeDetail>('ins:tree', onAction, (d) => d.tree === el.current);
  return (
    <ul
      ref={el}
      className={cx('ins-tree', className)}
      aria-label={label}
      data-ins-tree-checks={checks ? '' : undefined}
      data-ins-tree-select={selectable ? undefined : 'none'}
      {...rest}
    >
      <TreeItems nodes={items} checks={checks} name={name} />
    </ul>
  );
}

/* --------------------------------------------------------------- timeline */

export interface TimelineProps extends HTMLAttributes<HTMLOListElement> {
  /** For a narrow column. */
  compact?: boolean;
  /** Events on both sides of a centre rule, once the timeline is wide enough. */
  split?: boolean;
}
/* Pure CSS; only relative times need the plugin's script. */
export const Timeline = forwardRef<HTMLOListElement, TimelineProps>(function Timeline({ compact, split, className, ...rest }, ref) {
  return <ol ref={ref} className={cx('ins-timeline', compact && 'ins-timeline--compact', split && 'ins-timeline--split', className)} {...rest} />;
});

export interface TimelineItemProps extends Omit<HTMLAttributes<HTMLLIElement>, 'title'> {
  title: ReactNode;
  /** A `<RelativeTime>`, or any text: a place, "expected tomorrow". */
  time?: ReactNode;
  body?: ReactNode;
  tone?: 'ok' | 'bad' | 'warn' | 'info';
  /** `current`: in progress, with a halo. `pending`: still to come, hollow on a dashed rule. */
  state?: 'current' | 'pending';
  /** An icon chip in place of the dot. */
  icon?: ReactNode;
}
export const TimelineItem = forwardRef<HTMLLIElement, TimelineItemProps>(function TimelineItem(
  { title, time, body, tone, state, icon, className, children, ...rest },
  ref
) {
  const timeEl = time == null ? null : typeof time === 'string' || typeof time === 'number' ? <span className="ins-timeline-time">{time}</span> : withClass(time, 'ins-timeline-time');
  return (
    <li ref={ref} className={cx('ins-timeline-item', tone && 'ins-timeline-item--' + tone, state && 'is-' + state, className)} {...rest}>
      {icon != null && <span className="ins-timeline-ico">{icon}</span>}
      <span className="ins-timeline-title">{title}</span>
      {timeEl}
      {body != null && <p className="ins-timeline-body">{body}</p>}
      {children}
    </li>
  );
});

/* A date heading between events. */
export const TimelineDay = forwardRef<HTMLLIElement, HTMLAttributes<HTMLLIElement>>(function TimelineDay({ className, ...rest }, ref) {
  return <li ref={ref} className={cx('ins-timeline-day', className)} {...rest} />;
});

export interface RelativeTimeProps extends Omit<TimeHTMLAttributes<HTMLTimeElement>, 'dateTime'> {
  /** ISO: `2026-09-24T09:15`, or a day, `2026-09-24`. */
  dateTime: string;
  /** Always the date, never "3 hours ago". */
  dateOnly?: boolean;
  /** Written until the plugin writes the time; what a page without it shows. */
  children?: ReactNode;
}
/* «قبل 5 دقائق», «أمس»: written by the timeline plugin from Intl, and again every
   minute. As a timeline item's `time` it takes the timeline's look. */
export const RelativeTime = forwardRef<HTMLTimeElement, RelativeTimeProps>(function RelativeTime({ dateTime, dateOnly, className, ...rest }, ref) {
  const el = useRef<HTMLTimeElement>(null);
  useImperativeHandle(ref, () => el.current!, []);
  useEnhance(el, { 'data-ins-time': dateOnly ? 'date' : '' }, { scope: 'parent' });
  return <time ref={el} className={className} dateTime={dateTime} {...rest} />;
});

/* --------------------------------------------------------------- carousel */

export interface CarouselProps extends HTMLAttributes<HTMLDivElement> {
  /** Names the carousel for a screen reader. */
  label: string;
  /** Slides in view at once, when the carousel is wide enough. */
  perView?: 1 | 2 | 3;
  /** Show the edge of the next slide. */
  peek?: boolean;
  /** The dots under it; on by default. */
  dots?: boolean;
  onIndexChange?: (index: number) => void;
}
/* Slides in a row, on the browser's own scroll-snap. It never moves on its own. */
export const Carousel = forwardRef<HTMLDivElement, CarouselProps>(function Carousel(props, ref) {
  const count = Children.count(props.children);
  return <CarouselRoot key={count} {...props} ref={ref} />;
});

const CarouselRoot = forwardRef<HTMLDivElement, CarouselProps>(function CarouselRoot({ label, perView, peek, dots = true, onIndexChange, className, children, ...rest }, ref) {
  const el = useRef<HTMLDivElement>(null);
  useImperativeHandle(ref, () => el.current!, []);
  useEnhance(el, { 'data-ins-carousel': '' });
  useInsEvent<{ carousel: Element; index: number }>('ins:carousel', (d) => onIndexChange?.(d.index), (d) => d.carousel === el.current);
  return (
    <div
      ref={el}
      className={cx('ins-carousel', perView && perView > 1 && 'ins-carousel--' + perView, peek && 'ins-carousel--peek', className)}
      aria-label={label}
      data-ins-carousel-dots={dots ? undefined : 'off'}
      {...rest}
    >
      <div className="ins-carousel-track">{childrenWithClass(children, 'ins-carousel-slide')}</div>
    </div>
  );
});

/* -------------------------------------------------------------- scrollspy */

/* Marks the link for the section being read in any nav you render yourself: the
   link gets `is-active` and `aria-current="location"`. */
export function useScrollspy(ref: RefObject<HTMLElement | null>, options: { offset?: number; onChange?: (link: Element | null) => void } = {}) {
  useEnhance(ref, { 'data-ins-scrollspy': '', 'data-ins-scrollspy-offset': options.offset != null ? String(options.offset) : undefined });
  useInsEvent<{ nav: Element; link: Element | null }>('ins:scrollspy', options.onChange && ((d) => options.onChange!(d.link)), (d) => d.nav === ref.current);
}

export interface TocLink {
  href: string;
  label: ReactNode;
  /** A deeper heading. */
  sub?: boolean;
}
export interface TocProps extends Omit<HTMLAttributes<HTMLElement>, 'title'> {
  links: TocLink[];
  title?: ReactNode;
  label?: string;
  offset?: number;
  onActiveChange?: (link: Element | null) => void;
}
/* A contents list that sticks under the top bar and follows the reading. Needs the
   scrollspy plugin's stylesheet. */
export const Toc = forwardRef<HTMLElement, TocProps>(function Toc({ links, title, label, offset, onActiveChange, className, ...rest }, ref) {
  const el = useRef<HTMLElement>(null);
  useImperativeHandle(ref, () => el.current!, []);
  useScrollspy(el, { offset, onChange: onActiveChange });
  return (
    <nav ref={el} className={cx('ins-toc', className)} aria-label={label} {...rest}>
      {title != null && <div className="ins-toc-title">{title}</div>}
      {links.map((l) => (
        <a key={l.href} href={l.href} className={l.sub ? 'ins-toc-sub' : undefined}>
          {l.label}
        </a>
      ))}
    </nav>
  );
});

/* -------------------------------------------------------- command palette */

export interface PaletteItem extends Omit<PaletteItemSpec, 'run'> {
  run?: (event: MouseEvent) => void;
}
export interface CommandPaletteProps extends Omit<DialogHTMLAttributes<HTMLDialogElement>, 'open'> {
  items?: PaletteItem[];
  /** Also offer every link in here, read each time it opens: `.ins-shell-side`. */
  from?: string;
  /** `mod+k /` by default; `none` for no shortcut. */
  keys?: string;
  /** How many recent choices come first; 0 for none. */
  recent?: number;
  placeholder?: string;
  label?: string;
  open?: boolean;
  onOpenChange?: (open: boolean) => void;
}
/* One search box for every page and command, on Ctrl/⌘+K. The plugin builds the
   whole dialog, so nothing inside it is React's: the items are given as data and
   added with `Insiyab.palette.add`, and changing them replaces them. */
export const CommandPalette = forwardRef<HTMLDialogElement, CommandPaletteProps>(function CommandPalette(
  { items, from, keys, recent, placeholder, label, open, onOpenChange, className, ...rest },
  ref
) {
  const el = useRef<HTMLDialogElement>(null);
  useImperativeHandle(ref, () => el.current!, []);
  const report = useLatest(onOpenChange);
  useEnhance(el, { 'data-ins-palette': '' });
  const specs = useMemo(() => items ?? [], [items]);
  useEffect(() => {
    const api = core(), dlg = el.current;
    if (!api?.palette || !dlg || !specs.length) return;
    const added = api.palette.add(specs as PaletteItemSpec[], dlg);
    return () => added.forEach((a) => a.remove());
  }, [specs]);
  useIsoLayoutEffect(() => {
    const dlg = el.current;
    if (open === undefined || !dlg || dlg.open === open) return;
    core()?.palette?.(dlg, open ? 'open' : 'close');
  }, [open]);
  useEffect(() => {
    const dlg = el.current;
    if (!dlg) return;
    let last = dlg.open;
    const mo = new MutationObserver(() => {
      if (dlg.open !== last) report.current?.((last = dlg.open));
    });
    mo.observe(dlg, { attributes: true, attributeFilter: ['open'] });
    return () => mo.disconnect();
  }, []);
  return (
    <dialog
      ref={el}
      className={cx('ins-dialog', 'ins-palette', className)}
      aria-label={label}
      data-ins-palette-from={from}
      data-ins-palette-keys={keys}
      data-ins-palette-recent={recent != null ? String(recent) : undefined}
      data-ins-palette-placeholder={placeholder}
      {...rest}
    />
  );
});
