// Small DOM helpers for the design system.
//
// The password toggle is deliberately written as a delegated listener rather than a
// Blazor @onclick: Login and Register must stay static SSR (ADR-0003) so their form
// post can set the auth cookie, which rules out any interactivity on those pages.
window.rm = window.rm || {};

window.rm.dialog = {
    show: (element) => element?.showModal(),
    close: (element) => element?.close()
};

document.addEventListener('click', (event) => {
    const toggle = event.target.closest('[data-password-toggle]');
    if (!toggle) {
        return;
    }

    const input = document.getElementById(toggle.dataset.passwordToggle);
    if (!input) {
        return;
    }

    const revealed = input.type === 'text';
    input.type = revealed ? 'password' : 'text';
    toggle.setAttribute('aria-label', toggle.dataset[revealed ? 'labelShow' : 'labelHide'] ?? '');
    toggle.querySelectorAll('[data-icon]').forEach((icon) => {
        icon.hidden = (icon.dataset.icon === 'show') === !revealed;
    });
});
