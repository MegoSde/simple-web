// template.js (samlet)
(function () {
    // ---------- Registry + helpers ----------
    const registry = new Map(); // key = `${type}@${version}`
    const byType = new Map();
    let idCounter = 1; // unik id-tæller til nodes

    function key(type, version) { return `${type}@${version}`; }

    function register(descriptor) {
        const k = key(descriptor.type, descriptor.version);
        registry.set(k, descriptor);
        const arr = byType.get(descriptor.type) || [];
        if (!arr.includes(descriptor.version)) arr.push(descriptor.version);
        arr.sort((a, b) => b - a);
        byType.set(descriptor.type, arr);
    }

    function getLatest(type) {
        const vers = byType.get(type);
        if (!vers || vers.length === 0) return null;
        return registry.get(key(type, vers[0]));
    }

    function get(type, version) {
        return registry.get(key(type, version)) || null;
    }

    // Create a fresh component node (+ unique id)
    function createNode(type, version, initSettings = {}) {
        return {
            id: idCounter++,
            type,
            v: version,
            settings: { ...initSettings },
            children: []
        };
    }

    // ---------- TemplateControls (move up/down/delete) ----------
    function moveUp(el) {
        const parent = el?.parentElement;
        if (!parent) return;
        const prev = el.previousElementSibling;
        if (prev) parent.insertBefore(el, prev);
    }
    function moveDown(el) {
        const parent = el?.parentElement;
        if (!parent) return;
        const next = el.nextElementSibling;
        if (next) parent.insertBefore(next, el);
    }
    function remove(el) {
        el?.remove();
    }
    // Eksponer globalt til evt. andre scripts
    window.TemplateControls = { moveUp, moveDown, remove };

    // ---------- Render ----------
    function renderNode(ctx, node, customControls = null) {
        const container = document.createElement("div");
        container.className = "component";
        container.dataset.id = String(node.id);       // <-- unik id
        container.dataset.type = node.type;
        container.dataset.version = String(node.v);
        //setSettings(container, node.settings);

        // HEAD
        const head = document.createElement("div");
        head.className = "component__head";

        const iconWrap = document.createElement("div");
        iconWrap.className = "component__icon";
        const icon = document.createElement("img");
        icon.src = `/componentlayout/${encodeURIComponent(node.type)}.svg`;
        icon.alt = `${node.type} layout`;
        icon.loading = "lazy";
        iconWrap.appendChild(icon);

        const title = document.createElement("div");
        title.className = "component__title";
        title.textContent = `${node.type}@${node.v}`;

        const controls = document.createElement("div");
        controls.className = "component__controls";

        function makeBtn(label, action, useId) {
            const b = document.createElement("button");
            b.type = "button";
            b.className = "btn btn--icon";
            b.title = label;
            b.setAttribute("aria-label", label);
            b.dataset.action = action;
            b.innerHTML = `<svg viewBox="0 0 24 24" aria-hidden="true"><use href="/icons.v1.svg#${useId}"></use></svg>`;
            return b;
        }
        const btnUp   = makeBtn("Move up",   "move-up",   "i-move-up");
        const btnDown = makeBtn("Move down", "move-down", "i-move-down");
        const btnDel  = makeBtn("Delete",    "delete",    "i-delete");
        controls.append(btnUp, btnDown, btnDel);

        // Evt. custom controls
        if (Array.isArray(customControls)) {
            for (const cc of customControls) {
                const b = document.createElement("button");
                b.type = "button";
                b.className = "btn btn--icon";
                b.title = cc.label || "Action";
                b.setAttribute("aria-label", cc.label || "Action");
                b.dataset.action = "custom";
                if (cc.iconUseId) {
                    b.innerHTML = `<svg viewBox="0 0 24 24" aria-hidden="true"><use href="/icons.v1.svg#${cc.iconUseId}"></use></svg>`;
                } else {
                    b.textContent = cc.label || "…";
                }
                b.addEventListener("click", () => cc.onClick?.(container, node));
                controls.appendChild(b);
            }
        }

        // BODY
        const body = document.createElement("div");
        body.className = "component__body";

        // Event delegation for standard controls (tåler klik på svg/path)
        controls.addEventListener("click", (e) => {
            const target = e.target;
            if (!(target instanceof Element)) return;
            const actionEl = target.closest("[data-action]");
            if (!actionEl || !controls.contains(actionEl)) return;

            if (actionEl instanceof HTMLButtonElement &&
                (actionEl.disabled || actionEl.getAttribute("aria-disabled") === "true")) return;

            const action = actionEl.getAttribute("data-action");
            if (action === "move-up")   moveUp(container);
            if (action === "move-down") moveDown(container);
            if (action === "delete")    remove(container);
        });

        head.append(iconWrap, title, controls);
        container.append(head, body);
        ctx.templateEl.appendChild(container);
        return container;
    }

    // ---------- API: add by name ----------
    function addByName(type, targetEl) {
        const desc = getLatest(type);
        if (!desc) { console.warn("Unknown component:", type); return null; }
        const ctx = { templateEl: targetEl, createNode, renderNode };
        return desc.add(ctx);
    }

    // ---------- Load template (JSON fra /template/{name}.json) ----------
    function loadTemplate(json, targetEl) {
        if (!json || !Array.isArray(json.components)) return;
        const ctx = { templateEl: targetEl, createNode, renderNode };
        for (const item of json.components) {
            const version = item.v || item.version || 1;
            const desc = get(item.type, version);
            if (desc?.load) {
                desc.load(ctx, { version, settings: item.settings || {} });
            } else if (desc?.add) {
                desc.add(ctx);
            } else {
                console.warn("No load/add for component:", item.type, version);
            }
        }
    }

    // ---------- Enable editor controls + bind Add ----------
    function enableEditorControls() {
        const selectEl = document.getElementById('editorControllerSelect');
        const addBtn   = document.getElementById('addEditorControllerBtn');
        const templateRoot = document.getElementById('template');

        if (selectEl) {
            selectEl.disabled = false;
            selectEl.setAttribute('aria-disabled', 'false');
        }
        if (addBtn) {
            addBtn.disabled = false;
            addBtn.setAttribute('aria-disabled', 'false');
        }

        addBtn?.addEventListener('click', () => {
            const type = selectEl?.value || '';
            if (!type || !templateRoot) return;
            addByName(type, templateRoot);
        });
    }
    
    // ---------- Save ----------------------------------------------
    function clearValidationUI() {
        document.querySelectorAll('.component').forEach(el => {
            el.classList.remove('component--error');
            const box = el.querySelector('.component__errors');
            if (box) box.remove();
        });
    }

    function showValidationErrors(errors) {
        if (!Array.isArray(errors) || errors.length === 0) return;

        const general = [];

        for (const e of errors) {
            const id = e.componentId || e.ComponentId || null;
            if (id) {
                const el = document.querySelector(`.component[data-id="${CSS.escape(String(id))}"]`);
                if (el) {
                    el.classList.add('component--error');
                    let box = el.querySelector('.component__errors');
                    if (!box) {
                        box = document.createElement('div');
                        box.className = 'component__errors';
                        // Sæt den i head, så det er tydeligt
                        const head = el.querySelector('.component__head') || el;
                        head.appendChild(box);
                    }
                    const line = document.createElement('div');
                    line.className = 'component__error';
                    const code = (e.code ?? e.Code) ? ` [${e.code ?? e.Code}]` : '';
                    line.textContent = `${e.message ?? e.Message}${code}`;
                    box.appendChild(line);
                    continue;
                }
            }
            // Faldt igennem → generel fejl, vises som alert bagefter
            const code = (e.code ?? e.Code) ? ` [${e.code ?? e.Code}]` : '';
            const path = (e.path ?? e.Path) || '$';
            general.push(`${path}${code}: ${e.message ?? e.Message}`);
        }

        if (general.length) {
            alert(general.join('\n'));
        }
    }
    
    function collectTopLevelComponents() {
        const root = document.getElementById("template");
        if (!root) return [];
        const comps = root.querySelectorAll(":scope > .component");
        const list = [];
        comps.forEach(ctrl => {
            const type = ctrl.dataset.type || "";
            const v = Number(ctrl.dataset.version) || 1;
            const desc = window.Editor.get?.(type, v);
            if (desc && typeof desc.getJson === "function") {
                list.push(desc.getJson(ctrl));
            } else {
                // fallback if a component hasn't implemented getJson yet
                list.push({ v, type, id: ctrl.dataset.id, settings: {} });
            }
        });
        return list;
    }
    // Wire up the form submit for "Save"
    function wireSave() {
        // adjust selector if your form action differs
        const form = document.querySelector('form[action^="/template/"]');
        if (!form) return;

        form.addEventListener("submit", async (e) => {
            e.preventDefault();

            clearValidationUI();

            const originalName = (form.querySelector('input[name="OriginalName"]')?.value || "").trim();
            const name = (form.querySelector('input[name="Name"]')?.value || "").trim();
            const components = collectTopLevelComponents();

            const url = `/template/${encodeURIComponent(originalName)}.json`;
            const payload = { originalName, name, components };

            try {
                const res = await fetch(url, {
                    method: "POST",
                    headers: { "Content-Type": "application/json", "Accept": "application/json" },
                    body: JSON.stringify(payload)
                });

                if (res.status === 204) {
                    alert("Saved ✔");
                    return;
                }

                const text = await res.text();
                try {
                    const data = text ? JSON.parse(text) : null;
                    if (data?.errors?.length) {
                        showValidationErrors(data.errors);
                    } else {
                        alert(text || `Save failed (${res.status})`);
                    }
                } catch {
                    alert(text || `Save failed (${res.status})`);
                }
            } catch (err) {
                alert(`Network error: ${err?.message || err}`);
            }
        });
    }

    // ---------- Init: fetch JSON via OriginalName + load ----------
    function init() {
        const nameInput = document.querySelector('input[name="OriginalName"]');
        const templateName = nameInput ? nameInput.value : "";
        const templateRoot = document.getElementById('template');
        if (!templateName || !templateRoot) return;
        wireSave();

        fetch(`/template/${encodeURIComponent(templateName)}.json`, { headers: { Accept: 'application/json' } })
            .then(r => r.ok ? r.json() : Promise.reject(r))
            .then(data => {
                loadTemplate(data, templateRoot);
                enableEditorControls();
            })
            .catch(err => console.error('Failed to load template json', err));
    }

    // ---------- Expose ----------
    window.Editor = {
        register,
        addByName,
        createNode,
        renderNode,
    };
    window.enableEditorControls = enableEditorControls; // if others want to call it

    // Kick off after DOM is ready
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
