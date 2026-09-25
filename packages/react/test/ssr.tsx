import { renderToString } from 'react-dom/server';
import { App } from './app.js';

export function renderApp(): string {
  return renderToString(<App />);
}
