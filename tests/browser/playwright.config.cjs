const { defineConfig } = require("@playwright/test");
const path = require("node:path");
const framework = require("./frameworks.json").find(item => item.tfm === (process.env.BROWSER_TFM || "net10.0"));
if (!framework) throw new Error("Add the target framework to frameworks.json first.");
const project = path.resolve(__dirname, "../Blazor.WebAssembly.DynamicCulture.BrowserTests.App/Blazor.WebAssembly.DynamicCulture.BrowserTests.App.csproj");
const artifacts = `../../artifacts/browser/${framework.tfm}${process.env.BROWSER_COVERAGE ? "/coverage" : ""}`;
module.exports = defineConfig({
    testDir: __dirname,
    testMatch: "**/*.spec.cjs",
    timeout: 60000,
    expect: { timeout: 30000 },
    workers: 1,
    retries: 0,
    outputDir: `${artifacts}/test-results`,
    reporter: [["list"], ["html", { outputFolder: `${artifacts}/report`, open: "never" }], ["junit", { outputFile: `${artifacts}/results.xml` }]],
    use: {
        baseURL: "http://127.0.0.1:5290",
        locale: "en-US",
        trace: "retain-on-failure",
        screenshot: "only-on-failure",
        launchOptions: process.env.PLAYWRIGHT_CHROMIUM ? { executablePath: process.env.PLAYWRIGHT_CHROMIUM } : {}
    },
    webServer: {
        command: `dotnet run --project "${project}" -c Release --no-build --no-launch-profile -p:BrowserTargetFramework=${framework.tfm} -p:BrowserFrameworkVersion=${framework.version} --urls http://127.0.0.1:5290`,
        url: "http://127.0.0.1:5290",
        timeout: 120000,
        reuseExistingServer: false
    }
});
