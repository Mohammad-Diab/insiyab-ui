'use client';
import {
  Children,
  cloneElement,
  createContext,
  forwardRef,
  isValidElement,
  useContext,
  useImperativeHandle,
  useRef,
  useState,
  type AnchorHTMLAttributes,
  type ButtonHTMLAttributes,
  type DetailsHTMLAttributes,
  type ElementType,
  type HTMLAttributes,
  type ReactElement,
  type ReactNode
} from 'react';
import { core } from './core.js';
import { Form, type FormProps } from './forms.js';
import { cx, useInsEvent, useInsId, useIsoLayoutEffect } from './util.js';

/* ------------------------------------------------------------- page head */

export interface PageHeadProps extends Omit<HTMLAttributes<HTMLElement>, 'title'> {
  title: ReactNode;
  titleAs?: ElementType;
  sub?: ReactNode;
  eyebrow?: ReactNode;
  /** Above the title: a `<Breadcrumb>`. */
  breadcrumb?: ReactNode;
  /** At the end: the page's buttons. */
  actions?: ReactNode;
}
export const PageHead = forwardRef<HTMLElement, PageHeadProps>(function PageHead(
  { title, titleAs: T = 'h1', sub, eyebrow, breadcrumb, actions, className, children, ...rest },
  ref
) {
  return (
    <header ref={ref} className={cx('ins-page-head', className)} {...rest}>
      {breadcrumb}
      <div className="ins-page-head-text">
        {eyebrow != null && <span className="ins-eyebrow">{eyebrow}</span>}
        <T className="ins-page-title">{title}</T>
        {sub != null && <p className="ins-page-sub">{sub}</p>}
        {children}
      </div>
      {actions != null && <div className="ins-page-actions">{actions}</div>}
    </header>
  );
});

export interface Crumb {
  label: ReactNode;
  href?: string;
}
export interface BreadcrumbProps extends HTMLAttributes<HTMLElement> {
  /** The last one is the current page, and needs no `href`. */
  items: Crumb[];
  label?: string;
  /** Renders a link, for a router's own `<Link>`. */
  renderLink?: (crumb: Crumb) => ReactNode;
}
export const Breadcrumb = forwardRef<HTMLElement, BreadcrumbProps>(function Breadcrumb({ items, label = 'مسار التنقّل', renderLink, className, ...rest }, ref) {
  return (
    <nav ref={ref} className={cx('ins-breadcrumb', className)} aria-label={label} {...rest}>
      <ol>
        {items.map((c, i) => (
          <li key={i}>
            {i === items.length - 1 ? <span aria-current="page">{c.label}</span> : renderLink ? renderLink(c) : <a href={c.href}>{c.label}</a>}
          </li>
        ))}
      </ol>
    </nav>
  );
});

/* ------------------------------------------------------------------ tabs */
/* The strip and its panels, with the tab pattern's roles written out. The core
   switches them on a click and on the arrow keys (in the page's direction) and
   reports it; these follow. */

interface TabsState {
  base: string;
  current: string | undefined;
  first: (value: string) => void;
}
const TabsContext = createContext<TabsState | null>(null);
const TabListContext = createContext({ seg: false });

function hash(s: string): string {
  let h = 5381;
  for (let i = 0; i < s.length; i++) h = ((h << 5) + h + s.charCodeAt(i)) | 0;
  return (h >>> 0).toString(36);
}
const tabId = (base: string, v: string) => base + '-t' + hash(v);
const panelId = (base: string, v: string) => base + '-p' + hash(v);

export interface TabsProps {
  value?: string;
  defaultValue?: string;
  onValueChange?: (value: string) => void;
  children?: ReactNode;
}
export function Tabs({ value, defaultValue, onValueChange, children }: TabsProps) {
  const base = useInsId();
  const [own, setOwn] = useState(defaultValue);
  const [fallback, setFallback] = useState<string>();
  const current = value ?? own ?? fallback;
  const [, force] = useState(0);
  const quiet = useRef(false);

  useInsEvent<{ tab: Element; panel: Element }>(
    'ins:tab',
    (d) => {
      if (quiet.current) return;
      const v = d.tab.getAttribute('data-value') ?? '';
      if (value === undefined) setOwn(v);
      else force((n) => n + 1);
      onValueChange?.(v);
    },
    (d) => d.tab.id.indexOf(base + '-t') === 0
  );

  /* A controlled value the page did not change: the core already moved, so it is
     moved back, without reporting it as a new choice. */
  useIsoLayoutEffect(() => {
    if (current === undefined) return;
    const tab = document.getElementById(tabId(base, current));
    const api = core();
    if (!tab || !api || tab.classList.contains('is-active')) return;
    quiet.current = true;
    try {
      api.tab(tab);
    } finally {
      quiet.current = false;
    }
  });

  return <TabsContext.Provider value={{ base, current, first: (v) => fallback === undefined && setFallback(v) }}>{children}</TabsContext.Provider>;
}

export interface TabListProps extends HTMLAttributes<HTMLDivElement> {
  label?: string;
  /** The segmented look, for a small switch in a panel's head. */
  seg?: boolean;
  /** The lighter look: a row over a hairline with a travelling underline. */
  line?: boolean;
}
export const TabList = forwardRef<HTMLDivElement, TabListProps>(function TabList({ label, seg, line, className, children, ...rest }, ref) {
  const ctx = useContext(TabsContext);
  /* With no value given, the first tab is the one shown, as the core would choose. */
  const firstValue = Children.toArray(children).find((c): c is ReactElement<{ value: string }> => isValidElement(c) && typeof (c.props as { value?: unknown }).value === 'string')?.props.value;
  useIsoLayoutEffect(() => {
    if (ctx && ctx.current === undefined && firstValue !== undefined) ctx.first(firstValue);
  });
  return (
    <div ref={ref} className={cx(seg ? 'ins-seg' : line ? 'ins-tablist ins-tablist--line' : 'ins-tablist', className)} role="tablist" aria-label={label} {...rest}>
      <TabListContext.Provider value={{ seg: !!seg }}>{children}</TabListContext.Provider>
    </div>
  );
});

/* No `id`: a tab's id is made from its value, because its panel points at it and the
   core's scan may already have written that pointer into the server's HTML. */
export interface TabProps extends Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'value' | 'id'> {
  value: string;
}
export const Tab = forwardRef<HTMLButtonElement, TabProps>(function Tab({ value, className, ...rest }, ref) {
  const ctx = useContext(TabsContext);
  const { seg } = useContext(TabListContext);
  if (!ctx) throw new Error('<Tab> belongs inside <Tabs>.');
  const on = ctx.current === value;
  return (
    <button
      ref={ref}
      type="button"
      className={cx(!seg && 'ins-tab', on && 'is-active', className)}
      data-ins-tab={'#' + panelId(ctx.base, value)}
      data-value={value}
      id={tabId(ctx.base, value)}
      role="tab"
      aria-controls={panelId(ctx.base, value)}
      aria-selected={on}
      tabIndex={on ? 0 : -1}
      {...rest}
    />
  );
});

export interface TabPanelProps extends HTMLAttributes<HTMLDivElement> {
  value: string;
}
export const TabPanel = forwardRef<HTMLDivElement, TabPanelProps>(function TabPanel({ value, className, ...rest }, ref) {
  const ctx = useContext(TabsContext);
  if (!ctx) throw new Error('<TabPanel> belongs inside <Tabs>.');
  return (
    <div
      ref={ref}
      className={cx('ins-tabpanel', ctx.current === value && 'is-active', className)}
      id={panelId(ctx.base, value)}
      role="tabpanel"
      aria-labelledby={tabId(ctx.base, value)}
      tabIndex={0}
      {...rest}
    />
  );
});

/* ------------------------------------------------------------ pagination */

export interface PaginationProps extends HTMLAttributes<HTMLElement> {
  /** The current page, from 1. */
  page: number;
  count: number;
  /** Pages as links: the address of page `n`. */
  href?: (page: number) => string;
  /** Pages as buttons, for a list that changes in place. */
  onPageChange?: (page: number) => void;
  /** Pages shown either side of the current one. */
  siblings?: number;
  label?: string;
  prevLabel?: ReactNode;
  nextLabel?: ReactNode;
}

function pageWindow(page: number, count: number, siblings: number): Array<number | 'gap'> {
  const out: Array<number | 'gap'> = [];
  const from = Math.max(2, page - siblings), to = Math.min(count - 1, page + siblings);
  out.push(1);
  if (from > 2) out.push(from === 3 ? 2 : 'gap');
  for (let n = from; n <= to; n++) out.push(n);
  if (to < count - 1) out.push(to === count - 2 ? count - 1 : 'gap');
  if (count > 1) out.push(count);
  return out;
}

export const Pagination = forwardRef<HTMLElement, PaginationProps>(function Pagination(
  { page, count, href, onPageChange, siblings = 1, label, prevLabel = 'السابق', nextLabel = 'التالي', className, ...rest },
  ref
) {
  const link = (n: number, content: ReactNode, cls: string | undefined, key?: string | number) => {
    const current = n === page && !cls;
    const off = n < 1 || n > count;
    if (href) {
      return (
        <a key={key} className={cx('ins-page', cls)} href={current || off ? undefined : href(n)} aria-current={current ? 'page' : undefined} aria-disabled={off ? true : undefined}>
          {content}
        </a>
      );
    }
    return (
      <button key={key} type="button" className={cx('ins-page', cls)} aria-current={current ? 'page' : undefined} disabled={off} onClick={current ? undefined : () => onPageChange?.(n)}>
        {content}
      </button>
    );
  };
  return (
    <nav ref={ref} className={cx('ins-pagination', className)} aria-label={label} {...rest}>
      {link(page - 1, prevLabel, 'ins-page--prev', 'prev')}
      {pageWindow(page, count, siblings).map((n, i) =>
        n === 'gap' ? (
          <span key={'gap' + i} className="ins-page-gap">
            …
          </span>
        ) : (
          link(n, n, undefined, n)
        )
      )}
      {link(page + 1, nextLabel, 'ins-page--next', 'next')}
    </nav>
  );
});

/* ---------------------------------------------------------------- navbar */

export interface NavbarProps extends HTMLAttributes<HTMLElement> {
  /** The brand at the start: text, a logo, or both. */
  brand?: ReactNode;
  brandHref?: string;
  /** Names the menu's <nav>. */
  label?: string;
  toggleLabel?: string;
  /** Held at the end of the menu (`.ins-navbar-end`). */
  end?: ReactNode;
  /** In the page's flow instead of stuck to the top. */
  static?: boolean;
  open?: boolean;
  defaultOpen?: boolean;
  onOpenChange?: (open: boolean) => void;
}
/* A site's top bar. Below 900px the links fold behind the toggle, which the core
   opens and closes. */
export const Navbar = forwardRef<HTMLElement, NavbarProps>(function Navbar(
  { brand, brandHref = '/', label, toggleLabel = 'القائمة', end, static: isStatic, open, defaultOpen, onOpenChange, className, children, ...rest },
  ref
) {
  const el = useRef<HTMLElement>(null);
  useImperativeHandle(ref, () => el.current!, []);
  const menuId = useInsId() + '-menu';
  const [own, setOwn] = useState(!!defaultOpen);
  const isOpen = open ?? own;
  const quiet = useRef(false);
  const [, force] = useState(0);
  useInsEvent<{ el: Element; open: boolean }>(
    'ins:navbar',
    (d) => {
      if (quiet.current) return;
      if (open === undefined) setOwn(d.open);
      else force((n) => n + 1);
      onOpenChange?.(d.open);
    },
    (d) => d.el === el.current
  );
  useIsoLayoutEffect(() => {
    const bar = el.current, api = core();
    if (!bar || !api || bar.classList.contains('is-open') === isOpen) return;
    quiet.current = true;
    try {
      api.navbar(bar, isOpen);
    } finally {
      quiet.current = false;
    }
  });
  return (
    <header ref={el} className={cx('ins-navbar', isStatic && 'ins-navbar--static', isOpen && 'is-open', className)} {...rest}>
      {brand != null && (
        <a className="ins-navbar-brand" href={brandHref}>
          {brand}
        </a>
      )}
      <button className="ins-navbar-toggle" data-ins-navbar="" aria-label={toggleLabel} aria-expanded={isOpen} aria-controls={menuId} type="button" />
      <nav className="ins-navbar-menu" id={menuId} aria-label={label}>
        {children}
        {end != null && <div className="ins-navbar-end">{end}</div>}
      </nav>
    </header>
  );
});

export interface NavbarLinkProps extends AnchorHTMLAttributes<HTMLAnchorElement> {
  /** This page: `aria-current="page"`. */
  current?: boolean;
}
export const NavbarLink = forwardRef<HTMLAnchorElement, NavbarLinkProps>(function NavbarLink({ current, className, ...rest }, ref) {
  return <a ref={ref} className={cx('ins-navbar-link', className)} aria-current={current ? 'page' : undefined} {...rest} />;
});

/* ------------------------------------------------------------- accordion */

export const Accordion = forwardRef<HTMLDivElement, HTMLAttributes<HTMLDivElement>>(function Accordion({ className, ...rest }, ref) {
  return <div ref={ref} className={cx('ins-accordion', className)} {...rest} />;
});

export interface CollapseProps extends Omit<DetailsHTMLAttributes<HTMLDetailsElement>, 'title'> {
  summary: ReactNode;
  defaultOpen?: boolean;
  onOpenChange?: (open: boolean) => void;
  /** Sections sharing a `name` open one at a time: the platform's own exclusive accordion. */
  name?: string;
  bodyClassName?: string;
}
/* A <details> section. It opens with no script at all. */
export const Collapse = forwardRef<HTMLDetailsElement, CollapseProps>(function Collapse(
  { summary, defaultOpen, open, onOpenChange, onToggle, name, bodyClassName, className, children, ...rest },
  ref
) {
  return (
    <details
      ref={ref}
      className={cx('ins-collapse', className)}
      open={open ?? defaultOpen}
      {...({ name } as object)}
      onToggle={(e) => {
        onToggle?.(e);
        onOpenChange?.(e.currentTarget.open);
      }}
      {...rest}
    >
      <summary>{summary}</summary>
      <div className={cx('ins-collapse-body', bodyClassName)}>{children}</div>
    </details>
  );
});

/* ----------------------------------------------------------------- steps */

export interface StepsProps extends HTMLAttributes<HTMLOListElement> {
  items: ReactNode[];
  /** The step in progress, from 0: those before it are done. */
  current?: number;
  /** Small dots instead of numbers (`ins-steps--dots`), for a login card. */
  dots?: boolean;
}
export const Steps = forwardRef<HTMLOListElement, StepsProps>(function Steps({ items, current, dots, className, ...rest }, ref) {
  return (
    <ol ref={ref} className={cx('ins-steps', dots && 'ins-steps--dots', className)} {...rest}>
      {items.map((label, i) => (
        <li key={i} className={cx('ins-steps-item', current !== undefined && i < current && 'is-done')} aria-current={i === current ? 'step' : undefined}>
          <span className="ins-steps-label">{label}</span>
        </li>
      ))}
    </ol>
  );
});

/* ---------------------------------------------------------------- wizard */

const WizardContext = createContext<{ step: number }>({ step: 0 });

export interface WizardProps extends Omit<FormProps, 'validate'> {
  /** The names over the steps. */
  steps: ReactNode[];
  step?: number;
  defaultStep?: number;
  onStepChange?: (step: number) => void;
  /** Inline messages in each field; on by default. */
  validate?: boolean;
  prevLabel?: ReactNode;
  nextLabel?: ReactNode;
  finishLabel?: ReactNode;
  bodyClassName?: string;
  /** How a step arrives. Either one puts the panels in `.ins-wizard-body`, so the
      wizard changes height smoothly: 'pane' fades the new step in, 'slide' slides the
      steps side by side (`data-ins-wizard-motion="slide"`). */
  motion?: 'pane' | 'slide';
  /** Dots instead of numbered steps. */
  dots?: boolean;
}
/* A form in steps. Next checks the step's own fields with the browser's validation
   before it moves on. Without script it is one long form, which still submits. */
export const Wizard = forwardRef<HTMLFormElement, WizardProps>(function Wizard(
  { steps, step, defaultStep = 0, onStepChange, validate = true, prevLabel = 'السابق', nextLabel = 'التالي', finishLabel = 'إنهاء', bodyClassName, motion, dots, className, children, ...rest },
  ref
) {
  const el = useRef<HTMLFormElement>(null);
  useImperativeHandle(ref, () => el.current!, []);
  const [own, setOwn] = useState(defaultStep);
  const current = step ?? own;
  const quiet = useRef(false);
  const [, force] = useState(0);
  const panels = Children.toArray(children).filter((c) => isValidElement(c) && c.type === WizardPanel) as ReactElement<WizardPanelProps>[];
  const last = panels.length - 1;

  useInsEvent<{ el: Element; step: number }>(
    'ins:wizard',
    (d) => {
      if (quiet.current) return;
      if (step === undefined) setOwn(d.step);
      else force((n) => n + 1);
      onStepChange?.(d.step);
    },
    (d) => d.el === el.current
  );
  useIsoLayoutEffect(() => {
    const w = el.current, api = core();
    if (!w || !api || api.wizard(w) === current) return;
    quiet.current = true;
    try {
      api.wizard(w, current);
    } finally {
      quiet.current = false;
    }
  });

  return (
    <WizardContext.Provider value={{ step: current }}>
      <Form
        ref={el}
        validate={validate}
        className={cx('ins-panel', 'ins-wizard', className)}
        data-ins-first={current === 0 ? '' : undefined}
        data-ins-last={current === last ? '' : undefined}
        data-ins-wizard-motion={motion === 'slide' ? 'slide' : undefined}
        {...rest}
      >
        <div className={cx('ins-panel-body', bodyClassName)}>
          <Steps items={steps} current={current} dots={dots} />
          {motion ? (
            <div className="ins-wizard-body">{panels.map((p, i) => cloneElement(p, { key: p.key ?? i, index: i }))}</div>
          ) : (
            panels.map((p, i) => cloneElement(p, { key: p.key ?? i, index: i }))
          )}
          <div className="ins-wizard-foot">
            <button type="button" className="ins-btn ins-btn--secondary" data-ins-wizard="prev">
              {prevLabel}
            </button>
            <button type="button" className="ins-btn ins-btn--primary" data-ins-wizard="next">
              {nextLabel}
            </button>
            <button className="ins-btn ins-btn--primary ins-wizard-finish">{finishLabel}</button>
          </div>
        </div>
      </Form>
    </WizardContext.Provider>
  );
});

export interface WizardPanelProps extends HTMLAttributes<HTMLElement> {
  /** The step's name, for a screen reader. */
  label?: string;
  /** Set by the wizard. */
  index?: number;
}
export const WizardPanel = forwardRef<HTMLElement, WizardPanelProps>(function WizardPanel({ label, index = 0, className, ...rest }, ref) {
  const { step } = useContext(WizardContext);
  return <section ref={ref} className={cx('ins-wizard-panel', step === index && 'is-active', className)} aria-label={label} tabIndex={-1} {...rest} />;
});
