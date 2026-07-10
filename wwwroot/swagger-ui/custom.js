(function () {
    var originalFetch = window.fetch;
    window.fetch = function () {
        if (!arguments[1]) {
            arguments[1] = {};
        }
        arguments[1].credentials = 'include';
        return originalFetch.apply(this, arguments);
    };
})();