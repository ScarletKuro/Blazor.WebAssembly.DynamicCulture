const fs = require("node:fs");
const path = require("node:path");
const frameworks = require("./frameworks.json");
for (const framework of frameworks) {
    if (!framework.tfm || !framework.version || !framework.sdk) {
        throw new Error("Each browser matrix entry requires tfm, version, and sdk.");
    }
}
for (const project of ["Blazor.WebAssembly.DynamicCulture", "Blazor.WebAssembly.DynamicCulture.Loader"]) {
    const xml = fs.readFileSync(path.join(__dirname, "../../src", project, project + ".csproj"), "utf8");
    const targets = xml.match(/<TargetFrameworks>([^<]+)<\/TargetFrameworks>/)[1].split(";").map(tfm => tfm.trim()).filter(Boolean).sort();
    if (JSON.stringify(targets) !== JSON.stringify(frameworks.map(item => item.tfm).sort())) {
        throw new Error(`${project}: update browser/frameworks.json to cover every supported TFM (${targets.join(", ")}).`);
    }
}
process.stdout.write(JSON.stringify({ include: frameworks }));
