const { test, expect } = require("@playwright/test");
const diagnostics = new WeakMap();

test.beforeEach(({ page }) => {
    const errors = [];
    diagnostics.set(page, errors);
    page.on("pageerror", error => errors.push(error.message));
});

test.afterEach(async ({ page }, testInfo) => {
    const state = await page.evaluate(() => globalThis.browserTests).catch(() => null);
    await testInfo.attach("satellite-state", { body: JSON.stringify({ state, pageErrors: diagnostics.get(page) }, null, 2), contentType: "application/json" });
    expect(diagnostics.get(page)).toEqual([]);
    if (state) {
        expect(state.failures).toEqual([]);
        expect(state.reads.filter(read => read.bodyUsed)).toEqual([]);
        const major = (process.env.BROWSER_TFM || "net10.0").match(/\d+/)[0];
        const framework = require("./frameworks.json").find(item => item.tfm === `net${major}.0`);
        expect(state.runtime).toBe(`.NET ${framework.version}`);
        expect(state.frameworks).toEqual({ loader: `.NETCoreApp,Version=v${major}.0`, main: `.NETCoreApp,Version=v${major}.0` });
    }
});

async function start(page, query = "") {
    await page.goto("/?" + query);
    await expect(page.locator("#ready")).toBeVisible();
}

for (const [name, query, cultures] of [
    ["loads the current culture before RunAsync", "", ["en-US", "cs-CZ"]],
    ["deduplicates shared parents", "cultures=fr-FR,fr-CA,fr", ["fr-FR", "fr-CA", "fr"]],
    ["handles a sibling of the startup culture", "startup=fr-FR&cultures=fr-CA", ["fr-CA"]],
    ["handles duplicate cultures", "cultures=cs-CZ,cs-CZ,cs", ["cs-CZ", "cs"]],
    ["coalesces repeated calls", "mode=repeated", ["en-US", "cs-CZ"]],
    ["coalesces concurrent calls", "mode=concurrent", ["en-US", "cs-CZ"]],
    ["resolves middleware cultures before loading", "mode=middleware", ["en-US", "cs-CZ"]],
    ["supports independent UI culture", "mode=ui-only", ["en-US", "cs-CZ"]],
    ["supports a culture set before loading", "mode=changed-before", ["en-US", "cs-CZ"]],
    ["supports a culture changed after loading", "mode=changed-after", ["en-US", "cs-CZ"]]
]) {
    test(name, async ({ page }) => {
        await start(page, query);
        const loaded = await page.evaluate(() => browserTests.resources);
        for (const culture of cultures) expect(loaded[culture]).toBe(culture);
    });
}

test("loads an unloaded current culture after startup", async ({ page }) => {
    await start(page, "mode=none");
    const resource = await page.evaluate(() => DotNet.invokeMethodAsync("DynamicCultureBrowserTests", "SelectAndLoad", "fr-CA"));
    expect(resource).toBe("fr-CA");
});

test("loads parent resources when a specific culture has no satellite", async ({ page }) => {
    await start(page, "cultures=fr-BE");
    expect(await page.evaluate(() => browserTests.resources["fr-BE"])).toBe("fr");
});

test("ignores cultures without any satellite resources", async ({ page }) => {
    await start(page, "cultures=de-DE");
    expect(await page.evaluate(() => browserTests.resources["de-DE"])).toBe("neutral");
});

test("downloads only requested cultures and does not repeat successful library loads", async ({ page }) => {
    const cultures = [];
    page.on("request", request => {
        const match = new URL(request.url()).pathname.match(/\/_framework\/([^/]+)\/DynamicCultureBrowserTests\.resources/);
        if (match) cultures.push(match[1]);
    });
    await start(page, "mode=repeated");
    expect([...new Set(cultures)].sort()).toEqual(["cs", "cs-CZ", "en", "en-US"]);
    expect(cultures.filter(culture => culture === "cs-CZ")).toHaveLength(1);
    expect(cultures.filter(culture => culture === "cs")).toHaveLength(1);
});

test("resolves the loader module under an application base path", async ({ page }) => {
    await page.route("**/nested/**", async route => {
        const url = new URL(route.request().url());
        url.pathname = url.pathname.replace(/^\/nested/, "");
        const response = await route.fetch({ url: url.href });
        if (route.request().isNavigationRequest()) {
            const body = (await response.text()).replace('<base href="/"', '<base href="/nested/"');
            const headers = { ...response.headers() };
            delete headers["content-encoding"];
            delete headers["content-length"];
            await route.fulfill({ response, body, headers });
        } else {
            await route.fulfill({ response });
        }
    });
    await page.goto("/nested/");
    await expect(page.locator("#ready")).toBeVisible();
    expect(await page.evaluate(() => DotNet.invokeMethodAsync("DynamicCultureBrowserTests", "SelectAndLoad", "fr-CA"))).toBe("fr-CA");
});

test("switches translations without reloading the page", async ({ page }) => {
    await start(page, "mode=middleware");
    for (const culture of ["en-US", "cs-CZ", "fr-CA", "en-US", "cs-CZ"]) {
        await page.getByRole("button", { name: culture, exact: true }).click();
        await expect(page.locator("#greeting")).toHaveText(culture);
        await expect(page.locator("#ui-culture")).toHaveText(culture);
    }
});

test("restores a saved language and can switch back after reopening", async ({ page }) => {
    await page.addInitScript(() => {
        if (!localStorage.getItem("BlazorCulture")) localStorage.setItem("BlazorCulture", "cs-CZ");
    });
    await start(page, "mode=stored&selected=en-US");
    await expect(page.locator("#greeting")).toHaveText("cs-CZ");
    await page.getByRole("button", { name: "en-US", exact: true }).click();
    await expect(page.locator("#greeting")).toHaveText("en-US");
    await page.reload();
    await expect(page.locator("#greeting")).toHaveText("en-US");
    await page.getByRole("button", { name: "cs-CZ", exact: true }).click();
    await expect(page.locator("#greeting")).toHaveText("cs-CZ");
});

test("keeps native preloading compatible with manual loading", async ({ page }) => {
    test.skip(Number((process.env.BROWSER_TFM || "net10.0").match(/\d+/)[0]) < 8, "configureRuntime requires .NET 8+");
    await start(page, "native=true&mode=middleware");
    await page.getByRole("button", { name: "fr-CA", exact: true }).click();
    await expect(page.locator("#greeting")).toHaveText("fr-CA");
});

test("retries a failed satellite without losing successful loads", async ({ page }) => {
    let failures = 0;
    await page.route(/\/_framework\/cs-CZ\/DynamicCultureBrowserTests\.resources.*\.(dll|wasm)$/, async route => {
        // Older runtimes can retry internally. Keep failing until the library caller
        // observes a failure, then let its explicit retry reach the real server.
        if (!await page.evaluate(() => browserTests.retried)) {
            failures++;
            const major = Number((process.env.BROWSER_TFM || "net10.0").match(/\d+/)[0]);
            if (major < 8) {
                // The legacy loader reads response bodies without checking HTTP status.
                await route.abort("failed");
            } else {
                // Preserve the valid body for SRI. A 404 reaches the caller without
                // automatic retry/cache recovery masking the injected failure.
                const response = await route.fetch();
                await route.fulfill({ response, status: 404 });
            }
        } else {
            await route.continue();
        }
    });
    await start(page, "mode=retry");
    expect(failures).toBeGreaterThan(0);
    expect(await page.evaluate(() => browserTests.retried)).toBe(true);
    expect(await page.evaluate(() => browserTests.resources["cs-CZ"])).toBe("cs-CZ");
});
