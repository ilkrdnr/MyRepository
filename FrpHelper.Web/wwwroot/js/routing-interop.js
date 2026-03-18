window.frpRouting = {
    takeHashRoute: function () {
        const hash = window.location.hash || "";
        if (!hash.startsWith("#/")) {
            return "";
        }

        const route = hash.substring(1);
        if (!route.startsWith("/")) {
            return "";
        }

        const cleanUrl = window.location.pathname + window.location.search;
        window.history.replaceState(null, "", cleanUrl);
        return route;
    }
};
