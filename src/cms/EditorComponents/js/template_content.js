(function () {
    const Content = {
        type: "content",
        version: 1,
        label: "Content",
        
        add(ctx) {
            const node = ctx.createNode("content", 1, {});
            window.Editor.renderNode(ctx, node);
            return node;
        },
        load(ctx, data) {
            const node = ctx.createNode("content", data.version, data.settings);
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

    window.Editor.register(Content);
})();