/* The client-render page's bundle: the core and its plugins imported as a bundler
   user would import them, then the app mounted after the core's own scan. */
import '../../../dist/insiyab.js';
import '../../../dist/plugins/insiyab-hijri.js';
import '../../../dist/plugins/insiyab-palette.js';
import '../../../dist/plugins/insiyab-otp.js';
import '../../../dist/plugins/insiyab-phone.js';
import '../../../dist/plugins/insiyab-file.js';
import '../../../dist/plugins/insiyab-scrollspy.js';
import '../../../dist/plugins/insiyab-timeline.js';
import '../../../dist/plugins/insiyab-tree.js';
import '../../../dist/plugins/insiyab-color.js';
import '../../../dist/plugins/insiyab-carousel.js';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './app.js';

function mount() {
  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <App />
    </StrictMode>
  );
  (window as unknown as { __mounted: boolean }).__mounted = true;
}
if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', mount);
else mount();
