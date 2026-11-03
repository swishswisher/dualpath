'use strict';

const publicHeader = document.querySelector('.public-header');
const publicToggle = document.querySelector('.mobile-menu');
const sidebar = document.getElementById('sidebar');
const workspaceToggle = document.querySelector('.app-mobile-menu');

function toggleWorkspace(open) {
    sidebar.classList.toggle('visible', open);
    workspaceToggle.setAttribute('aria-expanded', String(open));
    if (open) sidebar.querySelector('.sidebar-close').focus();
    else workspaceToggle.focus();
}

document.addEventListener('click', event => {
    const button = event.target.closest('[data-menu]');
    if (!button) return;
    if (button.dataset.menu === 'public') {
        const open = publicHeader.classList.toggle('open');
        publicToggle.setAttribute('aria-expanded', String(open));
        if (open) publicHeader.querySelector('nav a').focus();
    } else {
        toggleWorkspace(!sidebar.classList.contains('visible'));
    }
});

document.addEventListener('keydown', event => {
    if (event.key !== 'Escape') return;
    if (publicHeader?.classList.contains('open')) {
        publicHeader.classList.remove('open');
        publicToggle.setAttribute('aria-expanded', 'false');
        publicToggle.focus();
    }
    if (sidebar?.classList.contains('visible')) toggleWorkspace(false);
});

document.addEventListener('submit', event => {
    const button = event.submitter;
    if (button?.dataset.confirm && !confirm(button.dataset.confirm)) {
        event.preventDefault();
        return;
    }
    button?.setAttribute('aria-busy', 'true');
});

window.addEventListener('pageshow', () => {
    document.querySelectorAll('button[aria-busy="true"]').forEach(button => button.removeAttribute('aria-busy'));
});
