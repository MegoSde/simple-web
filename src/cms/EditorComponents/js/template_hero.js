// hero.js
(function () {
    const Hero = {
        type: "hero",
        version: 1,
        label: "Hero",

        add(ctx) {
            const node = ctx.createNode("hero", 1, {});
            const el = window.Editor.renderNode(ctx, node);

            // du kan nu selv populere body:
            //const body = el.querySelector(".component__body");
            //if (body) body.textContent = ""; // start tom

            return node;
        },
        load(ctx, data) {
            const node = ctx.createNode("hero", data.version, data.settings);
            window.Editor.renderNode(ctx, node);
            return node;
        },
        getJson(ctrl) {
        return {
            v: Number(ctrl.dataset.version) || 1,
            type: ctrl.dataset.type || "hero",
            id: ctrl.dataset.id,
            settings: {}
        };
    }
    };
    window.Editor.register(Hero);
})();