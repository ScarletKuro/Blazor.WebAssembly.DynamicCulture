const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");
const assemblies = ["Blazor.WebAssembly.DynamicCulture", "Blazor.WebAssembly.DynamicCulture.Loader"];
const bin = path.resolve(__dirname, "../Blazor.WebAssembly.DynamicCulture.BrowserTests.App/bin/Release", process.env.BROWSER_TFM || "net10.0");

module.exports = async function verifyInstrumentation() {
    if (!process.env.BROWSER_COVERAGE) return;
    for (const assembly of assemblies) {
        // This namespace is injected into the PE metadata by Coverlet. Check it
        // before starting the suite, so a missing source mapping fails once.
        if (!fs.readFileSync(path.join(bin, `${assembly}.dll`)).includes(Buffer.from("Coverlet.Core.Instrumentation.Tracker"))) {
            throw new Error(`Coverlet did not instrument ${assembly}. Check the PDB source-root mapping passed to --source-mapping-file.`);
        }
    }
};

module.exports.prepare = async page => {
    if (!process.env.BROWSER_COVERAGE) return;
    await page.addInitScript(() => { globalThis.browserCoverage = true; });
    await page.route("**/__coverage/*.dll", async route => {
        const name = path.basename(new URL(route.request().url()).pathname, ".dll");
        if (!assemblies.includes(name)) throw new Error(`Unexpected coverage assembly: ${name}`);
        await route.fulfill({ path: path.join(bin, `${name}.dll`), contentType: "application/octet-stream" });
    });
};

module.exports.collect = async page => {
    if (!process.env.BROWSER_COVERAGE) return;
    const modules = await page.evaluate(() => DotNet.invokeMethodAsync("DynamicCultureBrowserTests", "GetCoverage"));
    if (modules.length !== assemblies.length) throw new Error("Missing browser coverage modules");
    for (const { name, hitsFilePath, hits } of modules) {
        // Only accept Coverlet's temporary hit files for the two instrumented libraries.
        if (!assemblies.includes(name) || path.dirname(path.resolve(hitsFilePath)) !== path.resolve(os.tmpdir()) ||
            !path.basename(hitsFilePath).startsWith(`${name}_`) || !Array.isArray(hits) || !hits.length) {
            throw new Error(`Invalid Coverlet tracker for ${name}: ${hitsFilePath}`);
        }
        const previous = fs.existsSync(hitsFilePath) ? fs.readFileSync(hitsFilePath) : null;
        if (previous && (previous.length !== (hits.length + 1) * 4 || previous.readInt32LE(0) !== hits.length)) {
            throw new Error(`Coverlet counter count changed for ${name}`);
        }
        const buffer = Buffer.alloc((hits.length + 1) * 4);
        buffer.writeInt32LE(hits.length);
        hits.forEach((hit, index) => {
            if (!Number.isInteger(hit) || hit < 0) throw new Error("Invalid browser hit count");
            buffer.writeInt32LE(hit + (previous?.readInt32LE((index + 1) * 4) || 0), (index + 1) * 4);
        });
        fs.writeFileSync(hitsFilePath, buffer);
    }
};
