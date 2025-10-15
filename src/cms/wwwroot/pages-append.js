// /wwwroot/js/pages-append.js
(function () {
    'use strict';

    var titleEl = document.getElementById('Title');
    var slugEl = document.getElementById('Slug');
    var parentUrlEl = document.getElementById('ParentUrl');
    var previewEl = document.getElementById('ComposedUrl');

    if (!titleEl || !slugEl || !parentUrlEl || !previewEl) return;

    var parentUrl = parentUrlEl.getAttribute('data-parent-url') || '/';
    var slugTouched = false; // markeres hvis brugeren selv ændrer slug

    function toSlug(s) {
        if (!s) return '';
        // lowercase først, så vi kan nøjes med lowercase mappings
        s = String(s).toLowerCase();

        // eksplicit translitterering (dk)
        s = s
            .replace(/æ/g, 'ae')
            .replace(/ø/g, 'oe')
            .replace(/å/g, 'aa');

        // fjern diakritiske tegn (andre sprog)
        s = s.normalize('NFD').replace(/[\u0300-\u036f]/g, '');

        // alt ikke [a-z0-9] -> '-'
        s = s.replace(/[^a-z0-9]+/g, '-');

        // trim '-' i begge ender og begræns længde
        s = s.replace(/^-+|-+$/g, '').slice(0, 64);

        // undgå dobbelte '-' (skulle allerede være håndteret, men for en sikkerheds skyld)
        s = s.replace(/-+/g, '-');

        return s;
    }

    function composeUrl(parentPath, slug) {
        var p = parentPath || '/';
        var s = slug || '';
        if (p !== '/' && p.endsWith('/')) p = p.replace(/\/+$/, ''); // trim trailing /
        if (!s) return p;
        return (p === '/') ? ('/' + s) : (p + '/' + s);
    }

    function updatePreview() {
        previewEl.textContent = composeUrl(parentUrl, slugEl.value.trim());
    }

    // Brugeren har “touched” slug hvis de skriver i det
    slugEl.addEventListener('input', function () {
        slugTouched = true;
        var v = slugEl.value;
        var sanitized = toSlug(v);
        if (v !== sanitized) slugEl.value = sanitized;
        updatePreview();
    });

    // Autofyld slug fra title (kun hvis user ikke har touched slug)
    titleEl.addEventListener('input', function () {
        if (!slugTouched) {
            slugEl.value = toSlug(titleEl.value);
        }
        updatePreview();
    });

    // Init
    if (!slugEl.value && titleEl.value) {
        slugEl.value = toSlug(titleEl.value);
    }
    updatePreview();
})();
