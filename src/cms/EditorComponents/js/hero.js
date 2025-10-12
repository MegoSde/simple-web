// hero.js
(function () {
    const Hero = {
        type: "hero",
        version: 1,
        label: "Hero",

        /**
         * @param {{ templateEl: HTMLElement, createNode:Function, renderNode:Function }} ctx
         */
        add(ctx) {
            const node = ctx.createNode("hero", 1, { title: "Hero", imgId: "" });

            const el = document.createElement("div");
            el.className = "component component--hero";
            el.dataset.type = node.type;
            el.dataset.version = String(node.v);
            el.innerHTML = `
        <div class="component__head">${node.type}@${node.v}</div>
        <div class="component__body">
          <div class="form-row">
            <label>Title</label>
            <input type="text" class="input-title" value="${node.props.title}">
          </div>
          <div class="form-row">
            <label>Image</label>
            <div class="imgpicker">
              <button type="button" class="btn choose-img">Choose…</button>
              <span class="img-id">${node.props.imgId || ""}</span>
            </div>
            <div class="img-preview"></div>
          </div>
          <pre class="json-view"></pre>
        </div>
      `;

            const inputTitle = el.querySelector(".input-title");
            const btnChoose  = el.querySelector(".choose-img");
            const spanId     = el.querySelector(".img-id");
            const preview    = el.querySelector(".img-preview");
            const jsonView   = el.querySelector(".json-view");

            function refresh() {
                jsonView.textContent = JSON.stringify(node.props, null, 2);
                preview.innerHTML = node.props.imgId
                    ? `<img src="/cmsimg/thumbnail/${encodeURIComponent(node.props.imgId)}.webp" alt="">`
                    : "";
                spanId.textContent = node.props.imgId || "";
            }

            inputTitle.addEventListener("input", () => {
                node.props.title = inputTitle.value;
                refresh();
            });

            btnChoose.addEventListener("click", async () => {
                try {
                    const picked = await window.Editor.openImagePickerDialog();
                    if (picked && picked.url) {
                        node.props.imgId = picked.url; // gem reference
                        refresh();
                    }
                } catch (e) {
                    console.warn("Image picker error:", e);
                }
            });

            refresh();
            ctx.templateEl.appendChild(el);
            return node;
        }
    };

    window.Editor.register(Hero);
})();