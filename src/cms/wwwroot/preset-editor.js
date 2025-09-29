(function () {
    function ready(fn) {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', fn, { once: true });
        } else { fn(); }
    }
    function qs(s)  { return document.querySelector(s); }
    function qsa(s) { return Array.from(document.querySelectorAll(s)); }
    function clamp(v, min, max) { return Math.max(min, Math.min(max, v)); }

    ready(function init() {
        const root = qs('#cropEditorRoot');
        const img  = qs('#img');
        const rect = qs('#rect');
        if (!root || !img || !rect) return;

        const activeEl     = qs('#activePreset');
        const saveBtn      = qs('#saveBtn');
        const saveGroupBtn = qs('#saveGroupBtn');
        const savedMsg     = qs('#savedMsg');
        const fx = qs('#fx'), fy = qs('#fy'), fw = qs('#fw'), fh = qs('#fh');
        const form = qs('#saveForm');

        const presetBtns = qsa('.preset-pill');
        const ratioPills = qsa('#presetGroupBar .ratio-pill');
        if (presetBtns.length === 0) return;

        const MIN_SIZE = 20; // px

        // Data-model fra DOM (server har allerede sat data-ratio/-crop)
        const presets = presetBtns.map(btn => {
            const W = parseInt(btn.dataset.w || '0', 10) || 0;
            const H = parseInt(btn.dataset.h || '0', 10) || 0;
            const ratioKey = btn.dataset.ratio || 'free';
            const vals = (btn.dataset.crop || '').split(',').map(Number);
            const crop = (vals.length === 4 && vals.every(v => !isNaN(v))) ? vals : null;
            return { Name: btn.dataset.p, Width: W, Height: H, ratioKey, btn, crop };
        });

        // Gruppér presets efter ratioKey (fra server)
        const groups = new Map();
        presets.forEach(p => {
            if (!groups.has(p.ratioKey)) groups.set(p.ratioKey, []);
            groups.get(p.ratioKey).push(p);
        });

        // State
        let activePreset = null;
        let activeRatio  = null;
        let dragging = false, resizing = false;
        let startX=0, startY=0, startL=0, startT=0, startW=0, startH=0;

        function getImgBox() { return img.getBoundingClientRect(); }
        function aspectOf(p) {
            if (p.Width > 0 && p.Height > 0) return p.Width / p.Height;
            return null;
        }

        // 100% best-fit (maximalt inde i billedet)
        function bestFitRect(imgW, imgH, aspect) {
            if (!aspect) return { l: 0, t: 0, w: imgW, h: imgH };
            let w = Math.min(imgW, imgH * aspect);
            let h = w / aspect;
            const l = (imgW - w) / 2;
            const t = (imgH - h) / 2;
            return { l, t, w, h };
        }

        function enforceAspectWithin(l, t, w, h, aspect, imgW, imgH) {
            if (!aspect) {
                l = clamp(l, 0, imgW - MIN_SIZE);
                t = clamp(t, 0, imgH - MIN_SIZE);
                w = clamp(w, MIN_SIZE, imgW - l);
                h = clamp(h, MIN_SIZE, imgH - t);
                return { l, t, w, h };
            }
            // Hold center, tilpas til aspect, bliv inde
            const cx = l + w/2, cy = t + h/2;
            // Start med at bruge fuld bredde eller højde afhængigt af plads
            let newW = Math.min(w, imgW);
            let newH = newW / aspect;
            if (newH > imgH) { newH = imgH; newW = newH * aspect; }
            if (newW < MIN_SIZE) { newW = MIN_SIZE; newH = newW / aspect; }
            if (newH < MIN_SIZE) { newH = MIN_SIZE; newW = newH * aspect; }
            let newL = cx - newW/2;
            let newT = cy - newH/2;
            newL = clamp(newL, 0, imgW - newW);
            newT = clamp(newT, 0, imgH - newH);
            return { l: newL, t: newT, w: newW, h: newH };
        }

        function positionRect(l, t, w, h) {
            const r = getImgBox();
            l = clamp(l, 0, r.width - MIN_SIZE);
            t = clamp(t, 0, r.height - MIN_SIZE);
            w = clamp(w, MIN_SIZE, r.width - l);
            h = clamp(h, MIN_SIZE, r.height - t);
            rect.style.left = l + 'px';
            rect.style.top  = t + 'px';
            rect.style.width  = w + 'px';
            rect.style.height = h + 'px';
        }

        function updateActiveLabel() {
            if (!activePreset || !activeEl) return;
            activeEl.textContent = `${activePreset.Name} (${activePreset.Width}×${activePreset.Height})`;
        }

        function setActive(name) {
            activePreset = presets.find(p => p.Name === name);
            if (!activePreset) return;

            presets.forEach(p => p.btn.classList.remove('selected'));
            activePreset.btn.classList.add('selected');

            if (saveBtn) saveBtn.disabled = false;
            if (saveGroupBtn) saveGroupBtn.disabled = !activeRatio;
            if (savedMsg) savedMsg.style.display = 'none';
            rect.style.display = 'block';
            updateActiveLabel();

            const r = getImgBox();
            const ar = aspectOf(activePreset);

            let l, t, w, h;
            const ex = activePreset.crop;
            if (ex) {
                l = ex[0]*r.width; t=ex[1]*r.height; w=ex[2]*r.width; h=ex[3]*r.height;
                ({ l, t, w, h } = enforceAspectWithin(l, t, w, h, ar, r.width, r.height));
            } else {
                ({ l, t, w, h } = bestFitRect(r.width, r.height, ar));
            }
            positionRect(l, t, w, h);
        }

        function selectRatioGroup(ratioKey) {
            activeRatio = ratioKey;

            ratioPills.forEach(b => b.classList.toggle('active', b.dataset.ratio === ratioKey));
            presets.forEach(p => p.btn.classList.toggle('selected', p.ratioKey === ratioKey));

            // vælg første preset i gruppen hvis aktiv ikke passer
            const first = presets.find(p => p.ratioKey === ratioKey);
            if (first && (!activePreset || activePreset.ratioKey !== ratioKey)) setActive(first.Name);

            if (saveGroupBtn) saveGroupBtn.disabled = !first;
        }

        // Events
        presetBtns.forEach(b => b.addEventListener('click', () => setActive(b.dataset.p)));
        ratioPills.forEach(b => b.addEventListener('click', () => selectRatioGroup(b.dataset.ratio)));

        rect.addEventListener('mousedown', (e) => {
            const isHandle = e.target && e.target.classList && e.target.classList.contains('handle');
            resizing = !!isHandle;
            dragging = !isHandle;
            startX = e.clientX; startY = e.clientY;
            const s = rect.getBoundingClientRect();
            const r = img.getBoundingClientRect();
            startL = s.left - r.left; startT = s.top - r.top; startW = s.width; startH = s.height;
            e.preventDefault();
        });

        window.addEventListener('mousemove', (e) => {
            if (!dragging && !resizing) return;
            const r = getImgBox();
            const ar = activePreset ? aspectOf(activePreset) : null;
            const dx = e.clientX - startX;
            const dy = e.clientY - startY;

            if (dragging) {
                let l = startL + dx;
                let t = startT + dy;
                l = clamp(l, 0, r.width - startW);
                t = clamp(t, 0, r.height - startH);
                positionRect(l, t, startW, startH);
            }

            if (resizing) {
                let w, h;
                if (ar) {
                    // Lås aspect til ar, justér efter dominerende akse, bliv inde i billede
                    const base = Math.abs(dx) >= Math.abs(dy) ? dx : dy;
                    w = clamp(startW + base, MIN_SIZE, r.width - startL);
                    h = w / ar;
                    if (startT + h > r.height) { h = r.height - startT; w = h * ar; }
                } else {
                    w = clamp(startW + dx, MIN_SIZE, r.width - startL);
                    h = clamp(startH + dy, MIN_SIZE, r.height - startT);
                }
                positionRect(startL, startT, w, h);
            }
        });

        window.addEventListener('mouseup', () => { dragging = false; resizing = false; });

        // Helpers: build form body fra nuværende rect
        function buildBody() {
            const r = rect.getBoundingClientRect();
            const imgR = img.getBoundingClientRect();
            const l = r.left - imgR.left;
            const t = r.top - imgR.top;
            const nx = l / imgR.width, ny = t / imgR.height, nw = r.width / imgR.width, nh = r.height / imgR.height;

            if (fx) fx.value = nx; if (fy) fy.value = ny; if (fw) fw.value = nw; if (fh) fh.value = nh;
            return new URLSearchParams(new FormData(form));
        }

        async function postSingle(presetName, body) {
            const url = `/media/crop/${encodeURIComponent(presetName)}/${encodeURIComponent(root.dataset.hash)}`;
            const res = await fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/x-www-form-urlencoded' }, body });
            return res.ok;
        }

        async function postGroup(ratioKey, body) {
            const url = `/media/crop-group/${encodeURIComponent(ratioKey)}/${encodeURIComponent(root.dataset.hash)}`;
            const res = await fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/x-www-form-urlencoded' }, body });
            return res.ok;
        }

        function reflectSaved(presetName, body) {
            const nx = parseFloat(body.get('x')), ny = parseFloat(body.get('y'));
            const nw = parseFloat(body.get('w')), nh = parseFloat(body.get('h'));
            const p = presets.find(x => x.Name === presetName);
            if (p) p.crop = [nx, ny, nw, nh];
            const btn = presetBtns.find(b => b.dataset.p === presetName);
            if (btn) btn.dataset.crop = `${nx},${ny},${nw},${nh}`;
        }

        // Save current preset
        if (saveBtn) {
            saveBtn.addEventListener('click', async () => {
                if (!activePreset) return;
                const body = buildBody();
                const ok = await postSingle(activePreset.Name, body);
                if (ok) {
                    reflectSaved(activePreset.Name, body);
                    if (savedMsg) { savedMsg.style.display = 'inline'; setTimeout(() => savedMsg.style.display = 'none', 1200); }
                } else {
                    alert('Kunne ikke gemme crop.');
                }
            });
        }

        // Save group (server opdaterer alle presets i ratio)
        if (saveGroupBtn) {
            saveGroupBtn.addEventListener('click', async () => {
                if (!activePreset || !activeRatio) return;
                const body = buildBody();
                const ok = await postGroup(activeRatio, body);
                if (ok) {
                    // opdatér alle i gruppen lokalt
                    presets.filter(p => p.ratioKey === activeRatio).forEach(p => reflectSaved(p.Name, body));
                    if (savedMsg) { savedMsg.style.display = 'inline'; setTimeout(() => savedMsg.style.display = 'none', 1200); }
                } else {
                    alert('Kunne ikke gemme gruppe-crop.');
                }
            });
        }

        // Init når billedet kendes
        img.addEventListener('load', () => {
            // vælg første ratio, der findes
            const firstRatioBtn = ratioPills[0];
            if (firstRatioBtn) {
                selectRatioGroup(firstRatioBtn.dataset.ratio);
            } else {
                // fallback: vælg første preset
                setActive(presets[0].Name);
            }
        });

        function selectRatioGroup(ratioKey) {
            activeRatio = ratioKey;
            ratioPills.forEach(b => b.classList.toggle('active', b.dataset.ratio === ratioKey));
            presets.forEach(p => p.btn.classList.toggle('selected', p.ratioKey === ratioKey));
            const first = presets.find(p => p.ratioKey === ratioKey);
            if (first && (!activePreset || activePreset.ratioKey !== ratioKey)) setActive(first.Name);
            if (saveGroupBtn) saveGroupBtn.disabled = !first;
        }
    });
})();