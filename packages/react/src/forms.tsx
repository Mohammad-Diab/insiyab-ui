'use client';
import {
  createContext,
  forwardRef,
  useContext,
  useImperativeHandle,
  useRef,
  useState,
  type ButtonHTMLAttributes,
  type CSSProperties,
  type FormHTMLAttributes,
  type HTMLAttributes,
  type InputHTMLAttributes,
  type ReactNode,
  type SelectHTMLAttributes,
  type TextareaHTMLAttributes
} from 'react';
import { buttonClass, type ButtonLook } from './buttons.js';
import { cx, useInsEvent, useInsId, useIsoLayoutEffect, useOwned, withClass } from './util.js';

/* ------------------------------------------------------------------ Form */

const FormContext = createContext({ validate: false });

export interface FormProps extends FormHTMLAttributes<HTMLFormElement> {
  /** The browser's own validation, with each message shown in its field's `.ins-error`. */
  validate?: boolean;
}
export const Form = forwardRef<HTMLFormElement, FormProps>(function Form({ validate, ...rest }, ref) {
  return (
    <FormContext.Provider value={{ validate: !!validate }}>
      <form ref={ref} data-ins-validate={validate ? '' : undefined} {...rest} />
    </FormContext.Provider>
  );
});

/* ----------------------------------------------------------------- Field */
/* The field gives its control an id, its label's `for`, and `aria-describedby`
   naming the hint and the message: the same links the core's field-messages
   builder writes, written here first, so a server-rendered field and the built
   one are the same markup. */

interface FieldState {
  id: string;
  describedBy?: string;
  invalid: boolean;
  required?: boolean;
}
const FieldContext = createContext<FieldState | null>(null);

export interface FieldProps extends HTMLAttributes<HTMLDivElement> {
  label?: ReactNode;
  hint?: ReactNode;
  /** The message under the field; the control is marked invalid while there is one. */
  error?: ReactNode;
  /** In a validating form, what to say when the browser finds the field invalid, in
      place of the browser's own words. It shows only then. */
  validationMessage?: ReactNode;
  success?: ReactNode;
  /** The star after the label, and `required` on the control. */
  required?: boolean;
  /** The control's id. Made up when not given. */
  controlId?: string;
}
export const Field = forwardRef<HTMLDivElement, FieldProps>(function Field(
  { label, hint, error, validationMessage, success, required, controlId, className, children, ...rest },
  ref
) {
  const id = useInsId(controlId);
  const { validate } = useContext(FormContext);
  const hasError = error != null && error !== false;
  const own = hasError ? error : validationMessage;
  const showError = own != null || validate;
  const hasSuccess = !hasError && success != null && success !== false;
  const describedBy = cx(hint != null && id + '-hint', showError && id + '-error', hasSuccess && id + '-ok');
  return (
    <div ref={ref} className={cx('ins-field', className)} {...rest}>
      {label != null && (
        <label className="ins-label" htmlFor={id}>
          {label}
          {required && <span className="ins-req" aria-hidden="true">*</span>}
        </label>
      )}
      <FieldContext.Provider value={{ id, describedBy, invalid: hasError, required }}>{children}</FieldContext.Provider>
      {hint != null && (
        <span className="ins-hint" id={id + '-hint'}>
          {hint}
        </span>
      )}
      {/* Two keys, so a message the page wrote and the empty one the core fills in are
          never the same element: React replaces one with the other instead of editing
          text the core has written into. */}
      {own != null ? (
        <p className="ins-error" id={id + '-error'} key="own">
          {own}
        </p>
      ) : validate ? (
        <p className="ins-error" id={id + '-error'} key="auto" />
      ) : null}
      {hasSuccess && (
        <p className="ins-success" id={id + '-ok'}>
          {success}
        </p>
      )}
    </div>
  );
});

/* What a control takes from the field around it, under its own props. */
export function useFieldControl(p: { id?: string; 'aria-describedby'?: string; 'aria-invalid'?: unknown; required?: boolean; invalid?: boolean }) {
  const f = useContext(FieldContext);
  const invalid = p.invalid ?? f?.invalid;
  return {
    id: p.id ?? f?.id,
    'aria-describedby': cx(p['aria-describedby'], f?.describedBy),
    'aria-invalid': (p['aria-invalid'] as boolean | undefined) ?? (invalid ? true : undefined),
    required: p.required ?? f?.required
  };
}

/* --------------------------------------------------------------- controls */

export interface InputProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'size'> {
  size?: 'sm' | 'lg';
  /** Marked invalid, as a field with an `error` does. */
  invalid?: boolean;
  /** The confirmed look (`ins-input--ok`). */
  ok?: boolean;
}
export const Input = forwardRef<HTMLInputElement, InputProps>(function Input({ size, invalid, ok, className, ...rest }, ref) {
  const f = useFieldControl({ ...rest, invalid });
  return <input ref={ref} className={cx('ins-input', size && 'ins-input--' + size, ok && 'ins-input--ok', className)} {...rest} {...f} />;
});

export interface SelectProps extends Omit<SelectHTMLAttributes<HTMLSelectElement>, 'size'> {
  size?: 'sm' | 'lg';
  invalid?: boolean;
}
export const Select = forwardRef<HTMLSelectElement, SelectProps>(function Select({ size, invalid, className, ...rest }, ref) {
  const f = useFieldControl({ ...rest, invalid });
  return <select ref={ref} className={cx('ins-select', size && 'ins-select--' + size, className)} {...rest} {...f} />;
});

export interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  invalid?: boolean;
}
export const Textarea = forwardRef<HTMLTextAreaElement, TextareaProps>(function Textarea({ invalid, className, ...rest }, ref) {
  const f = useFieldControl({ ...rest, invalid });
  return <textarea ref={ref} className={cx('ins-textarea', className)} {...rest} {...f} />;
});

export const InputGroup = forwardRef<HTMLDivElement, HTMLAttributes<HTMLDivElement>>(function InputGroup({ className, ...rest }, ref) {
  return <div ref={ref} className={cx('ins-input-group', className)} {...rest} />;
});

export const Addon = forwardRef<HTMLSpanElement, HTMLAttributes<HTMLSpanElement>>(function Addon({ className, ...rest }, ref) {
  return <span ref={ref} className={cx('ins-addon', className)} {...rest} />;
});

/* The composite fields below all follow one rule: `ref` and the input's own
   attributes go to the real input, and `className` and `style` go to the outermost
   element, the one you see. `inputClassName` reaches the input itself. */

interface Outer {
  className?: string;
  style?: CSSProperties;
  inputClassName?: string;
}

export interface CheckProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'>, Outer {
  label?: ReactNode;
  type?: 'checkbox' | 'radio';
  /** "Some selected": the dash, and `aria-checked="mixed"` from the platform. */
  indeterminate?: boolean;
}
export const Check = forwardRef<HTMLInputElement, CheckProps>(function Check(
  { label, type = 'checkbox', indeterminate, className, style, inputClassName, children, ...rest },
  ref
) {
  const input = useRef<HTMLInputElement>(null);
  useImperativeHandle(ref, () => input.current!, []);
  useIsoLayoutEffect(() => {
    if (input.current && indeterminate !== undefined) input.current.indeterminate = indeterminate;
  }, [indeterminate]);
  return (
    <label className={cx('ins-check', className)} style={style}>
      <input ref={input} type={type} className={inputClassName} data-ins-indeterminate={indeterminate ? '' : undefined} {...rest} />
      <span className="ins-check-text">{label ?? children}</span>
    </label>
  );
});

export const Radio = forwardRef<HTMLInputElement, Omit<CheckProps, 'type' | 'indeterminate'>>(function Radio(props, ref) {
  return <Check ref={ref} type="radio" {...props} />;
});

export interface SwitchProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'>, Outer {
  label?: ReactNode;
  desc?: ReactNode;
  /** A bordered card, for a setting with a description. */
  card?: boolean;
}
export const Switch = forwardRef<HTMLInputElement, SwitchProps>(function Switch(
  { label, desc, card, className, style, inputClassName, children, ...rest },
  ref
) {
  return (
    <label className={cx('ins-switch', card && 'ins-switch--card', className)} style={style}>
      <input ref={ref} type="checkbox" className={inputClassName} {...rest} />
      <span className="ins-switch-track" />
      <span className="ins-switch-text">
        <span className="ins-switch-label">{label ?? children}</span>
        {desc != null && <span className="ins-switch-desc">{desc}</span>}
      </span>
    </label>
  );
});

export const SwitchGrid = forwardRef<HTMLDivElement, HTMLAttributes<HTMLDivElement>>(function SwitchGrid({ className, ...rest }, ref) {
  return <div ref={ref} className={cx('ins-switch-grid', className)} {...rest} />;
});

export interface TileProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type' | 'title'>, Outer {
  title: ReactNode;
  desc?: ReactNode;
  type?: 'radio' | 'checkbox';
}
/* A radio or checkbox drawn as a card. */
export const Tile = forwardRef<HTMLInputElement, TileProps>(function Tile(
  { title, desc, type = 'radio', className, style, inputClassName, ...rest },
  ref
) {
  return (
    <label className={cx('ins-tile', className)} style={style}>
      <input ref={ref} type={type} className={inputClassName} {...rest} />
      <span className="ins-tile-title">{title}</span>
      {desc != null && <span className="ins-tile-desc">{desc}</span>}
    </label>
  );
});

export interface TileGridProps extends HTMLAttributes<HTMLDivElement> {
  /** Names the choice; the grid becomes a `radiogroup` with it. */
  label?: string;
}
export const TileGrid = forwardRef<HTMLDivElement, TileGridProps>(function TileGrid({ label, className, ...rest }, ref) {
  return <div ref={ref} className={cx('ins-tile-grid', className)} role={label ? 'radiogroup' : undefined} aria-label={label} {...rest} />;
});

export interface SearchProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'size'>, Outer {
  /** The magnifier, drawn at the start of the field. It gets `ins-search-ico`. */
  icon?: ReactNode;
}
export const Search = forwardRef<HTMLInputElement, SearchProps>(function Search({ icon, className, style, inputClassName, ...rest }, ref) {
  const f = useFieldControl(rest);
  return (
    <div className={cx('ins-search', className)} style={style}>
      {icon != null && withClass(icon, 'ins-search-ico')}
      <input ref={ref} className={cx('ins-input', inputClassName)} type="search" {...rest} {...f} />
    </div>
  );
});

export interface PasswordInputProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type' | 'size'>, Outer {
  /** The show-password button's name for a screen reader. */
  toggleLabel?: string;
  /** A tooltip on that button, from its name. */
  toggleTip?: boolean;
  invalid?: boolean;
}
/* A password field with the show-password button. The core flips the field's type
   and the button's `aria-pressed`. */
export const PasswordInput = forwardRef<HTMLInputElement, PasswordInputProps>(function PasswordInput(
  { toggleLabel = 'إظهار كلمة المرور', toggleTip, invalid, className, style, inputClassName, ...rest },
  ref
) {
  const f = useFieldControl({ ...rest, invalid });
  const input = useRef<HTMLInputElement>(null);
  useImperativeHandle(ref, () => input.current!, []);
  /* Shown, the field is `type="text"`: read back, so a re-render does not hide it. */
  const owned = useOwned(input);
  return (
    <div className={cx('ins-input-group', className)} style={style}>
      <input ref={input} className={cx('ins-input', inputClassName)} {...rest} {...f} type={owned('type', 'password')} />
      <button className="ins-password-toggle" data-ins-password="" aria-label={toggleLabel} data-ins-tip={toggleTip ? '' : undefined} type="button" aria-pressed="false" />
    </div>
  );
});

export interface NumberInputProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type' | 'size'>, Outer {
  downLabel?: string;
  upLabel?: string;
  invalid?: boolean;
}
/* A number field between − and + buttons. The core steps it, fires `input` and
   `change` as typing would (so `onChange` sees every step), and disables a button at
   its limit. */
export const NumberInput = forwardRef<HTMLInputElement, NumberInputProps>(function NumberInput(
  { downLabel = 'إنقاص', upLabel = 'زيادة', invalid, className, style, inputClassName, ...rest },
  ref
) {
  const f = useFieldControl({ ...rest, invalid });
  const id = useInsId(f.id);
  const value = parseFloat(String(rest.value ?? rest.defaultValue ?? ''));
  const min = parseFloat(String(rest.min ?? ''));
  const max = parseFloat(String(rest.max ?? ''));
  const atMin = !!rest.disabled || (!isNaN(value) && !isNaN(min) && value <= min);
  const atMax = !!rest.disabled || (!isNaN(value) && !isNaN(max) && value >= max);
  return (
    <div className={cx('ins-input-group', className)} style={style}>
      <button className="ins-spin" data-ins-spin="down" aria-label={downLabel} type="button" aria-controls={id} disabled={atMin} />
      <input ref={ref} className={cx('ins-input', inputClassName)} type="number" {...rest} {...f} id={id} />
      <button className="ins-spin" data-ins-spin="up" aria-label={upLabel} type="button" aria-controls={id} disabled={atMax} />
    </div>
  );
});

export interface RangeProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {}
/* A slider whose track fills up to the thumb. The fill is worked out here the way the
   core's rangeFill does, so a value set from code, which fires no `input` event,
   moves the fill too. */
export const Range = forwardRef<HTMLInputElement, RangeProps>(function Range({ className, style, ...rest }, ref) {
  const f = useFieldControl(rest);
  const min = parseFloat(String(rest.min ?? 0)) || 0;
  const maxRaw = parseFloat(String(rest.max ?? 100));
  const max = isNaN(maxRaw) ? 100 : maxRaw;
  const value = parseFloat(String(rest.value ?? rest.defaultValue ?? (min + max) / 2));
  const pct = max > min ? Math.max(0, Math.min(100, ((value - min) / (max - min)) * 100)) : 0;
  const nudge = (0.5 - pct / 100) * 1.25;
  const fill = { '--ins-fill': 'calc(' + pct.toFixed(3) + '% + ' + nudge.toFixed(4) + 'rem)' } as CSSProperties;
  return <input ref={ref} type="range" className={cx('ins-range', className)} style={{ ...fill, ...style }} {...rest} {...f} />;
});

/* -------------------------------------------------------------- segmented */

export interface SegOption {
  value: string;
  label: ReactNode;
  disabled?: boolean;
}
export interface SegProps extends Omit<HTMLAttributes<HTMLDivElement>, 'onChange' | 'defaultValue'> {
  options: SegOption[];
  value?: string;
  defaultValue?: string;
  onValueChange?: (value: string) => void;
  /** Names the control; it becomes a `group` with it. */
  label?: string;
}
/* A segmented control. The core moves the selection on a click and reports it; this
   follows, and puts a controlled value back if the page did not take the change. */
export const Seg = forwardRef<HTMLDivElement, SegProps>(function Seg(
  { options, value, defaultValue, onValueChange, label, className, ...rest },
  ref
) {
  const root = useRef<HTMLDivElement>(null);
  useImperativeHandle(ref, () => root.current!, []);
  const [own, setOwn] = useState(defaultValue ?? options[0]?.value);
  const [, force] = useState(0);
  const current = value !== undefined ? value : own;
  useInsEvent<{ el: Element; value: string }>(
    'ins:seg',
    (d) => {
      if (value === undefined) setOwn(d.value);
      else force((n) => n + 1);
      onValueChange?.(d.value);
    },
    (d) => d.el.parentNode === root.current
  );
  useIsoLayoutEffect(() => {
    const el = root.current;
    if (!el) return;
    for (const b of Array.from(el.children)) b.classList.toggle('is-active', b.getAttribute('data-ins-value') === current);
  });
  return (
    <div ref={root} className={cx('ins-seg', className)} role={label ? 'group' : undefined} aria-label={label} {...rest}>
      {options.map((o) => (
        <button key={o.value} type="button" className={o.value === current ? 'is-active' : undefined} data-ins-value={o.value} disabled={o.disabled}>
          {o.label}
        </button>
      ))}
    </div>
  );
});

/* ---------------------------------------------------------- toggle button */

export interface ToggleButtonProps extends ButtonLook, Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'type'> {
  pressed?: boolean;
  defaultPressed?: boolean;
  onPressedChange?: (pressed: boolean) => void;
  tip?: boolean | string;
}
/* A button that stays down: `aria-pressed`, flipped by the core. */
export const ToggleButton = forwardRef<HTMLButtonElement, ToggleButtonProps>(function ToggleButton(
  { pressed, defaultPressed, onPressedChange, variant, size, iconOnly, full, lift, tip, className, ...rest },
  ref
) {
  const btn = useRef<HTMLButtonElement>(null);
  useImperativeHandle(ref, () => btn.current!, []);
  const initial = pressed ?? defaultPressed ?? false;
  useInsEvent<{ el: Element; pressed: boolean }>('ins:toggle', (d) => onPressedChange?.(d.pressed), (d) => d.el === btn.current);
  useIsoLayoutEffect(() => {
    if (pressed !== undefined) btn.current?.setAttribute('aria-pressed', String(pressed));
  });
  return (
    <button
      ref={btn}
      type="button"
      className={buttonClass({ variant, size, iconOnly, full, lift }, className)}
      data-ins-toggle=""
      aria-pressed={initial}
      data-ins-tip={tip === true ? '' : tip || undefined}
      {...rest}
    />
  );
});
