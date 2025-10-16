
(function () {
    const Hero = {
    type: "hero",
    version: 1,
    label: "Hero",

    load(ctx, data = {}) {
        const state = {
            title: data.title || "",
            imgId: data.imgId || ""
        };

        const el = document.createElement("div");
        el.className = "component component--hero";
        el.dataset.type = this.type;
        el.dataset.version = String(this.version);

        el.innerHTML = `
            <div class="form-row">
              <label>Title</label>
              <input type="text" class="input-title" value="${state.title}">
            </div>
            <div class="form-row">
              <label>Image</label>
              <div class="imgpicker">
                <button type="button" class="btn choose-img">Choose…</button>
                <span class="img-id">${state.imgId}</span>
              </div>
              <div class="img-preview">${state.imgId ? `<img src="/cmsimg/thumbnail/${state.imgId}.webp" alt="">` : ""}</div>
            </div>
          `;

        const inputTitle = el.querySelector(".input-title");
        const btnChoose  = el.querySelector(".choose-img");
        const spanId     = el.querySelector(".img-id");
        const preview    = el.querySelector(".img-preview");

        inputTitle.addEventListener("input", () => { state.title = inputTitle.value; });
    
        btnChoose.addEventListener("click", async () => {
            try {
                const picked = await window.Editor.openImagePickerDialog();
                if (picked && (picked.id || picked.url)) {
                // foretræk ID, fald tilbage til url hvis nødvendigt
                state.imgId = picked.id || picked.url;
                spanId.textContent = state.imgId;
                preview.innerHTML = state.imgId
                ? `<img src="/cmsimg/thumbnail/${state.imgId}.webp" alt="">`
                : "";
            }
            } catch (e) {
                console.warn("Image picker error:", e);
            }
        });

        ctx.root.appendChild(el);
        return el;
    },
    getJson(ctrl) {
        return {
            v: Number(ctrl.dataset.version) || 1,
            type: ctrl.dataset.type || "hero",
            data: {
                title: ctrl.querySelector(".input-title")?.value || "",
                imgId: ctrl.querySelector(".img-id")?.textContent?.trim() || ""
            }
        };
    }
};

    window.Editor.register(Hero);
})();

