(function () {
    // --- små utils ---
    const qs  = (s, r=document) => r.querySelector(s);
    const qsa = (s, r=document) => Array.from(r.querySelectorAll(s));
    const guidRe = /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/i;
    function getPageIdFromUrl() {
        const m = location.pathname.match(guidRe);
        if (m) return m[0];
        const u = new URL(location.href);
        const id = u.searchParams.get("id");
        if (id && guidRe.test(id)) return id;
        throw new Error("Missing page id in URL");
    }
    function getCsrf() {
        return qs('input[name="__RequestVerificationToken"]')?.value || "";
    }

    // --- Editor kernel ---
    const Editor = {
        _registry: Object.create(null),
        _renderedRoot: null,
        _pageId: null,
        _version: null,
        _content: { components: [], settings: {} },

        register(def) {
        if (!def?.type) return;
        this._registry[def.type] = def;
    },

    async init() {
        this._renderedRoot = qs("#page-content");
        if (!this._renderedRoot) return;
        this._pageId = getPageIdFromUrl();

        await this.load();   // <- enkel load
        this.render();

        // Save-knap (du kan også binde til din eksisterende knap)
        const saveBtn = document.createElement("button");
        saveBtn.className = "btn btn--primary";
        saveBtn.textContent = "Save";
        saveBtn.addEventListener("click", () => this.save());
        this._renderedRoot.parentElement.insertBefore(saveBtn, this._renderedRoot);
    },

    async load() {
        const res = await fetch(`/pages/${this._pageId}.json`, { headers: { Accept: "application/json" }});
        if (!res.ok) throw new Error("Failed to load page json");
        const j = await res.json();
        this._version = j.version ?? j.Version ?? null;

        // indhold kan være content eller data afhængigt af backend
        const content = j.content || j.data || {};
        this._content.components = Array.isArray(content.components) ? content.components : (content.Components || []);
        this._content.settings   = content.settings || content.Settings || {};
    },

    render() {
        this._renderedRoot.innerHTML = "";
        const ctx = {
            root: this._renderedRoot,
            openImagePickerDialog: () => this.openImagePickerDialog()
        };

        for (const comp of this._content.components) {
            const def = this._registry[comp.type] || null;
            if (!def || typeof def.load !== "function") {
                console.warn("Unknown component type", comp.type);
                continue;
            }

            // comp.data kan ligge fladt i nogle skemaer – normaliser
            const data = comp.data ?? comp.Data ?? comp;
        
            def.load(ctx, data);
        }
    },

    async save() {
        // Saml JSON ved at kalde getJson() pr. component
        const payload = {
            pageId: this._pageId,
            version: this._version,
            content: {
                components: qsa(".component", this._renderedRoot)
                .map(ctrl => {
                    const type = ctrl.dataset.type;
                    const def = this._registry[type];
                    if (!def || typeof def.getJson !== "function") return null;
                    return def.getJson(ctrl);
                })
                .filter(Boolean)
            }
        };

        const res = await fetch(`/pages/${this._pageId}.json`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Accept": "application/json",
                "RequestVerificationToken": getCsrf()
            },
            body: JSON.stringify(payload)
        });

        if (res.status === 422) {
            const err = await res.json().catch(()=>({}));
            console.warn("Validation", err);
            alert("Validation errors—se console for detaljer.");
            return;
        }
        if (!res.ok) {
            alert("Save failed");
            return;
        }
        const ok = await res.json().catch(()=> ({}));
        this._version = ok.version ?? this._version;
        alert("Saved");
    },

    // --- Image picker: bruger dit dialog #imagepicker ---
    async openImagePickerDialog() {
        const dlg = qs("#imagepicker");
        if (!dlg) {
            console.warn("Image picker dialog not found");
            return null;
        }
        // første åbning kan loade billeder (valgfrit)
        await this._ensureImageGridLoaded(dlg, "");
    
        return new Promise((resolve) => {
            function onClose() {
                dlg.removeEventListener("close", onClose);
                const v = dlg.returnValue;
                if (v && v !== "cancel") {
                    try { resolve(JSON.parse(v)); } catch { resolve(null); }
                } else resolve(null);
            }
            dlg.addEventListener("close", onClose);
            dlg.showModal();
        });
    },

    async _ensureImageGridLoaded(dlg, q) {
        const grid = qs(".dlg__grid", dlg);
        const search = qs(".dlg__search", dlg);
        if (!grid) return;

        const items = await this._fetchImages(q);
        grid.innerHTML = "";
        for (const it of items) {
            // værdier vi forsøger at sende tilbage: id OG/ELLER url + alt
            const val = JSON.stringify({url: it.url || "", alt: it.alt || "" });
            const btn = document.createElement("button");
            btn.type = "submit";
            btn.className = "imgpick";
            btn.value = val;
            btn.innerHTML = `
                  <img src="/cmsimg/thumbnail/${it.url}.webp" alt="${it.alt || ""}">
                `;
            grid.appendChild(btn);
        }

        if (search && !search._wired) {
            search._wired = true;
            search.addEventListener("input", (e) => {
                this._ensureImageGridLoaded(dlg, e.target.value.trim());
            });
        }
    },

    async _fetchImages(query) {
        try {
            const u = new URL("/media", location.origin);
            if (query) u.searchParams.set("q", query);
            const r = await fetch(u, { headers: { Accept: "application/json" }});
            if (r.ok) {
            const j = await r.json();
            const arr = j.items || j || [];
            // normaliser
            return arr.map(x => ({
                url: x.url || x.hash || "",
                alt: x.altText || x.alt || "",
            }));
        }
        } catch {}
            // fallback demo
            return [
        { id: "demo1", url: "/images/demo1.jpg", thumb: "/images/demo1.jpg", alt: "Demo 1", name: "Demo 1" },
        { id: "demo2", url: "/images/demo2.jpg", thumb: "/images/demo2.jpg", alt: "Demo 2", name: "Demo 2" }
            ];
        }
    };

    window.Editor = Editor;

    document.addEventListener("DOMContentLoaded", () => {
        if (!qs("#page-content")) return;
        Editor.init().catch(err => {
            console.error(err);
            alert("Init failed");
        });
    });
})();
