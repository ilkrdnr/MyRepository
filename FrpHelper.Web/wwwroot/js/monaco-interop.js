window.frpMonaco = (function () {
    const state = {
        loaderPromise: null,
        editors: {}
    };

    function ensureLoader() {
        if (state.loaderPromise) {
            return state.loaderPromise;
        }

        state.loaderPromise = new Promise((resolve, reject) => {
            if (window.monaco && window.monaco.editor) {
                resolve();
                return;
            }

            const script = document.createElement("script");
            script.src = "https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.52.2/min/vs/loader.min.js";
            script.onload = function () {
                window.require.config({ paths: { vs: "https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.52.2/min/vs" } });
                window.require(["vs/editor/editor.main"], function () {
                    resolve();
                }, reject);
            };
            script.onerror = reject;
            document.head.appendChild(script);
        });

        return state.loaderPromise;
    }

    async function create(id, value, language, readOnly, dotNetRef) {
        await ensureLoader();

        dispose(id);

        const host = document.getElementById(id);
        if (!host) {
            return;
        }

        const model = window.monaco.editor.createModel(value || "", language || "plaintext");
        const editor = window.monaco.editor.create(host, {
            model: model,
            automaticLayout: true,
            minimap: { enabled: false },
            roundedSelection: false,
            scrollBeyondLastLine: false,
            fontFamily: "JetBrains Mono, Consolas, monospace",
            fontSize: 12,
            lineHeight: 18,
            readOnly: !!readOnly,
            theme: "vs"
        });

        const changeSubscription = model.onDidChangeContent(function () {
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync("NotifyValueChanged", model.getValue());
            }
        });

        state.editors[id] = {
            editor: editor,
            model: model,
            changeSubscription: changeSubscription,
            dotNetRef: dotNetRef
        };
    }

    function setValue(id, value) {
        const ctx = state.editors[id];
        if (!ctx || !ctx.model) {
            return;
        }

        const nextValue = value || "";
        if (ctx.model.getValue() === nextValue) {
            return;
        }

        ctx.model.setValue(nextValue);
    }

    function setLanguage(id, language) {
        const ctx = state.editors[id];
        if (!ctx || !ctx.model) {
            return;
        }

        window.monaco.editor.setModelLanguage(ctx.model, language || "plaintext");
    }

    function setReadOnly(id, readOnly) {
        const ctx = state.editors[id];
        if (!ctx || !ctx.editor) {
            return;
        }

        ctx.editor.updateOptions({ readOnly: !!readOnly });
    }

    function dispose(id) {
        const ctx = state.editors[id];
        if (!ctx) {
            return;
        }

        if (ctx.changeSubscription) {
            ctx.changeSubscription.dispose();
        }

        if (ctx.editor) {
            ctx.editor.dispose();
        }

        if (ctx.model) {
            ctx.model.dispose();
        }

        delete state.editors[id];
    }

    return {
        create: create,
        setValue: setValue,
        setLanguage: setLanguage,
        setReadOnly: setReadOnly,
        dispose: dispose
    };
})();
