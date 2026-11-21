// PROTOTYPE (#658): the views prototype's side rail (proto-rail.js), for the plan prototype. Same behaviour:
// an icon opens its panel, and the panel folds away when the pointer leaves, on Escape or on a tap elsewhere.
// Two changes for forms: it doesn't fold while a field in it has focus, and a form sent from the panel comes
// back with the panel open. Delegated on document, so htmx body swaps keep working.
(function () {
  if (window.__planRail) return;
  window.__planRail = true;

  function zone(el) {
    return el && el.closest ? el.closest('.rail, .rail__flyout') : null;
  }

  function typing(host) {
    var active = document.activeElement;
    return !!(active && host.contains(active) && active.closest('.rail__flyout') && active.matches('input, select, textarea'));
  }

  function setPanel(host, key) {
    host.dataset.open = key ? 'true' : 'false';
    host.dataset.wide = key === 'line' || key === 'category' ? 'true' : 'false';
    host.querySelectorAll('[data-plan-rail-open]').forEach(function (a) {
      a.setAttribute('aria-expanded', a.dataset.planRailOpen === key ? 'true' : 'false');
    });
    if (key) {
      host.querySelectorAll('[data-plan-rail-section]').forEach(function (d) {
        d.open = d.dataset.planRailSection === key;
      });
    }
    var url = new URL(window.location.href);
    if (key) url.searchParams.set('panel', key); else url.searchParams.delete('panel');
    history.replaceState(history.state, '', url);
  }

  document.addEventListener('click', function (e) {
    var icon = e.target.closest && e.target.closest('[data-plan-rail-open]');
    if (!icon) return;
    e.preventDefault();
    var host = icon.closest('[data-plan-rail]');
    var key = icon.dataset.planRailOpen;
    var already = host.dataset.open === 'true' && icon.getAttribute('aria-expanded') === 'true';
    setPanel(host, already ? null : key);
  });

  document.addEventListener('pointerout', function (e) {
    if (e.pointerType === 'touch') return;
    if (!zone(e.target) || zone(e.relatedTarget)) return;
    var host = e.target.closest('[data-plan-rail]');
    if (host && host.dataset.open === 'true' && !typing(host)) setPanel(host, null);
  });

  document.addEventListener('pointerdown', function (e) {
    if (zone(e.target)) return;
    document.querySelectorAll('[data-plan-rail][data-open="true"]').forEach(function (h) { setPanel(h, null); });
  });

  document.addEventListener('keydown', function (e) {
    if (e.key !== 'Escape') return;
    document.querySelectorAll('[data-plan-rail][data-open="true"]').forEach(function (h) {
      setPanel(h, null);
      var icon = h.querySelector('.rail__icon');
      if (icon) icon.focus();
    });
  });

  // A form in the panel goes back to the page as it is now, panel included
  document.addEventListener('submit', function (e) {
    var form = e.target;
    if (!form.closest || !form.closest('.rail__flyout')) return;
    var back = form.querySelector('input[name="back"]');
    if (!back) return;
    var now = new URL(window.location.href);
    var sent = new URL(back.value, window.location.origin);
    var panel = now.searchParams.get('panel');
    if (panel) sent.searchParams.set('panel', panel);
    back.value = sent.pathname + sent.search;
  }, true);
})();

// PROTOTYPE (#596, reused): the Category column's width toggle on small screens, remembered for the tab.
(function () {
  if (window.__planNames) return;
  window.__planNames = true;

  function apply() {
    var wide = sessionStorage.getItem('proto-names') === 'wide';
    document.querySelectorAll('table[data-names]').forEach(function (t) {
      t.dataset.names = wide ? 'wide' : 'narrow';
    });
    document.querySelectorAll('[data-plan-names]').forEach(function (b) {
      b.setAttribute('aria-pressed', wide ? 'true' : 'false');
    });
  }

  document.addEventListener('click', function (e) {
    var button = e.target.closest && e.target.closest('[data-plan-names]');
    if (!button) return;
    var wide = sessionStorage.getItem('proto-names') === 'wide';
    sessionStorage.setItem('proto-names', wide ? 'narrow' : 'wide');
    apply();
  });

  document.addEventListener('htmx:afterSettle', apply);
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', apply); else apply();
})();
