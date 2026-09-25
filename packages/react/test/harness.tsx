/* The browser side of the tests, bundled with React in its development build, so
   a hydration mismatch or a React warning reaches the console, where every test
   fails on it. */
import { StrictMode } from 'react';
import { flushSync } from 'react-dom';
import { createRoot, hydrateRoot, type Root } from 'react-dom/client';
import { App } from './app.js';
import { cases } from './cases.js';

let root: Root | null = null;

(window as unknown as { __react: unknown }).__react = {
  cases: Object.keys(cases),
  render(name: string, container: HTMLElement) {
    root = createRoot(container);
    flushSync(() => root!.render(<StrictMode>{cases[name].el}</StrictMode>));
    return 0;
  },
  unmount() {
    root?.unmount();
    root = null;
    return 0;
  },
  app(container: HTMLElement) {
    root = createRoot(container);
    flushSync(() => root!.render(<StrictMode><App /></StrictMode>));
    return 0;
  },
  hydrate(container: HTMLElement) {
    root = hydrateRoot(container, <StrictMode><App /></StrictMode>);
    return 0;
  }
};
