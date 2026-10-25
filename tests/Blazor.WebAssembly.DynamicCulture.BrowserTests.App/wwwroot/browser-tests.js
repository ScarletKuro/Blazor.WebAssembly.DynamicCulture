const query = new URLSearchParams(location.search);
globalThis.browserTests = {
    failures: [],
    reads: [],
    resources: null,
    runtime: null,
    retried: false,
    getOptions: () => ({
        mode: query.get("mode") || "loader",
        cultures: (query.get("cultures") || "en-US,cs-CZ").split(","),
        selectedCulture: query.get("selected") || "cs-CZ",
        native: query.get("native") === "true"
    }),
    beforeRun(resources, runtime, frameworks) {
        this.resources = resources;
        this.runtime = runtime;
        this.frameworks = frameworks;
    },
    retryObserved() { this.retried = true; },
    fail(error) {
        this.failures.push(error);
        document.querySelector("#app").textContent = "Failed: " + error;
    }
};

const arrayBuffer = Response.prototype.arrayBuffer;
Response.prototype.arrayBuffer = function () {
    if (/DynamicCultureBrowserTests\.resources/.test(this.url)) {
        browserTests.reads.push({ url: this.url, bodyUsed: this.bodyUsed });
    }
    return arrayBuffer.call(this);
};
