// Small DOM helpers for the design system.
//
// The password toggle is deliberately written as a delegated listener rather than a
// Blazor @onclick: Login and Register must stay static SSR (ADR-0003) so their form
// post can set the auth cookie, which rules out any interactivity on those pages.
// The theme toggle follows the same pattern so it works on static SSR pages too.
window.rm = window.rm || {};

window.rm.dialog = {
    show: (element) => element?.showModal(),
    close: (element) => element?.close()
};

// --- Theme -----------------------------------------------------------------
// The inline script in App.razor sets data-theme/data-resolved-theme before paint.
// This helper keeps them in sync with user choices and OS changes, and notifies
// subscribers (e.g. the Chart.js dashboard) via a 'rm:theme' CustomEvent.
window.rm.theme = {
    get: function () { return localStorage.getItem('rm-theme') || 'system'; },

    _resolve: function (choice) {
        return choice === 'system'
            ? (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light')
            : choice;
    },

    _syncButtons: function (choice) {
        document.querySelectorAll('[data-theme-toggle]').forEach((btn) => {
            btn.setAttribute('aria-pressed', String(btn.dataset.themeToggle === choice));
        });
    },

    _apply: function (choice) {
        const resolved = this._resolve(choice);
        const el = document.documentElement;
        // Only write when different to avoid unnecessary MutationObserver loops.
        if (el.getAttribute('data-theme') !== choice) {
            el.setAttribute('data-theme', choice);
        }
        if (el.getAttribute('data-resolved-theme') !== resolved) {
            el.setAttribute('data-resolved-theme', resolved);
        }
        this._syncButtons(choice);
        window.dispatchEvent(new CustomEvent('rm:theme', { detail: { choice: choice, resolved: resolved } }));
    },

    set: function (mode) {
        localStorage.setItem('rm-theme', mode);
        this._apply(mode);
    }
};

document.addEventListener('click', (event) => {
    const toggle = event.target.closest('[data-password-toggle]');
    if (toggle) {
        const input = document.getElementById(toggle.dataset.passwordToggle);
        if (input) {
            const revealed = input.type === 'text';
            input.type = revealed ? 'password' : 'text';
            toggle.setAttribute('aria-label', toggle.dataset[revealed ? 'labelShow' : 'labelHide'] ?? '');
            toggle.querySelectorAll('[data-icon]').forEach((icon) => {
                icon.hidden = (icon.dataset.icon === 'show') === !revealed;
            });
        }
        return;
    }

    const themeBtn = event.target.closest('[data-theme-toggle]');
    if (themeBtn) {
        window.rm.theme.set(themeBtn.dataset.themeToggle);
    }
});

// Mark the active button in every toggle group after the DOM loads.
document.addEventListener('DOMContentLoaded', () => {
    window.rm.theme._apply(window.rm.theme.get());
});

// Blazor enhanced navigation replaces <main> content without re-firing
// DOMContentLoaded, so freshly inserted toggles keep their default aria-pressed.
// This observer re-syncs them whenever new [data-theme-toggle] buttons appear.
const _themeObserver = new MutationObserver(() => {
    const current = window.rm.theme.get();
    document.querySelectorAll('[data-theme-toggle]').forEach((btn) => {
        btn.setAttribute('aria-pressed', String(btn.dataset.themeToggle === current));
    });
});
_themeObserver.observe(document.body, { childList: true, subtree: true });

// Re-resolve "system" when the OS theme changes, without a reload.
window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
    if (window.rm.theme.get() === 'system') {
        window.rm.theme._apply('system');
    }
});

// Blazor enhanced navigation and Interactive Server navigation can patch the
// <html> element and remove the data-* theme attributes set by the inline
// anti-FOUC script. Re-apply the stored choice after every enhanced page update
// and watch <html> attributes so any external DOM reset is corrected.
const _htmlThemeObserver = new MutationObserver(() => {
    const current = window.rm.theme.get();
    window.rm.theme._apply(current);
});
_htmlThemeObserver.observe(document.documentElement, {
    attributes: true,
    attributeFilter: ['data-theme', 'data-resolved-theme']
});

// The Blazor script is loaded after rm.js, so register the enhanced load
// listener once the Blazor object becomes available.
(function attachEnhancedLoad() {
    if (typeof Blazor !== 'undefined' && typeof Blazor.addEventListener === 'function') {
        Blazor.addEventListener('enhancedload', () => {
            window.rm.theme._apply(window.rm.theme.get());
        });
    } else {
        setTimeout(attachEnhancedLoad, 50);
    }
})();
