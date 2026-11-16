// Applies the theme before the first paint, so the page never flashes the wrong one.
// Loaded without "defer" on purpose. The stored choice wins; otherwise the system setting is used.
(function () {
  var stored = null;
  try {
    stored = localStorage.getItem('menlo.theme');
  } catch (e) {
    // Storage can be blocked; fall back to the system setting
  }

  var theme =
    stored === 'light' || stored === 'dark'
      ? stored
      : window.matchMedia('(prefers-color-scheme: dark)').matches
        ? 'dark'
        : 'light';

  document.documentElement.setAttribute('data-theme', theme);
})();
