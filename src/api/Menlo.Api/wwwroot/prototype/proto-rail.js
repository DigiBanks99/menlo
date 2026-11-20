// PROTOTYPE (#596) variant D: opens the side rail's panel on click and folds it away when the pointer leaves
// the rail and panel, on Escape, or on a tap elsewhere. Delegated on document, so htmx body swaps keep working.
(function () {
  if (window.__protoRail) return;
  window.__protoRail = true;

  function zone(el) {
    return el && el.closest ? el.closest('.rail, .rail__flyout') : null;
  }

  function setPanel(host, key) {
    host.dataset.open = key ? 'true' : 'false';
    host.querySelectorAll('[data-proto-rail-open]').forEach(function (a) {
      a.setAttribute('aria-expanded', a.dataset.protoRailOpen === key ? 'true' : 'false');
    });
    if (key) {
      host.querySelectorAll('[data-proto-rail-section]').forEach(function (d) {
        d.open = d.dataset.protoRailSection === key;
      });
    }
    var url = new URL(window.location.href);
    if (key) url.searchParams.set('panel', key); else url.searchParams.delete('panel');
    url.searchParams.delete('commit');
    history.replaceState(history.state, '', url);
  }

  document.addEventListener('click', function (e) {
    var icon = e.target.closest && e.target.closest('[data-proto-rail-open]');
    if (!icon) return;
    e.preventDefault();
    var host = icon.closest('[data-proto-rail]');
    var key = icon.dataset.protoRailOpen;
    var already = host.dataset.open === 'true' && icon.getAttribute('aria-expanded') === 'true';
    setPanel(host, already ? null : key);
  });

  document.addEventListener('pointerout', function (e) {
    if (e.pointerType === 'touch') return;
    if (!zone(e.target) || zone(e.relatedTarget)) return;
    var host = e.target.closest('[data-proto-rail]');
    if (host && host.dataset.open === 'true') setPanel(host, null);
  });

  document.addEventListener('pointerdown', function (e) {
    if (zone(e.target)) return;
    document.querySelectorAll('[data-proto-rail][data-open="true"]').forEach(function (h) { setPanel(h, null); });
  });

  document.addEventListener('keydown', function (e) {
    if (e.key !== 'Escape') return;
    document.querySelectorAll('[data-proto-rail][data-open="true"]').forEach(function (h) {
      setPanel(h, null);
      var icon = h.querySelector('.rail__icon');
      if (icon) icon.focus();
    });
  });
})();

// PROTOTYPE (#596): the Category column's width toggle on small screens. Remembered for the tab, so
// opening a category (a page load) keeps the column as it was.
(function () {
  if (window.__protoNames) return;
  window.__protoNames = true;

  function apply() {
    var wide = sessionStorage.getItem('proto-names') === 'wide';
    document.querySelectorAll('table[data-names]').forEach(function (t) {
      t.dataset.names = wide ? 'wide' : 'narrow';
    });
    document.querySelectorAll('[data-proto-names]').forEach(function (b) {
      b.setAttribute('aria-pressed', wide ? 'true' : 'false');
    });
  }

  document.addEventListener('click', function (e) {
    var button = e.target.closest && e.target.closest('[data-proto-names]');
    if (!button) return;
    var wide = sessionStorage.getItem('proto-names') === 'wide';
    sessionStorage.setItem('proto-names', wide ? 'narrow' : 'wide');
    apply();
  });

  document.addEventListener('htmx:afterSettle', apply);
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', apply); else apply();
})();
