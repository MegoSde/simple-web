(function() {
    const Content = {
        type: "content",
        version: 1,
        label: "Content",

        load(ctx, data = {}) {
            const state = {
                mode: "wysiwyg", // "wysiwyg" | "source"
                body: data.body || ""
            };

            const root = document.createElement("div");
            root.className = "component component--content";
            root.dataset.type = this.type;
            root.dataset.version = String(this.version);

            // --- toolbar (let og uden libs) ---
            const toolbar = document.createElement("div");
            toolbar.className = "content-toolbar";
            toolbar.innerHTML = `
        <button type="button" data-cmd="bold"      title="Bold"><b>B</b></button>
        <button type="button" data-cmd="italic"    title="Italic"><i>I</i></button>
        <button type="button" data-cmd="createLink" title="Link">Link</button>
        <button type="button" data-cmd="insertUnorderedList" title="• List">• List</button>
        <button type="button" data-cmd="insertOrderedList" title="1. List">1. List</button>
        <button type="button" data-block="p"  title="Paragraf">P</button>
        <button type="button" data-block="h2" title="H2">H2</button>
        <button type="button" data-block="h3" title="H3">H3</button>
        <span class="sep"></span>
        <button type="button" data-cmd="undo"  title="Undo">↶</button>
        <button type="button" data-cmd="redo"  title="Redo">↷</button>
        <span class="sep"></span>
        <button type="button" class="toggle-src" title="Vis kilde">{"</>"}</button>
      `;

            // --- editor (wysiwyg) ---
            const editorWrap = document.createElement("div");
            editorWrap.className = "content-editor";
            const editor = document.createElement("div");
            editor.className = "contenteditable";
            editor.contentEditable = "true";
            editor.innerHTML = state.body;

            // --- source (textarea) ---
            const source = document.createElement("textarea");
            source.className = "content-source";
            source.value = state.body;
            source.style.display = "none";

            // --- events ---
            toolbar.addEventListener("click", async(e) => {
                const btn = e.target.closest("button");
                if (!btn) return;

                if (btn.classList.contains("toggle-src")) {
                    if (state.mode === "wysiwyg") {
                        // skift til kilde
                        source.value = editor.innerHTML;
                        editor.style.display = "none";
                        source.style.display = "block";
                        state.mode = "source";
                        btn.title = "Vis WYSIWYG";
                    } else {
                        // skift til wysiwyg
                        editor.innerHTML = source.value;
                        source.style.display = "none";
                        editor.style.display = "block";
                        state.mode = "wysiwyg";
                        btn.title = "Vis kilde";
                    }
                    return;
                }

                const cmd = btn.dataset.cmd;
                const block = btn.dataset.block;

                if (state.mode !== "wysiwyg") {
                    // i kilde-tilstand manipulerer vi ikke DOM—brug WYSIWYG til kommandoer
                    return;
                }

                if (cmd) {
                    if (cmd === "createLink") {
                        const url = prompt("Indtast URL:");
                        if (!url) return;
                        document.execCommand("createLink", false, url);
                    } else {
                        document.execCommand(cmd, false, null);
                    }
                    editor.dispatchEvent(new Event("input", {
                        bubbles: true
                    }));
                } else if (block) {
                    document.execCommand("formatBlock", false, block);
                    editor.dispatchEvent(new Event("input", {
                        bubbles: true
                    }));
                }
            });

            // hold state opdateret
            editor.addEventListener("input", () => {
                state.body = editor.innerHTML;
            });
            source.addEventListener("input", () => {
                state.body = source.value;
            });

            // (valgfrit) let paste-cleanup der fjerner <script> og on* attributter
            editor.addEventListener("paste", (e) => {
                e.preventDefault();
                const html = (e.clipboardData || window.clipboardData).getData("text/html") ||
                    (e.clipboardData || window.clipboardData).getData("text/plain");
                const clean = sanitizeHtml(html);
                pasteHtmlAtCursor(clean);
                editor.dispatchEvent(new Event("input", {
                    bubbles: true
                }));
            });

            editorWrap.appendChild(editor);
            editorWrap.appendChild(source);
            root.appendChild(toolbar);
            root.appendChild(editorWrap);
            ctx.root.appendChild(root);

            return root;
        },

        getJson(ctrl) {
            // hvis vi står i kilde-tilstand, er textarea synlig og indeholder sandheden
            const isSource = ctrl.querySelector(".content-source") ?.style.display !== "none";
            const val = isSource ?
                ctrl.querySelector(".content-source") ?.value || "":
            ctrl.querySelector(".contenteditable") ?.innerHTML || "";

            return {
                v: Number(ctrl.dataset.version) || 1,
                type: ctrl.dataset.type || "content",
                data: {
                    body: val
                }
            };
        }
    };

    // --- små helpers ---
    function sanitizeHtml(input) {
        if (!input) return "";
        const parser = new DOMParser();
        const doc = parser.parseFromString(input, "text/html");
        // Fjern scripts og on* attrs
        doc.querySelectorAll("script, style").forEach(n => n.remove());
        doc.querySelectorAll("*").forEach(n => {
            [...n.attributes].forEach(a => {
                if (a.name.startsWith("on")) n.removeAttribute(a.name);
            });
        });
        return doc.body.innerHTML;
    }

    function pasteHtmlAtCursor(html) {
        const sel = window.getSelection();
        if (!sel || !sel.rangeCount) return;
        const range = sel.getRangeAt(0);
        range.deleteContents();

        const temp = document.createElement("div");
        temp.innerHTML = html;
        const frag = document.createDocumentFragment();
        let node, lastNode;
        while ((node = temp.firstChild)) {
            lastNode = frag.appendChild(node);
        }
        range.insertNode(frag);

        // flyt caret efter indsatte indhold
        if (lastNode) {
            range.setStartAfter(lastNode);
            range.collapse(true);
            sel.removeAllRanges();
            sel.addRange(range);
        }
    }

    // registrér komponent
    window.Editor ?.register(Content);
})();