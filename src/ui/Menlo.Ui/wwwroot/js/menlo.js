// Menlo's client behaviour. It is deliberately small: the server renders HTML and htmx swaps it.
// Everything here is presentation behaviour that needs no data from the server:
//
//   theme      [data-mnl-theme-toggle] switches and remembers the theme
//   csrf       adds the anti-forgery header to every htmx request
//   panels     opens <dialog data-mnl-panel> when it arrives, removes it when it closes
//   toasts     shows the "mnl:toast" event as a toast, and dismisses toasts after a few seconds
//   split rows [data-mnl-rows] adds and removes repeated form rows and keeps their total up to date
//   sign-in    sends the browser to the sign-in page when a fragment request comes back 401
//
// Rules: no business logic, no fetching, no state beyond the DOM. If a behaviour needs data, make
// it an htmx request and let the server render the result.
(function () {
  'use strict';

  var TOAST_DURATION_MS = 5000;
  var MAX_TOASTS = 3;
  var THEME_KEY = 'menlo.theme';

  // ---- theme ---------------------------------------------------------------------------------

  function setTheme(theme, remember) {
    document.documentElement.setAttribute('data-theme', theme);
    if (remember) {
      try {
        localStorage.setItem(THEME_KEY, theme);
      } catch (e) {
        // Storage can be blocked; the theme still applies for this page
      }
    }
  }

  function hasStoredTheme() {
    try {
      return localStorage.getItem(THEME_KEY) !== null;
    } catch (e) {
      return false;
    }
  }

  document.addEventListener('click', function (event) {
    if (!event.target.closest('[data-mnl-theme-toggle]')) {
      return;
    }

    var current = document.documentElement.getAttribute('data-theme');
    setTheme(current === 'dark' ? 'light' : 'dark', true);
  });

  window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function (event) {
    if (!hasStoredTheme()) {
      setTheme(event.matches ? 'dark' : 'light', false);
    }
  });

  // ---- csrf ----------------------------------------------------------------------------------

  function readCookie(name) {
    var match = document.cookie.match(new RegExp('(?:^|; )' + name + '=([^;]*)'));
    return match ? decodeURIComponent(match[1]) : null;
  }

  document.addEventListener('htmx:configRequest', function (event) {
    var token = readCookie('XSRF-TOKEN');
    if (token) {
      event.detail.headers['X-XSRF-TOKEN'] = token;
    }
  });

  // ---- panels --------------------------------------------------------------------------------

  function openPanels(root) {
    var panels = root.querySelectorAll ? root.querySelectorAll('dialog[data-mnl-panel]') : [];
    panels.forEach(function (panel) {
      if (panel.open) {
        return;
      }

      panel.addEventListener('close', function () {
        panel.remove();
      });
      panel.showModal();
    });
  }

  function closePanels() {
    document.querySelectorAll('dialog[data-mnl-panel][open]').forEach(function (panel) {
      panel.close();
    });
  }

  document.addEventListener('htmx:afterSettle', function (event) {
    openPanels(event.target.parentNode || document);
  });

  document.addEventListener('click', function (event) {
    var closer = event.target.closest('[data-mnl-close]');
    if (closer) {
      var panel = closer.closest('dialog[data-mnl-panel]');
      if (panel) {
        panel.close();
      }
      return;
    }

    // A click on the dialog element itself is a click on the backdrop
    if (event.target.matches && event.target.matches('dialog[data-mnl-panel]')) {
      event.target.close();
    }
  });

  document.addEventListener('mnl:close-panel', closePanels);

  // ---- toasts --------------------------------------------------------------------------------

  function scheduleDismiss(toast) {
    window.setTimeout(function () {
      toast.remove();
    }, TOAST_DURATION_MS);
  }

  function showToast(message, tone) {
    var region = document.getElementById('mnl-toast-region');
    var template = document.getElementById('mnl-toast-template');
    if (!region || !template || !message) {
      return;
    }

    var toast = template.content.firstElementChild.cloneNode(true);
    toast.setAttribute('data-tone', tone || 'info');
    toast.querySelector('.mnl-toast__message').textContent = message;
    region.appendChild(toast);

    var toasts = region.querySelectorAll('[data-mnl-toast]');
    for (var i = 0; i < toasts.length - MAX_TOASTS; i++) {
      toasts[i].remove();
    }

    scheduleDismiss(toast);
  }

  document.addEventListener('mnl:toast', function (event) {
    var detail = event.detail || {};
    showToast(detail.message, detail.tone);
  });

  document.addEventListener('click', function (event) {
    var dismiss = event.target.closest('[data-mnl-toast-dismiss]');
    if (dismiss) {
      dismiss.closest('[data-mnl-toast]').remove();
    }
  });

  function dismissRenderedToasts(root) {
    root.querySelectorAll('#mnl-toast-region > [data-mnl-toast]').forEach(scheduleDismiss);
  }

  // ---- repeated form rows ----------------------------------------------------------------------
  //
  // <div data-mnl-rows>
  //   <div data-mnl-rows-list> ...rows, each with data-mnl-row... </div>
  //   <template data-mnl-rows-template> one row </template>
  //   <button data-mnl-rows-add>   adds a copy of the template
  //   <button data-mnl-rows-remove> (inside a row) removes that row
  //   <output data-mnl-rows-total data-target="100"> shows the sum of inputs marked data-mnl-rows-value
  // </div>

  function updateRowsTotal(container) {
    var output = container.querySelector('[data-mnl-rows-total]');
    if (!output) {
      return;
    }

    var total = 0;
    container.querySelectorAll('[data-mnl-rows-value]').forEach(function (input) {
      var value = parseFloat(input.value);
      total += isNaN(value) ? 0 : value;
    });

    var suffix = output.getAttribute('data-suffix') || '';
    output.textContent = total + suffix;

    var target = parseFloat(output.getAttribute('data-target'));
    if (!isNaN(target)) {
      output.setAttribute('data-on-target', Math.abs(total - target) < 0.001 ? 'true' : 'false');
    }
  }

  document.addEventListener('click', function (event) {
    var add = event.target.closest('[data-mnl-rows-add]');
    if (add) {
      var container = add.closest('[data-mnl-rows]');
      var template = container.querySelector('[data-mnl-rows-template]');
      var list = container.querySelector('[data-mnl-rows-list]');
      list.appendChild(template.content.cloneNode(true));
      updateRowsTotal(container);
      var first = list.lastElementChild.querySelector('input, select');
      if (first) {
        first.focus();
      }
      return;
    }

    var remove = event.target.closest('[data-mnl-rows-remove]');
    if (remove) {
      var owner = remove.closest('[data-mnl-rows]');
      remove.closest('[data-mnl-row]').remove();
      updateRowsTotal(owner);
    }
  });

  document.addEventListener('input', function (event) {
    if (event.target.matches && event.target.matches('[data-mnl-rows-value]')) {
      updateRowsTotal(event.target.closest('[data-mnl-rows]'));
    }
  });

  // ---- errors and sign-in ----------------------------------------------------------------------

  document.addEventListener('htmx:responseError', function (event) {
    var status = event.detail.xhr.status;
    if (status === 401) {
      var returnUrl = window.location.pathname + window.location.search;
      window.location.assign('/sign-in?returnUrl=' + encodeURIComponent(returnUrl));
      return;
    }

    showToast(
      status === 403
        ? "You don't have permission to do that."
        : 'Something went wrong. Please try again.',
      'error',
    );
  });

  document.addEventListener('htmx:sendError', function () {
    showToast("Menlo can't be reached. Check your connection and try again.", 'error');
  });

  // ---- start -----------------------------------------------------------------------------------

  function start() {
    openPanels(document);
    dismissRenderedToasts(document);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', start);
  } else {
    start();
  }

  // A boosted navigation replaces the body, including any toasts the server rendered into it
  document.addEventListener('htmx:afterSwap', function (event) {
    if (event.target === document.body) {
      dismissRenderedToasts(document);
    }
  });
})();
