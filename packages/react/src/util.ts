import {
  Children,
  cloneElement,
  isValidElement,
  useEffect,
  useId,
  useLayoutEffect,
  useRef,
  type ReactElement,
  type ReactNode,
  type RefObject
} from 'react';
import { core } from './core.js';

/* Class names, joined; `undefined` rather than an empty attribute. */
export function cx(...parts: Array<string | false | null | undefined | 0>): string | undefined {
  const out = parts.filter(Boolean).join(' ');
  return out || undefined;
}

/* A layout effect in the browser, so the core builds before the first paint, as it
   does on a plain page; nothing on a server, where there is nothing to build. */
export const useIsoLayoutEffect = typeof window !== 'undefined' ? useLayoutEffect : useEffect;

/* An id React makes, made safe for a CSS selector: the core finds a tab's panel, a
   range's other end and a stepper's field with querySelector, and React's own ids
   carry colons or guillemets. The same on the server and in the browser. */
export function useInsId(own?: string): string {
  const raw = useId();
  return own || 'ins' + raw.replace(/[^A-Za-z0-9_-]/g, '');
}

/* The latest value of something, for listeners set up once. */
export function useLatest<T>(value: T): RefObject<T> {
  const ref = useRef(value);
  ref.current = value;
  return ref;
}

/* Listens to one of the core's `ins:*` events. The core dispatches on `document`
   and the plugins on their own element; both bubble to `document`, so that is where
   this listens, and `mine` picks this component's events out of the page's. */
export function useInsEvent<D>(
  name: string,
  handler: ((detail: D, event: CustomEvent<D>) => void) | undefined,
  mine: (detail: D, event: CustomEvent<D>) => boolean
): void {
  const h = useLatest(handler);
  const m = useLatest(mine);
  useEffect(() => {
    const listen = (e: Event) => {
      const ev = e as CustomEvent<D>;
      if (h.current && m.current(ev.detail, ev)) h.current(ev.detail, ev);
    };
    document.addEventListener(name, listen);
    return () => document.removeEventListener(name, listen);
  }, [name]);
}

/* Hands a subtree to the core once it is in the page.

   `attrs` are the attributes that ask a builder for its work: `data-ins-date`,
   `data-ins-phone` and the like. They are set here, after React has mounted,
   and never rendered, for two reasons. On a server-rendered page the core's own
   scan runs at DOMContentLoaded, which can come before React hydrates, and a field
   it had already rebuilt would no longer match the server's HTML. And React never
   touches an attribute it did not render, so it cannot take one away again.

   `scope` is where `Insiyab.init` looks: the core's builders search inside the
   element they are given, so a field that is itself the element is scanned from
   its parent. */
export function useEnhance(
  ref: RefObject<Element | null>,
  attrs: Record<string, string | undefined | null | false> = {},
  options: { scope?: 'self' | 'parent'; onReady?: (el: Element) => void } = {}
): void {
  const ready = useLatest(options.onReady);
  useIsoLayoutEffect(() => {
    const el = ref.current;
    if (!el) return;
    for (const name of Object.keys(attrs)) {
      const value = attrs[name];
      if (value === undefined || value === null || value === false) continue;
      if (!el.hasAttribute(name)) el.setAttribute(name, value);
    }
    const api = core();
    if (!api) return;
    const scope = options.scope === 'parent' ? el.parentElement || el : el;
    api.init(scope);
    ready.current?.(el);
  }, []);
}

/* Runs `dispose` once the element has really left the page. A layout effect's
   cleanup also runs in Strict Mode's rehearsal, when nothing has left, so it looks
   again a moment later rather than trusting the cleanup alone. */
export function useWhenGone(ref: RefObject<Element | null>, dispose: (el: Element) => void): void {
  const d = useLatest(dispose);
  useEffect(() => {
    const el = ref.current;
    return () => {
      if (!el) return;
      setTimeout(() => {
        if (!el.isConnected) d.current(el);
      }, 0);
    };
  }, []);
}

/* An attribute the core takes over once it builds: the date field's `type` and
   `name`, a phone field's `type`, a shown password's `type`. React 19 writes an
   input's `type` and `name` again on every update of that input, so after the first
   render these are read back from the element and handed to React as they are:
   React then writes what is already there. Before the first render, and on a server,
   it is the value the markup starts with. */
export function useOwned(ref: RefObject<HTMLInputElement | null>) {
  return (attr: 'type' | 'name', initial: string | undefined): string | undefined => {
    const el = ref.current;
    if (!el) return initial;
    return el.getAttribute(attr) ?? undefined;
  };
}

/* Adds a class to an element given as a prop (an icon, say) without wrapping it,
   so `icon={<svg>…</svg>}` becomes `<svg class="… ins-search-ico">`. */
export function withClass(node: ReactNode, className: string): ReactNode {
  if (!isValidElement(node)) return node;
  const el = node as ReactElement<{ className?: string }>;
  return cloneElement(el, { className: cx(el.props.className, className) });
}

/* The same, for every element child. */
export function childrenWithClass(children: ReactNode, className: string): ReactNode {
  return Children.map(children, (child) => withClass(child, className));
}

/* `ins-gap-3` and friends, from a number on the spacing scale. */
export type Space = 0 | 1 | 2 | 3 | 4 | 5 | 6 | 8 | 12;
export function gap(n: Space | undefined): string | undefined {
  return n === undefined ? undefined : 'ins-gap-' + n;
}
