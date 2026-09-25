'use client';
import { useEffect, useState } from 'react';
import { core, type ThemeMode } from './core.js';
import { useInsEvent } from './util.js';

/* The theme actually showing, and a setter that remembers the choice (`system`
   forgets it). `undefined` on the server and in the first render, which cannot know. */
export function useTheme(): [theme: 'dark' | 'light' | undefined, setTheme: (mode: ThemeMode) => void] {
  const [theme, setTheme] = useState<'dark' | 'light'>();
  useEffect(() => setTheme(core()?.theme()), []);
  useInsEvent<{ theme: 'dark' | 'light' }>('ins:theme', (d) => setTheme(d.theme), () => true);
  return [theme, (mode) => core()?.theme(mode)];
}

/* Any of the core's `ins:*` events, for as long as the component is mounted. */
export function useInsiyabEvent<D = unknown>(name: `ins:${string}`, handler: (detail: D, event: CustomEvent<D>) => void): void {
  useInsEvent<D>(name, handler, () => true);
}
