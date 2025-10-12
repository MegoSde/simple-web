// editor-controls.js
// editor.js  (loaded before any component files)
(function () {
    const registry = new Map(); // key = `${type}@${version}`, value = component descriptor
    const byType = new Map();   // type -> array of versions (sorted desc)

    function key(type, version) { return `${type}@${version}`; }

    function register(descriptor) {
        // descriptor: { type, version, label, add(ctx) }
        const k = key(descriptor.type, descriptor.version);
        
        registry.set(k, descriptor);

        const arr = byType.get(descriptor.type) || [];
        if (!arr.includes(descriptor.version)) arr.push(descriptor.version);
        arr.sort((a, b) => b - a); // keep highest first
        byType.set(descriptor.type, arr);
    }

    function getLatest(type) {
        const vers = byType.get(type);
        if (!vers || vers.length === 0) return null;
        return registry.get(key(type, vers[0]));
    }

    // Creates a fresh component node with common parts set
    function createNode(type, version, initProps = {}) {
        return {
            type,
            v: version,
            props: { ...initProps },
            children: [] // container components can use this
        };
    }

    // Super-simple placeholder renderer to #template (you can replace with your real renderer)
    function renderNode(node) {
        const container = document.createElement("div");
        container.className = "component";
        container.dataset.type = node.type;
        container.dataset.version = String(node.v);

        const header = document.createElement("div");
        header.className = "component__head";
        header.textContent = `${node.type}@${node.v}`;

        const body = document.createElement("div");
        body.className = "component__body";
        body.textContent = JSON.stringify(node.props);

        container.appendChild(header);
        container.appendChild(body);
        return container;
    }

    function addByName(type, targetEl) {
        const desc = getLatest(type);
        if (!desc) {
            console.warn("Unknown component:", type);
            return null;
        }
        const ctx = {
            templateEl: targetEl,
            createNode,
            renderNode,
        };
        return desc.add(ctx);
    }

    // expose
    window.Editor = {
        register,     // components call this at load time
        addByName,    // UI calls this on "Add"
        createNode,   // optional reuse in custom flows
        renderNode,   // optional reuse
    };
})();

(function () {
    let dlg, gridEl, searchEl, inited = false, cachedItems = [], currentResolve = null;
    let pageSize = 40;

    function ensureDialog() {
        if (!dlg) {
            dlg = document.getElementById('imagepicker');
            if (!dlg) throw new Error('#imagepicker <dialog> not found');
            gridEl = dlg.querySelector('.dlg__grid');
            searchEl = dlg.querySelector('.dlg__search');

            // Luk med Esc / Cancel → resolve(null) men behold markup
            dlg.addEventListener('close', () => {
                if (currentResolve) { currentResolve(null); currentResolve = null; }
            });

            // Debounced søgning
            let t = 0;
            searchEl?.addEventListener('input', () => {
                clearTimeout(t);
                t = setTimeout(() => reload(searchEl.value.trim()), 250);
            });
        }
    }

    async function loadOnce() {
        if (inited) return;
        await reload("");
        inited = true;
    }

    async function reload(search) {
        gridEl.setAttribute('aria-busy', 'true');
        try {
            const res = await fetch(`/media?page=1&pageSize=${pageSize}`, {
                headers: { "Accept": "application/json" },
                credentials: "same-origin"
            });
            if (!res.ok) throw new Error('media list failed');
            const data = await res.json();
            cachedItems = data.items || [];
            renderItems(cachedItems);
        } catch (e) {
            gridEl.innerHTML = `<div class="dlg__error">Failed to load images</div>`;
            console.warn(e);
        } finally {
            gridEl.removeAttribute('aria-busy');
        }
    }

    function renderItems(items) {
        gridEl.innerHTML = "";
        for (const it of items) {
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'dlg__thumb';
            btn.innerHTML = `
        <img loading="lazy" src="/cmsimg/thumbnail/${it.url}.webp" alt="${it.alt ?? ""}">
      `;
            btn.addEventListener('click', () => {
                // Vælg → resolve med item og luk dialogen (markup bevares)
                if (currentResolve) { currentResolve(it); currentResolve = null; }
                dlg.close();
            });
            gridEl.appendChild(btn);
        }
    }

    async function openImagePickerDialog(opts = {}) {
        ensureDialog();
        if (!inited) await loadOnce(); // første gang henter vi data og renderer grid
        // (senere: overvej at respektere opts.search ved første åbning; ellers behold cache)
        return new Promise((resolve) => {
            currentResolve = resolve;
            // Safari fallback: hvis dialog.showModal ikke findes, simuler modal med class
            if (typeof dlg.showModal === 'function') {
                dlg.showModal();
            } else {
                dlg.setAttribute('open', '');
                dlg.classList.add('is-open'); // style som modal
            }
        });
    }

    // eksponer i Editor-namespace
    window.Editor = window.Editor || {};
    window.Editor.openImagePickerDialog = openImagePickerDialog;

    // (valgfrit) API til at “preload’e” uden at åbne dialog (fx kald ved side-load)
    window.Editor.preloadImagePicker = async function () {
        ensureDialog();
        await loadOnce();
    };
})();

(function () {
    const selectEl = document.getElementById('editorControllerSelect');
    const addBtn = document.getElementById('addEditorControllerBtn');
    const templateRoot = document.getElementById('template');

    function enableEditorControls() {
        if (selectEl) {
            selectEl.disabled = false;
            selectEl.setAttribute('aria-disabled', 'false');
        }
        if (addBtn) {
            addBtn.disabled = false;
            addBtn.setAttribute('aria-disabled', 'false');
        }
    }

    addBtn?.addEventListener('click', () => {
        const type = selectEl?.value || '';
        if (!type || !templateRoot) return;

        // Ask the registry to add the latest version of this component
        const node = window.Editor?.addByName?.(type, templateRoot);
        if (!node) return;

        // TODO: optional—push node into your in-memory template JSON, e.g.:
        // window.EditorState?.addNode(node);
    });

    // Expose a global hook the editor bundle can call when ready
    window.enableEditorControls = enableEditorControls;

})();

enableEditorControls();