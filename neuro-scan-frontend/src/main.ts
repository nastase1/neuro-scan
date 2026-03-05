import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

// Suppress the benign ResizeObserver browser quirk from polluting the console
const resizeObserverErr = /ResizeObserver loop/;
const originalError = window.onerror;
window.onerror = (msg, ...rest) => {
  if (typeof msg === 'string' && resizeObserverErr.test(msg)) return true;
  return originalError ? originalError(msg, ...rest) : false;
};

bootstrapApplication(App, appConfig)
  .catch((err) => console.error(err));
