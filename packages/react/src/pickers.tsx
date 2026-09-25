'use client';
import { forwardRef, useImperativeHandle, useRef, type CSSProperties, type InputHTMLAttributes, type ReactNode } from 'react';
import { core } from './core.js';
import { useFieldControl } from './forms.js';
import { cx, useEnhance, useInsEvent, useInsId, useIsoLayoutEffect, useOwned } from './util.js';

interface Outer {
  className?: string;
  style?: CSSProperties;
  inputClassName?: string;
}

/* ----------------------------------------------------------- autocomplete */

export interface ComboOption {
  value: string;
  label: string;
}

export interface ComboProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'value' | 'defaultValue' | 'name'>, Outer {
  options: ComboOption[];
  /** The name the chosen option's value is sent under, from a hidden input. */
  name?: string;
  value?: string;
  defaultValue?: string;
  onValueChange?: (value: string, label: string) => void;
  /** Shown when nothing matches what was typed. */
  emptyText?: ReactNode;
  invalid?: boolean;
}

/* The autocomplete. Its matching is the core's: hamza seats, harakat and the tatweel
   ignored, ة as ه, Arabic-Indic digits as Latin. Every role and id the core's builder
   would stamp is rendered here, so it finds nothing to change. */
export const Combo = forwardRef<HTMLInputElement, ComboProps>(function Combo(
  { options, name, value, defaultValue, onValueChange, emptyText, invalid, className, style, inputClassName, ...rest },
  ref
) {
  const f = useFieldControl({ ...rest, invalid });
  const listId = useInsId() + '-list';
  const root = useRef<HTMLDivElement>(null);
  const input = useRef<HTMLInputElement>(null);
  const hidden = useRef<HTMLInputElement>(null);
  useImperativeHandle(ref, () => input.current!, []);
  const start = value ?? defaultValue ?? '';
  const startLabel = options.find((o) => o.value === start)?.label ?? '';

  useInsEvent<{ el: Element; value: string; label: string }>('ins:combo', (d) => onValueChange?.(d.value, d.label), (d) => d.el === root.current);

  /* A value set from outside: the hidden input and the text follow it. */
  useIsoLayoutEffect(() => {
    if (value === undefined || !hidden.current || !input.current) return;
    if (hidden.current.value === value) return;
    hidden.current.value = value;
    input.current.value = options.find((o) => o.value === value)?.label ?? '';
  }, [value]);

  return (
    <div ref={root} className={cx('ins-combo', className)} style={style}>
      <input
        ref={input}
        className={cx('ins-input', inputClassName)}
        autoComplete="off"
        defaultValue={startLabel || undefined}
        {...rest}
        {...f}
        role="combobox"
        aria-autocomplete="list"
        aria-controls={listId}
        aria-expanded="false"
      />
      <input ref={hidden} type="hidden" name={name} defaultValue={start || undefined} />
      <ul className="ins-combo-list" id={listId} role="listbox">
        {options.map((o, i) => (
          <li key={o.value} className="ins-combo-option" data-value={o.value} id={listId + '-' + i} role="option" aria-selected={!!startLabel && o.label === startLabel}>
            {o.label}
          </li>
        ))}
        {emptyText != null && (
          <li className="ins-combo-empty" role="presentation" hidden>
            {emptyText}
          </li>
        )}
      </ul>
    </div>
  );
});

/* -------------------------------------------------------------- the date */

export interface DateFieldProps
  extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type' | 'value' | 'defaultValue' | 'min' | 'max'>,
    Outer {
  /** ISO, `2026-09-23`. */
  value?: string;
  defaultValue?: string;
  min?: string;
  max?: string;
  onValueChange?: (iso: string, date: Date | null) => void;
  /** `hijri` or `hijri-civil` with the Hijri plugin loaded; `gregory` to opt out of a page-wide one. */
  calendar?: string;
  /** A language other than the page's, for this field. */
  locale?: string;
  /** This is the start of a range: the id of its end field. */
  rangeEnd?: string;
  /** This is the end of a range: the id of its start field. */
  rangeStart?: string;
  invalid?: boolean;
  /** Inside the group, after the field: an addon, say. */
  children?: ReactNode;
}

const sel = (id?: string) => (id ? (id.charAt(0) === '#' ? id : '#' + id) : undefined);

/* The date field. Until the core builds it, and without script, it is the native
   date input. Built, it shows the date in the page's language, sends ISO from a
   hidden input under the same `name`, and opens the core's calendar. */
export const DateField = forwardRef<HTMLInputElement, DateFieldProps>(function DateField(
  { value, defaultValue, min, max, onValueChange, calendar, locale, rangeEnd, rangeStart, invalid, className, style, inputClassName, children, ...rest },
  ref
) {
  const f = useFieldControl({ ...rest, invalid });
  const input = useRef<HTMLInputElement>(null);
  useImperativeHandle(ref, () => input.current!, []);
  const setting = useRef(false);
  const built = useRef(false);
  const owned = useOwned(input);
  const { name, ...attrs } = rest;

  useEnhance(input, { 'data-ins-date': '' }, { scope: 'parent', onReady: () => (built.current = true) });

  useInsEvent<{ input: Element; value: string; date: Date | null }>(
    'ins:date',
    (d) => {
      if (!setting.current) onValueChange?.(d.value, d.date);
    },
    (d) => d.input === input.current
  );

  /* A value set from outside, written through the core so the text and the hidden
     input agree; the change it reports is this one, so it is not reported back. */
  useIsoLayoutEffect(() => {
    const api = core();
    if (value === undefined || !built.current || !api || !input.current) return;
    if ((api.date(input.current) ?? '') === value) return;
    setting.current = true;
    try {
      api.date(input.current, value);
    } finally {
      setting.current = false;
    }
  }, [value]);

  /* Limits that change after the build: the core keeps them in data attributes,
     because a text field has no `min`. */
  useIsoLayoutEffect(() => {
    const el = input.current;
    if (!el || !built.current) return;
    for (const [attr, v] of [['data-ins-min', min], ['data-ins-max', max]] as const) {
      if (v) el.setAttribute(attr, v);
      else el.removeAttribute(attr);
    }
    el.removeAttribute('min');
    el.removeAttribute('max');
  }, [min, max]);

  return (
    <div className={cx('ins-input-group', className)} style={style}>
      <input
        ref={input}
        className={cx('ins-input', inputClassName)}
        defaultValue={value ?? defaultValue}
        min={min}
        max={max}
        data-ins-calendar={calendar}
        data-ins-locale={locale}
        data-ins-date-end={sel(rangeEnd)}
        data-ins-date-start={sel(rangeStart)}
        {...attrs}
        {...f}
        type={owned('type', 'date')}
        name={owned('name', name)}
      />
      {children}
    </div>
  );
});
